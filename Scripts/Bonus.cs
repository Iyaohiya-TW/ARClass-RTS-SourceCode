using UnityEngine;

public enum BonusType
{
    Addition = 0,
    Multiplication = 1
}

[System.Serializable]
public struct BonusEntry
{
    public UnitTag TargetTag;
    public BonusType Type;
    public float Value;

    public bool Temporary;
    public float Duration;
}