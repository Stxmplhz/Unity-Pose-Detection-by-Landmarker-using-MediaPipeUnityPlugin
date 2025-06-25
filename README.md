# Unity-Pose-Detection-by-Landmarker-using-MediaPipeUnityPlugin

This project was built to use in Unity for a Unity game that detects real-time player poses from the camera (e.g. jump, squat, twist, etc.) in Count/ Holding time required within time limit. Using Pose Landmarker from MediaPipeUnityPlugin.

This project reposity using MediaPipeUnityPlugin from Homuler (https://github.com/homuler/MediaPipeUnityPlugin), and Kanit Thai font (https://fonts.google.com/specimen/Kanit)

**How to use**: You can use PoseMechanic.scene as the set-up sample scene in the real game.

**File:**
  - PoseLandmarkerLive.cs: Main interface that communicates with MediaPipe Plugin to open webcam, send image to PoseLandmarker, get landmark result, and pass to other script via event.
  - PoseLogic.cs: Function to detect various postures (e.g. jump, squat, punch...) from List<Vector3> landmark data.
  - PoseGameManager.cs: Manage the detection order of poses according to config.
  - PoseGameConfig.cs: Keep a list of the moves you want to perform in each stage, such as
    - Jump - Count 10 - TimeLimit 30s
    - Twist - Hold 5s - TimeLimit 20s
  - PoseMechanic.scene to be as the simple set-up in real game.

**After clone this reposity:**
1. Download 'mediapipe_android.aar' from https://drive.google.com/file/d/1UDWCq5JbVBwnqvQ1O1TLXW3Kq0QlAVN5/view?usp=sharing
2. Put it in "Unity-Pose-Detection-by-Landmarker-using-MediaPipeUnityPlugin\Packages\com.github.homuler.mediapipe\Runtime\Plugins\Android"

**Enforce these packages with the following versions:**
- 2d.common as 8.0.2
- 2d.psdimporter as 8.0.0
- 2d.spriteshape as 8.0.0


