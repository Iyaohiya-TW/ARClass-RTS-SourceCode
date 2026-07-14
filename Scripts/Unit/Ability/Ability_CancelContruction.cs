using UnityEngine;

[CreateAssetMenu(fileName = "Ability_CancelContruction", menuName = "Scriptable Objects/Ability/Ability_CancelContruction")]
public class Ability_CancelContruction : Ability
{
    public override void Use(GameObject Owner, PlayerController Caller)
    {
        if(Owner.TryGetComponent<ConstructionSite>(out var site))
        {
            site.Cancel();
        }
    }
}
