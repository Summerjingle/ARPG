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
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI enText;
    

    // ����ֵ���
    public TextMeshProUGUI soulAmount;
    [SerializeField] private float energyFillSpeed = 4f;  // ������������ƽ���ٶ�

    // ����ֵ���
    public TextMeshProUGUI armorText;

    private Coroutine energyCoroutine;  // 
    private Coroutine soulCoroutine;    // soulAmount 

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
            soulAmount.text = playerProperty.currSoulAmount.ToString();

            
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

    public void UpdateSoulAmount()//更新SoulAmount 
    {
        if (soulAmount != null && playerProperty != null)
        {
            if (soulCoroutine != null)
                StopCoroutine(soulCoroutine);

            int targetAmount = playerProperty.currSoulAmount;//读取更新后的currsoulamount

            int currentDisplay;
            if (!int.TryParse(soulAmount.text, out currentDisplay))
                currentDisplay = targetAmount;

            soulCoroutine = StartCoroutine(AnimateNumber(currentDisplay, targetAmount, 0.5f));
        }
    }

    IEnumerator AnimateNumber(int from, int to, float duration)//更新SoulAmount-数字跳动效果
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0, 1, t);
            int current = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            soulAmount.text = current.ToString();
            yield return null;
        }
        soulAmount.text = to.ToString();
        soulCoroutine = null;
    }

    //护甲值更新
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
        if (soulAmount != null && playerProperty != null)
            soulAmount.text = playerProperty.currSoulAmount.ToString();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}