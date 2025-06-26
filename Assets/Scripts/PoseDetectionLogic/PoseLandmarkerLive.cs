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

                for (int i = 0; i < lmList.Count; i++)
                {
                    var lm = lmList[i];
                    Debug.Log($"Landmark[{i}] - X: {lm.x:F3}, Y: {lm.y:F3}, Z: {lm.z:F3}");
                }

                List<Vector3> landmarks = new List<Vector3>();
                foreach (var lm in lmList)
                {
                    landmarks.Add(new Vector3(lm.x, lm.y, lm.z));
                }

                _mainThreadActions.Enqueue(() => OnLandmarksUpdated?.Invoke(landmarks));
            });

        landmarker = PoseLandmarker.CreateFromOptions(options);
    }

    void Update()
    {
        while (_mainThreadActions.Count > 0)
        {
            var action = _mainThreadActions.Dequeue();
            action?.Invoke();
        }

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
}
