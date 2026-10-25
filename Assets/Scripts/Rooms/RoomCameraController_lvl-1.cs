using UnityEngine;

public class RoomCameraController : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 5f;
    public bool isAreaCleared = false;
    public GameObject bridgeBlocker; 
    public GameObject bridgeArcCollider; 
    public float lockedCameraX = 3.6f;
    public float minX = 3.6f;
    public float maxX = 5.5f; 

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isAreaCleared = !isAreaCleared;
            Debug.Log("Zone clearance status: " + isAreaCleared);

            if (bridgeBlocker != null)
            {
                bridgeBlocker.SetActive(!isAreaCleared); 
            }

            if (bridgeArcCollider != null)
            {
                bridgeArcCollider.SetActive(isAreaCleared); 
            }
        }

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
            targetX = Mathf.Clamp(targetX, minX, maxX);
        }

        Vector3 targetPosition = new Vector3(targetX, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }
}