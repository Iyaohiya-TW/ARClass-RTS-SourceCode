using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public enum ControllerState { Neutral, ViewUnit, HoldUnit, PlaceBuilding }

public class PlayerController : MonoBehaviour
{
    // ==========================================
    // 變數宣告 (Variables)
    // ==========================================

    [Header("模組引用")]
    public Camera cam;
    public RTSUIManager UIManager;
    public TechTree techTree;
    public KeyCode techTreeHotkey = KeyCode.CapsLock;

    [Header("玩家狀態")]
    public TeamTag TeamTag;
    public ResourceSet resourceSet = new ResourceSet();
    public List<GameObject> AllUnitList = new List<GameObject>();

    [Header("控制器當前狀態")]
    public ControllerState currentState = ControllerState.Neutral;

    [Header("選取系統")]
    public LayerMask unitLayer;
    public List<GameObject> selectedUnits = new List<GameObject>();
    private Vector3 dragStartPosition;
    private bool isDragging = false;

    [Header("攝影機控制")]
    public LayerMask groundLayer;
    public float moveSpeed = 20f;
    public float edgeSize = 20f;
    public float topDownHeight = 20f;
    public float topDownAngle = 90f;
    // 縮放
    public float zoomSpeed = 10f;       // 縮放速度
    public float minZoom = 3f;        // 最小縮放值 (最遠)
    public float maxZoom = 15f;       // 最大縮放值 (最遠)
    public float smoothness = 10f;    // 縮放平滑度
    private float targetZoom; //目標大小

    [Header("建築系統")]
    public KeyCode buildModeHotkey = KeyCode.Tab;
    public KeyCode snapHotkey = KeyCode.LeftShift;
    public GameObject buildManager;
    private bool BuildModeToggle = false;

    // --- 新增：用來記錄建築預製體的原始 Layer ---
    private int originalPlacingLayer = 0;

    private Dictionary<KeyCode, int> AbilityHotKeyMap = new Dictionary<KeyCode, int>
    {
        { KeyCode.Q, 0 }, { KeyCode.W, 1 }, { KeyCode.E, 2 }, { KeyCode.R, 3 }, { KeyCode.T, 4 },
        { KeyCode.A, 5 }, { KeyCode.S, 6 }, { KeyCode.D, 7 }, { KeyCode.F, 8 }, { KeyCode.G, 9 },
        { KeyCode.Z, 10 }, { KeyCode.X, 11 }, { KeyCode.C, 12 }, { KeyCode.V, 13 }, { KeyCode.B, 14 }
    };
    private GameObject PlacingTarget = null;

    // ==========================================
    // 生命週期 (Life Cycle)
    // ==========================================

    void Start()
    {
        InitializeTopDownView();
        techTree.ToggleUI();
    }

    void Update()
    {
        // 在處理輸入前，先清除已銷毀的單位引用
        if (CleanSelectedUnits())
        {
            UIManager.UpdateInspectorPanel(); // 如果有單位死亡或消失，更新 UI 顯示
        }
        // 同理，清除全單位列表中的以銷毀單位
        if (CleanAllUnitList())
        {
            techTree.UpdateReqirement(); // 更新科技需求節點 (人員、建築持有量需求)
        }

        if (Input.GetKeyDown(techTreeHotkey))
        {
            techTree.ToggleUI();
        }

        MoveCamera();
        ZoomCamera();
        ResolveState();
    }

    public void AddUnitToList(GameObject unit)
    {
        AllUnitList.Add(unit);
        techTree.UpdateReqirement();
    }

