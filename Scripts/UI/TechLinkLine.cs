using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class TechLinkLine : MonoBehaviour
{
    public RectTransform startNode;
    public RectTransform endNode;
    public float thickness = 2f;
    public Color DefaultlineColor = Color.gray;

    private Image lineImage;
    private RectTransform rectTransform;

    void Awake()
    {
        lineImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        lineImage.color = DefaultlineColor;
        UpdateLine();
    }


    public void UpdateLine()
    {
        if (startNode == null || endNode == null || lineImage == null) return;

        // Calculate direction and distance
        Vector2 dir = endNode.anchoredPosition - startNode.anchoredPosition;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Set the line position to the start node
        rectTransform.anchoredPosition = startNode.anchoredPosition;

        // Size the line: Width is the distance, Height is thickness
        rectTransform.sizeDelta = new Vector2(distance, thickness);

        // Rotate to point at the target
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);

        // Pivot to the left side so it rotates around the start point
        rectTransform.pivot = new Vector2(0, 0.5f);
    }
}