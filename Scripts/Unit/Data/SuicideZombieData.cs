using UnityEngine;

[CreateAssetMenu(fileName = "SuicideZombieData", menuName = "Scriptable Objects/UnitData/SuicideZombieData")]
public class SuicideZombieData : ZombieData
{
    [Header("Suicide Explosion")]
    [Tooltip("觸發自爆的接近距離")]
    public float TriggerRange = 2f;

    [Tooltip("自爆傷害（對建築）")]
    public float ExplosionDamage = 100f;

    [Tooltip("爆炸傷害半徑")]
    public float ExplosionRadius = 5f;

    [Tooltip("觸發自爆前的延遲時間（秒），可用於播放動畫）")]
    public float FuseTime = 0.5f;

    [Tooltip("爆炸特效 Prefab（可選）")]
    public GameObject ExplosionVFXPrefab;

    [Tooltip("是否只對建築造成傷害，忽略其他單位")]
    public bool DamageOnlyBuildings = true;

    [Tooltip("受傷害的 UnitTag（為 0 代表不過濾，全部都打）")]
    public UnitTag DamageTargetTag = 0;

    [Header("Suicide Audio")]
    [Tooltip("引爆倒數開始時播放的音效")]
    public AudioClip FuseSound;

    [Tooltip("爆炸瞬間播放的音效")]
    public AudioClip ExplosionSound;

    [Tooltip("音效音量（0～1）")]
    [Range(0f, 1f)]
    public float SoundVolume = 1f;

    public LayerMask ExplosionLayerMask = Physics.DefaultRaycastLayers;
}