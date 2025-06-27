using UnityEngine;
using System;
using System.Collections.Generic;

public class PoseLogic : MonoBehaviour
{
    // Previous value used for comparison in posture detection.
    public float prevHipX = float.NaN;
    public float prevNoseY = float.NaN;
    public string nodStage = null;
    public bool prevArmOpen = false;

    // Help with walking timing
    private Queue<bool> walkHistory = new Queue<bool>();
    private const int maxFrames = 45; // Extend the time (1.5 seconds at 30 FPS)
    private const float walkThreshold = 0.02f; // Count walk slowly
    private const float walkTriggerRatio = 0.15f; // Walking 15% of the frame is considered walking.
    private float lastRunDetectedTime = -1f; // Adjustable up to 0.15f

    // Help with running timing
    private Queue<bool> runHistory = new Queue<bool>();
    private const float runThreshold = 0.10f;
    private const int runMaxFrames = 30;
    private const float runTriggerRatio = 0.25f;

    // For detectTwist()
    private static bool isTwisting = false;  
    private static bool readyToCount = false;  
    private static float twistThreshold = 0.08f;    
    private static float centerThreshold = 0.03f;  

    // For detectLeftKick() && detectRightKick()
    private static bool rightLegUp = false;
    private static bool leftLegUp = false;

    public bool IsPoseDetected(string poseName, List<Vector3> landmarks)
    {
        if (landmarks == null || landmarks.Count < 33)
            return false;

        switch (poseName.ToLower())
        {
            case "jump":
                return DetectJump(landmarks, prevNoseY, out prevNoseY);

            case "squat":
                return DetectSquat(landmarks);

            case "twist":
                return DetectTwist(landmarks);

            case "walk":
                return this.DetectWalk(landmarks);

            case "run":
                return this.DetectRun(landmarks);

            case "bendforward":
                return DetectBendForward(landmarks);

            case "l-crossarmstretch":
                return DetectLeftCrossArmStretch(landmarks);

            case "r-crossarmstretch":
                return DetectRightCrossArmStretch(landmarks);

            case "l-kick":
                return DetectLeftKick(landmarks);
            
            case "r-kick":
                return DetectRightKick(landmarks);

            case "iceskate":
                return DetectIceSkate(landmarks, prevHipX, out prevHipX);

            case "l-punch":
                return DetectLeftPunch(landmarks);

            case "r-punch":
                return DetectRightPunch(landmarks);

            case "punch":
                return DetectPunch(landmarks);

            case "headnod":
                return DetectHeadNod(landmarks, prevNoseY, nodStage, out prevNoseY, out nodStage, out bool moved) && moved;

            case "armopenclose":
                return DetectArmOpenClose(landmarks, prevArmOpen, out prevArmOpen, out bool triggered) && triggered;

            default:
                return false;
        }
    }

    // ----- Functions for normalized coordinates -----

    // Detect walking by looking at the vertical difference at the left and right knees.
    public bool DetectWalk(List<Vector3> lm)
    {
        float diff = Mathf.Abs(lm[Pose.LEFT_KNEE].y - lm[Pose.RIGHT_KNEE].y);
        bool isStep = diff > walkThreshold;

        walkHistory.Enqueue(isStep);
        if (walkHistory.Count > maxFrames)
            walkHistory.Dequeue();

        int walkCount = 0;
        foreach (bool b in walkHistory)
            if (b) walkCount++;

        return walkCount > maxFrames * walkTriggerRatio;
    }

    // Detect running using the position of both ankles
    public bool DetectRun(List<Vector3> lm)
    {
        float diff = Mathf.Abs(lm[Pose.LEFT_ANKLE].y - lm[Pose.RIGHT_ANKLE].y);
        bool isRunningStep = diff > runThreshold;

        runHistory.Enqueue(isRunningStep);
        if (runHistory.Count > runMaxFrames)
            runHistory.Dequeue();

        int runCount = 0;
        foreach (bool b in runHistory)
            if (b) runCount++;

        return runCount > runMaxFrames * runTriggerRatio;
    }
    
    // Detects forward bending by calculating the angle between the hips, shoulders and nose.
    public static bool DetectBendForward(List<Vector3> lm)
    {
        float angle = GetAngle(lm[Pose.LEFT_HIP], lm[Pose.LEFT_SHOULDER], lm[Pose.NOSE]);
        return angle < 160f;  
    }

