using System.Collections.Generic;
using System.Net.NetworkInformation;
using Unity.VisualScripting;
using Unity.XR.OpenVR;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility_ResearchTech", menuName = "Scriptable Objects/Ability/Ability_ResearchTech")]
public class Ability_ResearchTech : ProductionAbility
{
    bool isResearching = false;
    public TechData ResearchTarget;

    public Ability_ResearchTech NextLevel;
    public Ability EmptyAbility;

    private PlayerController _caller;

    public void Initialize(PlayerController caller)
    {
        _caller = caller;
    }

    public override void Use(GameObject Owner, PlayerController Caller)
    {
        _caller = Caller;
        foreach(TechNode techNode in Caller.techTree.TechList)
        {
            // 找出資源樹上對應的節點
            if(techNode.Data.TechName == ResearchTarget.TechName)
            {
                // 如果可以研究
                if (techNode.CanResearch(Caller.resourceSet))
                {
                    // 扣除資源
                    foreach (Resource cost in techNode.Data.Cost)
                    {
                        Caller.resourceSet.CostResource(cost);
                    }
                    techNode.isResearching = true;
                    // 加入列隊
                    base.Use(Owner, Caller);
                    return;
                }
            }
        }
        Debug.Log($"Ability: 現在無法研究 \"{ResearchTarget.name}\"，可能因為資源不足或已經在研究中.");
    }

    public override List<Resource> GetCost()
    {
        if(_caller)
        {
            foreach (TechNode techNode in _caller.techTree.TechList)
            {
                // 找出資源樹上對應的節點
                if (techNode.Data.TechName == ResearchTarget.TechName)
                {
                    return techNode.Data.Cost;
                }
            }
        }
        return null;
    }

    public override void Cancel(GameObject Owner, PlayerController Caller)
    {
        foreach (TechNode techNode in Caller.techTree.TechList)
        {
            // 找出資源樹上對應的節點
            if (techNode.Data.TechName == ResearchTarget.TechName)
            {
                // 退還資源
                foreach (Resource cost in techNode.Data.Cost)
                {
                    Caller.resourceSet.AddResource(cost);
                }
                techNode.isResearching = false;
            }
        }
    }

    public override float GetProductionTime()
    {
        return ResearchTarget.ResearchTime;
    }

    public override void OnProductionEnd(GameObject Owner, PlayerController Caller)
    {
        foreach (TechNode techNode in Caller.techTree.TechList)
        {
            // 找出資源樹上對應的節點
            if (techNode.Data.TechName == ResearchTarget.TechName)
            {
                techNode.Researched = true;
                techNode.UpdateStatus();
                techNode.ResolveEffect();

                foreach (GameObject go in Caller.AllUnitList)
                {
                    // 找出所有相同 Building
                    if (go.TryGetComponent<Building>(out Building building)
                        && building.Data.UnitName == Owner.GetComponent<Building>().Data.UnitName)
                    {
                        int index = 0;
                        // 找出相同的 Ability
                        foreach(Ability ability in building.CurrentAbilities)
                        {
                            if(ability.AbilityName == AbilityName)
                            {
                                if (NextLevel)
                                {
                                    building.CurrentAbilities[index] = NextLevel;
                                    break;
                                }
                                else
                                {
                                    building.CurrentAbilities[index] = EmptyAbility;
                                    break;
                                }
                            }
                            index++;
                        }
                        Caller.UIManager.UpdateInspectorPanel();
                    }
                }
            }
        }
    }
}
