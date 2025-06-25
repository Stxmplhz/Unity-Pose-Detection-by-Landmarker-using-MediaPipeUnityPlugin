using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    public TextMeshProUGUI poseThaiName;
    public TextMeshProUGUI countText;
    public TextMeshProUGUI uiText;
    public TextMeshProUGUI resultText;

    [Header("Result UI")]
    public GameObject retryButton; 
    public float introDelay = 4f;   
    public float resultSuccessDelay = 3f; 

    [Header("Icons")]
    public Sprite hourglassIcon;
    public Sprite successIcon;
    public Sprite failIcon;

    [Header("Audio")]
    public AudioClip countSound;
    private AudioSource audioSource;

    [Header("Timing")]
    public float countCooldown = 1.0f;

    private int currentPoseIndex = 0;
    private PoseRequirement currentPose;
    private float timeRemaining;
    private float holdTimer = 0f;
    private int counter = 0;
    private float lastCountTime = 0f;
    private bool isPoseActive = false;
    private Coroutine poseTimerCoroutine;
    private string lastUIText = "";

    void Start()
    {
        if (config == null || config.PosesInScene.Count == 0)
        {
            Debug.LogError("❌ No poses configured in PoseGameConfig");
            return;
        }

        poseIntroPanel?.SetActive(false);
        CameraPreview?.SetActive(true);
        resultPanel?.SetActive(false);
        uiText?.gameObject.SetActive(false);
        poseIconUI?.gameObject.SetActive(false);
        poseIconImage?.gameObject.SetActive(false);

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
            uiText?.gameObject.SetActive(true);
            uiText.text = "เสร็จสิ้น!";
            return;
        }

        currentPose = config.PosesInScene[currentPoseIndex];
        timeRemaining = currentPose.TimeLimit;
        holdTimer = 0f;
        counter = 0;
        isPoseActive = false;

        poseIntroPanel?.SetActive(true);
        CameraPreview?.SetActive(false);
        poseIconUI?.gameObject.SetActive(true); 
        uiText?.gameObject.SetActive(false);
        retryButton?.SetActive(false);
        poseIconImage?.gameObject.SetActive(false);

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

        if (poseIconImage != null)
        {
            poseIconImage.sprite = currentPose.PoseIcon;
            poseIconImage.preserveAspect = true;
        }    

        isPoseActive = true;
        lastUIText = "";

        if (poseTimerCoroutine != null) StopCoroutine(poseTimerCoroutine);
        poseTimerCoroutine = StartCoroutine(PoseTimeLimit());
    }

    IEnumerator PoseTimeLimit()
    {
        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.deltaTime;
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

        resultPanel?.SetActive(true);
        CameraPreview?.SetActive(false);
        poseIconUI?.gameObject.SetActive(true);
        poseIntroPanel?.SetActive(false);
        uiText?.gameObject.SetActive(false);
        poseIconImage?.gameObject.SetActive(false);

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
            retryButton?.SetActive(false);
            currentPoseIndex++;
            Invoke(nameof(ProceedToNext), resultSuccessDelay);
        }
        else
        {
            retryButton?.SetActive(true); 
        }
    }

    public void RetryCurrentPose()
    {
        resultPanel?.SetActive(false);
        retryButton?.SetActive(false);
        poseIconUI?.gameObject.SetActive(true);
        poseIconImage?.gameObject.SetActive(false);

        RestartCurrentPose();
    }

    void RestartCurrentPose()
    {
        timeRemaining = currentPose.TimeLimit;
        holdTimer = 0f;
        counter = 0;
        isPoseActive = false;
        
        poseIntroPanel?.SetActive(true);
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
            if (detected) holdTimer += Time.deltaTime;
            else holdTimer = 0f;

            string newText = $"{currentPose.PoseNameThai}\nค้าง: {holdTimer:F1}s / {currentPose.DurationRequired}s\nเวลาที่เหลือ: {displayTime}s";
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
                if (countSound != null) audioSource.PlayOneShot(countSound);
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
}
