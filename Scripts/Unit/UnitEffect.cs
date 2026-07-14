using System;
using UnityEngine;

[System.Serializable]
public class UnitEffect
{
    public UnitEffect()
    {
        BonusEntry BE = new BonusEntry();
        BE.Temporary = false;
        BE.Value = 0;
        BE.Type = BonusType.Addition;
        BE.TargetTag = UnitTag.None;
        AtkBonus = BE;
        DefBonus = BE;
        MoveBonus = BE;
    }
    
    public enum EffectCastMethod
    {
        Single = 0,
        MultiCastAOE = 1,
    }

    public string EffectName;
    public EffectCastMethod CastMethod = EffectCastMethod.Single;
    public int HealthChange;
    public BonusEntry AtkBonus;
    public BonusEntry DefBonus;
    public BonusEntry MoveBonus;

    public GameObject ProjectilePrefab; // Direct apply if no projectile
    public bool Homing;
    public float ProjectileSpeed;

    public float AOERadius;
    
    public GameObject Instigator;

    public UnitEffect Clone()
    {
        return (UnitEffect)this.MemberwiseClone();
    }
}
