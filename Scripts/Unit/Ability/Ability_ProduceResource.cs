using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility_ProduceResource", menuName = "Scriptable Objects/Ability/Ability_ProduceResource")]
public class Ability_ProduceResource : Ability
{
    [Header("生產設定")]
    [Tooltip("選擇要生產的資源類型")]
    public ResourceType ResourceToProduce;

    [Tooltip("每次產出的數量")]
    public int Amount = 10;

    [Tooltip("每隔幾秒產出一次")]
    public float IntervalSeconds = 5f;

    public override void Use(GameObject Owner, PlayerController Caller)
    {
        base.Use(Owner, Caller);

        // 核心修正：從 Owner (農場物件) 身上取得生產組件
        ResourceProducer producer = Owner.GetComponent<ResourceProducer>();

        if (producer != null)
        {
            // 將 ScriptableObject 面板上填寫的資料傳遞給組件，並啟動協程
            producer.Setup(ResourceToProduce, Amount, IntervalSeconds, Caller);
        }
        else
        {
            Debug.LogWarning($"[Ability] {Owner.name} 身上缺少 ResourceProducer 組件，無法啟動生產！");
        }
    }
}