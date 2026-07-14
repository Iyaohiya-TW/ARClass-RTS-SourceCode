using TMPro;
using UnityEngine;

public class UnitCountRequirment : TechRequirement
{
    public UnitData RequiredUnit;
    public int RequiredAmount;
    public TextMeshProUGUI MultiplierText;

    public override void Awake()
    {
        base.Awake();
        if(RequiredAmount > 1)
        {
            MultiplierText.text = $"x{RequiredAmount}";
        }
        else
        {
            MultiplierText.gameObject.SetActive(false);
        }
        UpdateStatus();
    }

    public override bool CheckRequirment()
    {
        int UnitCount = 0;
        if(isOneTimeTrigger && Triggered) return true;

        if(PC && PC.AllUnitList.Count > 0)
        {
            foreach (GameObject unitObj in PC.AllUnitList)
            {
                if(unitObj.TryGetComponent<Unit>(out  Unit unit))
                {
                    // 檢查單位名稱
                    if(unit.Data.UnitName == RequiredUnit.UnitName)
                    {
                        UnitCount++;
                    }
                    // Early termination
                    if(UnitCount >= RequiredAmount)
                    {
                        Triggered = true;
                        return true;
                    }
                }
            }
        }
        return false;
    }
    public override void UpdateStatus()
    {
        base.UpdateStatus();
        if (RequiredAmount > 1 && Triggered)
        {
            MultiplierText.color = Color.gray2;
        }
    }
}
