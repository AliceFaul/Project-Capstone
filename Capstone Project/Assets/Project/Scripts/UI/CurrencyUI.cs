using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class CurrencyUI : MonoBehaviour
{
    [Header("Currency Texts")]
    [SerializeField] private TMP_Text goldCurrencyText;
    [SerializeField] private TMP_Text gemCurrencyText;
    [SerializeField] private float duration = 1f;

    private IPlayerRuntime _player;
    
    private Coroutine _goldRoutine;
    private Coroutine _gemRoutine;
    
    private int _currentGold;
    private int _currentGem;

    public void Initialize(IPlayerRuntime player)
    {
        _player = player;
        if(_player?.Currency == null) return;
        
        _player.Currency.OnCurrencyChanged += UpdateCurrency;
        _currentGold = _player.Currency.Gold();
        _currentGem = _player.Currency.Gem();
            
        if(goldCurrencyText != null) goldCurrencyText.text = _currentGold.ToString();
        if(gemCurrencyText != null) gemCurrencyText.text = _currentGem.ToString();
    }

    private void OnDestroy()
    {
        if(_player?.Currency == null) return;
        _player.Currency.OnCurrencyChanged -= UpdateCurrency;
    }

    private void UpdateCurrency(CurrencyType type, int amount)
    {
        switch (type)
        {
            case CurrencyType.Gold when goldCurrencyText != null:
                if(_goldRoutine != null) StopCoroutine(_goldRoutine);
                _goldRoutine = StartCoroutine(UpdateCurrencyRoutine(goldCurrencyText, _currentGold, amount,
                    val => _currentGold = val));
                break;
            case CurrencyType.Gem when gemCurrencyText != null:
                if(_gemRoutine != null) StopCoroutine(_gemRoutine);
                _gemRoutine = StartCoroutine(UpdateCurrencyRoutine(gemCurrencyText, _currentGem, amount, 
                    val => _currentGem = val));
                break;
        }
    }

    // Run Coroutine to make Smooth Animation.
    private IEnumerator UpdateCurrencyRoutine(TMP_Text currencyText, int start, int end, Action<int> onUpdateCurrent)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            int current = Mathf.RoundToInt(Mathf.Lerp(start, end, t));
            currencyText.text = current.ToString();
            
            yield return null;
        }
        
        currencyText.text = end.ToString();
        onUpdateCurrent?.Invoke(end);
    }
}