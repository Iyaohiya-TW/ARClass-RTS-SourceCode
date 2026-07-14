using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public abstract class TechRequirement : MonoBehaviour
{
    public string TechName;
    public Sprite IconSource;

    public bool isOneTimeTrigger;
    public bool Triggered = false;

    public PlayerController PC;
    public Image Icon;

    [Header("Link Settings")]
    // Changed to a List to support linking to multiple technologies
    public List<TechNode> TargetTechs = new List<TechNode>();
    public GameObject linePrefab;

    private List<TechLinkLine> activeLines = new List<TechLinkLine>();

    public abstract bool CheckRequirment();

    public virtual void UpdateStatus()
    {
        CheckRequirment();

        if (Icon != null)
        {
            Icon.color = Triggered ? Color.white : Color.gray;
        }

        // Update all outbound lines to the technologies
        SetLinesColor(Triggered ? Color.white : Color.black);
    }

    public virtual void Awake()
    {
        Icon = GetComponent<Image>();
        if (Icon != null && IconSource != null) Icon.sprite = IconSource;

        CreateLinks();
        UpdateStatus();
    }

    public void CreateLinks()
    {
        // Clean up if re-initializing
        activeLines.Clear();

        if (TargetTechs != null)
        {
            foreach (TechNode node in TargetTechs)
            {
                if (node != null)
                {
                    DrawLinkLine(node.GetComponent<RectTransform>());
                }
            }
        }
    }

    public void DrawLinkLine(RectTransform targetTransform)
    {
        // Instantiate line on the parent container so it sits behind icons
        GameObject lineObj = Instantiate(linePrefab, transform.parent);
        lineObj.transform.SetAsFirstSibling();

        TechLinkLine lineScript = lineObj.GetComponent<TechLinkLine>();

        // Start: This Requirement Icon | End: The target Tech Node
        lineScript.startNode = this.GetComponent<RectTransform>();
        lineScript.endNode = targetTransform;
        lineScript.UpdateLine();

        activeLines.Add(lineScript);
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

    // ... existing code (variables, Awake, CreateLinks, etc.)

    protected virtual void OnDrawGizmos()
    {
        if (TargetTechs == null || TargetTechs.Count == 0) return;

        // Requirements use Cyan to show they "point toward" the TechNodes they unlock
        Gizmos.color = Color.cyan;

        foreach (TechNode node in TargetTechs)
        {
            if (node != null)
            {
                DrawGizmoArrow(transform.position, node.transform.position);
            }
        }
    }

    /// <summary>
    /// Helper to draw a line with a small indicator at the target.
    /// Shared logic with TechNode for visual consistency.
    /// </summary>
    private void DrawGizmoArrow(Vector3 start, Vector3 end)
    {
        Gizmos.DrawLine(start, end);

        // Draw a small diamond/cube to show the direction of the requirement flow
        Vector3 direction = (end - start).normalized;
        float indicatorSize = 5f; // Adjust this based on your Canvas/Scene scale

        // Offset slightly from the exact center of the node so it's visible
        Gizmos.DrawWireCube(end - direction * (indicatorSize * 1.5f), Vector3.one * indicatorSize);
    }
}