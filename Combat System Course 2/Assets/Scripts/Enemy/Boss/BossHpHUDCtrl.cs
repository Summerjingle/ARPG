using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class BossHpHUDCtrl : MonoBehaviour
{
    public BossController bossController;
    private HealthSystem bossHealthSystem;
    public CanvasGroup bossHUDCanvasGroup;
    public Image hpFillBar;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI bossNaemText;
    private float fadeInDuration=0.5f;
    private float fadeOutDuration=0.3f;

    void Awake()
    {
        bossController.TryGetComponent<HealthSystem>(out bossHealthSystem);
        bossHUDCanvasGroup.alpha = 0f;
    }
    void OnEnable()
    {
        bossController.OnBossFightEnter+=ShowHUD;
        bossController.OnBossFightExit+=HideHUD;
        bossHealthSystem.OnHealthChanged+=UpdateHpBar;
    }
    void OnDisable()
    {
        bossController.OnBossFightEnter-=ShowHUD;
        bossController.OnBossFightExit-=HideHUD;
        bossHealthSystem.OnHealthChanged-=UpdateHpBar;
    }
    void Start()
    {
        UpdateHpBarUI();
        bossNaemText.text=bossController.BossName;
    }
    
    private void ShowHUD()
    {
        bossHUDCanvasGroup.DOFade(1f, fadeInDuration);
    }
    private void HideHUD()
    {
        bossHUDCanvasGroup.DOFade(0f,fadeOutDuration);
    }
    private void UpdateHpBar(HealthSystem system, HealthChangeInfo info)//花架子
    {
        UpdateHpBarUI();  
    }
    void UpdateHpBarUI()
    {
        if (bossHealthSystem != null)
        {
            hpFillBar.fillAmount = bossHealthSystem.HealthPercent;
            hpText.text = $"{bossHealthSystem.Health:F0}/{bossHealthSystem.MaxHealth:F0}";
        }
    }
}
