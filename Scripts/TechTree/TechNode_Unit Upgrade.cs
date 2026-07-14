using System.Collections.Generic;
using UnityEngine;

public class TechNode_UnitUpdata : TechNode
{
    [Header("UnitUpdata")]
    public GameObject originalPrefab;
    public GameObject newPrefab;

    // Loop PlayerController All Unit List 替換 Prefab
    // 抓 繼承 (x,y)、血量百分比
    public override void ResolveEffect()
    {
        if (originalPrefab == null || newPrefab == null)
        {
            Debug.LogError("TechNode_UnitUpdata: Prefab 未指定！");
            return;
        }

        // 取得原預製件的 UnitData 來做名稱/類型比對
        Unit originalUnitScript = originalPrefab.GetComponent<Unit>();
        if (originalUnitScript == null || originalUnitScript.Data == null)
        {
            Debug.LogError("TechNode_UnitUpdata: originalPrefab 缺少 Unit 或 UnitData 元件！");
            return;
        }
        string targetUnitName = originalUnitScript.Data.UnitName;

        /* * 注意：這裡示範使用 FindObjectsByType 找尋場上所有單位。
         * 如果你的基底類別 TechNode 有記錄當前玩家 (例如 public PlayerController Owner)，
         * 或者你的 PlayerController 內有維護 List<Unit> (例如 Owner.MyUnits)，
         * 強烈建議直接 foreach 那個 List，效能會更好。
         * * 範例替換寫法：
         * Unit[] allUnits = Owner.MyUnits.ToArray();
         */
        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);

        // 使用 List 先收集需要升級的單位，避免在迴圈中直接 Instantiate/Destroy 造成陣列存取錯誤
        List<Unit> unitsToUpgrade = new List<Unit>();

        foreach (Unit unit in allUnits)
        {
            // 條件判斷：UnitData 存在、名稱相符。
            // (若有 TechNode 擁有者概念，請加上 && unit.Caller == this.Owner 來確保只升級自己的單位)
            if (unit.Data != null && unit.Data.UnitName == targetUnitName)
            {
                unitsToUpgrade.Add(unit);
            }
        }

        // 開始進行替換
        foreach (Unit oldUnit in unitsToUpgrade)
        {
            // 1. 紀錄繼承資訊：座標與旋轉
            Vector3 position = oldUnit.transform.position;
            Quaternion rotation = oldUnit.transform.rotation;

            // 2. 紀錄繼承資訊：血量百分比 (目前血量 / 最大血量)
            float hpPercentage = (float)oldUnit.HP / oldUnit.Data.MaxHP;

            // 紀錄所屬玩家，以便傳遞給新單位
            PlayerController caller = oldUnit.Caller;

            // 3. 實例化新單位的 Prefab
            GameObject newUnitGO = Instantiate(newPrefab, position, rotation);
            Unit newUnitScript = newUnitGO.GetComponent<Unit>();

            if (newUnitScript != null && newUnitScript.Data != null)
            {
                // 初始化新單位 (會幫忙設定 TeamTag 並套用對應陣營顏色)
                newUnitScript.Initialize(caller);

                // 根據剛剛的百分比，賦予新單位對應的血量
                newUnitScript.HP = Mathf.RoundToInt(newUnitScript.Data.MaxHP * hpPercentage);

                // 限制血量範圍，確保不會因為進位問題超過 MaxHP 或低於 0
                newUnitScript.HP = Mathf.Clamp(newUnitScript.HP, 1, newUnitScript.Data.MaxHP);

                // 更新血條 UI
                newUnitScript.UpdateHpSlider();

                if (caller != null) caller.AllUnitList.Add(newUnitGO);
            }

            Destroy(oldUnit.gameObject);
        }
    }
}