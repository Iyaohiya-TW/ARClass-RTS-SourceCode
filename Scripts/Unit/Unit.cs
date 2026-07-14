using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public enum UnitState
{
    Neutral,
    Combat_Auto,
    Combat_Command,
}

public class Unit : MonoBehaviour
{
    public UnitData Data;
    public int HP;
    public float AtkCD;
    public List<UnitEffect> TempUnitEffects;
    public List<Ability> CurrentAbilities;
    public GameObject Target;

    public PlayerController Caller;
    public TeamTag TeamTag;

    [Header("UI Component")]
    public Slider hpSlider;
    public DecalProjector selectionIndicator;
    public List<Material> IndicatorTeamColor;
    public Material IndicatorSelectedColor;
    public SpriteRenderer MinimapAvatar;

    [Header("Animation Setup")]
    protected Animator animator;
    [Tooltip("The exact string parameter name of the attack trigger in the Animator Controller")]
    public string attackTriggerName = "Attack";

    public UnitState unitState = UnitState.Neutral;

    private bool _isMoving = false;

    // true = 玩家下令移動中，優先於一切主動行為（巡敵、攻擊）
    // 只有被攻擊（TryCounterAttack）才能打斷，抵達目的地後自動清除
    protected bool _isCommandMoving = false;

    protected Unit()
    {
        if (Data) HP = Data.MaxHP;
        UpdateHpSlider();
    }

    protected virtual void Awake()
    {
        CurrentAbilities = new List<Ability>(Data.DefaultAbilities);
        if (Data) HP = Data.MaxHP;
        UpdateHpSlider();
        ApplyTeamColor();

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            // Optional: Check if animator is located in a child object (common for 3D model rigs)
            animator = GetComponentInChildren<Animator>();
        }
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

