using System.Collections.Generic;
using UnityEngine;

public enum WorkerState
{
    Neutral,
    GatherResource,
    Build,
    Repair,
    DumpInventory,
    Combat
}

public class Worker : Unit
{
    public new WorkerData Data => (WorkerData)base.Data;
    public WorkerState state = WorkerState.Neutral;
    public Resource Inventory;
    public ResourceType PreviousResType;

    public float WorkCD;

    private GameObject nearestResStorage;

    // 記住採礦目標，供卸貨後返回
    private GameObject _lastResTarget;

    // 記住被打斷前的工作狀態，反擊結束後還原
    private WorkerState _savedState = WorkerState.Neutral;
    private GameObject _savedTarget = null;

    protected override void Awake()
    {
        base.Awake();
        Inventory = new Resource(ResourceType.Wood, 0);
    }

    protected override void Update()
    {
        base.Update();
        ResolveWorkerState();
    }

    // 玩家指令移動：先清所有工作狀態，再呼叫 base.Move()
    // base.Move() 會設 _isCommandMoving = true，讓 ResolveUnitState 暫停巡敵與攻擊
    // 抵達目的地後 StopMoving() 清除 _isCommandMoving，回到 Neutral 正常巡邏
    public override void Move(Vector3 destination)
    {
        state = WorkerState.Neutral;
        Target = null;
        _lastResTarget = null;
        nearestResStorage = null;

        // 清掉已儲存的工作狀態，玩家主動移動代表放棄原本工作
        _savedState = WorkerState.Neutral;
        _savedTarget = null;

        base.Move(destination);
    }

    // 工作途中的內部移動：不觸發狀態切換，不設 _isMoving
    private void MoveInternal(Vector3 destination)
    {
        MoveWithoutCallback(destination);
    }

    void ResolveWorkerState()
    {
        switch (state)
        {
            case WorkerState.Neutral:
                break;
            case WorkerState.GatherResource:
                HandleGatherRes();
                break;
            case WorkerState.Build:
                HandleBuild();
                break;
            case WorkerState.Repair:
                break;
            case WorkerState.DumpInventory:
                HandleDumpInventory();
                break;
            case WorkerState.Combat:
                HandleAutoCombat();
                break;
        }
    }

    // 工兵工作時不讓 Unit 層的自動尋敵覆寫行為
    protected override void ResolveUnitState()
    {
        bool isWorking = state == WorkerState.GatherResource
                      || state == WorkerState.Build
                      || state == WorkerState.DumpInventory;

        if (!isWorking)
        {
            base.ResolveUnitState();
        }
    }

    protected override void ResolveCD(float DeltaTime)
    {
        base.ResolveCD(DeltaTime);
        if (WorkCD > 0) WorkCD -= DeltaTime;
        else WorkCD = 0;
    }

    // =========================================================
    // 受傷反擊：儲存目前工作狀態，切換到 Combat
    // - 若已在 Combat_Command（玩家指令攻擊）則不打斷
    // - 若在指令移動中，打斷移動並儲存工作狀態
    // - 若已在 Combat（已在反擊中）則只更新 Target 為最新攻擊者
    // =========================================================
    protected override void TryCounterAttack(GameObject instigator)
    {
        if (instigator == null) return;

        // 玩家下了指令攻擊，不被受傷打斷
        if (unitState == UnitState.Combat_Command) return;

        // 被攻擊打斷指令移動
        if (_isCommandMoving)
        {
            _isCommandMoving = false;
            StopMoving();
        }

        // 已在反擊中：只更新目標為最新攻擊者，不重複儲存工作狀態
        if (state == WorkerState.Combat)
        {
            Target = instigator;
            unitState = UnitState.Combat_Auto;
            return;
        }

        // 第一次被打斷：儲存工作狀態
        _savedState = state;
        _savedTarget = Target;

        // 切換到戰鬥
        Target = instigator;
        unitState = UnitState.Combat_Auto;
        state = WorkerState.Combat;
    }

    // =========================================================
    // 工人不主動掃描敵人；只在 Combat 狀態下呼叫 base 掃描，
    // 並在敵人消滅後還原工作
    // =========================================================
    protected override void SearchForEnemy()
    {
        if (state != WorkerState.Combat) return;

        base.SearchForEnemy();

        // base.SearchForEnemy() 把 unitState 切回 Neutral 代表附近沒有敵人了
        if (unitState == UnitState.Neutral)
        {
            RestoreWork();
        }
    }

    // 還原被中斷前的工作狀態
    private void RestoreWork()
    {
        Target = _savedTarget;
        state = _savedState;

        _savedState = WorkerState.Neutral;
        _savedTarget = null;
    }