    /// <summary>
    /// 清除選取清單中為空(已銷毀)的物件
    /// </summary>
    bool CleanSelectedUnits()
    {
        if (selectedUnits.Count == 0) return false;

        int initialCount = selectedUnits.Count;
        // 使用 RemoveAll 配合 Unity 的 null 檢查 (處理已 Destroy 的物件)
        selectedUnits.RemoveAll(unit => unit == null);

        bool changed = selectedUnits.Count != initialCount;

        // 如果清除後發現沒東西了，將狀態轉回 Neutral
        if (changed && selectedUnits.Count == 0 && currentState == ControllerState.HoldUnit)
        {
            ChangeToState(ControllerState.Neutral);
        }
        else if (changed && selectedUnits.Count == 0 && currentState == ControllerState.ViewUnit)
        {
            ChangeToState(ControllerState.Neutral);
        }

        return changed;
    }

    bool CleanAllUnitList()
    {
        if (AllUnitList.Count == 0) return false;

        int initialCount = AllUnitList.Count;
        // 使用 RemoveAll 配合 Unity 的 null 檢查 (處理已 Destroy 的物件)
        AllUnitList.RemoveAll(unit => unit == null);

        bool changed = AllUnitList.Count != initialCount;

        // 如果清除後發現沒東西了，將狀態轉回 Neutral
        if (changed && AllUnitList.Count == 0 && currentState == ControllerState.HoldUnit)
        {
            ChangeToState(ControllerState.Neutral);
        }

        return changed;
    }

    // ==========================================
    // 狀態機核心 (State Machine Core)
    // ==========================================

    void ResolveState()
    {
        switch (currentState)
        {
            case ControllerState.Neutral: HandleNeutral(); break;
            case ControllerState.HoldUnit: HandleHoldUnit(); break;
            case ControllerState.ViewUnit: HandleViewUnit(); break;
            case ControllerState.PlaceBuilding: HandlePlaceBuilding(); break;
        }
    }

    void ChangeToState(ControllerState newState)
    {
        Debug.Log($"狀態切換: {currentState} -> {newState}");
        UIManager.UpdateInspectorPanel();
        currentState = newState;
    }


    // --- 已選取單位狀態 ---
    void HandleHoldUnit()
    {
        // 避開 UI 點擊
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // --- 滑鼠右鍵：執行指令 ---
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            // 檢查是否點擊到具有 Unit 組件的目標 (資源、敵人等)
            if (Physics.Raycast(ray, out RaycastHit rayHit, 1000f, unitLayer))
            {
                if (rayHit.collider.TryGetComponent<Unit>(out Unit targetUnit))
                {
                    // 如果點擊的是資源點，命令選中的 Worker 去採集
                    if ((targetUnit.Data.UnitTag & UnitTag.ResouceNode) != 0)
                    {
                        foreach (var obj in selectedUnits)
                        {
                            if (obj.TryGetComponent<Worker>(out Worker worker))
                            {
                                worker.Interact(rayHit.collider.gameObject);
                            }
                        }
                    }
                    else if ((targetUnit.Data.UnitTag & UnitTag.ConstructionSite) != 0)
                    {
                        foreach (var obj in selectedUnits)
                        {
                            if (obj.TryGetComponent<Worker>(out Worker worker))
                            {
                                worker.Interact(rayHit.collider.gameObject);
                            }
                        }
                    }
                    else // 否則視為攻擊/交互指令
                    {
                        foreach (var obj in selectedUnits)
                        {
                            Debug.Log("發送攻擊指令");
                            if (obj.TryGetComponent<Unit>(out Unit unit))
                            {
                                if (unit.Data.CanAtk) unit.CommandAttack(targetUnit.gameObject);
                            }
                        }
                    }
                }
                // 點擊地面：移動指令
                else if (GetGroundPosition(out Vector3 targetPos))
                {
                    if (selectedUnits.Count > 0 && selectedUnits[0] != null)
                    {
                        Unit firstUnit = selectedUnits[0].GetComponent<Unit>();
                        UnitTag type = firstUnit.Data.UnitTag;

                        // 1. 如果選中的是建築物 (設置集結點)
                        if ((type & UnitTag.Building) != 0)
                        {
                            foreach (var obj in selectedUnits)
                            {
                                obj.GetComponent<Building>()?.SetRallyPoint(targetPos);
                            }
                        }
                        // 2. 如果選中的是普通單位 (執行移動)
                        else if ((type & UnitTag.Unit) != 0)
                        {
                            CommandUnitsMove(targetPos);
                        }
                    }
                }
            }
        }

