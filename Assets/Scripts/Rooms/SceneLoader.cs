using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Tooltip("Name of the scene to load")]
    [SerializeField]
    private string targetSceneName;

    [SerializeField]
    private LayerMask playerLayer;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((playerLayer.value & (1 << collision.gameObject.layer)) == 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("Target scene name not specified in the Inspector!");
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }
}