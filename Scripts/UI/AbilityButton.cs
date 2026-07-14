using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class AbilityButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject tooltipTextDisplay;
    public GameObject tooltipPanel;

    // Call this when initializing the button to pass the target details
    public void Setup(string name)
    {
        tooltipTextDisplay.GetComponent<TextMeshProUGUI>().text = name;
        tooltipPanel.SetActive(false);
    }

    // Triggered when mouse hovers over the button
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(true);
        }
    }

    // Triggered when mouse leaves the button
    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}