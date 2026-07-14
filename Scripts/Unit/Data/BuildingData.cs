using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Scriptable Objects/UnitData/BuildingData")]
public class BuildingData : UnitData 
{
    [Header("建築專屬數據")]

    [Tooltip("有無集結點")]
    public bool hasRallyPoint = true;

    public int RequiredBuildStep;        // 1個 建築效率為1的工人 要敲幾下才能蓋完
    public float ConstructionSiteHPRatio = 1.0f; // 此建築施工地的血量(基於此建築血量的比例)
    
    [Tooltip("資源儲存節點")]
    public List<ResourceType> CanStoreTypes;
}