using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 掛在場景中的空物件上。
/// 每隔 SpawnInterval 秒，在地圖邊緣生成一波殭屍。
/// Inspector 可調：波次設定、每波數量、強化倍率等。
/// </summary>
public class ZombieManager : MonoBehaviour
{
    // -------------------------------------------------------
    // Inspector 資料結構
    // -------------------------------------------------------

    [System.Serializable]
    public class ZombieSpawnEntry
    {
        [Tooltip("殭屍 Prefab（需掛有 Zombie 腳本）")]
        public GameObject Prefab;

        [Tooltip("第一波時的基礎生成比重（相對權重，越高越常出現）")]
        [Min(1)]
        public int BaseWeight = 1;

        [Tooltip("每過一波，此兵種額外增加的權重（可以用來讓後期兵種越來越常出現）")]
        [Min(0)]
        public int WeightBonusPerWave = 0;
    }

    [System.Serializable]
    public class WaveScalingConfig
    {
        [Header("HP 強化")]
        [Tooltip("每波 HP 增加百分比（0.1 = 每波 +10%）")]
        public float HpScalePerWave = 0.1f;

        [Header("攻擊強化")]
        [Tooltip("每波攻擊力增加百分比")]
        public float AtkScalePerWave = 0.05f;

        [Header("移速強化")]
        [Tooltip("每波移速增加百分比")]
        public float MoveSpeedScalePerWave = 0.02f;

        [Header("數量強化")]
        [Tooltip("每波額外增加的生成數量")]
        public int ExtraCountPerWave = 1;

        [Header("上限")]
        [Tooltip("單波最多生成數量上限（0 = 不限）")]
        public int MaxSpawnCountPerWave = 30;
    }

    // -------------------------------------------------------
    // Inspector 欄位
    // -------------------------------------------------------

    [Header("殭屍種類")]
    [Tooltip("可生成的殭屍清單，支援多種類型與比重")]
    public List<ZombieSpawnEntry> ZombiePool = new List<ZombieSpawnEntry>();

    [Tooltip("殭屍生成的音效")]
    public AudioClip Audio_ZombieGenerate;

    [Range(0f, 1f)]
    [Tooltip("殭屍生成音效的音量")]
    public float ZombieVolume = 1.0f;

    [Header("波次設定")]
    [Tooltip("第一波開始前的延遲（秒）")]
    public float InitialDelay = 5f;

    [Header("波次提示 UI")]
    public GameObject Panel_Hits;
    public TextMeshProUGUI WaveNoticeText;
    public float WaveNoticeDuration = 3f;

    [Tooltip("每波之間的間隔（秒）")]
    public float SpawnInterval = 30f;

    [Tooltip("第一波基礎生成數量")]
    public int BaseSpawnCount = 5;

    [Header("地圖邊界")]
    [Tooltip("生成點的隨機半徑（以此物件為中心）")]
    public float MapRadius = 50f;

    [Tooltip("生成點距離中心的最小距離（確保不在玩家腳下生成）")]
    public float MinSpawnDistance = 40f;

    [Header("強化設定")]
    public WaveScalingConfig Scaling = new WaveScalingConfig();

    [Header("陣營")]
    [Tooltip("生成的殭屍屬於哪個陣營（通常是獨立的 Zombie 陣營）")]
    public TeamTag ZombieTeamTag = TeamTag.P2;

    [Header("擊殺數追蹤區塊")]
    [Tooltip("用來顯示擊殺總數的 TextMeshPro UI")]
    public TextMeshProUGUI KillCountText;

    // -------------------------------------------------------
    // Runtime
    // -------------------------------------------------------

    private int _currentWave = 0;
    private float _timer = 0f;
    private int _totalZombiesKilled = 0; // 新增：儲存總擊殺數
    // -------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------

    private void Start()
    {
        _timer = -InitialDelay; // 負值讓第一波延遲

        if (Panel_Hits != null) Panel_Hits.SetActive(false);
        UpdateKillCountUI();
    }

    private void Update()
    {
        if (ZombiePool.Count == 0) return;

        _timer += Time.deltaTime;
        if (_timer >= SpawnInterval)
        {
            _timer = 0f;
            SpawnWave();
        }
    }
    public void RegisterKill()
    {
        _totalZombiesKilled++;
        UpdateKillCountUI();
    }

    private void UpdateKillCountUI()
    {
        if (KillCountText != null)
        {
            KillCountText.text = $"{_totalZombiesKilled}";
        }
    }

    // -------------------------------------------------------
    // 生成波次
    // -------------------------------------------------------

    private void SpawnWave()
    {
        _currentWave++;

        int count = BaseSpawnCount + Scaling.ExtraCountPerWave * (_currentWave - 1);
        if (Scaling.MaxSpawnCountPerWave > 0)
            count = Mathf.Min(count, Scaling.MaxSpawnCountPerWave);

        Debug.Log($"[ZombieManager] Wave {_currentWave}：生成 {count} 隻殭屍");

        // 每次生成波次時，計算當前波次下的總權重
        int currentTotalWeights = GetTotalWeightsForWave(_currentWave);

        for (int i = 0; i < count; i++)
        {
            ZombieSpawnEntry entry = PickRandomEntry(currentTotalWeights, _currentWave);
            if (entry == null || entry.Prefab == null) continue;

            Vector3 spawnPos = GetSpawnPosition();
            GameObject go = Instantiate(entry.Prefab, spawnPos, Quaternion.identity);

            ApplyScaling(go);
            AssignTeam(go);
            go.GetComponent<Zombie>().Initialization(this);
        }

        ShowWaveNotice(_currentWave);
    }

