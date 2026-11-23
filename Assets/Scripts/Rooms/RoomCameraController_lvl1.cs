using UnityEngine;

public class RoomCameraController_lvl1 : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject bridgeBlocker;
    public GameObject bridgeArcCollider;

    [Header("Camera Settings")]
    public float moveSpeed = 5f;
    public float lockedCameraX = 3.6f;
    public float minX = 3.6f;
    public float maxX = 5.5f;

    public bool isAreaCleared = false;

    private void Start()
    {
        ApplyAreaState();
    }

    private void Update()
    {
        float targetX;

        if (!isAreaCleared)
        {
            targetX = lockedCameraX;
        }
        else
        {
            if (player != null)
            {
                targetX = player.position.x;
            }
            else
            {
                targetX = transform.position.x;
            }

            targetX = Mathf.Clamp(
                targetX,
                minX,
                maxX
            );
        }

        Vector3 targetPosition =
            new Vector3(
                targetX,
                transform.position.y,
                transform.position.z
            );

        transform.position =
            Vector3.Lerp(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
    }

    public void CompleteArea()
    {
        if (isAreaCleared)
        {
            return;
        }

        isAreaCleared = true;

        ApplyAreaState();

        Debug.Log("All waves completed. Area unlocked.");
    }

    private void ApplyAreaState()
    {
        if (bridgeBlocker != null)
        {
            bridgeBlocker.SetActive(
                !isAreaCleared
            );
        }

        if (bridgeArcCollider != null)
        {
            bridgeArcCollider.SetActive(
                isAreaCleared
            );
        }
    }
}