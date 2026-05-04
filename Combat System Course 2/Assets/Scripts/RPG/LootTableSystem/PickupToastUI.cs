using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class PickupToastUI : MonoBehaviour
{
    
    public TextMeshProUGUI nameText;
    
    public CanvasGroup canvasGroup;

    private Coroutine currentRoutine;

    public void Show(ItemSO item, float duration = 2f)
    {
       
        nameText.text = item.nameOfItem;
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine(duration));
    }

    IEnumerator ShowRoutine(float duration)
    {
        yield return Fade(0f, 1f, 0.2f);
        yield return new WaitForSeconds(duration);
        yield return Fade(1f, 0f, 0.3f);
    }

    IEnumerator Fade(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
