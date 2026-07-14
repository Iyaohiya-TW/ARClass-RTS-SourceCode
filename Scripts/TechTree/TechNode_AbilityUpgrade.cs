using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 專門用來處理「單位實體 Prefab 替換」以及「特定單位 (如 BuildingManager) 技能替換」的科技節點
/// </summary>
public class TechNode_AbilityUpgrade : TechNode
{
    [Header("Unit Upgrade (單位實體升級設定)")]
    [Tooltip("選填：若不替換單位實體請保持空白")]
    public GameObject originalPrefab;
    public GameObject newPrefab;

    [Header("Ability Upgrade (技能/建造清單替換設定)")]
    [Tooltip("請拖入要升級技能的 UnitData (例如你的 BuildingManager 專屬 UnitData)")]
    public UnitData targetUnitData;

    [Tooltip("要被替換掉的舊能力 (例如：蓋一級農場)")]
    public Ability oldAbility;

    [Tooltip("升級後的新能力 (例如：蓋自動農場)")]
    public Ability newAbility;

    public override void ResolveEffect()
    {
        // ==========================================
        // 1. 處理「單位 Prefab 替換升級」
        // ==========================================
        if (originalPrefab != null && newPrefab != null)
        {
            Unit originalUnitScript = originalPrefab.GetComponent<Unit>();
            if (originalUnitScript != null && originalUnitScript.Data != null)
            {
                string targetUnitName = originalUnitScript.Data.UnitName;
                Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
                List<Unit> unitsToUpgrade = new List<Unit>();

                foreach (Unit unit in allUnits)
                {
                    if (unit.Data != null && unit.Data.UnitName == targetUnitName)
                    {
                        unitsToUpgrade.Add(unit);
                    }
                }

                foreach (Unit oldUnit in unitsToUpgrade)
                {
                    Vector3 position = oldUnit.transform.position;
                    Quaternion rotation = oldUnit.transform.rotation;
                    float hpPercentage = (float)oldUnit.HP / oldUnit.Data.MaxHP;
                    PlayerController caller = oldUnit.Caller;

                    GameObject newUnitGO = Instantiate(newPrefab, position, rotation);
                    Unit newUnitScript = newUnitGO.GetComponent<Unit>();

                    if (newUnitScript != null && newUnitScript.Data != null)
                    {
                        newUnitScript.Initialize(caller);
                        newUnitScript.HP = Mathf.RoundToInt(newUnitScript.Data.MaxHP * hpPercentage);
                        newUnitScript.HP = Mathf.Clamp(newUnitScript.HP, 1, newUnitScript.Data.MaxHP);
                        newUnitScript.UpdateHpSlider();

                        if (caller != null) caller.AllUnitList.Add(newUnitGO);
                    }
                    Destroy(oldUnit.gameObject);
                }
            }
        }

        // ==========================================
        // 2. 處理「特定單位的技能/建造清單替換」
        // ==========================================
        if (targetUnitData != null && oldAbility != null && newAbility != null)
        {
            Debug.Log($"[TechNode] 開始執行技能替換，目標 UnitData: {targetUnitData.name}");

            // 【修正點】：改為全域尋找 TechTree，避免 UI 層級問題導致找不到
            TechTree myTechTree = Object.FindAnyObjectByType<TechTree>();

            if (myTechTree == null)
            {
                Debug.LogError("[TechNode] 找不到場景中的 TechTree！技能替換失敗。");
                return;
            }
            if (myTechTree.controller == null)
            {
                Debug.LogError("[TechNode] TechTree 沒有綁定 PlayerController！無法判斷陣營，技能替換失敗。");
                return;
            }

            TeamTag myTeam = myTechTree.controller.TeamTag;
            Unit[] allUnitsInScene = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);

            bool foundTarget = false;

            foreach (Unit unit in allUnitsInScene)
            {
                // 比對 UnitData 是否為我們設定的目標 (例如 PlayerBuildingManager)
                if (unit.Data == targetUnitData)
                {
                    foundTarget = true;
                    Debug.Log($"[TechNode] 找到目標單位 [{unit.gameObject.name}]，檢查陣營 (單位:{unit.TeamTag} / 玩家:{myTeam})...");

                    if (unit.TeamTag == myTeam)
                    {
                        ReplaceAbilityInInstance(unit, oldAbility, newAbility);
                    }
                    else
                    {
                        Debug.LogWarning($"[TechNode] 單位陣營不符，跳過替換。");
                    }
                }
            }

            if (!foundTarget)
            {
                Debug.LogWarning($"[TechNode] 尋遍場上所有單位，找不到 Data 為 {targetUnitData.name} 的單位！");
            }
        }
    }

    /// <summary>
    /// 執行單一單位的技能替換
    /// </summary>
    private void ReplaceAbilityInInstance(Unit unit, Ability oldAb, Ability newAb)
    {
        if (unit.CurrentAbilities == null)
        {
            Debug.LogWarning($"[TechNode] 單位 {unit.gameObject.name} 的 CurrentAbilities 是 null！");
            return;
        }

        int index = unit.CurrentAbilities.IndexOf(oldAb);
        if (index != -1)
        {
            unit.CurrentAbilities[index] = newAb;
            Debug.Log($"<color=green>[成功]</color> 已將場上 [{unit.gameObject.name}] 的技能從 {oldAb.AbilityName} 替換為 {newAb.AbilityName}！");
        }
        else
        {
            Debug.LogWarning($"[TechNode] 在 [{unit.gameObject.name}] 的技能清單中找不到舊技能: {oldAb.name}！是否已經替換過了？");
        }
    }
}