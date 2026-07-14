using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NewAbility_TrainUnit", menuName = "Scriptable Objects/Ability/Ability_TrainUnit")]
public class Ability_TrainUnit : ProductionAbility
{
    public GameObject UnitPrefab;

    public override void Use(GameObject Owner, PlayerController Caller)
    {
        bool canUse = true;
        if (UnitPrefab != null)
        {
            foreach(Resource cost in UnitPrefab.GetComponent<Unit>().Data.Cost)
            {
                if(Caller.resourceSet.GetResourceByType(cost.Type) < cost.Amount)
                {
                    canUse = false;
                }
            }

            if (canUse)
            {
                foreach (Resource cost in UnitPrefab.GetComponent<Unit>().Data.Cost)
                {
                    Caller.resourceSet.CostResource(cost);
                }
                base.Use(Owner, Caller);
            } 
            else { Debug.Log($"Ability: 資源不足，無法生產 \"{UnitPrefab.GetComponent<Unit>().Data.name}\""); }
        }
    }

    public override void Cancel(GameObject Owner, PlayerController Caller)
    {
            foreach (Resource cost in UnitPrefab.GetComponent<Unit>().Data.Cost)
            {
                Caller.resourceSet.AddResource(cost);
            }
    }

    public override float GetProductionTime()
    {
        return UnitPrefab.GetComponent<Unit>().Data.TrainTime;
    }

    public override void OnProductionEnd(GameObject Owner, PlayerController Caller)
    {
        if (Owner.TryGetComponent<Building>(out Building building))
        {
            Vector3 spawnPos = building.WorldSpawnPoint;
            Quaternion spawnRot = building.transform.rotation;

            GameObject unitGO = Instantiate(UnitPrefab, spawnPos, spawnRot);

            Unit unitScript = unitGO.GetComponent<Unit>();
            unitScript.Initialize(Caller);
            Caller.AddUnitToList(unitGO);

            if (unitScript != null)
            {
                unitScript.Move(building.rallyPoint);
            }
        }
    }

    public override List<Resource> GetCost()
    {
       if(UnitPrefab)  return UnitPrefab.GetComponent<Unit>().Data.Cost;
       else return null;
    }

}
