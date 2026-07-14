using UnityEngine;

public class HideInFog : MonoBehaviour
{
    [Tooltip("勾選：離開視野就隱形 (適合敵人) / 不勾選：探索過就永遠可見 (適合礦物)")]
    public bool hideInShadow = true;
    private Renderer[] renderers;

    void Start()
    {
        // 遊戲開始時，自動抓取這個物件(包含子物件)身上的所有 3D 模型渲染器
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    void Update()
    {
        // 防呆機制
        if (FogManager.Instance == null || FogManager.Instance.fogValues == null) return;

        // 1. 取得自己目前在地圖陣列中的座標
        Vector2Int gridPos = FogManager.Instance.WorldToGrid(transform.position);

        // 2. 確保座標沒有超出邊界
        if (gridPos.x >= 0 && gridPos.x < FogManager.Instance.mapWidth &&
            gridPos.y >= 0 && gridPos.y < FogManager.Instance.mapHeight)
        {
            // 3. 計算出一維陣列的 Index，並讀取該格的迷霧值
            int index = gridPos.y * FogManager.Instance.mapWidth + gridPos.x;
            byte fogValue = FogManager.Instance.fogValues[index];

            bool isVisible = false;

            if (hideInShadow)
            {
                // 敵軍邏輯：只有在 255 (全亮視野) 內才看得到
                isVisible = (fogValue == 255);
            }
            else
            {
                // 礦物/地形邏輯：只要探索過 (128) 或全亮 (255) 都看得到
                isVisible = (fogValue >= 128);
            }

            // 4. 開關模型的顯示狀態
            foreach (Renderer r in renderers)
            {
                r.enabled = isVisible;
            }
        }
    }
}