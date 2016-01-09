using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

    public PlayerController playerController;
    public Transform cameraNode;
    public float positionSmoothing;
    public float rotationSmoothing;
    public float maxFov;
    public float minFov;
    public float fovSmoothing;

    public bool smoothlyMoveWithShip = false;
    public bool rotateWithShip = false;

    private Camera camera_;

    void Start()
    {
        camera_ = GetComponent<Camera>();
        if (camera_ == null)
        {
            Debug.Log("CameraController: Unable to get Camera component");
        }
    }

    void FixedUpdate() {
        if (smoothlyMoveWithShip)
        {
            transform.position = Vector3.Lerp(transform.position, cameraNode.position, Time.deltaTime * positionSmoothing);
        }
        else
        {
            transform.position = cameraNode.position;
        }

        if (rotateWithShip)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, cameraNode.rotation, Time.deltaTime * rotationSmoothing);
        }

        // Zooms the camera based on the ship's speed
        float velocityRatio = playerController.accelerationSum.magnitude / playerController.maxForwardSpeed;
        if (maxFov > minFov)
        {
            // TODO(jaween): Only warps the field of view when the player is travelling forward
            if (playerController.transform.forward.Equals(Vector3.Normalize(playerController.accelerationSum)))
            {
                float targetFov = minFov + (maxFov - minFov) * velocityRatio;
                targetFov++; // Temp to stop warnings
                //camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFov, Time.deltaTime * fovSmoothing);
            }
        }
    }
}
