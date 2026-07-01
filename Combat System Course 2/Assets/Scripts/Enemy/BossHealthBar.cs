using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    public TextMeshProUGUI bossName;
    public Image healthBarFill;
    public Image healthBarBG;

    private HealthSystem healthSystem;
    

    void Start()
    {
        

        healthSystem = GetComponentInParent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += OnHealthChanged;
            healthSystem.OnDeath += OnBossDeath;
            RefreshHealthBar();
        }

       

        // BossѪ��ʼ����ʾ
        healthBarFill.enabled = true;
        healthBarBG.enabled = true;

        
    }

  
    void OnHealthChanged(HealthSystem hs, HealthChangeInfo info)
    {
        RefreshHealthBar();
    }

    void OnBossDeath(HealthSystem hs)
    {
        bossName.enabled = false;
        healthBarBG.enabled = false;
        healthBarFill.enabled = false;
    }

    public void RefreshHealthBar()
    {
        if (healthSystem == null || healthBarFill == null) return;

        float fillAmount = healthSystem.Health / healthSystem.MaxHealth;
        healthBarFill.fillAmount = fillAmount;

    }

    void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= OnHealthChanged;
            healthSystem.OnDeath -= OnBossDeath;
        }
    }
}