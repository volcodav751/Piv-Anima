using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float lifetime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}