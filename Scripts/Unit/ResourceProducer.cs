using System.Collections;
using UnityEngine;

public class ResourceProducer : MonoBehaviour
{
    private ResourceType resourceType;
    private int amount;
    private float interval;
    private PlayerController caller;

    // 接收來自 Ability 的設定
    public void Setup(ResourceType type, int amount, float interval, PlayerController caller)
    {
        this.resourceType = type;
        this.amount = amount;
        this.interval = interval;
        this.caller = caller;

        StopAllCoroutines();
        StartCoroutine(ProduceRoutine());
    }

    private IEnumerator ProduceRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);

            Resource newResource = new Resource(resourceType, amount);

            if (caller != null && caller.resourceSet != null)
            {
                // 加入資源
                caller.resourceSet.AddResource(newResource);
            }
        }
    }
}