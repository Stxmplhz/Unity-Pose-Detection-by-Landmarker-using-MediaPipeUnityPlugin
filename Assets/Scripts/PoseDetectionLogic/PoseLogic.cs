using UnityEngine;
using System;
using System.Collections.Generic;

public class PoseLogic : MonoBehaviour
{
    // ค่าก่อนหน้าที่ใช้เปรียบเทียบในการตรวจจับท่า
    public float prevHipX = float.NaN;
    public float prevNoseY = float.NaN;
    public string nodStage = null;
    public bool prevArmOpen = false;

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
                return DetectWalk(landmarks);

            case "run":
                return DetectRun(landmarks);

            case "bendforward":
                return DetectBendForward(landmarks);

            case "leftarmstretchhold":
                return DetectLeftArmStretchHold(landmarks);

            case "rightarmstretchhold":
                return DetectRightArmStretchHold(landmarks);

            case "kick":
                return DetectKick(landmarks);

            case "iceskate":
                return DetectIceSkate(landmarks, prevHipX, out prevHipX);

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

    // ----- Functions ปรับ threshold แล้ว (สำหรับ normalized coordinates) -----

    // ตรวจจับ walking โดยดูความแตกต่างของแนวตั้งที่เข่าทั้งซ้ายและขวา
    public static bool DetectWalk(List<Vector3> lm)
    {
        // หากความแตกต่างมากกว่า 0.08 (ปรับได้ตาม log ที่ได้)
        return Mathf.Abs(lm[Pose.LEFT_KNEE].y - lm[Pose.RIGHT_KNEE].y) > 0.08f;
    }

    // ตรวจจับ running โดยใช้ตำแหน่งของข้อเท้าทั้งสองข้าง
    public static bool DetectRun(List<Vector3> lm)
    {
        return Mathf.Abs(lm[Pose.LEFT_ANKLE].y - lm[Pose.RIGHT_ANKLE].y) > 0.12f;
    }

    // ตรวจจับการก้มตัวไปข้างหน้า โดยคำนวณมุมระหว่าง สะโพก, ไหล่ และ จมูก
    public static bool DetectBendForward(List<Vector3> lm)
    {
        float angle = GetAngle(lm[Pose.LEFT_HIP], lm[Pose.LEFT_SHOULDER], lm[Pose.NOSE]);
        return angle < 160f;  // ปรับ threshold ให้ผ่อนคลายขึ้น
    }

    // ตรวจจับการยืดแขนด้านซ้าย (อาจต้องปรับขึ้นอยู่กับทิศทางของกล้อง)
    public static bool DetectLeftArmStretchHold(List<Vector3> lm)
    {
        float wristX = lm[Pose.LEFT_WRIST].x;
        float shoulderX = lm[Pose.LEFT_SHOULDER].x;
        float oppX = lm[Pose.RIGHT_SHOULDER].x;
        float wristY = lm[Pose.LEFT_WRIST].y;
        float shoulderY = lm[Pose.LEFT_SHOULDER].y;

        // ในกรณีนี้ สมมติว่าท่าซ้ายหมายถึงข้อมือซ้ายอยู่ทางซ้ายมากกว่าสะโพกขวา
        bool stretchX = wristX < oppX - 0.1f;
        bool extended = Mathf.Abs(wristX - shoulderX) > 0.06f && Mathf.Abs(wristY - shoulderY) < 0.2f;

        return stretchX && extended;
    }

    // ตรวจจับการยืดแขนด้านขวา
    public static bool DetectRightArmStretchHold(List<Vector3> lm)
    {
        float wristX = lm[Pose.RIGHT_WRIST].x;
        float shoulderX = lm[Pose.RIGHT_SHOULDER].x;
        float oppX = lm[Pose.LEFT_SHOULDER].x;
        float wristY = lm[Pose.RIGHT_WRIST].y;
        float shoulderY = lm[Pose.RIGHT_SHOULDER].y;

        bool stretchX = wristX > oppX + 0.1f;
        bool extended = Mathf.Abs(wristX - shoulderX) > 0.06f && Mathf.Abs(wristY - shoulderY) < 0.2f;

        return stretchX && extended;
    }

    // ตรวจจับ twist โดยดูความต่างในแกน z ระหว่างส่วนบนและส่วนล่าง
    public static bool DetectTwist(List<Vector3> lm)
    {
        float lsZ = lm[Pose.LEFT_SHOULDER].z;
        float rsZ = lm[Pose.RIGHT_SHOULDER].z;
        float lhZ = lm[Pose.LEFT_HIP].z;
        float rhZ = lm[Pose.RIGHT_HIP].z;

        return Mathf.Abs((lsZ - lhZ) - (rsZ - rhZ)) > 0.08f;
    }

    // ตรวจจับ squat โดยดูมุมที่เข่า
    public static bool DetectSquat(List<Vector3> lm)
    {
        float angle = GetAngle(lm[Pose.LEFT_HIP], lm[Pose.LEFT_KNEE], lm[Pose.LEFT_ANKLE]);
        return angle > 60f && angle < 110f;
    }

    // ตรวจจับการชก (punch)
    public static bool DetectPunch(List<Vector3> lm)
    {
        float r = lm[Pose.RIGHT_WRIST].x - lm[Pose.RIGHT_ELBOW].x;
        float l = lm[Pose.LEFT_ELBOW].x - lm[Pose.LEFT_WRIST].x;
        return (r > 0.07f) || (l > 0.07f);
    }

    // ตรวจจับการเตะ (kick)
    public static bool DetectKick(List<Vector3> lm)
    {
        float rkneeY = lm[Pose.RIGHT_KNEE].y;
        float rhipY = lm[Pose.RIGHT_HIP].y;
        float rleg = Mathf.Abs(lm[Pose.RIGHT_ANKLE].x - lm[Pose.RIGHT_KNEE].x);
        bool rk = rleg > 0.05f && rkneeY < rhipY - 0.05f;

        float lkneeY = lm[Pose.LEFT_KNEE].y;
        float lhipY = lm[Pose.LEFT_HIP].y;
        float lleg = Mathf.Abs(lm[Pose.LEFT_ANKLE].x - lm[Pose.LEFT_KNEE].x);
        bool lk = lleg > 0.05f && lkneeY < lhipY - 0.05f;

        return rk || lk;
    }

    // ตรวจจับ Ice Skating
    public static bool DetectIceSkate(List<Vector3> lm, float prevX, out float newX)
    {
        newX = lm[Pose.LEFT_HIP].x;
        if (float.IsNaN(prevX))
            return false;
        return Mathf.Abs(newX - prevX) > 0.05f;
    }

    // ตรวจจับการกระโดด (jump)
    public static bool DetectJump(List<Vector3> lm, float prevY, out float newY)
    {
        newY = lm[Pose.NOSE].y;
        if (float.IsNaN(prevY))
            return false;
        return (prevY - newY) > 0.03f;
    }

    // ตรวจจับการเปิด/ปิดแขน (arm open/close)
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

    // ตรวจจับการพยักหัว (head nod)
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

    // คำนวณมุมระหว่างจุด 3 จุด โดยใช้ dot product ของเวกเตอร์ 2 ค่า
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
