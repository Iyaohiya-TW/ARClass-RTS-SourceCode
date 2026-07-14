using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;

public class Zombie : Unit
{
    public ZombieManager Manager;
    private ZombieData ZombieData => Data as ZombieData;

    private NavMeshAgent _agent;

    private enum WanderState { Idle, Moving }
    private WanderState _wanderState = WanderState.Idle;
    private float _wanderIdleTimer = 0f;

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();

        unitState = UnitState.Neutral;
        //Debug.Log($"[Zombie:Awake] unitState={unitState}, TeamTag={TeamTag}, agent={_agent != null}, isOnNavMesh={_agent?.isOnNavMesh}");
    }

    public void Initialization(ZombieManager ZM)
    {
        Manager = ZM;
        if (MinimapAvatar != null)
        {
            // 1. Remember the exact Sliced Size you set in the Inspector
            Vector2 fixedSize = MinimapAvatar.size;

            // 2. Change the sprite (Unity will secretly reset the size here)
            MinimapAvatar.sprite = Data.Icon;

            // 3. Force Unity to use your fixed size again
            MinimapAvatar.size = fixedSize;
        }
        ApplyMinimapColor();
    }

    protected override void Update()
    {
        ResolveCD(Time.deltaTime);
        ResolveUnitState();
        ResolveEffectDuration(Time.deltaTime);
    }

    protected override void ResolveUnitState()
    {
        SearchForTarget();

        switch (unitState)
        {
            case UnitState.Neutral:
                Wander();
                break;
            case UnitState.Combat_Auto:
                if (Target == null) { SwitchToWander(); break; }
                HandleAutoCombat();
                break;
            case UnitState.Combat_Command:
                HandleCommandCombat();
                break;
        }
    }

    private void SearchForTarget()
    {
        if (Target != null)
        {
            float leash = ZombieData != null ? ZombieData.LeashRange : Data.DetectionRange;
            if (!InRange(Target, leash))
                SwitchToWander();
            return;
        }

        if (this.TeamTag == TeamTag.None) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, Data.DetectionRange);
        Unit nearest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            Unit candidate = hit.GetComponent<Unit>();
            if (candidate == null) continue;
            if (candidate == this) continue;
            if (candidate.HP <= 0) continue;
            if (candidate.TeamTag == TeamTag.None) continue;
            if (candidate.TeamTag == this.TeamTag) continue;
            if ((candidate.Data.UnitTag & UnitTag.ResouceNode) != 0) continue;

            float dist = Vector3.Distance(transform.position, candidate.transform.position);
            if (dist < minDist) { minDist = dist; nearest = candidate; }
        }

        if (nearest != null)
        {
            //Debug.Log($"[Zombie:SearchForTarget] µo²{¼Ä¤H {nearest.gameObject.name}¡A¤Á´« Combat_Auto");
            Target = nearest.gameObject;
            unitState = UnitState.Combat_Auto;
            _wanderState = WanderState.Idle;
            _wanderIdleTimer = 0f;
        }
    }

    private void Wander()
    {
        switch (_wanderState)
        {
            case WanderState.Moving:
                if (_agent.hasPath && !_agent.pathPending && _agent.remainingDistance < 0.2f)
                {
                    _agent.ResetPath();
                    _wanderState = WanderState.Idle;
                    _wanderIdleTimer = ZombieData != null ? ZombieData.WanderIdleTime : 2f;
                }
                break;

            case WanderState.Idle:
                _wanderIdleTimer -= Time.deltaTime;
                if (_wanderIdleTimer > 0f) break;

                Vector3 point = GetRandomNavMeshPoint();
                if (point != Vector3.zero)
                {
                    _agent.speed = Data.MoveSpeed;
                    _agent.SetDestination(point);
                    _wanderState = WanderState.Moving;
                }
                else
                {
                    _wanderIdleTimer = ZombieData != null ? ZombieData.WanderIdleTime : 2f;
                }
                break;
        }
    }

    private Vector3 GetRandomNavMeshPoint()
    {
        float radius = ZombieData != null ? ZombieData.WanderRadius : 10f;
        for (int i = 0; i < 10; i++)
        {
            Vector2 rand2D = Random.insideUnitCircle * radius;
            Vector3 candidate = transform.position + new Vector3(rand2D.x, 0f, rand2D.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return hit.position;
        }
        return Vector3.zero;
    }

    private void SwitchToWander()
    {
        Target = null;
        unitState = UnitState.Neutral;
        _wanderState = WanderState.Idle;
        _wanderIdleTimer = 0f;
        if (_agent != null && _agent.isOnNavMesh)
            _agent.ResetPath();
    }

    protected override void Die()
    {
        if (ZombieData != null && ZombieData.CanInfectOnDeath)
            TryInfect();

        Manager.RegisterKill();
        base.Die();
    }

    private void TryInfect()
    {
        if (ZombieData.InfectedUnitPrefab == null) return;
        if (Random.value > ZombieData.InfectionChance) return;
        GameObject infected = Instantiate(ZombieData.InfectedUnitPrefab, transform.position, transform.rotation);
        Unit infectedUnit = infected.GetComponent<Unit>();
        if (infectedUnit != null)
            infectedUnit.Initialize(Caller);
    }
}