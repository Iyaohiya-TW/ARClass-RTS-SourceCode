using UnityEngine;

// 繼承自原有的 UnitData，保留所有基礎屬性
[CreateAssetMenu(fileName = "NewWashMachineData", menuName = "Scriptable Objects/UnitData/WashMachineData")]
public class WashMachineData : UnitData
{
    [Header("Wash Machine Specific Skill")]
    [Tooltip("技能冷卻時間")]
    public float SkillCooldown = 10f;

    [Tooltip("拉扯（吸引）持續時間")]
    public float PullDuration = 1.5f;

    [Tooltip("拉扯的移動速度")]
    public float PullSpeed = 8f;

    [Header("Visualization")]
    [Tooltip("用於顯示技能效果範圍的 Prefab (例：半透明圓圈)")]
    public GameObject RangeVisualizationPrefab;
}