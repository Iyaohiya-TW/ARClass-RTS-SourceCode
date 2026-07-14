using System.Collections.Generic;
using UnityEngine;


// Unit Data is the single source of truth for the base state of an unit.
[CreateAssetMenu(fileName = "NewUnitData", menuName = "Scriptable Objects/UnitData/BaseUnitData")]
public class UnitData : ScriptableObject
{
    [Header("General")]
    public string UnitName;
    public float TrainTime = 5.0f;
    public Sprite Icon;
    public UnitTag UnitTag;
    public List<Resource> Cost;
    public bool CanHide;
    public float InteractRange;
    public float DetectionRange;// Range of search for enemies

    [Header("Attack")]
    public bool CanAtk;
    public UnitEffect AutoAttackEffect;
    public int AtkDamage;
    public float AtkRange;
    public float AtkSpeed; // How many atk can be done in one second. AtkCD = 1 / AtkSpeed.
    public List<BonusEntry> AtkDamageBonuses; // This store PERMANENT attack boost to specific type of enemy.
    
    [Header("Defence")]
    public int MaxHP;
    public int Def;

    [Header("Abilities")]
    public List<Ability> DefaultAbilities;

    [Header("Movement")]
    public bool CanMove;
    public float MoveSpeed;

    [Header("Vision")]
    public float VisionRange;

    //=== Make List O(n) -> Dictionary O(1) for better performance ===
    private Dictionary<UnitTag, List<BonusEntry>> _cache;

    private void BuildCache()
    {
        _cache = new Dictionary<UnitTag, List<BonusEntry>>();
        foreach (var bonus in AtkDamageBonuses)
        {
            if (!_cache.ContainsKey(bonus.TargetTag))
                _cache[bonus.TargetTag] = new List<BonusEntry>();

            _cache[bonus.TargetTag].Add(bonus);
        }
    }

    public float GetDamageAgainst(UnitTag targetTag)
    {
        if (_cache == null) BuildCache();

        float flat = 0;
        float multi = 1f;

        // Iterate through our cached bonus categories
        foreach (var kvp in _cache)
        {
            // Check if the target has the tag this bonus category is for
            // Using Bitwise AND: if the result is not 0, the target has this tag.
            if ((targetTag & kvp.Key) != 0)
            {
                // Apply all bonuses in this category
                foreach (var b in kvp.Value)
                {
                    if (b.Type == BonusType.Addition)
                        flat += b.Value;
                    else
                        multi *= b.Value;
                }
            }
        }

        return (AtkDamage + flat) * multi;
    }
}
