using UnityEngine;
using UnityEngine.UI;

public class ResourceNode : Unit
{
    public new ResNodeData Data => (ResNodeData)base.Data;

    public Image fillImage;

    private Resource CurrentRes;

    protected override void Awake()
    {
        base.Awake();

        CurrentRes = new Resource(Data.DefaultType, Data.MaxResAmn);
        if (fillImage != null)
        {
            fillImage.color = Color.white;
        }
        UpdateHpSlider();
    }
    
    protected override void Update()
    {
        base.Update();
    }
    
    public void OnGather(int GatherAmn)
    {
        CurrentRes.Amount -= GatherAmn;

        UpdateHpSlider();

        if (CurrentRes.Amount<=0)
        {
            Die();
        }
    }

    public override void UpdateHpSlider()
    {
        if(Data) hpSlider.maxValue = Data.MaxResAmn;
        if(CurrentRes != null) hpSlider.value = CurrentRes.Amount;
    }

    public ResourceType GetResType()
    {
        return CurrentRes.Type;
    }

    public int GetResAmn()
    {
        return CurrentRes.Amount;
    }
}