    // Detect Left cross arm stretching 
    public static bool DetectLeftCrossArmStretch(List<Vector3> lm)
    {
        bool across = lm[Pose.LEFT_WRIST].x > lm[Pose.RIGHT_SHOULDER].x - 0.02f;
        bool aligned = Mathf.Abs(lm[Pose.LEFT_WRIST].y - lm[Pose.RIGHT_SHOULDER].y) < 0.2f;
        bool extended = Mathf.Abs(lm[Pose.LEFT_WRIST].x - lm[Pose.LEFT_SHOULDER].x) > 0.08f;

        return across && aligned && extended;
    }

    // Detect Right cross arm stretching 
    public static bool DetectRightCrossArmStretch(List<Vector3> lm)
    {
        bool across = lm[Pose.RIGHT_WRIST].x < lm[Pose.LEFT_SHOULDER].x + 0.02f;
        bool aligned = Mathf.Abs(lm[Pose.RIGHT_WRIST].y - lm[Pose.LEFT_SHOULDER].y) < 0.2f;
        bool extended = Mathf.Abs(lm[Pose.RIGHT_WRIST].x - lm[Pose.RIGHT_SHOULDER].x) > 0.08f;

        return across && aligned && extended;
    }

    // Detect Torso twist by looking at the difference in the z-axis between the top and bottom.
    public static bool DetectTwist(List<Vector3> lm)
    {
        float lsZ = lm[Pose.LEFT_SHOULDER].z;
        float rsZ = lm[Pose.RIGHT_SHOULDER].z;
        float lhZ = lm[Pose.LEFT_HIP].z;
        float rhZ = lm[Pose.RIGHT_HIP].z;

        float upperLowerDiff = Mathf.Abs((lsZ - lhZ) - (rsZ - rhZ));

        if (!isTwisting && upperLowerDiff > twistThreshold)
        {
            // Start twist
            isTwisting = true;
            readyToCount = true;
        }

        if (isTwisting && upperLowerDiff < centerThreshold && readyToCount)
        {
            // Come back straight, count 1 time.
            isTwisting = false;
            readyToCount = false;
            return true;
        }

        // Not yet returned from twist or failed
        if (isTwisting && upperLowerDiff > twistThreshold * 1.5f)
        {
            isTwisting = false;
            readyToCount = false;
        }

        return false;
    }

    // Detect squat by looking at knee angle
    public static bool DetectSquat(List<Vector3> lm)
    {
        float angle = GetAngle(lm[Pose.LEFT_HIP], lm[Pose.LEFT_KNEE], lm[Pose.LEFT_ANKLE]);
        return angle > 60f && angle < 110f;
    }
    
    // Detect punch
    public static bool DetectRightPunch(List<Vector3> lm)
    {
        Vector3 shoulder = lm[Pose.LEFT_SHOULDER];
        Vector3 elbow = lm[Pose.LEFT_ELBOW];
        Vector3 wrist = lm[Pose.LEFT_WRIST];

        float armExtension = Vector3.Distance(shoulder, wrist);
        float upperArm = Vector3.Distance(shoulder, elbow);
        float forearm = Vector3.Distance(elbow, wrist);

        bool isExtended = armExtension > (upperArm + forearm) * 0.7f;

        bool correctDirection = wrist.x < elbow.x &&
                                Mathf.Abs(wrist.y - shoulder.y) < 0.4f;

        return isExtended && correctDirection;
    }

    public static bool DetectLeftPunch(List<Vector3> lm)
    {
        Vector3 shoulder = lm[Pose.RIGHT_SHOULDER];
        Vector3 elbow = lm[Pose.RIGHT_ELBOW];
        Vector3 wrist = lm[Pose.RIGHT_WRIST];

        float armExtension = Vector3.Distance(shoulder, wrist);
        float upperArm = Vector3.Distance(shoulder, elbow);
        float forearm = Vector3.Distance(elbow, wrist);

        bool isExtended = armExtension > (upperArm + forearm) * 0.7f;

        bool correctDirection = wrist.x > elbow.x &&
                                Mathf.Abs(wrist.y - shoulder.y) < 0.4f;

        return isExtended && correctDirection;
    }

    public static bool DetectPunch(List<Vector3> lm)
    {
        return DetectLeftPunch(lm) || DetectRightPunch(lm);
    }

