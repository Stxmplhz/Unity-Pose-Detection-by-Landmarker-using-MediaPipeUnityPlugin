using System.Collections;  
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

public class PoseGameManager : MonoBehaviour
{
    public PoseGameConfig config;
    public PoseLandmarkerLive detector;
    public PoseLogic logic;
    public GameObject CameraPreview;

    [Header("UI References")]
    public GameObject poseIntroPanel;
    public GameObject resultPanel;
    public Image poseIconUI;
    public Image poseIconImage;
    public Image introStatusIcon;
    public Image resultStatusIcon;
    public Image blackFilter;
    public TextMeshProUGUI poseThaiName;
    public TextMeshProUGUI countText;
    public TextMeshProUGUI uiText;
    public TextMeshProUGUI resultText;
    public UnityEngine.Video.VideoPlayer videoPlayer;
    public RawImage howToVideoRawImage;

    [Header("Button")]
    public Button howToButton;
    public Button pauseButton;
    public Button continueButton;
    public Button retryButton;

    [Header("Video")]
    public RenderTexture videoRenderTexture;
    public Button playVideoButton;
    public Button pauseVideoButton;
    public Button closeVideoButton;

    [Header("Result UI")]
    public float introDelay = 0f;
    public float resultSuccessDelay = 1f;

    [Header("Status Icons")]
    public Sprite hourglassIcon;
    public Sprite successIcon;
    public Sprite failIcon;

    [Header("Audio")]
    private AudioSource audioSource;
    public AudioClip introSound;
    public AudioClip successSound;
    public AudioClip failSound;

    [Header("Timing")]
    public float countCooldown = 0.3f;
    private int currentPoseIndex = 0;
    private PoseRequirement currentPose;
    private float timeRemaining;
    private float holdTimer = 0f;
    private int counter = 0;
    private float lastCountTime = 0f;
    private bool isPoseActive = false;
    private bool isHoldSFXPlaying = false;
    private Coroutine poseTimerCoroutine;
    private string lastUIText = "";
    private bool isPaused = false;

    void Start()
    {
        if (config == null || config.PosesInScene.Count == 0)
        {
            Debug.LogError("❌ No poses configured in PoseGameConfig");
            return;
        }

        CameraPreview?.SetActive(true);
        poseIntroPanel?.SetActive(false);
        resultPanel?.SetActive(false);
        uiText?.gameObject.SetActive(false);
        poseIconUI?.gameObject.SetActive(false);
        poseIconImage?.gameObject.SetActive(false);
        howToButton?.gameObject.SetActive(false);
        closeVideoButton.gameObject.SetActive(false);
        pauseButton?.gameObject.SetActive(false);
        continueButton?.gameObject.SetActive(false);
        blackFilter?.gameObject.SetActive(false);

        howToVideoRawImage?.gameObject.SetActive(false);
        videoPlayer.Stop();
        videoPlayer.gameObject.SetActive(false); 
        videoPlayer.playOnAwake = false;

        isPaused = false;
        isPoseActive = true;
        detector.SetPaused(false);
        detector.OnLandmarksUpdated += OnLandmarksDetected;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        StartNextPose();
    }

    void StartNextPose()
    {
        if (currentPoseIndex >= config.PosesInScene.Count)
        {
            return;
        }

        currentPose = config.PosesInScene[currentPoseIndex];
        timeRemaining = currentPose.TimeLimit;
        holdTimer = 0f;
        counter = 0;
        isPoseActive = false;
        detector.SetPaused(true);

        poseIntroPanel?.SetActive(true);
        if (introSound != null)
            audioSource.PlayOneShot(introSound);

        CameraPreview?.SetActive(false);
        poseIconUI?.gameObject.SetActive(true);
        uiText?.gameObject.SetActive(false);
        retryButton?.gameObject.SetActive(false);
        poseIconImage?.gameObject.SetActive(false);
        howToButton?.gameObject.SetActive(false);
        closeVideoButton.gameObject.SetActive(false);
        pauseButton?.gameObject.SetActive(false);
        blackFilter?.gameObject.SetActive(false);

        if (poseIconUI != null)
        {
            poseIconUI.sprite = currentPose.PoseIcon;
            poseIconUI.preserveAspect = true;
        }
        if (introStatusIcon != null && hourglassIcon != null) introStatusIcon.sprite = hourglassIcon;
        if (poseThaiName != null) poseThaiName.text = currentPose.PoseNameThai;

        if (countText != null)
        {
            countText.text = currentPose.Type == PoseType.Counting
                ? $"จำนวนครั้ง: {currentPose.CountRequired}"
                : $"ค้างท่า: {currentPose.DurationRequired:0.0} วินาที";
        }

        Invoke(nameof(BeginPoseDetection), introDelay);
    }

