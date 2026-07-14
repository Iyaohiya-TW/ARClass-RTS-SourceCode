using UnityEngine;


public abstract class ProductionAbility : Ability
{
    public override void Use(GameObject Owner, PlayerController Caller)
    {
        // RTS logic: Add to a queue in the Building script
        Building bldg = Owner.GetComponent<Building>();
        if (bldg != null)
        {
            bldg.AddProductionQueue(this);
        }
    }

    public abstract void Cancel(GameObject Owner, PlayerController Caller); // 提前寫的，需要更多測試

    public abstract float GetProductionTime();
    public abstract void OnProductionEnd(GameObject Owner, PlayerController Caller);
}