    // -------------------------------------------------------
    // 套用數值強化
    // -------------------------------------------------------

    private void ApplyScaling(GameObject go)
    {
        Zombie zombie = go.GetComponent<Zombie>();
        if (zombie == null || zombie.Data == null) return;

        int wave = _currentWave - 1; // 第一波不強化

        if (Scaling.HpScalePerWave > 0)
        {
            float hpMult = 1f + Scaling.HpScalePerWave * wave;
            int newMaxHp = Mathf.RoundToInt(zombie.Data.MaxHP * hpMult);
            zombie.HP = newMaxHp;
            zombie.UpdateHpSlider();
        }

        if (Scaling.AtkScalePerWave > 0)
        {
            float atkBonus = zombie.Data.AtkDamage * Scaling.AtkScalePerWave * wave;
            if (atkBonus > 0)
            {
                UnitEffect UE = new UnitEffect();
                UE.EffectName = "ZombieAtkScaling";
                BonusEntry BE = new BonusEntry();
                BE.Temporary = false;
                BE.Value = atkBonus;
                BE.Type = BonusType.Addition;
                BE.TargetTag = UnitTag.None;
                UE.AtkBonus = BE;
                zombie.TempUnitEffects.Add(UE);
            }
        }

        if (Scaling.MoveSpeedScalePerWave > 0 && zombie.Data.CanMove)
        {
            float speedBonus = zombie.Data.MoveSpeed * Scaling.MoveSpeedScalePerWave * wave;
            if (speedBonus > 0)
            {
                UnitEffect UE = new UnitEffect();
                UE.EffectName = "ZombieSpeedScaling";
                BonusEntry BE = new BonusEntry();
                BE.Temporary = false;
                BE.Value = speedBonus;
                BE.Type = BonusType.Addition;
                BE.TargetTag = UnitTag.None;
                UE.MoveBonus = BE;
                zombie.TempUnitEffects.Add(UE);

                var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null)
                    agent.speed = zombie.Data.MoveSpeed + speedBonus;
            }
        }
    }

    // -------------------------------------------------------
    // 設定陣營
    // -------------------------------------------------------

    private void AssignTeam(GameObject go)
    {
        Unit unit = go.GetComponent<Unit>();
        if (unit == null) return;

        unit.TeamTag = ZombieTeamTag;
        unit.ApplyTeamColor();
    }

    // -------------------------------------------------------
    // 隨機挑選殭屍種類（動態加權）
    // -------------------------------------------------------

    /// <summary>
    /// 計算特定波次下的總權重
    /// </summary>
    private int GetTotalWeightsForWave(int wave)
    {
        int total = 0;
        int waveOffset = wave - 1; // 第一波增幅為 0

        foreach (var entry in ZombiePool)
        {
            int currentWeight = entry.BaseWeight + (entry.WeightBonusPerWave * waveOffset);
            total += Mathf.Max(1, currentWeight); // 確保權重最少為 1
        }
        return total;
    }

    /// <summary>
    /// 根據當前波次的權重隨機抽取
    /// </summary>
    private ZombieSpawnEntry PickRandomEntry(int totalWeights, int wave)
    {
        if (totalWeights == 0 || ZombiePool.Count == 0) return null;

        int roll = Random.Range(0, totalWeights);
        int cumulative = 0;
        int waveOffset = wave - 1;

        foreach (var entry in ZombiePool)
        {
            int currentWeight = entry.BaseWeight + (entry.WeightBonusPerWave * waveOffset);
            cumulative += Mathf.Max(1, currentWeight);

            if (roll < cumulative)
                return entry;
        }

        return ZombiePool[ZombiePool.Count - 1];
    }

    // -------------------------------------------------------
    // 生成位置：地圖邊緣隨機點
    // -------------------------------------------------------

    private Vector3 GetSpawnPosition()
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(MinSpawnDistance, MapRadius);
            Vector3 candidate = transform.position + new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );

            if (UnityEngine.AI.NavMesh.SamplePosition(candidate, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                return hit.position;
        }

        float fallbackAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return transform.position + new Vector3(
            Mathf.Cos(fallbackAngle) * MapRadius,
            0f,
            Mathf.Sin(fallbackAngle) * MapRadius
        );
    }

    private Coroutine _waveNoticeCoroutine;

    private void ShowWaveNotice(int wave)
    {
        if (Panel_Hits == null) return;

        if (WaveNoticeText != null)
        {
            WaveNoticeText.text = $"Wave {wave} - Zombie is Coming !";
            // 修正原本代碼中因為沒有大括號導致 PlayClipAtPoint 無法受 if 控制的潛在 Bug
            if (Audio_ZombieGenerate != null)
            {
                AudioSource.PlayClipAtPoint(Audio_ZombieGenerate, transform.position, ZombieVolume);
            }
        }

        Panel_Hits.SetActive(true);

        if (_waveNoticeCoroutine != null)
            StopCoroutine(_waveNoticeCoroutine);

        _waveNoticeCoroutine = StartCoroutine(HideWaveNotice());
    }

    private System.Collections.IEnumerator HideWaveNotice()
    {
        yield return new WaitForSeconds(WaveNoticeDuration);
        if (Panel_Hits != null)
            Panel_Hits.SetActive(false);
    }

    // -------------------------------------------------------
    // Gizmos：在 Scene 視圖顯示生成範圍
    // -------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, MapRadius);

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, MinSpawnDistance);
    }
}