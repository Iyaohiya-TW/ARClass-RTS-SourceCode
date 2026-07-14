using UnityEngine;

public class FogManager : MonoBehaviour
{
    public static FogManager Instance;

    [Header("地圖設定")]
    public int mapWidth = 100;
    public int mapHeight = 100; // 對應世界座標的Z
    public float cellSize = 1f;

    [Header("渲染設定")]
    public Renderer fogRenderer;

    //private byte[] fogValues;        // 0: 未探索, 128: 已探索(迷霧), 255: 可見

    public byte[] fogValues;        // 0: 未探索, 128: 已探索(迷霧), 255: 可見
    private Color32[] textureColors; 
    private Texture2D fogTexture;
    private bool isDirty = false;

    private float autoCellSize; // 內部自動計算的格位大小
    private Vector3 mapOrigin;  // 地圖的左下角起始點 (世界座標)
    void Awake()
    {
        Instance = this;
        InitializeFog();
    }

    void InitializeFog()
    {
        // 1. 取得渲染平面的邊界
        if (fogRenderer == null) {
            Debug.LogError("請先指派 Fog Renderer (Plane)！");
            return;
        }

        // 取得 Plane 的世界空間大小
        Bounds bounds = fogRenderer.bounds;
        
        // 計算自動 CellSize：總寬度 / 解析度
        // 這樣不論 Plane 多大，都會被切成 mapWidth * mapHeight 份
        autoCellSize = bounds.size.x / mapWidth;
        
        // 記錄地圖左下角的座標，方便座標轉換
        mapOrigin = bounds.min;

        // 2. 初始化資料陣列
        fogValues = new byte[mapWidth * mapHeight];
        textureColors = new Color32[mapWidth * mapHeight];

        fogTexture = new Texture2D(mapWidth, mapHeight, TextureFormat.Alpha8, false);
        fogTexture.filterMode = FilterMode.Bilinear;
        fogTexture.wrapMode = TextureWrapMode.Clamp;

        for (int i = 0; i < fogValues.Length; i++)
        {
            fogValues[i] = 0;
            textureColors[i] = new Color32(0, 0, 0, 0); 
        }
        
        fogTexture.SetPixels32(textureColors);
        fogTexture.Apply();

        fogRenderer.material.mainTexture = fogTexture;
    }

    void LateUpdate()
    {
        if (isDirty)
        {
            UpdateTexture();
            isDirty = false;
        }
        
        for (int i = 0; i < fogValues.Length; i++)
        {
            if (fogValues[i] == 255) fogValues[i] = 128;
        }
    }

        public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        // 計算物件相對於地圖左下角 (mapOrigin) 的偏移量
        float offsetX = worldPos.x - mapOrigin.x;
        float offsetZ = worldPos.z - mapOrigin.z;

        // 除以自動計算的 cellSize 取得索引
        int x = Mathf.FloorToInt(offsetX / autoCellSize);
        int z = Mathf.FloorToInt(offsetZ / autoCellSize);

        // 限制範圍避免溢出陣列
        x = Mathf.Clamp(x, 0, mapWidth - 1);
        z = Mathf.Clamp(z, 0, mapHeight - 1);

        return new Vector2Int(x, z);
    }


    public void UpdateFogForUnit(Vector2Int unitPos, int radius)
    {
        for (int z = -radius; z <= radius; z++) {
            for (int x = -radius; x <= radius; x++) {
                if (x * x + z * z <= radius * radius) {
                    Vector2Int targetPos = new Vector2Int(unitPos.x + x, unitPos.y + z); // Vector2Int 的 y 存放 z 軸資訊
                    
                    if (IsInsideMap(targetPos)) {
                        SetVisible(targetPos);
                    }
                }
            }
        }
    }

    private void SetVisible(Vector2Int pos)
    {
        // 陣列索引計算：z座標 * 寬度 + x座標
        int index = pos.y * mapWidth + pos.x; 
        fogValues[index] = 255;
        isDirty = true;
    }

    private bool IsInsideMap(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < mapWidth && pos.y >= 0 && pos.y < mapHeight;
    }

    private void UpdateTexture()
    {
        for (int i = 0; i < fogValues.Length; i++)
        {
            textureColors[i].a = fogValues[i];
        }
        fogTexture.SetPixels32(textureColors);
        fogTexture.Apply();
    }
}
