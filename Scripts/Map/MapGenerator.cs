using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("地圖設定")]
    public int mapWidth = 50;  // 地圖寬度 (格)
    public int mapHeight = 50; // 地圖高度 (格)
    public float cellSize = 2f; // 格子大小

    [Header("玩家控制器")]
    public PlayerController playerController; 

    [Header("物件預置物 (Prefabs)")]
    public GameObject p1TownCenterPrefab; // 玩家主堡
    public GameObject aiTownCenterPrefab; // AI 主堡

    [Header("生成規則")]
    public int edgePadding = 8;        // 主堡距邊緣最小格數
    public int townCenterClearance = 6; // 資源距主堡最小格數

    [Header("資源數量")]
    public int woodQuantity = 10;
    public int stoneQuantity = 10;        
    public int goldQuantity = 10; 

    [Header("Prefab Setting")]
    public GameObject p1WorkerPrefab;     
    public GameObject aiWorkerPrefab;     

    public GameObject woodNodePrefab;     // 木頭
    public GameObject goldNodePrefab;     // 新增：黃金
    public GameObject stoneNodePrefab;    // 新增：石頭

    // 底層資料結構：0=空地, 1=P1主堡, 2=AI主堡, 9=工人, 其他=資源
    private int[,] mapGrid;
    // 記錄雙方主堡位置，供資源生成排除用
    private List<Vector2Int> townCenterPositions = new List<Vector2Int>();

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        mapGrid = new int[mapWidth, mapHeight];
        townCenterPositions.Clear();  

        int zoneSize = 8;

        Vector2Int p1Start = new Vector2Int(
            Random.Range(edgePadding, edgePadding + zoneSize),
            Random.Range(edgePadding, edgePadding + zoneSize)
        );

        Vector2Int aiStart = new Vector2Int(
            Random.Range(mapWidth - edgePadding - zoneSize, mapWidth - edgePadding),
            Random.Range(mapHeight - edgePadding - zoneSize, mapHeight - edgePadding)
        );

        // 登記主堡位置
        townCenterPositions.Add(p1Start);
        townCenterPositions.Add(aiStart);

        // 生成雙方主堡
        GameObject p1TC = PlaceObjectOnGrid(p1Start.x, p1Start.y, p1TownCenterPrefab, 1);
        RegisterAsPlayerUnit(p1TC);
        if (playerController != null && p1TC != null)
            playerController.SnapCameraTo(p1TC.transform.position);

        PlaceObjectOnGrid(aiStart.x, aiStart.y, aiTownCenterPrefab, 2);

        // 生成初始工人
        for (int i = 1; i <= 3; i++)
        {
            GameObject p1Worker = PlaceObjectOnGrid(p1Start.x + i, p1Start.y - 2, p1WorkerPrefab, 9);
            RegisterAsPlayerUnit(p1Worker);
            PlaceObjectOnGrid(aiStart.x - i, aiStart.y + 2, aiWorkerPrefab, 9);
        }

        // 在地圖上隨機撒資源
        SpawnResources(woodNodePrefab, 3, woodQuantity);
        SpawnResources(goldNodePrefab, 4, goldQuantity);
        SpawnResources(stoneNodePrefab, 5, stoneQuantity);
    }

    void SpawnResources(GameObject prefab, int gridValue, int count)
    {
        int attempts = count * 10; // 避免無限迴圈
        int spawned = 0;

        while (spawned < count && attempts-- > 0)
        {
            int randomX = Random.Range(0, mapWidth);
            int randomY = Random.Range(0, mapHeight);

            if (mapGrid[randomX, randomY] != 0) continue;

            // 檢查是否距任何主堡太近
            bool tooClose = false;
            foreach (var tc in townCenterPositions)
            {
                int dx = Mathf.Abs(randomX - tc.x);
                int dy = Mathf.Abs(randomY - tc.y);
                if (dx < townCenterClearance && dy < townCenterClearance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            PlaceObjectOnGrid(randomX, randomY, prefab, gridValue);
            spawned++;
        }
    }

    /// <summary>
    /// 將生成的 P1 單位設定 TeamTag、Caller，並登記進 PlayerController 的 AllUnitList
    /// </summary>
    void RegisterAsPlayerUnit(GameObject unit)
    {
        if (unit == null || playerController == null) return;

        if (unit.TryGetComponent<Unit>(out Unit u))
        {
            u.TeamTag = playerController.TeamTag;
        }
        
        if (unit.TryGetComponent<Worker>(out Worker worker))
        {
            worker.Caller = playerController;
        }
        playerController.AddUnitToList(unit);
    }

    GameObject PlaceObjectOnGrid(int x, int y, GameObject prefab, int gridValue)
    {
        mapGrid[x, y] = gridValue;

        float totalWidth = mapWidth * cellSize;
        float totalHeight = mapHeight * cellSize;

        float offsetX = totalWidth / 2f;
        float offsetZ = totalHeight / 2f;

        float worldX = (x * cellSize) - offsetX + (cellSize / 2f);
        float worldZ = (y * cellSize) - offsetZ + (cellSize / 2f);

        Vector3 worldPosition = new Vector3(worldX, 0f, worldZ);

        if (prefab != null)
        {
            return Instantiate(prefab, worldPosition, Quaternion.identity, this.transform);
        }
        return null;
    }
}