    void HandleGatherRes()
    {
        if (Inventory.Amount >= Data.MaxInventory)
        {
            _lastResTarget = Target;
            ChangeToDumpInventoryMode();
            return;
        }

        if (Target != null && Target.TryGetComponent<ResourceNode>(out ResourceNode ResNode))
        {
            if (Inventory.Amount <= 0 || Inventory.Type != ResNode.GetResType())
            {
                Inventory.Amount = 0;
                Inventory.Type = ResNode.GetResType();
            }

            if (IsWithinInteractRange(Target))
            {
                StopMoving();
                if (WorkCD <= 0)
                {
                    int gathered = Mathf.Min(Data.GatherAmn, ResNode.GetResAmn());
                    Inventory.Amount += gathered;
                    ResNode.OnGather(Data.GatherAmn);
                    WorkCD = 1f / Data.GatherSpeed;
                    PreviousResType = ResNode.GetResType();
                    _lastResTarget = Target;
                }
            }
            else
            {
                MoveTowardsTargetInternal();
            }
        }
        else
        {
            // 找不到目標時才換目標
            Target = FindNearestResNode(PreviousResType);

            if (Target == null)
                Target = FindNearestByTag(UnitTag.ResouceNode);

            if (Target == null)
            {
                if (Inventory.Amount > 0)
                    ChangeToDumpInventoryMode();
                else
                    ChangeToNeutralMode();
            }
        }
    }

    void HandleBuild()
    {
        if (Target != null && Target.TryGetComponent<ConstructionSite>(out var Site))
        {
            if (IsWithinInteractRange(Target))
            {
                StopMoving();
                if (WorkCD <= 0)
                {
                    Site.Build(Data.BuildAmn, Data.RepairAmn);
                    WorkCD = 1f / Data.BuildSpeed;
                }
            }
            else
            {
                MoveTowardsTargetInternal();
            }
        }
        else
        {
            Target = FindNearestByTag(UnitTag.ConstructionSite);
            if (Target == null)
                ChangeToNeutralMode();
        }
    }

    void HandleDumpInventory()
    {
        if (nearestResStorage == null)
        {
            ChangeToNeutralMode();
            return;
        }

        float storageRadius = 0f;
        if (nearestResStorage.TryGetComponent<Collider>(out Collider col))
            storageRadius = col.bounds.extents.magnitude;

        float dumpRange = storageRadius + Data.InteractRange;

        if (InRange(nearestResStorage, dumpRange))
        {
            Caller.resourceSet.AddResource(Inventory);
            Inventory.Amount = 0;

            if (_lastResTarget != null)
            {
                Target = _lastResTarget;
                state = WorkerState.GatherResource;
            }
            else
            {
                ChangeToGatherMode();
            }
        }
        else
        {
            Vector3 directionFromStorage = (transform.position - nearestResStorage.transform.position).normalized;
            Vector3 destination = nearestResStorage.transform.position + (directionFromStorage * storageRadius);
            MoveInternal(destination);
        }
    }

    // 工作途中追蹤目標的內部移動版本
    // state guard：若 state 已被外部清為 Neutral（玩家下了移動指令），
    // 就不再覆蓋 NavMesh 目的地
    private void MoveTowardsTargetInternal()
    {
        if (state == WorkerState.Neutral) return;
        if (Target == null) return;

        Collider targetCollider = Target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
            Vector3 direction = (transform.position - closestPoint).normalized;
            Vector3 destination = closestPoint + direction * 0.05f;
            MoveInternal(destination);
        }
        else
        {
            MoveInternal(Target.transform.position);
        }
    }

    GameObject FindNearestResNode(ResourceType ResType)
    {
        ResourceNode[] allNode = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
        GameObject nearest = null;
        float minDistance = float.MaxValue;
        Vector3 currentPos = transform.position;

        foreach (ResourceNode Node in allNode)
        {
            if (Node.GetResType() == ResType)
            {
                float dist = Vector3.Distance(currentPos, Node.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = Node.gameObject;
                }
            }
        }
        return nearest;
    }

    GameObject FindNearestResStorage(ResourceType ResType)
    {
        GameObject nearest = null;
        float minDistance = float.MaxValue;
        Vector3 currentPos = transform.position;
        List<Building> allPossibleStorage = new List<Building>();

        foreach (GameObject go in Caller.AllUnitList)
        {
            if (go.TryGetComponent<Building>(out Building building))
            {
                foreach (ResourceType type in building.Data.CanStoreTypes)
                {
                    if (type == ResType)
                    {
                        allPossibleStorage.Add(building);
                        break;
                    }
                }
            }
        }

        foreach (Building building in allPossibleStorage)
        {
            float dist = Vector3.Distance(currentPos, building.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = building.gameObject;
            }
        }
        return nearest;
    }

    public void Interact(GameObject InteractTarget)
    {
        if (InteractTarget.TryGetComponent<Unit>(out Unit unit))
        {
            if ((unit.Data.UnitTag & UnitTag.ResouceNode) != 0)
            {
                Target = InteractTarget;
                ChangeToGatherMode();
            }
            if ((unit.Data.UnitTag & UnitTag.ConstructionSite) != 0)
            {
                Target = InteractTarget;
                ChangeToBuildMode();
            }
        }
    }

    public void ChangeToGatherMode()
    {
        state = WorkerState.GatherResource;
    }

    public void ChangeToBuildMode()
    {
        state = WorkerState.Build;
    }

    public void ChangeToDumpInventoryMode()
    {
        nearestResStorage = FindNearestResStorage(Inventory.Type);
        state = WorkerState.DumpInventory;
    }

    public void ChangeToNeutralMode()
    {
        state = WorkerState.Neutral;
    }
}