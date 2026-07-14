using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EmptyAbility", menuName = "Scriptable Objects/Ability/EmptyAbility")]
public class Ability : ScriptableObject
{
    public string AbilityName;
    public Sprite Icon;

    [Tooltip("主動/被動技能")]
    public bool autoActivateOnSpawn = false;

    public virtual void Use(GameObject Owner, PlayerController Caller)
    {
        Debug.Log("Ability: 這是一個空的能力，什麼事都沒發生");
    }

    public virtual List<Resource> GetCost()
    {
        return null;
    }
}
