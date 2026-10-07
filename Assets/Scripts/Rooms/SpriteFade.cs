using UnityEngine;
using System.Collections;

public class SpriteFade : MonoBehaviour
{
    [Range(0f, 1f)]
    public float fadedAlpha = 0.4f; 
    public float fadeSpeed = 10f;   

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine fadeCoroutine;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        originalColor = spriteRenderer.color;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StopAllCoroutines();
            StartCoroutine(FadeTo(fadedAlpha));
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StopAllCoroutines();
            StartCoroutine(FadeTo(originalColor.a));
        }
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        Color currentColor = spriteRenderer.color;
        float startAlpha = currentColor.a;

        while (Mathf.Abs(currentColor.a - targetAlpha) > 0.01f)
        {
            currentColor.a = Mathf.MoveTowards(currentColor.a, targetAlpha, fadeSpeed * Time.deltaTime);
            spriteRenderer.color = currentColor;
            yield return null;
        }

        currentColor.a = targetAlpha;
        spriteRenderer.color = currentColor;
    }
}
