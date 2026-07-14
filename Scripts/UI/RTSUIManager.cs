using System.Collections.Generic;
using System.Resources;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RTSUIManager : MonoBehaviour
{
    private void Awake()
    {
        pc = Player.GetComponent<PlayerController>();
        UpdateResourcePanel();
    }

    public GameObject Player;
    PlayerController pc;

    [Header("檢視面板區塊")]
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI hpText;
    public Slider hpSlider;

    public TextMeshProUGUI resourceText;
    public Slider resourceSlider;

    [Header("資源面板區塊")]
    // Index 0: Wood, 1: Stone, 2: Gold, 3: Food
    public List<TextMeshProUGUI> resourceTexts;

    [Header("多單位選取區塊")]
    public Transform selectionGroupParent;
    public GameObject unitSmallIconPrefab; 

    [Header("技能欄位區塊")]
    public Transform abilityGridParent;
    public GameObject abilityButtonPrefab;

    private void Update()
    {
        Unit unit = null;

        // 檢查是否有選中單位
        if (pc.selectedUnits != null && pc.selectedUnits.Count > 0)
        {
            if (pc.selectedUnits[0] != null) unit = pc.selectedUnits[0].GetComponent<Unit>();
        }

        // 若無選中單位或資料遺失則跳過
        if (unit == null || unit.Data == null) return;

        // 每幀更新血量數值
        hpSlider.maxValue = unit.Data.MaxHP;
        hpSlider.value = unit.HP;
        hpText.text = $"{unit.HP} / {unit.Data.MaxHP}";

        // 若為資源點則更新資源數值
        if ((unit.Data.UnitTag & UnitTag.ResouceNode) != 0)
        {
            ResourceNode ResNode = pc.selectedUnits[0].GetComponent<ResourceNode>();
            resourceSlider.maxValue = ResNode.Data.MaxResAmn;
            resourceSlider.value = ResNode.GetResAmn();
            resourceText.text = $"{ResNode.GetResAmn()} / {ResNode.Data.MaxResAmn}";
        }
    }

    public void UpdateResourcePanel()
    {
        if (pc == null || pc.resourceSet == null) return;

        foreach (Resource res in pc.resourceSet.Resources)
        {
            int index = (int)res.Type;

            if (index >= 0 && index < resourceTexts.Count)
            {
                if (resourceTexts[index] != null)
                {
                    resourceTexts[index].text = res.Amount.ToString();
                }
            }
            else
            {
                Debug.LogWarning($"UI 面板缺少資源顯示欄位：找不到類型 {res.Type} (所需索引: {index})。請檢查 RTSUIManager 的 ResourceTexts 列表。");
            }
        }
    }

    public void UpdateInspectorPanel()
    {
        // 1. 重置 UI 狀態
        foreach (Transform child in abilityGridParent) Destroy(child.gameObject);
        foreach (Transform child in selectionGroupParent) Destroy(child.gameObject);

        hpSlider.gameObject.SetActive(false);
        hpText.gameObject.SetActive(false);
        resourceSlider.gameObject.SetActive(false);
        resourceText.gameObject.SetActive(false);

        pc.selectedUnits.RemoveAll(u => u == null);

        // 2. 處理未選取任何物件的情況
        if (pc.selectedUnits.Count == 0)
        {
            unitNameText.text = "None";
            return;
        }

        // 3. 產生所有選中單位的頭像清單
        foreach (GameObject obj in pc.selectedUnits)
        {
            if (obj == null) continue;
            Unit u = obj.GetComponent<Unit>();
            if (u != null && u.Data != null)
            {
                GameObject iconGO = Instantiate(unitSmallIconPrefab, selectionGroupParent);
                Image iconImg = iconGO.GetComponent<Image>();
                if (u.Data.Icon != null) iconImg.sprite = u.Data.Icon;

                // 可以額外加一個點擊小頭像就只選中該單位的邏輯 (可選)
                // iconGO.GetComponent<Button>().onClick.AddListener(() => pc.SelectSingleFromGroup(obj));
            }
        }

        // 4. 獲取「主選取」單位 (目前是第一個) 用於顯示詳細資訊與技能
        Unit unit = pc.selectedUnits[0].GetComponent<Unit>();
        if (unit == null || unit.Data == null) return;

        // 5. 更新檢視面板資訊 (血量、名字等)
        UpdateDetailedInspector(unit);

        // 6. 產生技能按鈕
        PopulateAbilityButtons(unit);
    }

    private void UpdateDetailedInspector(Unit unit)
    {
        unitNameText.text = pc.selectedUnits.Count > 1 ? $"{unit.Data.UnitName} (Total: {pc.selectedUnits.Count} Units)" : unit.Data.UnitName;

        hpSlider.gameObject.SetActive(true);
        hpSlider.maxValue = unit.Data.MaxHP;
        hpSlider.value = unit.HP;
        hpText.gameObject.SetActive(true);
        hpText.text = $"{unit.HP} / {unit.Data.MaxHP}";

        if ((unit.Data.UnitTag & UnitTag.ResouceNode) != 0)
        {
            ResourceNode ResNode = pc.selectedUnits[0].GetComponent<ResourceNode>();
            resourceSlider.gameObject.SetActive(true);
            resourceSlider.maxValue = ResNode.Data.MaxResAmn;
            resourceSlider.value = ResNode.GetResAmn();
            resourceText.gameObject.SetActive(true);
            resourceText.text = $"{ResNode.GetResAmn()} / {ResNode.Data.MaxResAmn}";
        }
        if((unit.Data.UnitTag & UnitTag.ConstructionSite) != 0)
        {
            ConstructionSite Site = pc.selectedUnits[0].GetComponent<ConstructionSite>();
            resourceSlider.gameObject.SetActive(true);
            resourceSlider.maxValue = Site.RequiredStep;
            resourceSlider.value = Site.CurrentBuildStep;
            resourceText.gameObject.SetActive(true);
            resourceText.text = $"{Site.CurrentBuildStep} / {Site.RequiredStep}";
        }
    }

    private void PopulateAbilityButtons(Unit unit)
    {
        for (int i = 0; i < unit.CurrentAbilities.Count; i++)
        {
            int index = i;
            string currentAbilityName = unit.CurrentAbilities[i].AbilityName;

            bool isAllSame = true;
            foreach (GameObject obj in pc.selectedUnits)
            {
                if (obj == null) continue;
                Unit otherUnit = obj.GetComponent<Unit>();
                if (otherUnit == null || index >= otherUnit.CurrentAbilities.Count || otherUnit.CurrentAbilities[index].AbilityName != currentAbilityName)
                {
                    isAllSame = false;
                    break;
                }
            }

            GameObject btnGO = Instantiate(abilityButtonPrefab, abilityGridParent);
            Image img = btnGO.GetComponent<Image>();
            if (unit.CurrentAbilities[i].Icon != null) img.sprite = unit.CurrentAbilities[i].Icon;

            Button btn = btnGO.GetComponent<Button>();
            if (!isAllSame)
            {
                btn.interactable = false;
                img.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }

            Ability ability = unit.CurrentAbilities[i];
            string buttonText = ability.AbilityName + "\n";
            List<Resource> Costs = ability.GetCost();

            // Ensure the ability has costs listed
            if (Costs != null && Costs.Count > 0)
            {
                List<string> costPieces = new List<string>();

                foreach (var resource in Costs)
                {
                    // Only display if the cost is greater than 0
                    if (resource.Amount > 0)
                    {
                        costPieces.Add($"{resource.Amount} {resource.Type}");
                    }
                }

                // If there are actual costs, append them in parentheses: "Fireball (50 Mana)"
                if (costPieces.Count > 0)
                {
                    buttonText += $" ({string.Join(", ", costPieces)})";
                }
            }

            // Pass the fully formatted text into your button
            btnGO.GetComponent<AbilityButton>().Setup(buttonText);

            btn.onClick.AddListener(() => pc.CommandUseAbility(index));
        }
    }
}