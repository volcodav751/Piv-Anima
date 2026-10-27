using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEndController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject victoryPanel;

    private bool gameEnded;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    public void ShowVictory()
    {
        if (gameEnded)
            return;

        if (victoryPanel == null)
        {
            Debug.LogError(
                "GameEndController: Victory Panel is not assigned.",
                this
            );

            return;
        }

        gameEnded = true;
        victoryPanel.SetActive(true);

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void RestartGame()
    {
        RestoreTime();

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void QuitGame()
    {
        RestoreTime();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        // Prevent the next scene from remaining paused.
        if (gameEnded)
            RestoreTime();
    }

    private void RestoreTime()
    {
        Time.timeScale =
            previousTimeScale > 0f ? previousTimeScale : 1f;

        gameEnded = false;
    }
}