using UnityEngine;

public class CameraMovementTest : MonoBehaviour
{
    private float panSpeed = 6f;
    private Camera mCam;

    public Vector2 panLimitX;
    public Vector2 panLimitZ;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        mCam = GetComponentInChildren<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        CameraMovement();
    }

    void CameraMovement()
    {
        Vector2 panPosition = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        transform.position += Quaternion.Euler(0, mCam.transform.eulerAngles.y, 0) * new Vector3(panPosition.x, 0, panPosition.y) * (panSpeed * Time.deltaTime);

        transform.position = new Vector3(Mathf.Clamp(transform.position.x, panLimitX.x, panLimitX.y), transform.position.y, Mathf.Clamp(transform.position.z, panLimitZ.x, panLimitZ.y));
    }    
}
