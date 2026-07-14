using UnityEngine;

[CreateAssetMenu(fileName = "NewWorkerData", menuName = "Scriptable Objects/UnitData/WorkerData")]
public class WorkerData : UnitData
{
    [Header("工人專屬數據")]
    public int MaxInventory = 10;

    public int GatherAmn = 1; // 一次收集動作會獲得(並扣除目標)幾點資源
    public float GatherSpeed = 1.0f; // GatherCD = 1 / GatherSpeed

    public int RepairAmn = 10; // 一次修復動作會恢復目標幾點HP
    public float RepairSpeed = 1.0f;

    public int BuildAmn = 1; // 一次建造動作會推進目標多少BuildStep
    public float BuildSpeed = 1.0f;
}
