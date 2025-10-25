using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHeathBar : MonoBehaviour
{
    public TextMeshProUGUI myName;
    [Header("血条设置")]
    public Image healthBarFill;
    public Image healthBarBG;

    public MeleeFighter fighter;

    [Header("位置偏移")]
    public Vector3 positionOffset = new Vector3(0, 2f, 0);
    public bool faceCamera = true;

    [Header("高级设置")]
    public bool hideWhenFull = true;
    public float showDurationAfterHit = 3f;
    private float hideTimer;
    private Canvas canvas;
    private Camera mainCamera;

    void Start()
    {
        // 自动获取组件
        canvas = GetComponent<Canvas>();
        mainCamera = Camera.main;

        // 如果没有指定fighter，尝试从父对象获取
        if (fighter == null)
        {
            fighter = GetComponentInParent<MeleeFighter>();
        }

        if (fighter != null)
        {
            // 修改1：订阅事件时使用带参数的方法
            fighter.OnGotHit += UpdateHealthBar;
            fighter.OnDeath += OnFighterDeath; // 这里会报错，需要修改方法签名
            fighter.OnDeathComplete += OnFighterDeathComplete;

            // 初始更新
            RefreshHealthBar();
        }

        // 配置Canvas
        if (canvas != null)
        {
            canvas.worldCamera = mainCamera;
        }

        healthBarFill.enabled = false;
        healthBarBG.enabled = false;
    }

    void Update()
    {
        // 如果角色已死亡，不再更新位置和旋转
        if (fighter != null && fighter.IsDead) return;

        // 更新血条位置（跟随角色）
        transform.position = fighter.transform.position + positionOffset;

        // 让血条始终面向相机
        if (faceCamera && mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }

        if (myName != null)
        {
            myName.transform.rotation = mainCamera.transform.rotation;
        }

        if (hideWhenFull && canvas != null && fighter != null)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0 && fighter.Health >= fighter.MaxHealth)
            {
                healthBarFill.enabled = false;
                healthBarBG.enabled = false;
            }
        }
    }

    void UpdateHealthBar(MeleeFighter attacker)
    {
        // 这里我们不需要attacker参数，但为了匹配委托签名必须添加
        RefreshHealthBar();
    }

    // 修改2：修改方法签名以匹配 Action<MeleeFighter>
    void OnFighterDeath(MeleeFighter deadFighter)
    {
        // 虽然参数可能用不到，但为了匹配事件签名必须接受
        if (canvas != null) canvas.enabled = false;
    }

    // 死亡序列完成时调用（可选）
    void OnFighterDeathComplete()
    {
        // 可以在这里添加额外的清理逻辑
        Debug.Log("Death sequence completed, health bar hidden");
    }

    void OnDestroy()
    {
        // 取消订阅事件
        if (fighter != null)
        {
            fighter.OnGotHit -= UpdateHealthBar;
            fighter.OnDeath -= OnFighterDeath;
            fighter.OnDeathComplete -= OnFighterDeathComplete;
        }
    }

    // 手动更新血条（外部调用）
    public void RefreshHealthBar()
    {
        // 如果角色已死亡，不再更新血条
        if (fighter != null && fighter.IsDead) return;

        if (healthBarFill == null || fighter == null) return;

        // 计算血量百分比
        float fillAmount = fighter.Health / fighter.MaxHealth;
        healthBarFill.fillAmount = fillAmount;

        if (hideWhenFull && canvas != null)
        {
            healthBarFill.enabled = true;
            healthBarBG.enabled = true;
            hideTimer = showDurationAfterHit;
        }
    }
}