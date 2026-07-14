using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAbility_PlanConstruction", menuName = "Scriptable Objects/Ability/Ability_PlanConstruction")]
public class Ability_PlanConstruction : Ability
{
    public GameObject ConstructionSitePrefab;

    public override void Use(GameObject Owner, PlayerController Caller)
    {
        if(ConstructionSitePrefab && ConstructionSitePrefab.TryGetComponent<ConstructionSite>(out var site))
        {
            if(site.TargetPrefab && site.TargetPrefab.TryGetComponent<Building>(out var building))
            {
                foreach(Resource res in building.Data.Cost)
                {
                    if(Caller.resourceSet.GetResourceByType(res.Type) < res.Amount)
                    {
                        Debug.Log($"Ability: {res.Type} 資源不足無法建造");
                        return;
                    }
                }
            }
        }
        Caller.ToPlaceBuildingMode(ConstructionSitePrefab);
    }

    public override List<Resource> GetCost()
    {
        if (ConstructionSitePrefab && ConstructionSitePrefab.TryGetComponent<ConstructionSite>(out var site))
        {
            if (site.TargetPrefab && site.TargetPrefab.TryGetComponent<Building>(out var building))
            {
                return building.Data.Cost;
            }
        }
        return null;
    }
}
