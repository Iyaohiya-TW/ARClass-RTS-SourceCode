using UnityEngine;

[CreateAssetMenu(fileName = "RoarerZombieData", menuName = "Scriptable Objects/UnitData/RoarerZombieData")]
public class RoarerZombieData : ZombieData
{
    [Header("Roar Ability")]
    [Tooltip("咆哮技能的影響半徑")]
    public float RoarRadius = 15f;

    [Tooltip("咆哮技能的冷卻時間（秒）")]
    public float RoarCooldown = 12f;

    [Tooltip("咆哮時播放的特效 Prefab（可選）")]
    public GameObject RoarVFXPrefab;

    [Header("Roar Buff Settings")]
    [Tooltip("給予的移動速度加成數值")]
    public float MoveSpeedBonus = 10f;

    [Tooltip("Buff的持續時間（秒）")]
    public float BuffDuration = 5f;

    [Header("Roar Audio")]
    [Tooltip("咆哮時播放的音效")]
    public AudioClip RoarSound;

    [Tooltip("音效音量（0～1）")]
    [Range(0f, 1f)]
    public float SoundVolume = 1f;

    [Tooltip("用於搜尋同伴的 LayerMask")]
    public LayerMask AllyLayerMask = Physics.DefaultRaycastLayers;
}