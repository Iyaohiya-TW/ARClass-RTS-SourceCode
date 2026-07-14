using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : Unit
{
    public new BuildingData Data => (BuildingData)base.Data;

    [Header("生產設定")]
    // Changed to local offset for better control in Inspector
    public Vector3 spawnOffset = new Vector3(0, 0, 2f);
    public Vector3 rallyPoint;
    public List<ProductionAbility> productionQueue;

    [Header("視覺輔助")]
    public GameObject rallyMarkerPrefab;
    private GameObject _activeRallyMarker;

    // Helper property to get the Spawn Point in World Space  
    public Vector3 WorldSpawnPoint => transform.TransformPoint(spawnOffset);

    protected override void Awake()
    {
        base.Awake();
        Initialize(Caller);
    }

    public override void Initialize(PlayerController caller)
    {
        base.Initialize(caller);
        // Set rally point to the world spawn point initially
        rallyPoint = WorldSpawnPoint;

        if (Data != null && Data.hasRallyPoint && rallyMarkerPrefab != null && _activeRallyMarker == null)
        {
            _activeRallyMarker = Instantiate(rallyMarkerPrefab);
            _activeRallyMarker.transform.position = new Vector3(rallyPoint.x, _activeRallyMarker.transform.position.y, rallyPoint.z);
            _activeRallyMarker.SetActive(false);
        }

        //啟動被動技能
        if (Data != null && Data.DefaultAbilities != null && caller != null)
        {
            foreach (Ability ability in Data.DefaultAbilities)
            {
                if (ability != null && ability.autoActivateOnSpawn)
                {
                    ability.Use(gameObject, caller);
                    Debug.Log($"[Building] 已自動啟動被動技能：{ability.name}");
                }
            }
        }
    }

    public void SetRallyPoint(Vector3 worldPosition)
    {
        if (Data != null && !Data.hasRallyPoint) return;

        rallyPoint = worldPosition;

        if (_activeRallyMarker != null)
        {
            _activeRallyMarker.transform.position = new Vector3(rallyPoint.x, _activeRallyMarker.transform.position.y, rallyPoint.z);
        }

        Debug.Log($"{name} 集結點設置至 {worldPosition}");
    }

    public void ToggleRallyPointMarker(bool show)
    {
        if (Data != null && !Data.hasRallyPoint) return;

        if (_activeRallyMarker != null)
        {
            _activeRallyMarker.SetActive(show);
        }
    }

    public void AddProductionQueue(ProductionAbility productionAbility)

    {
        Debug.Log($"Add Production Plan To Queue: {productionAbility.name}");

        productionQueue.Add(productionAbility);

        // If this is the only item, start the engine
        if (productionQueue.Count == 1)
            StartCoroutine(ProduceRoutine());

    }

    IEnumerator ProduceRoutine()
    {
        while (productionQueue.Count > 0)
        {
            ProductionAbility currentAbility = productionQueue[0];
            float trainTime = 2.0f;

            trainTime = productionQueue[0].GetProductionTime();

            yield return new WaitForSeconds(trainTime);

            // Routine 計時結束時若該生產已被取消，將不會執行
            if (productionQueue.Count > 0 && productionQueue[0] == currentAbility)
            {
                currentAbility.OnProductionEnd(gameObject, Caller);
                productionQueue.RemoveAt(0);
            }
        }
    }
    
    public void CancelProduction(int index) // 提前寫的，需要更多測試
    {
        if (index < 0 || index >= productionQueue.Count) return;

        Debug.Log($"Canceling production of: {productionQueue[index].name}");

        productionQueue[index].Cancel(gameObject, Caller);

        if (index == 0)
        {
            StopCoroutine(ProduceRoutine());

            productionQueue.RemoveAt(0);

            if (productionQueue.Count > 0)
            {
                StartCoroutine(ProduceRoutine());
            }
        }
        else
        {
            productionQueue.RemoveAt(index);
        }
    }

    protected override void Die() 
    {
        Destroy(_activeRallyMarker);
        // 提前寫的，需要更多測試
        StopAllCoroutines(); // Stop any active production timers

        // 關閉跳資源建築的被動能力
        ResourceProducer producer = GetComponent<ResourceProducer>();
        if (producer != null)
        {
            producer.StopAllCoroutines();
            producer.enabled = false;     
        }

        foreach (ProductionAbility ability in productionQueue) { ability.Cancel(gameObject, Caller); }

        productionQueue.Clear();
        base.Die();
    }

    // --- GIZMOS (Editor Only) ---
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        // transform.TransformPoint converts local offset to world position automatically
        Vector3 worldPos = transform.TransformPoint(spawnOffset);

        Gizmos.DrawWireSphere(worldPos, 0.3f);
        Gizmos.DrawLine(transform.position, worldPos);
    }
}