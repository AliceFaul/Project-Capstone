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

    public void Initialize(IPlayerRuntime player)
    {
        _player = player;
        _player.Currency.OnCurrencyChanged += UpdateCurrency;
        goldCurrencyText.text = _player.Currency.Gold().ToString();
        gemCurrencyText.text = _player.Currency.Gem().ToString();
    }

    private void UpdateCurrency(CurrencyType type, int amount)
    {
        switch (type)
        {
            case CurrencyType.Gold when goldCurrencyText != null:
                StartCoroutine(UpdateCurrencyRoutine(goldCurrencyText, amount));
                break;
            case CurrencyType.Gem when gemCurrencyText != null:
                StartCoroutine(UpdateCurrencyRoutine(gemCurrencyText, amount));
                break;
        }
    }

    // Run Coroutine to make Smooth Animation.
    private IEnumerator UpdateCurrencyRoutine(TMP_Text currencyText, int amount)
    {
        int start = int.Parse(currencyText.text);
        int end = amount;

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
    }
}