using UnityEngine;

public class PassageBlocker : MonoBehaviour
{
    [SerializeField]
    private Collider2D blockerCollider;

    private void Awake()
    {
        if (blockerCollider == null)
        {
            blockerCollider = GetComponent<Collider2D>();
        }
    }

    public void Unlock()
    {
        if (blockerCollider != null)
        {
            blockerCollider.enabled = false;
        }
    }
}