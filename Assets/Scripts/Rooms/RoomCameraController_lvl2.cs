using UnityEngine;

public class RoomCameraController : MonoBehaviour
{
    public bool isAreaCleared = false;
    public GameObject bridgeBlocker; 

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
        }
    }
}