    protected virtual void Update()
    {
        ResolveCD(Time.deltaTime);
        ResolveUnitState();
        ResolveEffectDuration(Time.deltaTime);

        if (_isMoving)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                if (!agent.pathPending && agent.remainingDistance < 0.1f)
                {
                    OnDestinationReached();
                    StopMoving();
                }
            }
        }
    }

    protected virtual void ResolveUnitState()
    {
        // 玩家指令移動中：不主動巡敵、不攻擊
        // 只有 TryCounterAttack（被打）才能打斷這個狀態
        if (_isCommandMoving) return;

        switch (unitState)
        {
            case UnitState.Neutral:
                // Neutral 狀態下也持續掃描，有敵人就自動進入 Combat_Auto
                SearchForEnemy();
                break;
            case UnitState.Combat_Auto:
                SearchForEnemy();
                HandleAutoCombat();
                break;
            case UnitState.Combat_Command:
                HandleCommandCombat();
                break;
        }
    }

    // =========================================================
    // Fix: 改用倒序 for 迴圈安全移除，避免 foreach 中 Remove 拋出例外
    // =========================================================
    public void ResolveEffectDuration(float deltaTime)
    {
        for (int i = TempUnitEffects.Count - 1; i >= 0; i--)
        {
            UnitEffect UE = TempUnitEffects[i];

            if (UE.AtkBonus.Temporary) UE.AtkBonus.Duration -= deltaTime;
            if (UE.DefBonus.Temporary) UE.DefBonus.Duration -= deltaTime;
            if (UE.MoveBonus.Temporary) UE.MoveBonus.Duration -= deltaTime;

            bool atkExpired = !UE.AtkBonus.Temporary || UE.AtkBonus.Duration <= 0;
            bool defExpired = !UE.DefBonus.Temporary || UE.DefBonus.Duration <= 0;
            bool moveExpired = !UE.MoveBonus.Temporary || UE.MoveBonus.Duration <= 0;

            if (atkExpired && defExpired && moveExpired)
            {
                TempUnitEffects.RemoveAt(i);
            }
        }
    }

    public virtual void Initialize(PlayerController caller)
    {
        if (caller)
        {
            Caller = caller;
            TeamTag = caller.TeamTag;
        }
        ApplyTeamColor();
    }

    protected virtual void ResolveCD(float deltaTime)
    {
        if (AtkCD > 0)
            AtkCD -= deltaTime;
        else
            AtkCD = 0;
    }

    public virtual void UpdateHpSlider()
    {
        if (Data && hpSlider != null) hpSlider.maxValue = Data.MaxHP;
        if (hpSlider != null) hpSlider.value = HP;
    }

    public void ApplyTeamColor()
    {
        if (selectionIndicator == null) return;

        if (this.TeamTag == TeamTag.None)
        {
            selectionIndicator.gameObject.SetActive(false);
            return;
        }

        int teamValue = (int)this.TeamTag;
        int index = 0;
        while (teamValue > 0 && (teamValue & 1) == 0)
        {
            teamValue >>= 1;
            index++;
        }

        if (index >= 0 && index < IndicatorTeamColor.Count)
        {
            if (IndicatorTeamColor[index] != null)
            {
                selectionIndicator.gameObject.SetActive(true);
                selectionIndicator.material = IndicatorTeamColor[index];
            }
            else
            {
                Debug.LogWarning($"Team Index {index} 有對應標籤但材質陣列中該位置是空的！");
            }
        }
        else
        {
            selectionIndicator.gameObject.SetActive(false);
        }
    }

    public void ApplySelectedColor()
    {
        if (selectionIndicator == null) return;
        selectionIndicator.gameObject.SetActive(true);
        selectionIndicator.material = IndicatorSelectedColor;
    }

    protected virtual void OnDestinationReached() { }

    public virtual void Move(Vector3 destination)
    {
        float finalSpeed = Data.MoveSpeed;
        foreach (UnitEffect UE in TempUnitEffects)
        {
            if (UE.MoveBonus.Duration > 0)
                finalSpeed += UE.MoveBonus.Value;
        }

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = finalSpeed;
            agent.SetDestination(destination);
            _isMoving = true;
            _isCommandMoving = true; // 標記為玩家指令移動
        }
    }

    protected void MoveWithoutCallback(Vector3 destination)
    {
        float finalSpeed = Data.MoveSpeed;
        foreach (UnitEffect UE in TempUnitEffects)
        {
            if (UE.MoveBonus.Duration > 0)
                finalSpeed += UE.MoveBonus.Value;
        }

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = finalSpeed;
            agent.SetDestination(destination);
        }
    }

    public void StopMoving()
    {
        _isMoving = false;
        _isCommandMoving = false; // 抵達目的地，解除指令移動狀態

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();
    }

    public bool IsMoving => _isMoving;

    public void HandleAutoCombat()
    {
        if (Target == null) return;

        if (IsWithinAttackRange(Target))
        {
            StopMoving();
            Attack();
        }
        else
        {
            if ((Data.UnitTag & UnitTag.Building) != 0) return;
            MoveTowardsTarget();
        }
    }

    public void HandleCommandCombat()
    {
        if (Target == null)
        {
            unitState = UnitState.Combat_Auto;
            return;
        }

        if (IsWithinAttackRange(Target))
        {
            StopMoving();
            Attack();
        }
        else
        {
            if ((Data.UnitTag & UnitTag.Building) != 0) return;
            MoveTowardsTarget();
        }
    }

    protected void MoveTowardsTarget()
    {
        if (Target == null) return;

        Collider targetCollider = Target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
            Vector3 direction = (transform.position - closestPoint).normalized;
            Vector3 destination = closestPoint + (direction * 0.05f);
            MoveWithoutCallback(destination); // 追擊不觸發 OnDestinationReached
        }
        else
        {
            MoveWithoutCallback(Target.transform.position);
        }
    }

    public virtual void Attack()
    {
        if (AtkCD > 0) return;
        if (Data.AutoAttackEffect == null)
        {
            Debug.LogError($"{gameObject.name}: Data.AutoAttackEffect is missing!", this);
            return;
        }

        bool isAOE = Data.AutoAttackEffect.CastMethod == UnitEffect.EffectCastMethod.MultiCastAOE;

        if (!isAOE && Target == null) return;

        bool attackSuccessful = false;

        if (isAOE)
        {
            float radius = Data.AutoAttackEffect.AOERadius > 0 ? Data.AutoAttackEffect.AOERadius : 5f;
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

            foreach (Collider col in colliders)
            {
                Unit enemyUnit = col.GetComponent<Unit>();
                if (enemyUnit != null && enemyUnit != this
                    && enemyUnit.TeamTag != this.TeamTag
                    && enemyUnit.Data.UnitTag != UnitTag.ResouceNode)
                {
                    int calculatedDamage = CalculateDamageForTarget(enemyUnit);
                    ExecuteAttackDelivery(enemyUnit, calculatedDamage);
                    attackSuccessful = true;
                }
            }
        }
        else
        {
            Unit targetUnit = Target.GetComponent<Unit>();
            if (targetUnit != null && targetUnit.TeamTag != this.TeamTag)
            {
                int calculatedDamage = CalculateDamageForTarget(targetUnit);
                ExecuteAttackDelivery(targetUnit, calculatedDamage);
                attackSuccessful = true;
            }
        }

        if (attackSuccessful)
        {
            AtkCD = 1f / Data.AtkSpeed;

            if (animator != null)
            {
                animator.SetTrigger(attackTriggerName);
            }
        }
    }

    private int CalculateDamageForTarget(Unit targetUnit)
    {
        int calculatedDamage = -(int)Data.GetDamageAgainst(targetUnit.Data.UnitTag);
        UnitTag targetTag = targetUnit.Data.UnitTag;

        foreach (UnitEffect UE in TempUnitEffects)
        {
            if (UE.AtkBonus.Duration > 0)
            {
                bool tagMatches = (UE.AtkBonus.TargetTag & targetTag) != UnitTag.None;
                if (UE.AtkBonus.TargetTag == UnitTag.None || tagMatches)
                {
                    calculatedDamage -= (int)UE.AtkBonus.Value;
                }
            }
        }

        if (calculatedDamage >= 0) calculatedDamage = -1;
        return calculatedDamage;
    }

    private void ExecuteAttackDelivery(Unit targetUnit, int finalDamage)
    {
        if (Data.AutoAttackEffect.ProjectilePrefab != null)
        {
            Vector3 spawnPoint = transform.position + new Vector3(0, 1, 0);
            GameObject projGO = Instantiate(Data.AutoAttackEffect.ProjectilePrefab, spawnPoint, Quaternion.identity);
            Projectile projScript = projGO.GetComponent<Projectile>();

            if (projScript != null)
            {
                UnitEffect instanceEffect = Data.AutoAttackEffect.Clone();
                instanceEffect.HealthChange = finalDamage;
                instanceEffect.Instigator = gameObject;

                projScript.Launch(
                    targetUnit.gameObject,
                    instanceEffect,
                    Data.AutoAttackEffect.ProjectileSpeed,
                    TeamTag,
                    Data.AutoAttackEffect.Homing
                );
            }
            else
            {
                Destroy(projGO);
            }
        }
        else
        {
            UnitEffect instantEffect = Data.AutoAttackEffect.Clone();
            instantEffect.HealthChange = finalDamage;
            instantEffect.Instigator = gameObject;
            targetUnit.RecieveEffect(instantEffect);
        }
    }

    public void RecieveEffect(UnitEffect effect)
    {
        Debug.Log($"接收效果: {effect.EffectName}");

        if (effect.HealthChange < 0)
        {
            // Fix: 用 localDef 累加，不污染 ScriptableObject 的 Data.Def
            int localDef = Data.Def;
            foreach (UnitEffect UE in TempUnitEffects)
            {
                if (UE.DefBonus.Duration > 0)
                    localDef += (int)UE.DefBonus.Value;
            }

            float finalDamage = Mathf.Min(-1, effect.HealthChange + localDef);
            HP += (int)finalDamage;

            TryCounterAttack(effect.Instigator);
        }
        else
        {
            HP += effect.HealthChange;
        }

        HP = Mathf.Clamp(HP, 0, Data.MaxHP);
        UpdateHpSlider();

        if (HP <= 0)
        {
            Die();
            return;
        }

        bool overrideEffect = false;
        foreach (UnitEffect UE in TempUnitEffects)
        {
            if (UE.EffectName == effect.EffectName)
            {
                UE.AtkBonus = effect.AtkBonus;
                UE.DefBonus = effect.DefBonus;
                UE.MoveBonus = effect.MoveBonus;
                overrideEffect = true;
                break;
            }
        }
        if (!overrideEffect) TempUnitEffects.Add(effect);
    }

    // 受傷反擊邏輯：
    // - CommandMoving  → 打斷移動，停下來反擊
    // - Neutral        → 立刻以攻擊者為目標進入 Combat_Auto
    // - Combat_Command → 保持原指令目標，不打斷
    // - Combat_Auto    → 已在戰鬥中，不干擾現有目標
    protected virtual void TryCounterAttack(GameObject instigator)
    {
        if (instigator == null) return;

        // 玩家下了指令攻擊，不被受傷打斷
        if (unitState == UnitState.Combat_Command) return;

        // 被攻擊打斷指令移動，停下來反擊
        if (_isCommandMoving)
        {
            _isCommandMoving = false;
            StopMoving();
        }

        // 已在自動戰鬥中，不干擾現有目標
        if (unitState == UnitState.Combat_Auto) return;

        Target = instigator;
        unitState = UnitState.Combat_Auto;
    }

    protected virtual void Die()
    {
        Debug.Log($"{Data.UnitName} has been destroyed.");
        Destroy(gameObject);
    }

    public void UseAbility(int index, PlayerController caller)
    {
        Caller = caller;
        CurrentAbilities[index].Use(gameObject, Caller);
    }

    public GameObject FindNearestByTag(UnitTag targetType)
    {
        Unit[] allUnit = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        GameObject nearest = null;
        float minDistance = float.MaxValue;
        Vector3 currentPos = transform.position;

        foreach (Unit unit in allUnit)
        {
            if (unit.gameObject == this.gameObject) continue;
            if ((unit.Data.UnitTag & targetType) != 0)
            {
                float dist = Vector3.Distance(currentPos, unit.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = unit.gameObject;
                }
            }
        }
        return nearest;
    }

    public bool InRange(GameObject target, float range)
    {
        if (target == null) return false;
        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0;
        return direction.sqrMagnitude <= (range * range);
    }

    public void CommandAttack(GameObject combatTarget)
    {
        Target = combatTarget;
        unitState = UnitState.Combat_Command;
    }

    // =========================================================
    // Fix: protected virtual — 讓子類別可以 override 掃描行為
    //   - 找到最近敵人 → 更新 Target，切 Combat_Auto
    //   - 找不到敵人且 Target 已消失或失效 → 回 Neutral
    //   - 找不到敵人但 Target 還在 → 保持追擊，不清 Target
    // =========================================================
    protected virtual void SearchForEnemy()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, Data.DetectionRange);

        Unit nearestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            Unit unit = hitCollider.GetComponent<Unit>();
            if (unit != null && unit != this
                && unit.HP > 0
                && unit.TeamTag != this.TeamTag
                && unit.Data.UnitTag != UnitTag.ResouceNode)
            {
                float dist = Vector3.Distance(transform.position, unit.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestEnemy = unit;
                }
            }
        }

        if (nearestEnemy != null)
        {
            // 偵測範圍內有敵人，更新為最近目標並進入戰鬥
            Target = nearestEnemy.gameObject;
            unitState = UnitState.Combat_Auto;
        }
        else if (Target == null || !Target.activeInHierarchy)
        {
            // 偵測範圍內沒有敵人，且原本的 Target 也不存在了，才回 Neutral
            Target = null;
            unitState = UnitState.Neutral;
        }
        // 否則：偵測圈內暫時沒人，但 Target 還存在 → 繼續追擊，不打斷
    }

    public bool IsWithinInteractRange(GameObject target)
    {
        if (target == null) return false;

        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
            Vector3 workerPos = new Vector3(transform.position.x, 0, transform.position.z);
            closestPoint.y = 0;
            return Vector3.Distance(workerPos, closestPoint) <= (Data.InteractRange + 0.1f);
        }

        return InRange(target, Data.InteractRange + 0.1f);
    }

    // 動態計算攻擊距離：量到目標 Collider 表面最近點的距離，而非中心點
    // 與 IsWithinInteractRange 邏輯一致，確保大型目標不需要走到中心才能攻擊
    public bool IsWithinAttackRange(GameObject target)
    {
        if (target == null) return false;

        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
            Vector3 selfPos = new Vector3(transform.position.x, 0, transform.position.z);
            closestPoint.y = 0;
            return Vector3.Distance(selfPos, closestPoint) <= Data.AtkRange;
        }

        return InRange(target, Data.AtkRange);
    }

    public void ApplyMinimapColor()
    {
        if (MinimapAvatar == null) return;

        // Map your 4 TeamTags to 4 distinct colors
        switch (TeamTag)
        {
            case TeamTag.P1:
                MinimapAvatar.color = new Color(0f, 0.5f, 1f, 0.7f);   // Player 1 (Blue)
                break;
            case TeamTag.P2:
                MinimapAvatar.color = new Color(1f, 0f, 0f, 0.7f);    // Player 2 / Zombies (Red)
                break;
            case TeamTag.P3:
                MinimapAvatar.color = new Color(0f, 1f, 0f, 0.7f);  // Player 3 (Green)
                break;
            case TeamTag.P4:
                MinimapAvatar.color = new Color(1f, 1f, 0.7f); // Player 4 (Yellow)
                break;
            default:
                MinimapAvatar.color = Color.white;  // Neutral / Fallback (White)
                break;
        }
    }
}