using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDUI : MonoBehaviour
{
    public static PlayerHUDUI Instance { get; private set; }

    // 外部组件
    private PlayerProperty playerProperty;
    private HealthSystem healthSystem;

    // 所有填充条
    public Image playerHealthBarFill;
    public Image playerEnergyBarFill;
    public Image playerEXPBarFill;

    // 经验值相关
    public TextMeshProUGUI levelText;
    [SerializeField] private float expFillSpeed = 1f;
    [SerializeField] private float energyFillSpeed = 4f;  // 新增：能量条平滑速度

    // 护甲值相关
    public TextMeshProUGUI armorText;

    private Coroutine energyCoroutine;  // 用于平滑能量条

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterPlayerComponents(PlayerProperty property, HealthSystem healthSys)
    {
        UnsubscribeFromEvents();

        playerProperty = property;
        healthSystem = healthSys;

        SubscribeToEvents();
        InitializeUI();

        Debug.Log("玩家组件注册到 PlayerHUDUI");
    }

    public void UnregisterPlayerComponents()
    {
        UnsubscribeFromEvents();
        playerProperty = null;
        healthSystem = null;
        Debug.Log("玩家组件从 PlayerHUDUI 取消注册");
    }

    private void SubscribeToEvents()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += OnHealthChanged;
        }

        if (playerProperty != null)
        {
            playerProperty.OnArmorChanged += UpdateArmorDisplay;
            playerProperty.OnEnergyChanged += OnEnergyChanged;  // 新增：订阅能量变化
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= OnHealthChanged;
        }

        if (playerProperty != null)
        {
            playerProperty.OnArmorChanged -= UpdateArmorDisplay;
            playerProperty.OnEnergyChanged -= OnEnergyChanged;  // 取消订阅
        }
    }

    private void InitializeUI()
    {
        if (playerProperty != null)
        {
            // 经验条
            int expRequired = playerProperty.level * 30;
            playerEXPBarFill.fillAmount = expRequired > 0 ? (float)playerProperty.currEXP / expRequired : 0f;
            levelText.text = playerProperty.level.ToString();

            // 能量条（直接设初始值，后续用事件平滑）
            if (playerEnergyBarFill != null)
                playerEnergyBarFill.fillAmount = playerProperty.EnergyNormalized;

            UpdateArmorDisplay();
        }

        UpdateHealthBar();
    }

    private void OnHealthChanged(HealthSystem sys)
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthSystem != null && playerHealthBarFill != null)
        {
            playerHealthBarFill.fillAmount = healthSystem.Health / healthSystem.MaxHealth;
        }
    }

    // ==================== 能量条事件驱动更新（推荐方式） ====================
    private void OnEnergyChanged(float normalizedValue)
    {
        if (playerEnergyBarFill == null) return;

        // 停止旧的协程
        if (energyCoroutine != null)
            StopCoroutine(energyCoroutine);

        // 启动平滑动画
        energyCoroutine = StartCoroutine(SmoothFillEnergyBar(normalizedValue));
    }

    private IEnumerator SmoothFillEnergyBar(float target)
    {
        float current = playerEnergyBarFill.fillAmount;
        float timer = 0f;
        float duration = 1f / energyFillSpeed;  // 使用你定义的 speed！

        while (timer < duration)
        {
            timer += Time.deltaTime;
            playerEnergyBarFill.fillAmount = Mathf.Lerp(current, target, timer / duration);
            yield return null;
        }

        playerEnergyBarFill.fillAmount = target;
        energyCoroutine = null;
    }
    // ==================== 经验条系统（你原来的完美保留） ====================
    public void UpdateEXPBar(bool levelUp = false)
    {
        if (levelUp)
        {
            StartCoroutine(LevelUpAnimation());
        }
        else
        {
            StartCoroutine(AnimateEXPBar());
        }
    }

    private IEnumerator LevelUpAnimation()
    {
        while (playerEXPBarFill.fillAmount < 1f)
        {
            playerEXPBarFill.fillAmount = Mathf.MoveTowards(
                playerEXPBarFill.fillAmount, 1f, expFillSpeed * Time.deltaTime);
            yield return null;
        }

        UpdateEXPText();
        yield return new WaitForSeconds(0.3f);
        playerEXPBarFill.fillAmount = 0f;

        if (playerProperty.currEXP > 0)
        {
            StartCoroutine(AnimateEXPBar());
        }
    }

    private IEnumerator AnimateEXPBar()
    {
        int expRequired = playerProperty.level * 30;
        if (expRequired <= 0) yield break;

        float targetFill = (float)playerProperty.currEXP / expRequired;

        while (playerEXPBarFill.fillAmount < targetFill - 0.001f)
        {
            playerEXPBarFill.fillAmount = Mathf.MoveTowards(
                playerEXPBarFill.fillAmount, targetFill, expFillSpeed * Time.deltaTime);
            yield return null;
        }

        playerEXPBarFill.fillAmount = targetFill;
    }

    public void UpdateEXPText()
    {
        if (levelText != null && playerProperty != null)
        {
            levelText.text = playerProperty.level.ToString();
        }
    }

    // ==================== 护甲显示 ====================
    public void UpdateArmorDisplay()
    {
        if (playerProperty != null && armorText != null)
        {
            armorText.text = playerProperty.armorValue.ToString();
        }
    }

    // ==================== 手动刷新（调试用） ====================
    public void RefreshUI()
    {
        UpdateHealthBar();
        if (playerProperty != null)
            OnEnergyChanged(playerProperty.EnergyNormalized); // 强制刷新能量条
        UpdateArmorDisplay();
        UpdateEXPBar();
        UpdateEXPText();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}