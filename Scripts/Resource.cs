using System;
using System.Collections.Generic;
using UnityEngine;

public enum ResourceType
{
    Wood = 0,
    Stone = 1,
    Gold = 2,
    Food = 3
}

[Serializable]
public class Resource
{
    public ResourceType Type;
    public int Amount = 0;

    public Resource() { }

    public Resource(ResourceType type)
    {
        this.Type = type;
    }
    public Resource(ResourceType type, int amount) : this(type)
    {
        this.Amount = amount;
    }

}

[Serializable]
public class ResourceSet
{
    public PlayerController Owner;
    public List<Resource> Resources = new List<Resource>();

    public ResourceSet()
    {
        // Get every constant defined in the ResourceType Enum
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        {
            // Add a new Resource instance for each type
            Resources.Add(new Resource(type));
        }
    }

    public void AddResource(Resource incomingResource)
    {
        // 1. Find the resource in our list that matches the incoming type
        Resource target = Resources.Find(r => r.Type == incomingResource.Type);

        // 2. If found, add the amount
        if (target != null)
        {
            target.Amount += incomingResource.Amount;
        }
        else
        {
            // Optional: Handle the case where the type wasn't in the list
            Debug.LogWarning($"Resource type {incomingResource.Type} not found in set!");
        }

        if(Owner)
        {
            Owner.UIManager.UpdateResourcePanel();
        }
    }

    public void CostResource(Resource incomingResource)
    {
        Resource ValueToCost = new Resource(incomingResource.Type, incomingResource.Amount);
        ValueToCost.Amount = -ValueToCost.Amount;
        AddResource(ValueToCost);
    }

    public int GetResourceByType(ResourceType type)
    {
        foreach (Resource Res in Resources)
        {
            if(Res.Type == type)
            {
                return Res.Amount;
            }
        }
        return 0;
    }
}