    void BeginPoseDetection()
    {
        poseIntroPanel?.SetActive(false);
        poseIconUI?.gameObject.SetActive(false);
        CameraPreview?.SetActive(true);
        uiText?.gameObject.SetActive(true);
        poseIconImage?.gameObject.SetActive(true);
        howToButton?.gameObject.SetActive(true);
        closeVideoButton.gameObject.SetActive(false);
        pauseButton?.gameObject.SetActive(true);
        continueButton?.gameObject.SetActive(false);
        blackFilter?.gameObject.SetActive(false);

        if (poseIconImage != null)
        {
            poseIconImage.sprite = currentPose.PoseIcon;
            poseIconImage.preserveAspect = true;
        }

        isPoseActive = true;
        detector.SetPaused(false);
        lastUIText = "";

        if (poseTimerCoroutine != null) StopCoroutine(poseTimerCoroutine);
        poseTimerCoroutine = StartCoroutine(PoseTimeLimit());
    }

    IEnumerator PoseTimeLimit()
    {
        while (timeRemaining > 0f)
        {
            if (!isPaused)
            {
                timeRemaining -= Time.deltaTime;
            }
            yield return null;

            if (currentPose.Type == PoseType.Holding && holdTimer >= currentPose.DurationRequired)
            {
                PoseCompleted(true);
                yield break;
            }

            if (currentPose.Type == PoseType.Counting && counter >= currentPose.CountRequired)
            {
                PoseCompleted(true);
                yield break;
            }
        }

        PoseCompleted(false);
    }

    void PoseCompleted(bool success)
    {
        isPoseActive = false;
        if (poseTimerCoroutine != null) StopCoroutine(poseTimerCoroutine);
        detector.SetPaused(true);

        if (isHoldSFXPlaying && audioSource.clip == currentPose.HoldSFX)
        {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.loop = false;
            isHoldSFXPlaying = false;
        }

        resultPanel?.SetActive(true);
        CameraPreview?.SetActive(false);
        poseIconUI?.gameObject.SetActive(true);
        poseIntroPanel?.SetActive(false);
        uiText?.gameObject.SetActive(false);
        poseIconImage?.gameObject.SetActive(false);
        howToButton?.gameObject.SetActive(false);
        closeVideoButton.gameObject.SetActive(false);
        pauseButton?.gameObject.SetActive(false);
        blackFilter?.gameObject.SetActive(true);    
        howToVideoRawImage?.gameObject.SetActive(false);

        if (poseIconUI != null)
        {
            poseIconUI.sprite = currentPose.PoseIcon;
            poseIconUI.preserveAspect = true;
        }

        if (resultStatusIcon != null)
        {
            resultStatusIcon.sprite = success ? successIcon : failIcon;
        }

        if (resultText != null)
        {
            resultText.text = success ? $"สำเร็จ!" : $"หมดเวลา!";

            if (ColorUtility.TryParseHtmlString(success ? "#3EC479" : "#F54447", out Color parsedColor))
            {
                resultText.color = parsedColor;
            }
        }

        Debug.Log(success
            ? $"✅ Pose {currentPose.PoseName} success"
            : $"❌ Pose {currentPose.PoseName} failed");

        if (success)
        {   
            if (successSound != null)
                audioSource.PlayOneShot(successSound);
            retryButton?.gameObject.SetActive(false);
            currentPoseIndex++;
            Invoke(nameof(ProceedToNext), resultSuccessDelay);
        }
        else
        {
            if (failSound != null)
                audioSource.PlayOneShot(failSound);
            retryButton?.gameObject.SetActive(true);
        }
    }

