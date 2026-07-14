using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class TechNode : MonoBehaviour
{
    public TechData Data;
    public List<TechNode> BranchedTech;
    public List<TechNode> PrerequisiteTech;
    public List<TechRequirement> Requirements;
    public bool Avaliable = true;
    public bool isResearching = false;
    public bool Researched = false;

    public Image IconImage;
    public GameObject linePrefab;

    // Store references to the lines created by THIS node
    private List<TechLinkLine> activeLines = new List<TechLinkLine>();

    public void Awake()
    {
        // IconImage assignment moved up to ensure it exists before UpdateStatus
        IconImage = GetComponent<Image>();
        if (Data) IconImage.sprite = Data.SourceIcon;

        // We create links in Start or after a slight delay to ensure 
        // all RectTransforms are properly positioned by the UI Layout groups
        CreateLinks();
        UpdateStatus();
    }

    public virtual void ResolveEffect() { }

    public virtual void UpdateStatus()
    {
        if (!Researched)
        {
            IconImage.color = Color.gray;
            SetLinesColor(Color.black); // Default color for unresearched paths
        }
        else
        {
            IconImage.color = Color.white;
            SetLinesColor(Color.white); // Research completed color
        }
    }

    private void SetLinesColor(Color targetColor)
    {
        foreach (var line in activeLines)
        {
            if (line != null)
            {
                var lineImage = line.GetComponent<Image>();
                if (lineImage != null) lineImage.color = targetColor;
            }
        }
    }

    public bool CanResearch(ResourceSet ResSet)
    {
        // 檢查前置科技
        foreach (TechNode tech in PrerequisiteTech)
        {
            if (!tech.Researched) return false;
        }
        // 檢查條件/里程碑
        foreach (TechRequirement req in Requirements)
        {
            if (!req.CheckRequirment()) return false;
        }
        // 檢查資源量
        if (Data.Cost != null)
        {
            foreach (Resource resource in Data.Cost)
            {
                if (ResSet.GetResourceByType(resource.Type) < resource.Amount) return false;
            }
        }

        // Logical Fix: Avaliable should probably be true to allow research
        if (!Avaliable) return false;
        if (Researched || isResearching) return false;

        return true;
    }

    public void CreateLinks()
    {
        // Clear existing lines if re-generating
        activeLines.Clear();

        // Draw lines TO the branched nodes (downwards/forward in tree)
        foreach (TechNode childNode in BranchedTech)
        {
            if (childNode != null)
            {
                DrawLinkLine(childNode.GetComponent<RectTransform>());
            }
        }
    }

    public void DrawLinkLine(RectTransform childTransform)
    {
        GameObject lineObj = Instantiate(linePrefab, transform.parent);
        lineObj.transform.SetAsFirstSibling();

        TechLinkLine lineScript = lineObj.GetComponent<TechLinkLine>();

        // Start is THIS node, End is the CHILD node
        lineScript.startNode = this.GetComponent<RectTransform>();
        lineScript.endNode = childTransform;
        lineScript.UpdateLine();

        // Store the reference
        activeLines.Add(lineScript);
    }

    /// <summary>
    /// Debug only
    /// </summary>
    private void OnDrawGizmos()
    {
        float offsetAmount = 10f; // Adjust this until lines are separated enough

        // --- DOWNLINK: Branched Tech (Cyan) ---
        if (BranchedTech != null)
        {
            Gizmos.color = Color.cyan;
            foreach (TechNode child in BranchedTech)
            {
                if (child != null)
                    DrawOffsetGizmoLine(transform.position, child.transform.position, offsetAmount);
            }
        }

        // --- UPLINK: Prerequisite Tech (Red) ---
        if (PrerequisiteTech != null)
        {
            Gizmos.color = Color.red;
            foreach (TechNode parent in PrerequisiteTech)
            {
                if (parent != null)
                    DrawOffsetGizmoLine(parent.transform.position, transform.position, -offsetAmount);
            }
        }

        // --- UPLINK: Requirements (Red) ---
        if (Requirements != null)
        {
            Gizmos.color = Color.red;
            foreach (TechRequirement req in Requirements)
            {
                if (req != null)
                    DrawOffsetGizmoLine(req.transform.position, transform.position, -offsetAmount);
            }
        }
    }

    private void DrawOffsetGizmoLine(Vector3 start, Vector3 end, float offset)
    {
        Vector3 direction = (end - start).normalized;
        // Get a vector perpendicular to the direction (90 degrees rotated)
        Vector3 sideDir = new Vector3(-direction.y, direction.x, 0) * offset;

        Vector3 shiftedStart = start + sideDir;
        Vector3 shiftedEnd = end + sideDir;

        Gizmos.DrawLine(shiftedStart, shiftedEnd);

        // Direction indicator
        Gizmos.DrawSphere(shiftedEnd - direction * 5f, 5f);
    }
}