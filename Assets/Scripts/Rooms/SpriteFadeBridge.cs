using UnityEngine;
using System.Collections;

public class SpriteFadeBridge : MonoBehaviour
{
    [Range(0.1f, 1f)]
    public float fadedAlpha = 0.4f; 
    public float fadeSpeed = 10f;   
    public RoomCameraController roomController; 

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine fadeCoroutine;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (roomController == null)
        {
            roomController = FindAnyObjectByType<RoomCameraController>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (roomController != null && !roomController.isAreaCleared) return;

        if (collision.CompareTag("Player") && spriteRenderer != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeTo(fadedAlpha));
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && spriteRenderer != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeTo(originalColor.a));
        }
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        Color currentColor = spriteRenderer.color;

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