    public void RetryCurrentPose()
    {
        resultPanel?.SetActive(false);
        retryButton?.gameObject.SetActive(false);
        poseIconUI?.gameObject.SetActive(true);
        poseIconImage?.gameObject.SetActive(false);
        blackFilter?.gameObject.SetActive(false);

        RestartCurrentPose();
    }

    void RestartCurrentPose()
    {
        timeRemaining = currentPose.TimeLimit;
        holdTimer = 0f;
        counter = 0;
        isPoseActive = false;

        poseIntroPanel?.SetActive(true);
        if (introSound != null)
            audioSource.PlayOneShot(introSound);

        poseIconUI?.gameObject.SetActive(true);
        uiText?.gameObject.SetActive(false);
        poseIconImage?.gameObject.SetActive(false);

        if (poseIconUI != null)
        {
            poseIconUI.sprite = currentPose.PoseIcon;
            poseIconUI.preserveAspect = true;
        }
        if (introStatusIcon != null && hourglassIcon != null)
            introStatusIcon.sprite = hourglassIcon;
        if (poseThaiName != null)
            poseThaiName.text = currentPose.PoseNameThai;

        if (countText != null)
        {
            countText.text = currentPose.Type == PoseType.Counting
                ? $"จำนวนครั้ง: {currentPose.CountRequired}"
                : $"ค้างท่า: {currentPose.DurationRequired:0.0} วินาที";
        }

        Invoke(nameof(BeginPoseDetection), introDelay);
    }

    void ProceedToNext()
    {
        resultPanel?.SetActive(false);
        retryButton?.gameObject.SetActive(false);
        poseIconUI?.gameObject.SetActive(false);
        poseIconImage?.gameObject.SetActive(false);
        blackFilter?.gameObject.SetActive(false);

        // Complete every pose in the scene
        if (currentPoseIndex >= config.PosesInScene.Count)
        {
            isPoseActive = false;
            detector.SetPaused(true);
            CameraPreview?.SetActive(false);
            return;
        }

        StartNextPose();
    }

    void OnLandmarksDetected(List<Vector3> landmarks)
    {
        if (!isPoseActive || currentPose == null || logic == null || landmarks == null)
            return;

        bool detected = logic.IsPoseDetected(currentPose.PoseName, landmarks);
        int displayTime = Mathf.CeilToInt(timeRemaining);

        if (currentPose.Type == PoseType.Holding)
        {
            if (detected)
            {
                holdTimer += Time.deltaTime;

                if (!isHoldSFXPlaying && currentPose.HoldSFX != null)
                {
                    audioSource.clip = currentPose.HoldSFX;
                    audioSource.loop = true; // ✅ ให้ลูป
                    audioSource.Play();
                    isHoldSFXPlaying = true;
                }
            }
            else
            {
                // ❌ หยุดเสียงทันทีเมื่อเลิกค้างท่า
                if (isHoldSFXPlaying && audioSource.clip == currentPose.HoldSFX)
                {
                    audioSource.Stop();
                    audioSource.clip = null;
                    audioSource.loop = false;
                    isHoldSFXPlaying = false;
                }
            }

            int holdSec = Mathf.FloorToInt(holdTimer);
            int requiredSec = Mathf.CeilToInt(currentPose.DurationRequired);
            string newText = $"{currentPose.PoseNameThai}\nค้าง: {holdSec}s / {requiredSec}s\nเวลาที่เหลือ: {displayTime}s";

            if (newText != lastUIText)
            {
                uiText.text = newText;
                lastUIText = newText;
            }
        }

        if (currentPose.Type == PoseType.Counting)
        {
            if (detected && Time.time - lastCountTime >= countCooldown)
            {
                counter++;
                lastCountTime = Time.time;

                if (currentPose.CountSFX != null)
                    audioSource.PlayOneShot(currentPose.CountSFX);

                Debug.Log($"✅ Counted! total = {counter}");
            }

            string newText = $"{currentPose.PoseNameThai}\nจำนวนครั้ง: {counter} / {currentPose.CountRequired}\nเวลาที่เหลือ: {displayTime}s";
            if (newText != lastUIText)
            {
                uiText.text = newText;
                lastUIText = newText;
            }
        }
    }

