using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClicFeedback : MonoBehaviour, IPointerClickHandler
{
    public GameState gameState;

    public void OnPointerClick(PointerEventData eventData)
    {
        Transform feedback = transform.Find("Feedback");
        if (feedback == null)
            return;

        RectTransform canvasRect = GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPos
        );

        feedback.GetComponent<RectTransform>().anchoredPosition = localPos;
        feedback.gameObject.SetActive(true);
        StartCoroutine(AnimateSize(feedback.gameObject, 0.3f, 0.2f));
    }

    private IEnumerator AnimateSize(GameObject obj, float delay, float duration)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();

        Vector3 originalScale = rt.localScale;
        Vector3 startScale = originalScale * 0.1f;
        Vector3 targetScale = originalScale;

        // Grow
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            rt.localScale = Vector3.Lerp(startScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(delay);
        rt.localScale = originalScale;
        obj.SetActive(false);
    }

    private IEnumerator DisableAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }
}
