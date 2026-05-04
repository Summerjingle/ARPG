using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDUI : MonoBehaviour
{
    public static PlayerHUDUI Instance { get; private set; }

    // �ⲿ���
    private PlayerProperty playerProperty;
    private HealthSystem healthSystem;

    // ���������
    public Image playerHealthBarFill;
    public Image playerEnergyBarFill;
    public Image playerEXPBarFill;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI enText;
    

    // ����ֵ���
    public TextMeshProUGUI levelText;
    [SerializeField] private float expFillSpeed = 1f;
    [SerializeField] private float energyFillSpeed = 4f;  // ������������ƽ���ٶ�

    // ����ֵ���
    public TextMeshProUGUI armorText;

    private Coroutine energyCoroutine;  // ����ƽ��������

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

        Debug.Log("������ע�ᵽ PlayerHUDUI");
    }

    public void UnregisterPlayerComponents()
    {
        UnsubscribeFromEvents();
        playerProperty = null;
        healthSystem = null;
        Debug.Log("�������� PlayerHUDUI ȡ��ע��");
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
            playerProperty.OnEnergyChanged += OnEnergyChanged;  // ���������������仯
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
            playerProperty.OnEnergyChanged -= OnEnergyChanged;  // ȡ������
        }
    }

    private void InitializeUI()
    {
        if (playerProperty != null)
        {
            // ������
            int expRequired = playerProperty.level * 30;
            playerEXPBarFill.fillAmount = expRequired > 0 ? (float)playerProperty.currEXP / expRequired : 0f;
            levelText.text = playerProperty.level.ToString();

            
            if (playerEnergyBarFill != null)
                playerEnergyBarFill.fillAmount = playerProperty.EnergyNormalized;
                
            enText.text = $"{playerProperty.energyValue:F0} / {playerProperty.MaxEnergy}";
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
            hpText.text = $"{healthSystem.Health:F0} / {healthSystem.MaxHealth:F0}";
        }
    }

    // ==================== �������¼��������£��Ƽ���ʽ�� ====================
    private void OnEnergyChanged(float normalizedValue)
    {
        if (playerEnergyBarFill == null) return;

        // ֹͣ�ɵ�Э��
        if (energyCoroutine != null)
            StopCoroutine(energyCoroutine);

        // ����ƽ������
        energyCoroutine = StartCoroutine(SmoothFillEnergyBar(normalizedValue));
        enText.text = $"{playerProperty.energyValue:F0} / {playerProperty.MaxEnergy:F0}";
    }

    private IEnumerator SmoothFillEnergyBar(float target)
    {
        float current = playerEnergyBarFill.fillAmount;
        float timer = 0f;
        float duration = 1f / energyFillSpeed;  // ʹ���㶨��� speed��

        while (timer < duration)
        {
            timer += Time.deltaTime;
            playerEnergyBarFill.fillAmount = Mathf.Lerp(current, target, timer / duration);
            yield return null;
        }

        playerEnergyBarFill.fillAmount = target;
        energyCoroutine = null;
    }
    // ==================== ������ϵͳ����ԭ�������������� ====================
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

    // ==================== ������ʾ ====================
    public void UpdateArmorDisplay()
    {
        if (playerProperty != null && armorText != null)
        {
            armorText.text = playerProperty.armorValue.ToString();
        }
    }

    // ==================== �ֶ�ˢ�£������ã� ====================
    public void RefreshUI()
    {
        UpdateHealthBar();
        if (playerProperty != null)
            OnEnergyChanged(playerProperty.EnergyNormalized); // ǿ��ˢ��������
        UpdateArmorDisplay();
        UpdateEXPBar();
        UpdateEXPText();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}