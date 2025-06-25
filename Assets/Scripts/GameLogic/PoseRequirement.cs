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
}

