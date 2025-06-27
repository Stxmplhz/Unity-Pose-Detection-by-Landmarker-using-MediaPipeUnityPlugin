using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PoseGameConfig", menuName = "PoseGame/SceneConfig")]
public class PoseGameConfig : ScriptableObject
{
    public List<PoseRequirement> PosesInScene;
}
