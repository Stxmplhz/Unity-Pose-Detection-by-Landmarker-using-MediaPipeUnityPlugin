using UnityEngine;

public enum PoseType
{
    Holding,
    Counting
}

[CreateAssetMenu(fileName = "NewPoseRequirement", menuName = "PoseGame/Pose Requirement")]
public class PoseRequirement : ScriptableObject
{
    [Header("Pose Identity")]
    public string PoseName;         // EN: "jump", "walk"
    public string PoseNameThai;     // TH: "กระโดด", "เดิน"
    public Sprite PoseIcon;         // Icon image for UI

    [Header("Pose Type")]
    public PoseType Type;

    [Header("Holding Pose")]
    public float DurationRequired;  // For Holding only

    [Header("Counting Pose")]
    public int CountRequired;       // For Counting only

    [Header("Timing")]
    public float TimeLimit = 10f;   // Time allowed for this pose

    [Header("How To Video (Local StreamingAssets)")]
    public string LocalVideoFileName;  // Video to teach player how to do each pose ex. "jump.mp4" 

    [Header("SFX Settings")]
    public AudioClip CountSFX;   // Sound when counting (play once per count)
    public AudioClip HoldSFX;    // Looping sound while holding (stop if pause > 1.5s)
}

