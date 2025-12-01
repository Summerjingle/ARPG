using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;

public class CurrencySystem : MonoBehaviour
{
    public static CurrencySystem Instance { get; private set; }
    [SerializeField] private int currentCoins = 0;
    public event Action<int,int> OnCoinChanged;//传参（当前，变化）
    public TextMeshProUGUI coinText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        UpdateCoinsDisplay();
    }
    public void AddCoins(int amount)
    {
        MessageUI.Instance.Show("[获得钱币:"+amount+"]");
        currentCoins += amount;
        OnCoinChanged?.Invoke(currentCoins, amount);
        UpdateCoinsDisplay();
    }
    public void RemoveCoins(int amount) 
    {
        if (currentCoins - amount < 0) { MessageUI.Instance.Show("钱币不足"); return; }
        MessageUI.Instance.Show("[失去钱币:"+amount+"]");
        currentCoins -= amount;
        OnCoinChanged?.Invoke(currentCoins, -amount);
        UpdateCoinsDisplay();
    }
    private void UpdateCoinsDisplay()
    {
        coinText.text= currentCoins.ToString() + "$";
    }

    public int GetCurrentCoins() => currentCoins;//给SaveManager用
    public void SetCurrentCoins(int amount)//给SaveManager用
    {
        currentCoins = amount;
        UpdateCoinsDisplay();
    }
   
    public void AddTest()
    {
        CurrencySystem.Instance.AddCoins(1);
    }
    public void RemoveTest() 
    {
        CurrencySystem.Instance.RemoveCoins(1);
    }

}
