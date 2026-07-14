using UnityEngine;

[CreateAssetMenu(fileName = "NewMeleeData", menuName = "Scriptable Objects/UnitData/MeleeUnitData")]
public class MeleeUnitData : UnitData
{
    [Header("Melee Timing & Logic")]
    [Tooltip("動畫播放後多久產生傷害(前搖)")]
    public float DamageDelay = 0.5f;
    
    [Tooltip("建議設為比 AtkRange 略小，防止模型重疊")]
    public float StoppingDistance = 1.2f;

    [Header("Melee Visuals")]
    public GameObject HitEffect;
    public string AttackAnimationTrigger = "Attack";
}
