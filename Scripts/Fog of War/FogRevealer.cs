using UnityEngine;

public class FogRevealer : MonoBehaviour
{
    [Header("視野設定")]
    [Tooltip("此單位的視野半徑 (以格子為單位)")]
    public int viewRadius = 5;

    [Tooltip("是否啟用開圖功能")]
    public bool isRevealing = true;

    [Header("優化設定")]
    [Tooltip("每秒更新幾次視野？降低數值可提升效能 (例如 0.1 代表每 0.1 秒更新一次)")]
    public float updateInterval = 0.1f;

    private float timer;

    void Update()
    {
        if (!isRevealing || FogManager.Instance == null) return;

        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            Reveal();
            timer = 0;
        }
    }

    private void Reveal()
    {
        // 1. 將單位的世界座標轉為地圖座標
        Vector2Int gridPos = FogManager.Instance.WorldToGrid(transform.position);

        // 2. 呼叫 Manager 執行圓形掃描
        FogManager.Instance.UpdateFogForUnit(gridPos, viewRadius);
    }

    // 在編輯器中畫出視野範圍，方便調校
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // 假設 cellSize 是 1，這裡畫出大略的圓形範圍
        float visualRadius = viewRadius; 
        Gizmos.DrawWireSphere(transform.position, visualRadius);
    }
}
