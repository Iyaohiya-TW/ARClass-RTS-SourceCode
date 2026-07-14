using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility_ChangeWorkMode", menuName = "Scriptable Objects/Ability/Ability_ChangeWorkMode")]
public class Ability_ChangeWorkMode : Ability
{
    public WorkerState ChangeToMode;

    public override void Use(GameObject Owner, PlayerController Caller)
    {
        if (Owner.TryGetComponent<Worker>(out Worker worker))
        {
            switch (ChangeToMode)
            {
                case WorkerState.Neutral:
                    break;
                case WorkerState.GatherResource:
                        worker.ChangeToGatherMode();
                    break;
                case WorkerState.Build:
                    worker.ChangeToBuildMode();
                    break;
                case WorkerState.Repair:
                    break;
            }
        }
    }
}
