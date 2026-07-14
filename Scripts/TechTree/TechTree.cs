using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TechTree : MonoBehaviour
{
    public PlayerController controller;
    public List<TechNode> TechList;
    public List<TechRequirement> RequirementList;
    public TechNode RootTech;
    public GameObject techTreePanel;

    public void Awake()
    {
        TechList = GetAllTechNodes();
        RequirementList = GetAllRequirment();
        foreach (TechRequirement Req in RequirementList)
        {
            Req.PC = controller;
            Req.UpdateStatus();
        }
    }

    public void UpdateReqirement()
    {
        foreach (TechRequirement requirement in RequirementList)
        {
            requirement.UpdateStatus();
        }
    }

    public void ToggleUI()
    {
        if (techTreePanel == null) return;


        // Flip the active state
        bool isActive = !techTreePanel.activeSelf;
        if (!isActive)
        {
            foreach(TechNode Node in TechList)
            {
                Node.UpdateStatus();
            }
            foreach(TechRequirement requirement in RequirementList)
            {
                requirement.UpdateStatus();
            }
        }
        techTreePanel.SetActive(isActive);
    }

    private List<TechNode> GetAllTechNodes()
    {
        // true includes inactive GameObjects if needed
        return new List<TechNode>(techTreePanel.GetComponentsInChildren<TechNode>(true));
    }

    private List<TechRequirement> GetAllRequirment()
    {
        // true includes inactive GameObjects if needed
        return new List<TechRequirement>(techTreePanel.GetComponentsInChildren<TechRequirement>(true));
    }

    public bool isResearched(string techName)
    {
        TechNode foundTech = TechList.FirstOrDefault(t => t.Data.TechName == techName);

        if (foundTech != null)
        {
            return foundTech.Researched;
        }

        Debug.LogWarning("Tech with name " + techName + " not found in TechList!");
        return false;
    }
}
