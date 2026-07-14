using UnityEngine;

[CreateAssetMenu(fileName = "ResNodeData", menuName = "Scriptable Objects/UnitData/ResNodeData")]
public class ResNodeData : UnitData
{
    [Header("資源節點專屬數據")]
    public int MaxResAmn;
    public ResourceType DefaultType;
}
