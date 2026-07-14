// WashMachine.cs (Modified)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WashMachine : Unit
{
    private float _currentCooldown = 0f;
    private bool _isPulling = false;
    private List<Unit> _pulledEnemies = new List<Unit>();

    // --- 新增：儲存實例化出來的範圍特效參考 ---
    private GameObject _spawnedRangeEffect;

    private WashMachineData WMData => Data as WashMachineData;

    protected override void Update()
    {
        base.Update();

        if (WMData == null) return;

        if (_currentCooldown > 0)
        {
            _currentCooldown -= Time.deltaTime;
        }

        if (_currentCooldown <= 0 && (unitState == UnitState.Combat_Auto || unitState == UnitState.Combat_Command))
        {
            CastPullAndTaunt();
        }

        if (_isPulling)
        {
            ProcessPulling();
        }

        // --- 新增：在 Update 中每幀處理範圍顯示 ---
        UpdateRangeVisualization();
    }

    /// <summary>
    /// 管理技能特效的顯示與隱藏
    /// </summary>
    private void UpdateRangeVisualization()
    {
        if (WMData.RangeVisualizationPrefab == null) return;

        if (_isPulling)
        {
            if (_spawnedRangeEffect == null)
            {
                _spawnedRangeEffect = Instantiate(
                    WMData.RangeVisualizationPrefab,
                    transform.position,
                    WMData.RangeVisualizationPrefab.transform.rotation,
                    transform
                );

                float visualScale = Data.DetectionRange * 2f;

                _spawnedRangeEffect.transform.localScale = new Vector3(visualScale, visualScale, 1f);

                Vector3 pos = _spawnedRangeEffect.transform.localPosition;
                pos.y = 0.01f;
                _spawnedRangeEffect.transform.localPosition = pos;
            }
        }
        else
        {
            // 如果不在拉扯中但存在特效，則銷毀
            if (_spawnedRangeEffect != null)
            {
                Destroy(_spawnedRangeEffect);
                _spawnedRangeEffect = null;
            }
        }
    }

    public void CastPullAndTaunt()
    {
        _pulledEnemies.Clear();
        _currentCooldown = WMData.SkillCooldown;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, Data.DetectionRange);

        foreach (var hitCollider in hitColliders)
        {
            Unit enemy = hitCollider.GetComponent<Unit>();
            if (enemy != null && enemy != this && enemy.HP > 0 && enemy.TeamTag != this.TeamTag && enemy.Data.UnitTag != UnitTag.ResouceNode)
            {
                enemy.CommandAttack(this.gameObject);
                _pulledEnemies.Add(enemy);
            }
        }

        if (_pulledEnemies.Count > 0)
        {
            _isPulling = true;
            // 注意：這裡 Invoke 結束拉扯，Update 下一幀會自動銷毀特效
            Invoke(nameof(StopPulling), WMData.PullDuration);
        }
    }

    private void ProcessPulling()
    {
        for (int i = _pulledEnemies.Count - 1; i >= 0; i--)
        {
            Unit enemy = _pulledEnemies[i];
            if (enemy == null || enemy.HP <= 0) { _pulledEnemies.RemoveAt(i); continue; }

            Vector3 direction = (transform.position - enemy.transform.position).normalized;
            direction.y = 0;
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh) { agent.Move(direction * WMData.PullSpeed * Time.deltaTime); }
            else { enemy.transform.position += direction * WMData.PullSpeed * Time.deltaTime; }
        }
    }

    private void StopPulling()
    {
        _isPulling = false;
        _pulledEnemies.Clear();
        // _isPulling 設為 false，Update 會自動處理特效銷毀
    }
}