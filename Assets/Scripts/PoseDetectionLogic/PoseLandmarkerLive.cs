using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.Core;

public class PoseLandmarkerLive : MonoBehaviour
{
    private PoseLandmarker landmarker;
    public RawImage cameraPreview;
    private WebCamTexture webcamTexture;

    public event Action<List<Vector3>> OnLandmarksUpdated;

    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    private Dictionary<int, Vector3> previousLandmarks = new Dictionary<int, Vector3>();

    private bool isPaused = false;

    void Start()
    {
        webcamTexture = new WebCamTexture();
        webcamTexture.Play();

        if (cameraPreview != null)
            cameraPreview.texture = webcamTexture;

        string modelPath = Path.Combine(Application.streamingAssetsPath, "pose_landmarker_full.bytes");

        var baseOptions = new BaseOptions(modelAssetPath: modelPath);

        var options = new PoseLandmarkerOptions(
            baseOptions: baseOptions,
            runningMode: RunningMode.LIVE_STREAM,
            resultCallback: (PoseLandmarkerResult result, Mediapipe.Image image, long timestampMs) =>
            {
                if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
                    return;

                var lmList = result.poseLandmarks[0].landmarks;

                List<Vector3> landmarks = new List<Vector3>();
                for (int i = 0; i < lmList.Count; i++)
                {
                    var lm = lmList[i];
                    var current = new Vector3(lm.x, lm.y, lm.z);

                    if (previousLandmarks.TryGetValue(i, out Vector3 prev))
                    {
                        if (IsMoving(prev, current))
                        {
                            Debug.Log($"Landmark[{i}] moved to X:{current.x:F3}, Y:{current.y:F3}, Z:{current.z:F3}");
                        }
                    }

                    previousLandmarks[i] = current;
                    landmarks.Add(current);
                }

                _mainThreadActions.Enqueue(() => OnLandmarksUpdated?.Invoke(landmarks));
            });

        landmarker = PoseLandmarker.CreateFromOptions(options);
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;

        if (isPaused)
        {
            Debug.Log("Paused - stop sending landmarks");
        }
        else
        {
            Debug.Log("Resumed - continue sending landmarks");
        }
    }

    void Update()
    {
        while (_mainThreadActions.Count > 0)
        {
            var action = _mainThreadActions.Dequeue();
            action?.Invoke();
        }

        if (isPaused)
            return;

        if (webcamTexture == null || !webcamTexture.didUpdateThisFrame || landmarker == null)
            return;

        Texture2D texture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);
        texture.SetPixels32(webcamTexture.GetPixels32());
        texture.Apply();

        Mediapipe.Image mpImage = new Mediapipe.Image(texture);
        long timestamp = (long)(Time.time * 1000);
        landmarker.DetectAsync(mpImage, timestamp);
    }
    

    private void OnDestroy()
    {
        webcamTexture?.Stop();
        landmarker?.Close();
    }

    private bool IsMoving(Vector3 prev, Vector3 curr, float threshold = 0.01f)
    {
        return Vector3.Distance(prev, curr) > threshold;
    }
}
