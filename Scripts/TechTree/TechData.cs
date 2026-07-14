using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TechData", menuName = "Scriptable Objects/TechData")]
public class TechData : ScriptableObject
{
    public string TechName;
    public Sprite SourceIcon;

    public float ResearchTime = 5.0f;

    public List<Resource> Cost;
}
