using UnityEngine;

public class RoomCameraController : MonoBehaviour
{
    public float leftCameraX = 3.6f;

    public float rightCameraX = 5.5f;

    public float moveSpeed = 5f;

    public bool isAreaCleared = false;

    public Transform player;

    public float triggerRightX = 5f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isAreaCleared = !isAreaCleared;
            Debug.Log("Zone clearance status: " + isAreaCleared);
        }

        float targetX = leftCameraX;

        if (isAreaCleared)
        {
            if (player != null && player.position.x > triggerRightX)
            {
                targetX = rightCameraX; 
            }
            else
            {
                targetX = leftCameraX;  
            }
        }
        else
        {
            targetX = leftCameraX;
        }

        Vector3 targetPosition = new Vector3(targetX, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }
}
