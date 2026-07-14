using System.Collections;
using UnityEngine;

public class Farm : MonoBehaviour
{
    [Header("資源生成設定")]
    [SerializeField] private GameObject foodNodePrefab; // 拖入你的 FoodResourceNode Prefab
    [SerializeField] private Transform spawnPoint;      // 若指派此欄位，則會固定在此位置（不指派則啟用下方的隨機尋找）
    [SerializeField] private float respawnCooldown = 15f; // 耗盡到再次生成的冷卻時間（秒）

    [Header("隨機往外尋找設定（未指派 spawnPoint 時生效）")]
    // 建築物半徑約 1.1，最小半徑設 1.8 可以確保完全不重疊，最大半徑決定它最遠可以噴多遠
    [SerializeField] private float minSpawnRadius = 1.8f;
    [SerializeField] private float maxSpawnRadius = 2.5f;

    private GameObject currentFoodNode; // 記錄當前場上的資源節點實例
    private bool isCooldownActive = false;

    void Start()
    {
        // 建造完成後，立刻生成第一個資源節點
        SpawnResourceNode();
    }

    void Update()
    {
        // 如果目前沒有節點，且也沒有在倒數冷卻中，代表節點剛剛被採集完「死掉了(Die)」
        if (currentFoodNode == null && !isCooldownActive)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private void SpawnResourceNode()
    {
        if (foodNodePrefab == null)
        {
            Debug.LogError($"[{gameObject.name}] 未指派 foodNodePrefab！");
            return;
        }

        Vector3 spawnPos;

        // 如果你有手動指定點，就用指定的
        if (spawnPoint != null)
        {
            spawnPos = spawnPoint.position;
        }
        else
        {
            // 【往旁邊找的核心邏輯】
            // 1. 隨機決定一個 0 ~ 360 度的角度
            float randomAngle = Random.Range(0f, 360f);

            // 2. 將角度轉換為 XZ 平面上的方向向量
            Vector3 direction = new Vector3(Mathf.Sin(randomAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(randomAngle * Mathf.Deg2Rad));

            // 3. 隨機決定距離（確保大於建築物半徑 1.1）
            float randomDistance = Random.Range(minSpawnRadius, maxSpawnRadius);

            // 4. 以農場中心點往該方向延伸，算出最終生成位置
            spawnPos = transform.position + direction * randomDistance;

            // (選擇性) 如果你的地形有高低落差，可以維持與農場相同的 Y 軸高度
            spawnPos.y = transform.position.y;
        }

        // 生成 Prefab 實例
        currentFoodNode = Instantiate(foodNodePrefab, spawnPos, Quaternion.identity);
    }

    private void OnDestroy()
    {
        // 當農場因為升級而被 Destroy 時，檢查場上的資源節點是否還在
        if (currentFoodNode != null)
        {
            Destroy(currentFoodNode);
        }
    }

    private IEnumerator RespawnRoutine()
    {
        isCooldownActive = true;

        // 等待設定的時間
        yield return new WaitForSeconds(respawnCooldown);

        // 冷卻結束，重新生成
        SpawnResourceNode();

        isCooldownActive = false;
    }
}