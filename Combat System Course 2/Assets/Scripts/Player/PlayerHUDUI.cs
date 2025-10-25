using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDUI : MonoBehaviour
{
    public static PlayerHUDUI Instance { get; private set; }
    //外部组件
    public PlayerProperty playerProperty;
    public MeleeFighter meleeFighter;
    //所有填充条
    public Image playerHealthBarFill;
    public Image playerEnergyBarFill;
    public Image playerEXPBarFill;
    //经验值相关
    public TextMeshProUGUI levelText;
    [SerializeField] private float expFillSpeed = 1f;
    //护甲值相关
    public TextMeshProUGUI armorText;

    void Start()
    {
        Instance = this;
        
    }

    void Update()
    {
        UpdateEnergyBar();
    }
    public void RegisterPlayerComponents(PlayerProperty property, MeleeFighter fighter)
    {
        // 先取消旧的订阅
        UnsubscribeFromEvents();

        // 设置新的引用
        playerProperty = property;
        meleeFighter = fighter;

        // 订阅事件
        SubscribeToEvents();

        // 更新UI
        InitializeUI();

        Debug.Log("玩家组件注册到 PlayerHUDUI");
    }

    // 取消注册
    public void UnregisterPlayerComponents()
    {
        UnsubscribeFromEvents();
        playerProperty = null;
        meleeFighter = null;
        Debug.Log("玩家组件从 PlayerHUDUI 取消注册");
    }

    // 订阅事件
    private void SubscribeToEvents()
    {
        if (meleeFighter != null)
        {
            meleeFighter.OnHealthChanged += OnHealthChanged;
        }

        if (playerProperty != null)
        {
            playerProperty.OnArmorChanged += UpdateArmorDisplay;
        }
    }

    // 取消订阅事件
    private void UnsubscribeFromEvents()
    {
        if (meleeFighter != null)
        {
            meleeFighter.OnHealthChanged -= OnHealthChanged;
        }

        if (playerProperty != null)
        {
            playerProperty.OnArmorChanged -= UpdateArmorDisplay;
        }
    }
    private void InitializeUI()
    {
        if (playerProperty != null)
        {
            playerEXPBarFill.fillAmount = playerProperty.currEXP * 1.0f / (playerProperty.level * 30);
            levelText.text = playerProperty.level.ToString();
        }

        UpdateHealthBar();
        UpdateEnergyBar();
        UpdateArmorDisplay();
    }

    private void OnHealthChanged()
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (meleeFighter != null && playerHealthBarFill != null)
        {
            float fillAmount = meleeFighter.Health / meleeFighter.MaxHealth;
            playerHealthBarFill.fillAmount = fillAmount;
        }
    }

    // 新的经验条更新方法，处理升级动画
    public void UpdateEXPBar(bool levelUp = false)
    {
        if (levelUp)
        {
            // 如果是升级，先填满再清空
            StartCoroutine(LevelUpAnimation());
        }
        else
        {
            // 普通经验增加
            StartCoroutine(AnimateEXPBar());
        }
    }

    // 升级动画：先填满再清空
    private IEnumerator LevelUpAnimation()
    {
        // 先填满当前经验条
        while (playerEXPBarFill.fillAmount < 1f)
        {
            playerEXPBarFill.fillAmount = Mathf.MoveTowards(
                playerEXPBarFill.fillAmount,
                1f,
                expFillSpeed * Time.deltaTime
            );
            yield return null;
        }

        // 更新等级文本
        UpdateEXPText();

        // 短暂停顿，让玩家看到满条
        yield return new WaitForSeconds(0.3f);

        // 清空经验条，准备下一级
        playerEXPBarFill.fillAmount = 0f;

        // 如果有剩余经验，继续填充
        if (playerProperty.currEXP > 0)
        {
            StartCoroutine(AnimateEXPBar());
        }
    }

    // 普通经验增加动画
    private IEnumerator AnimateEXPBar()
    {
        int expRequired = playerProperty.level * 30;
        if (expRequired <= 0) yield break;

        float targetFill = (float)playerProperty.currEXP / expRequired;

        // 动画填充经验条
        while (playerEXPBarFill.fillAmount < targetFill - 0.001f)
        {
            playerEXPBarFill.fillAmount = Mathf.MoveTowards(
                playerEXPBarFill.fillAmount,
                targetFill,
                expFillSpeed * Time.deltaTime
            );
            yield return null;
        }

        playerEXPBarFill.fillAmount = targetFill;
    }

    public void UpdateEXPText()//这里将经验填充条和等级文字的更新剥离，得以控制经验条动画
    {
        if (levelText != null && playerProperty != null)
        {
            levelText.text = playerProperty.level.ToString();
        }
    }

    private void UpdateEnergyBar()
    {
        if (playerProperty != null && playerEnergyBarFill != null)
        {
            float fillAmount = playerProperty.energyValue / 100f;
            playerEnergyBarFill.fillAmount = fillAmount;
        }
    }

    private void UpdateArmorDisplay()
    {
        if (playerProperty != null && armorText != null)
        {
            armorText.text = playerProperty.armorValue.ToString();
        }
    }
    public void RefreshUI()
    {
        UpdateHealthBar();
        UpdateEnergyBar();
        UpdateArmorDisplay();
        UpdateEXPBar();
        UpdateEXPText();
    }

    private void OnDestroy()//取消订阅
    {
        if (meleeFighter != null)
        {
            meleeFighter.OnHealthChanged -= OnHealthChanged;
        }

        if (playerProperty != null)
        {
            playerProperty.OnArmorChanged -= UpdateArmorDisplay;
        }
    }
}