    // Detect kick
    public static bool DetectRightKick(List<Vector3> lm)
    {
        float rkneeY = lm[Pose.RIGHT_KNEE].y;
        float rhipY = lm[Pose.RIGHT_HIP].y;
        float rleg = Mathf.Abs(lm[Pose.RIGHT_ANKLE].x - lm[Pose.RIGHT_KNEE].x);
        bool rightKickPos = rleg > 0.05f && rkneeY < rhipY - 0.05f;

        bool rightKickDetected = false;

        if (rightKickPos && !rightLegUp)
        {
            rightKickDetected = true;
            rightLegUp = true;
        }
        else if (!rightKickPos && rightLegUp)
        {
            rightLegUp = false;
        }

        return rightKickDetected;
    }

    public static bool DetectLeftKick(List<Vector3> lm)
    {
        float lkneeY = lm[Pose.LEFT_KNEE].y;
        float lhipY = lm[Pose.LEFT_HIP].y;
        float lleg = Mathf.Abs(lm[Pose.LEFT_ANKLE].x - lm[Pose.LEFT_KNEE].x);
        bool leftKickPos = lleg > 0.05f && lkneeY < lhipY - 0.05f;

        bool leftKickDetected = false;

        if (leftKickPos && !leftLegUp)
        {
            leftKickDetected = true;
            leftLegUp = true;
        }
        else if (!leftKickPos && leftLegUp)
        {
            leftLegUp = false;
        }

        return leftKickDetected;
    }

    // Detect Ice Skating
    public static bool DetectIceSkate(List<Vector3> lm, float prevX, out float newX)
    {
        newX = lm[Pose.LEFT_HIP].x;
        if (float.IsNaN(prevX))
            return false;
        return Mathf.Abs(newX - prevX) > 0.05f;
    }

    // Detect Jump
    public static bool DetectJump(List<Vector3> lm, float prevY, out float newY)
    {
        newY = lm[Pose.NOSE].y;
        if (float.IsNaN(prevY))
            return false;
        return (prevY - newY) > 0.03f;
    }

    // Detect arm open-close
    public static bool DetectArmOpenClose(List<Vector3> lm, bool prevOpen, out bool newOpen, out bool triggered)
    {
        float lwx = lm[Pose.LEFT_WRIST].x;
        float lsy = lm[Pose.LEFT_SHOULDER].y;
        float rwx = lm[Pose.RIGHT_WRIST].x;
        float rsy = lm[Pose.RIGHT_SHOULDER].y;

        bool isOpen = (lwx < lm[Pose.LEFT_SHOULDER].x - 0.1f) && (rwx > lm[Pose.RIGHT_SHOULDER].x + 0.1f) &&
                      (Mathf.Abs(lm[Pose.LEFT_WRIST].y - lsy) < 0.15f) &&
                      (Mathf.Abs(lm[Pose.RIGHT_WRIST].y - rsy) < 0.15f);

        bool isClosed = (lwx > lm[Pose.LEFT_SHOULDER].x - 0.05f) && (rwx < lm[Pose.RIGHT_SHOULDER].x + 0.05f);

        newOpen = isOpen;
        triggered = prevOpen && isClosed;
        return true;
    }

    // Detect Head nodding
    public static bool DetectHeadNod(List<Vector3> lm, float prevY, string nodStage, out float newY, out string newStage, out bool moved)
    {
        newY = lm[Pose.NOSE].y;
        moved = false;
        newStage = nodStage;

        if (float.IsNaN(prevY))
            return false;

        if (string.IsNullOrEmpty(nodStage) && (newY - prevY > 0.015f))
        {
            newStage = "down";
        }
        else if (nodStage == "down" && (prevY - newY > 0.015f))
        {
            moved = true;
            newStage = null;
        }

        return true;
    }

    // Calculate the angle between three points using the dot product of two vectors
    private static float GetAngle(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector2 ba = new Vector2(a.x - b.x, a.y - b.y);
        Vector2 bc = new Vector2(c.x - b.x, c.y - b.y);
        float dot = Vector2.Dot(ba.normalized, bc.normalized);
        return Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
    }
}

public static class Pose
{
    public const int NOSE = 0;
    public const int LEFT_EYE = 1;
    public const int RIGHT_EYE = 2;
    public const int LEFT_SHOULDER = 11;
    public const int RIGHT_SHOULDER = 12;
    public const int LEFT_ELBOW = 13;
    public const int RIGHT_ELBOW = 14;
    public const int LEFT_WRIST = 15;
    public const int RIGHT_WRIST = 16;
    public const int LEFT_HIP = 23;
    public const int RIGHT_HIP = 24;
    public const int LEFT_KNEE = 25;
    public const int RIGHT_KNEE = 26;
    public const int LEFT_ANKLE = 27;
    public const int RIGHT_ANKLE = 28;
}