        // --- 滑鼠左鍵：重新選取 (包含框選) ---
        CheckSelectionInput();

        // --- 其他功能 ---
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClearSelection();
            ChangeToState(ControllerState.Neutral);
        }

        CheckbuildModeHotkey();

        CheckAbilitiesHotkey();
    }

    void HandleViewUnit()
    {
        // 檢視模式下依然允許玩家進行新的選取動作
        CheckSelectionInput();

        // 按下 ESC 回到中性狀態
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClearSelection();
            ChangeToState(ControllerState.Neutral);
        }
    }

    // ==========================================
    // 各狀態輸入處理 (State Handlers)
    // ==========================================

    // --- 中性狀態 (未選取單位) ---
    void HandleNeutral()
    {
        CheckbuildModeHotkey();
        CheckSelectionInput();
    }

    // --- 建築物擺放狀態 ---
    void HandlePlaceBuilding()
    {
        ClearSelection();
        UpdateBuildIndicator();

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Destroy(PlacingTarget);
            ChangeToState(ControllerState.Neutral);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (PlacingTarget.TryGetComponent<ConstructionSite>(out var site))
            {
                // --- 檢查是否可以建造 ---
                if (!site.CanPlace)
                {
                    Debug.Log("位置重疊，無法建造！");
                    return;
                }

                if (site.TargetPrefab && site.TargetPrefab.TryGetComponent<Building>(out var building))
                {
                    foreach (Resource res in building.Data.Cost)
                    {
                        if (resourceSet.GetResourceByType(res.Type) < res.Amount)
                        {
                            Debug.Log($"Ability: {res.Type} 資源不足無法建造");
                            return;
                        }
                        else
                        {
                            resourceSet.CostResource(res);
                        }
                    }
                }

                SetLayerRecursive(PlacingTarget, originalPlacingLayer);
                site.SetPlaced();
            }
            PlacingTarget.GetComponent<ConstructionSite>().Initialize(this);
            AddUnitToList(PlacingTarget);
            PlacingTarget = null;
            ChangeToState(ControllerState.Neutral);
        }
    }

    // ==========================================
    // 選取邏輯 (Selection Logic)
    // ==========================================

    void SelectUnitsInBox(Vector3 start, Vector3 end)
    {
        Vector3 center = (start + end) / 2f;
        float sizeX = Mathf.Abs(start.x - end.x) / 2f;
        float sizeZ = Mathf.Abs(start.z - end.z) / 2f;
        Vector3 halfExtents = new Vector3(sizeX, 10f, sizeZ);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, unitLayer);

        List<GameObject> foundUnits = new List<GameObject>();
        List<GameObject> foundBuildings = new List<GameObject>();
        List<GameObject> foundResources = new List<GameObject>();
        List<GameObject> foundConstructionSite = new List<GameObject>();

        foreach (Collider col in hits)
        {
            if (col.TryGetComponent<Unit>(out Unit unit) && unit.Data != null)
            {
                if (unit.TeamTag == this.TeamTag)
                {
                    UnitTag tags = unit.Data.UnitTag;
                    if ((tags & UnitTag.Unit) != 0) foundUnits.Add(col.gameObject);
                    else if ((tags & UnitTag.Building) != 0) foundBuildings.Add(col.gameObject);
                    else if ((tags & UnitTag.ResouceNode) != 0) foundResources.Add(col.gameObject);
                    else if ((tags & UnitTag.ConstructionSite) != 0) foundConstructionSite.Add(col.gameObject);
                }
            }
        }

        ClearSelection();

        // 優先級判斷：戰鬥單位 > 建築物 > 資源點
        if (foundUnits.Count > 0)
        {
            foreach (GameObject go in foundUnits)
            {
                Select(go);
            }
        }
        else if (foundBuildings.Count > 0)
        {
            foreach (GameObject go in foundBuildings)
            {
                Select(go);
            }
        }
        else if (foundResources.Count > 0)
        {
            foreach (GameObject go in foundResources)
            {
                Select(go);
            }
        }
        else if (foundConstructionSite.Count > 0)
        {
            foreach (GameObject go in foundConstructionSite)
            {
                Select(go);
            }
        }

        CalculateSelectionCorners(start, end);
        UIManager.UpdateInspectorPanel();
    }

    // ==========================================
    // 輸入信號處理
    // ==========================================
    void CheckSelectionInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (GetGroundPosition(out Vector3 hitPoint))
            {
                dragStartPosition = hitPoint;
                isDragging = true;
            }
        }

        if (isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            GetGroundPosition(out Vector3 dragEndPosition);

            // 判斷位移距離
            float dragDistance = Vector3.Distance(dragStartPosition, dragEndPosition);

            if (dragDistance < 0.1f) // 距離極小，判定為「點擊單選」
            {
                TrySelectSingleSelectable();
                Debug.Log("單選");
            }
            else // 距離夠大，判定為「框選」
            {
                SelectUnitsInBox(dragStartPosition, dragEndPosition);
                Debug.Log("框選");
            }

            // 最後統一根據結果切換狀態
            if (selectedUnits.Count > 0)
            {
                // 檢查 友方、敵方單位
                bool hasMyUnit = selectedUnits.Any(go => {
                    Unit u = go.GetComponent<Unit>();
                    return u != null && u.TeamTag == this.TeamTag;
                });

                if (hasMyUnit)
                {
                    ChangeToState(ControllerState.HoldUnit);
                }
                else
                {
                    ChangeToState(ControllerState.ViewUnit);
                }
            }
            else
                ChangeToState(ControllerState.Neutral);
        }
    }

    void TrySelectSingleSelectable()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit rayHit, 1000f, unitLayer))
        {
            if (rayHit.collider.TryGetComponent<Unit>(out Unit unit))
            {
                if (IsSelectable(unit.Data.UnitTag))
                {
                    ClearSelection();
                    Select(rayHit.collider.gameObject);
                    return;
                }
            }
        }

        ClearSelection();
        UIManager.UpdateInspectorPanel();
    }

    void Select(GameObject gameObject)
    {
        if (gameObject.TryGetComponent<Building>(out Building building))
        {
            building.ToggleRallyPointMarker(true);
        }
        if (gameObject.TryGetComponent<Unit>(out Unit unit))
        {
            unit.ApplySelectedColor();
        }
        selectedUnits.Add(gameObject);
    }

    void CheckAbilitiesHotkey()
    {
        foreach (var entry in AbilityHotKeyMap)
        {
            if (Input.GetKeyDown(entry.Key))
            {
                CommandUseAbility(entry.Value);
                break;
            }
        }
    }

    void CheckbuildModeHotkey()
    {
        if (Input.GetKeyDown(buildModeHotkey))
        {
            if (BuildModeToggle)
            {
                ClearSelection();
                ChangeToState(ControllerState.Neutral);
                BuildModeToggle = false;
            }
            else
            {
                ClearSelection();
                Select(buildManager);
                ChangeToState(ControllerState.HoldUnit);
                BuildModeToggle = true;
            }
        }
    }

    bool IsSelectable(UnitTag tags)
    {
        return ((tags & (UnitTag.Unit | UnitTag.Building | UnitTag.ResouceNode | UnitTag.ConstructionSite)) != 0);
    }

    void ClearSelection()
    {
        foreach (GameObject UnitGO in selectedUnits)
        {
            if (UnitGO.TryGetComponent<Building>(out Building building))
            {
                building.ToggleRallyPointMarker(false);
            }
            if (UnitGO.TryGetComponent<Unit>(out Unit unit))
            {
                unit.ApplyTeamColor();
            }
        }
        BuildModeToggle = false;
        selectedUnits.Clear();
    }

    // ==========================================
    // 命令系統 (Command System)
    // ==========================================

    void CommandUnitsMove(Vector3 targetPos)
    {
        List<GameObject> movableUnits = selectedUnits.Where(GO =>
        {
            if (GO == null) return false;
            Unit unit = GO.GetComponent<Unit>();
            if (unit == null) return false;
            if (!unit.Data.CanMove) return false;
            UnitTag UT = unit.Data.UnitTag;
            return (UT & UnitTag.Building) == 0;
        }).ToList();
        if (movableUnits.Count == 0) return;

        Vector3 averagePos = Vector3.zero;
        foreach (var unit in movableUnits) averagePos += unit.transform.position;
        averagePos /= movableUnits.Count;

        Vector3 moveDir = (targetPos - averagePos).normalized;
        if (moveDir == Vector3.zero) moveDir = Vector3.forward;
        Vector3 moveRight = Vector3.Cross(Vector3.up, moveDir).normalized;

        float spacing = 2.0f;
        NavMeshAgent firstAgent = movableUnits[0].GetComponent<NavMeshAgent>();
        if (firstAgent != null)
            spacing = Mathf.Max(2.0f, firstAgent.radius * 2.5f);

        int columns = 3;

        for (int i = 0; i < movableUnits.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;

            int unitsInThisRow = Mathf.Min(columns, movableUnits.Count - row * columns);
            float xOffset = (col - (unitsInThisRow - 1) / 2f) * spacing;
            float zOffset = row * -spacing;

            Vector3 finalOffset = (moveRight * xOffset) + (moveDir * zOffset);
            Vector3 finalDestination = targetPos + finalOffset;

            if (NavMesh.SamplePosition(finalDestination, out NavMeshHit hit, spacing, NavMesh.AllAreas))
                finalDestination = hit.position;

            Unit targetUnit = movableUnits[i].GetComponent<Unit>();
            NavMeshAgent agent = movableUnits[i].GetComponent<NavMeshAgent>();

            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.avoidancePriority = 50 + i;
            }

            targetUnit.unitState = UnitState.Neutral;
            targetUnit.Move(finalDestination);
        }
    }

    public void CommandUseAbility(int abilityIndex)
    {
        if (selectedUnits.Count == 0 || abilityIndex == -1) return;

        // 1. 獲取第一個選中單位的技能名稱作為參考
        string targetAbilityName = "";
        if (selectedUnits[0].TryGetComponent<Unit>(out Unit firstUnit))
        {
            if (abilityIndex < firstUnit.CurrentAbilities.Count)
            {
                targetAbilityName = firstUnit.CurrentAbilities[abilityIndex].AbilityName;
            }
        }

        if (string.IsNullOrEmpty(targetAbilityName)) return;

        // 2. 檢查所有選中單位是否在同一位置具備相同技能，避免誤觸
        foreach (var obj in selectedUnits)
        {
            if (obj.TryGetComponent<Unit>(out Unit unit))
            {
                if (abilityIndex >= unit.CurrentAbilities.Count ||
                    unit.CurrentAbilities[abilityIndex].AbilityName != targetAbilityName)
                {
                    Debug.LogWarning("選中單位技能不統一，取消群體施放");
                    return;
                }
            }
        }

        // 3. 通過檢查後，讓所有選中單位施放技能
        foreach (var obj in selectedUnits)
        {
            if (obj.TryGetComponent<Unit>(out Unit unit))
            {
                unit.UseAbility(abilityIndex, this);
            }
        }
    }

    // ==========================================
    // 建築系統功能 (Building System)
    // ==========================================

    public void ToPlaceBuildingMode(GameObject ConstructionSitePrefab)
    {
        PlacingTarget = Instantiate(ConstructionSitePrefab);
        if (PlacingTarget.TryGetComponent<ConstructionSite>(out var site))
        {
            site.Initialize(this);
        }

        originalPlacingLayer = PlacingTarget.layer;
        SetLayerRecursive(PlacingTarget, 2); // 設為 Ignore Raycast，讓 OverlapSphere 跳過

        ChangeToState(ControllerState.PlaceBuilding);
    }

    void UpdateBuildIndicator()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            Vector3 TargetPos = new Vector3();
            if (Input.GetKey(snapHotkey))
            {
                TargetPos.x = Mathf.Round(hit.point.x);
                TargetPos.z = Mathf.Round(hit.point.z);
            }
            else
            {
                TargetPos = hit.point;
            }

            TargetPos.y = 0;
            PlacingTarget.transform.position = TargetPos;
            PlacingTarget.SetActive(true);

            // 主動每幀更新碰撞偵測，不依賴 Trigger（因為預覽物是 layer 2）
            if (PlacingTarget.TryGetComponent<ConstructionSite>(out var site))
            {
                site.RefreshOverlapCheck();
            }
        }
        else
        {
            PlacingTarget.SetActive(false);
        }
    }


    // ==========================================
    // 工具與輔助功能 (Tools & Utilities)
    // ==========================================

    bool GetGroundPosition(out Vector3 hitPoint)
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        // 限制座標在 (0, 0) 到 (Screen.width - 1, Screen.height - 1) 之間
        mousePos.x = Mathf.Clamp(mousePos.x, 0, UnityEngine.Screen.width - 1);
        mousePos.y = Mathf.Clamp(mousePos.y, 0, UnityEngine.Screen.height - 1);

        Ray ray = cam.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            hitPoint = hit.point;
            return true;
        }
        hitPoint = Vector3.zero;
        return false;
    }

    void InitializeTopDownView()
    {
        if (cam == null) cam = Camera.main;
        transform.rotation = Quaternion.identity;
        cam.transform.localPosition = new Vector3(0, topDownHeight, 0);
        cam.transform.localRotation = Quaternion.Euler(topDownAngle, 0, 0);

        targetZoom = cam.orthographicSize;
    }

    void MoveCamera()
    {
        Vector3 moveDir = Vector3.zero;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        if (mousePos.x >= UnityEngine.Screen.width - edgeSize) moveDir.x += 1;
        if (mousePos.x <= edgeSize) moveDir.x -= 1;
        if (mousePos.y >= UnityEngine.Screen.height - edgeSize) moveDir.z += 1;
        if (mousePos.y <= edgeSize) moveDir.z -= 1;

        if (moveDir != Vector3.zero)
            transform.position += moveDir.normalized * moveSpeed * Time.deltaTime;
    }

    void ZoomCamera()
    {
        // 1. 取得滑鼠滾輪輸入 (Input System)
        // scroll.y 通常是 120 (向上) 或 -120 (向下)
        float scrollInput = Mouse.current.scroll.ReadValue().y;

        if (scrollInput != 0)
        {
            // 2. 計算目標縮放 (滾輪向上 = 放大 = Size變小)
            targetZoom -= scrollInput * zoomSpeed * 0.01f;

            // 3. 限制縮放範圍
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        // 4. 使用 Lerp 達成平滑縮放效果
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * smoothness);
    }

    void CalculateSelectionCorners(Vector3 start, Vector3 end)
    {
        Vector3 p1 = start;
        Vector3 p2 = new Vector3(start.x, start.y, end.z);
        Vector3 p3 = end;
        Vector3 p4 = new Vector3(end.x, start.y, start.z);
        Debug.DrawLine(p1, p2, Color.green, 1f);
        Debug.DrawLine(p2, p3, Color.green, 1f);
        Debug.DrawLine(p3, p4, Color.green, 1f);
        Debug.DrawLine(p4, p1, Color.green, 1f);
    }

    void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, newLayer);
    }

    public void SnapCameraTo(Vector3 worldPos)
    {
        transform.position = new Vector3(worldPos.x, transform.position.y, worldPos.z);
    }
}