    // ----------------- Pause & Continue Play System -----------------
        public void OnPauseClicked()
    {
        isPaused = true;
        isPoseActive = false;
        CameraPreview?.SetActive(false);
        pauseButton?.gameObject.SetActive(true);
        continueButton?.gameObject.SetActive(true);
        blackFilter?.gameObject.SetActive(true);

        isPaused = true;
        isPoseActive = false;
        detector.SetPaused(true);
        if (poseTimerCoroutine != null)
            StopCoroutine(poseTimerCoroutine);

    }

    public void OnContinuePlayClicked()
    {
        if (poseTimerCoroutine != null)
            StopCoroutine(poseTimerCoroutine);

        isPaused = false;
        isPoseActive = true;
        detector.SetPaused(false);
        CameraPreview?.SetActive(true);
        pauseButton?.gameObject.SetActive(true);
        continueButton?.gameObject.SetActive(false);
        blackFilter?.gameObject.SetActive(false);
        poseTimerCoroutine = StartCoroutine(PoseTimeLimit());
        
    }

    // ----------------- Tutorial Video System -----------------
    IEnumerator PlayVideoOnAndroid(string path)
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(path))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError("Video Load Error: " + request.error);
                yield break;
            }

            videoPlayer.url = path;
            videoPlayer.gameObject.SetActive(true);
            videoPlayer.Play();
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;
        vp.Play();
        playVideoButton?.gameObject.SetActive(false);
        pauseVideoButton?.gameObject.SetActive(true);
        Debug.Log("▶️ Video is now playing.");
    }

    public void OnHowToClicked()
    {
        isPaused = true;
        isPoseActive = false;
        detector.SetPaused(true);

        if (poseTimerCoroutine != null)
            StopCoroutine(poseTimerCoroutine);

        videoPlayer.gameObject.SetActive(true);
        howToVideoRawImage.gameObject.SetActive(true);

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;
        howToVideoRawImage.texture = videoRenderTexture;

        CameraPreview?.SetActive(false);
        closeVideoButton?.gameObject.SetActive(true);
        blackFilter?.gameObject.SetActive(true);

        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, currentPose.LocalVideoFileName);

#if UNITY_ANDROID
        videoPath = "jar:file://" + Application.dataPath + "!/assets/" + currentPose.LocalVideoFileName;
#else
        videoPath = "file://" + videoPath;
#endif

        videoPlayer.url = videoPath;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare();

        pauseVideoButton?.gameObject.SetActive(false);
        playVideoButton?.gameObject.SetActive(true);

        Debug.Log("📺 Preparing video: " + videoPath);
    }

    public void PauseVideo()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            Debug.Log("⏸️ Video paused.");
        }
        pauseVideoButton?.gameObject.SetActive(false);
        playVideoButton?.gameObject.SetActive(true);
    }

    public void PlayVideo()
    {
        if (!videoPlayer.isPlaying)
        {
            videoPlayer.Play();
            Debug.Log("▶️ Video resumed.");
        }
        playVideoButton?.gameObject.SetActive(false);
        pauseVideoButton?.gameObject.SetActive(true);
    }

    public void OnCloseVideo()
    {
        if (videoPlayer.isPlaying)
            videoPlayer.Stop();

        videoPlayer.gameObject.SetActive(false);
        howToVideoRawImage?.gameObject.SetActive(false);

        CameraPreview?.SetActive(true);
        closeVideoButton?.gameObject.SetActive(false);
        blackFilter?.gameObject.SetActive(false);

        isPaused = false;
        isPoseActive = true;
        detector.SetPaused(false);
        poseTimerCoroutine = StartCoroutine(PoseTimeLimit());
    }
    
    IEnumerator WaitForPrepareAndPlay()
    {
        while (!videoPlayer.isPrepared)
            yield return null;

        videoPlayer.Play();
        Debug.Log("▶️ Video is now playing.");
    }

}
