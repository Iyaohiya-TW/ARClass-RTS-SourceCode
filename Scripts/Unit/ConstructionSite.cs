using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ConstructionSite : Unit
{
    public GameObject TargetPrefab;
    public int RequiredStep;
    public int CurrentBuildStep;
    public Slider ProgressSlider;

    // 在 ConstructionSite.cs 中增加
    [Header("碰撞檢測")]
    public int overlapCount = 0; // 當前重疊的物體數量

    [SerializeField]
    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private bool _isPlaced = false;
    private Color _originalColor;
    private Color warningColor = new Color(1f, 0f, 0f, 0.5f);
    public bool CanPlace => overlapCount == 0;

    protected override void Awake()
    {
        Initialize(Caller);
        base.Awake();
    }

    public override void Initialize(PlayerController caller)
    {
        base.Initialize(caller);
        if (TargetPrefab != null)
        {
            Building TargetBuilding = TargetPrefab.GetComponent<Building>();
            Data.MaxHP = Mathf.RoundToInt(TargetBuilding.Data.MaxHP * TargetBuilding.Data.ConstructionSiteHPRatio);
            HP = Data.MaxHP;
            RequiredStep = TargetBuilding.Data.RequiredBuildStep;
        }

        // 紀錄 URP 材質球的原本顏色
        _renderer = GetComponent<Renderer>();

        // 初始化 PropertyBlock
        _propBlock = new MaterialPropertyBlock();

        if (_renderer != null)
        {
            // 讀取目前的顏色作為原色
            _originalColor = _renderer.sharedMaterial.GetColor("_BaseColor");
        }
    }

    public void SetPlaced()
    {
        _isPlaced = true;
    }


    public void Build(int Step, int Repair)
    {
        if (!_isPlaced) return;

        if (!CanPlace) return;

        CurrentBuildStep += Step;
        UpdateProgressSlider();
        HP += Mathf.RoundToInt((float)Repair * 0.5f);
        if (HP > Data.MaxHP) HP = Data.MaxHP;
        UpdateHpSlider();
        if (CurrentBuildStep >= RequiredStep)
        {
            Complete();
        }
    }

    private void Complete()
    {
        GameObject target = Instantiate(TargetPrefab, transform.position, transform.rotation);
        Building targetBuilding = target.GetComponent<Building>();
        targetBuilding.Initialize(Caller);
        targetBuilding.HP = Mathf.RoundToInt(targetBuilding.Data.MaxHP * ((float)HP / Data.MaxHP)); // 繼承施工地血量比例
        targetBuilding.UpdateHpSlider();

        Caller.AddUnitToList(target);

        // 將單位移出建築範圍
        EjectUnitsFromArea();

        RebakeNavMesh();
        Die();
    }

    public void Cancel()
    {
        Building targetBuilding = TargetPrefab.GetComponent<Building>();
        foreach (Resource cost in targetBuilding.Data.Cost)
        {
            cost.Amount = Mathf.RoundToInt(cost.Amount * ((float)HP / Data.MaxHP)); // 依施工地血量返還資源
            Caller.resourceSet.AddResource(cost);
        }
        Die();
    }

    private void UpdateProgressSlider()
    {
        if (ProgressSlider != null) ProgressSlider.maxValue = RequiredStep;
        if (ProgressSlider != null) ProgressSlider.value = CurrentBuildStep;
    }

    // 計算重疊建築物
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 7 || other.gameObject.layer == 8)
        {
            overlapCount++;
            UpdateMaterialColor();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 7 || other.gameObject.layer == 8)
        {
            overlapCount--;
            if (overlapCount < 0) overlapCount = 0;
            UpdateMaterialColor();
        }
    }

    private void UpdateMaterialColor()
    {
        if (_renderer == null || _propBlock == null) return;
        if (_isPlaced) return;

        // 先獲取目前的 PropertyBlock 狀態
        _renderer.GetPropertyBlock(_propBlock);

        // 根據是否能放置，設定「只有這個物件」的 _BaseColor
        if (!CanPlace)
        {
            _propBlock.SetColor("_BaseColor", warningColor);
        }
        else
        {
            _propBlock.SetColor("_BaseColor", _originalColor);
        }

        // 把修改後的 PropertyBlock 塞回 Renderer 覆蓋
        _renderer.SetPropertyBlock(_propBlock);
    }

    private void EjectUnitsFromArea()
    {
        float ejectRadius = 5f;
        Collider[] hits = Physics.OverlapSphere(transform.position, ejectRadius);

        foreach (Collider col in hits)
        {
            NavMeshAgent agent = col.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.isOnNavMesh) continue;

            // 確保有 Unit 元件再讀速度
            Unit unit = col.GetComponent<Unit>();
            if (unit == null) continue;

            Vector3 directionOut = (col.transform.position - transform.position);
            directionOut.y = 0;
            if (directionOut == Vector3.zero) directionOut = Vector3.forward;
            directionOut = directionOut.normalized;

            Vector3 safePos = transform.position + directionOut * (ejectRadius + 1f);

            if (NavMesh.SamplePosition(safePos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                Vector3 currentDest = agent.destination;
                agent.Warp(hit.position);
                agent.speed = unit.Data.MoveSpeed; // unit 已確認非 null
                agent.SetDestination(currentDest);
            }
        }
    }

    // 新增：給 PlayerController 主動呼叫，不依賴 Trigger
    public void RefreshOverlapCheck()
    {
        // 取得自身 Collider 的範圍
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Bounds b = col.bounds;
        // 縮小一點點，避免邊緣誤判
        Vector3 halfExtents = b.extents * 0.95f;

        Collider[] hits = Physics.OverlapBox(b.center, halfExtents, transform.rotation);
        int count = 0;
        foreach (var h in hits)
        {
            if (h.gameObject == gameObject) continue; // 跳過自己
            int layer = h.gameObject.layer;
            if (layer == 7 || layer == 8) count++;
        }
        overlapCount = count;
        UpdateMaterialColor();
    }

    private void RebakeNavMesh()
    {
        NavMeshSurface surface = FindFirstObjectByType<NavMeshSurface>();
        if (surface != null)
            surface.BuildNavMesh();
        else
            Debug.LogWarning("場景中找不到 NavMeshSurface！");
    }
}
