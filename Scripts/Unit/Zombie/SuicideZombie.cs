using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 自爆殭屍：接近目標後進入引爆倒數，爆炸對範圍內建築造成傷害後死亡。
/// 所有數值（傷害、範圍、引爆延遲、音效…）皆於 SuicideZombieData 中調整。
/// </summary>
public class SuicideZombie : Zombie
{
    private SuicideZombieData SuicideData => Data as SuicideZombieData;

    private enum SuicidePhase { None, Fusing, Exploded }
    private SuicidePhase _phase = SuicidePhase.None;
    private float _fuseTimer = 0f;


    // ------------------------------------------------------------------ //
    //  Update loop                                                         //
    // ------------------------------------------------------------------ //

    protected override void Update()
    {
        // 引爆倒數期間只處理計時，不執行移動 / 戰鬥邏輯
        if (_phase == SuicidePhase.Fusing)
        {
            TickFuse();
            return;
        }

        base.Update();
    }

    // ------------------------------------------------------------------ //
    //  State resolution — override 插入自爆邏輯                            //
    // ------------------------------------------------------------------ //

    protected override void ResolveUnitState()
    {
        if (_phase == SuicidePhase.Exploded) return;

        base.ResolveUnitState();

        if (_phase == SuicidePhase.None
            && unitState == UnitState.Combat_Auto
            && Target != null)
        {
            // 動態計算：目標 Collider 半徑 + 自身 InteractRange（與 Worker 採集邏輯一致）
            float triggerRange = GetDynamicTriggerRange(Target);
            if (InRange(Target, triggerRange))
                StartFuse();
        }
    }

    private float GetDynamicTriggerRange(GameObject target)
    {
        float targetRadius = 0f;
        if (target.TryGetComponent<Collider>(out Collider col))
            targetRadius = col.bounds.extents.magnitude;

        float interactRange = Data != null ? Data.InteractRange : 1f;
        return targetRadius + interactRange;
    }

    // ------------------------------------------------------------------ //
    //  Fuse (引爆倒數)                                                     //
    // ------------------------------------------------------------------ //

    private void StartFuse()
    {
        _phase = SuicidePhase.Fusing;
        _fuseTimer = SuicideData != null ? SuicideData.FuseTime : 0.5f;

        // 停止移動
        var agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();

        // 引爆倒數音效
        if (SuicideData != null && SuicideData.FuseSound != null)
            AudioSource.PlayClipAtPoint(SuicideData.FuseSound, transform.position, SuicideData.SoundVolume);
    }

    private void TickFuse()
    {
        _fuseTimer -= Time.deltaTime;
        if (_fuseTimer <= 0f)
            Explode();
    }

    // ------------------------------------------------------------------ //
    //  Explosion                                                           //
    // ------------------------------------------------------------------ //

    private void Explode()
    {
        _phase = SuicidePhase.Exploded;

        float radius = SuicideData != null ? SuicideData.ExplosionRadius : 5f;
        float damage = SuicideData != null ? SuicideData.ExplosionDamage : 100f;
        bool bldOnly = SuicideData == null || SuicideData.DamageOnlyBuildings;

        if (SuicideData != null)
        {
            // 播放爆炸音效
            if (SuicideData.ExplosionSound != null)
            {
                AudioSource.PlayClipAtPoint(SuicideData.ExplosionSound, transform.position, SuicideData.SoundVolume);
            }

            // 生成爆炸特效 Prefab
            if (SuicideData.ExplosionVFXPrefab != null)
            {
                GameObject vfx = Instantiate(SuicideData.ExplosionVFXPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 3f);
            }
        }

        // =========================================================
        // 使用 LayerMask 排除 Layer 2（Ignore Raycast），
        // 避免打到 PlacingTarget（預覽建築）
        // =========================================================
        LayerMask explosionMask = SuicideData != null
            ? SuicideData.ExplosionLayerMask
            : Physics.DefaultRaycastLayers; // DefaultRaycastLayers 本身就排除了 Layer 2

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, explosionMask);
        foreach (var col in hits)
        {
            Unit unit = col.GetComponent<Unit>();
            if (unit == null || unit == this) continue;
            if (unit.HP <= 0) continue;

            if (col.gameObject.layer == 2) continue;

            if (bldOnly && (unit.Data.UnitTag & UnitTag.Building) == 0) continue;

            if (SuicideData != null
                && SuicideData.DamageTargetTag != 0
                && (unit.Data.UnitTag & SuicideData.DamageTargetTag) == 0)
                continue;

            UnitEffect explosionEffect = new UnitEffect
            {
                EffectName = $"{gameObject.name}'s Explosion",
                HealthChange = -(int)damage,
                Instigator = gameObject,
            };
            unit.RecieveEffect(explosionEffect);
        }

        Die();
    }

    // ------------------------------------------------------------------ //
    //  Die override — 確保不重複觸發感染邏輯                                //
    // ------------------------------------------------------------------ //

    protected override void Die()
    {
        // 未爆炸就直接被殺死時，不自爆，直接走父類流程
        base.Die();
    }
}