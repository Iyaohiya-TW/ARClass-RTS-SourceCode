using UnityEngine;

public class RoarerZombie : Zombie
{
    private RoarerZombieData RoarerData => Data as RoarerZombieData;
    private float _roarCooldownTimer = 0f;

    protected override void Update()
    {
        base.Update();

        // 處理咆哮技能的冷卻計時
        if (_roarCooldownTimer > 0)
        {
            _roarCooldownTimer -= Time.deltaTime;
        }
    }

    protected override void ResolveUnitState()
    {
        // 先執行父類的基礎索敵與戰鬥邏輯
        base.ResolveUnitState();

        // 移除戰鬥與目標限制：只要存在且冷卻完畢，就會發動
        if (_roarCooldownTimer <= 0f)
        {
            CastRoar();
        }
    }

    private void CastRoar()
    {
        // 重置冷卻時間
        _roarCooldownTimer = RoarerData != null ? RoarerData.RoarCooldown : 12f;

        // 1. 播放音效與特效
        if (RoarerData != null)
        {
            if (RoarerData.RoarSound != null)
                AudioSource.PlayClipAtPoint(RoarerData.RoarSound, transform.position, RoarerData.SoundVolume);

            if (RoarerData.RoarVFXPrefab != null)
            {
                // 生成特效並設定 3 秒後自動銷毀
                GameObject vfx = Instantiate(RoarerData.RoarVFXPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 3f);
            }
        }

        // 2. 搜尋範圍內的友軍並給予 Buff
        float radius = RoarerData != null ? RoarerData.RoarRadius : 15f;
        LayerMask mask = RoarerData != null ? RoarerData.AllyLayerMask : Physics.DefaultRaycastLayers;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, mask);
        foreach (var col in hits)
        {
            Unit ally = col.GetComponent<Unit>();
            if (ally == null || ally.HP <= 0) continue;

            // 確保只 Buff 同隊伍的單位
            if (ally.TeamTag == this.TeamTag)
            {
                // 建立移動速度 Buff 的 UnitEffect
                UnitEffect roarBuff = new UnitEffect();
                roarBuff.EffectName = "Roar Speed Buff";
                roarBuff.Instigator = this.gameObject;

                if (RoarerData != null)
                {
                    // 讀取面板上的移動速度加成
                    roarBuff.MoveBonus.Value = RoarerData.MoveSpeedBonus;

                    // 讀取面板上的持續時間
                    // 備註：請確認你的 UnitEffect 類別中有 Duration 這個欄位！如果沒有，請在 UnitEffect.cs 加上 public float Duration;
                    // roarBuff.Duration = RoarerData.BuffDuration; 
                }
                else
                {
                    // 防呆預設值
                    roarBuff.MoveBonus.Value = 10f;
                    // roarBuff.Duration = 5f;
                }

                // 將 Temporary 設為 true，代表這是一個狀態加成
                roarBuff.MoveBonus.Temporary = true;

                // 傳遞給同伴
                ally.RecieveEffect(roarBuff);
            }
        }
    }
}