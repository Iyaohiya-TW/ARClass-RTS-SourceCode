using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 殭屍專用的資料容器，繼承自 UnitData。
/// 在 UnitData 所有通用欄位之上，新增殭屍行為的可調參數。
/// 建立方式：Assets → Create → Scriptable Objects → UnitData → ZombieData
/// </summary>
[CreateAssetMenu(fileName = "NewZombieData", menuName = "Scriptable Objects/UnitData/ZombieData")]
public class ZombieData : UnitData
{
    [Header("Zombie - Wander")]
    [Tooltip("每次隨機遊蕩的最大半徑（World Units）")]
    public float WanderRadius = 10f;

    [Tooltip("抵達遊蕩點後等待多久才選下一個點（秒）")]
    public float WanderIdleTime = 2f;

    [Header("Zombie - Aggro")]
    [Tooltip("發現目標後，超過此距離才會放棄追擊並回到遊蕩（應 >= DetectionRange）")]
    public float LeashRange = 20f;

    [Tooltip("是否對任何非同 Team 的單位都主動追擊（false = 只有 DetectionRange 內才追）")]
    public bool AlwaysAggressive = true;

    [Header("Zombie - Special")]
    [Tooltip("死亡時是否嘗試傳播感染（留給子系統實作，這裡只是旗標）")]
    public bool CanInfectOnDeath = false;

    [Tooltip("感染成功率 0~1（僅 CanInfectOnDeath = true 時有效）")]
    [Range(0f, 1f)]
    public float InfectionChance = 0.25f;

    [Tooltip("感染後生成的殭屍 Prefab（可為 null，表示不生成）")]
    public GameObject InfectedUnitPrefab;
}