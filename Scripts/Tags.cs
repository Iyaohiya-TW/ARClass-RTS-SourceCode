using System.Collections.Generic;
using UnityEngine;

// RTS tag is in Flags, which is a Bitmask.
[System.Flags]
public enum UnitTag
{
    None = 0,

    Unit = 1 << 0,
    Building = 1 << 1,
    ResouceNode = 1 << 2,
    // Unit
    Military = 1 << 3,
    Worker = 1 << 4,
    // Unit Attack Type
    Melee = 1 << 5,
    Range = 1 << 6,
    // Unit Move Type
    Ground = 1 << 7,
    Naval = 1 << 8,

    ConstructionSite = 1 << 9,
}

// Each Unit only belong to one player, Team tag is use to mark who is this Unit's Allies.
[System.Flags]
public enum TeamTag
{
    None = 0,
    P1 = 1 << 0,
    P2 = 1 << 1,
    P3 = 1 << 2,
    P4 = 1 << 3,
}


