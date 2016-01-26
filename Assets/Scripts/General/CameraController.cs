using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraController : MonoBehaviour {

    public GameObject cameraNode;
    public float distanceFromNode;
    public float maxPositionLag;
    public float maxPositionLerpMultiplier;
    public float maxRotationLag;
    public float rotationLerpMultiplier;

    void FixedUpdate()
    {
        // Moves the camera behind the camera node
        Vector3 fromPosition = transform.position;
        Vector3 toPosition = cameraNode.transform.position -
            distanceFromNode * cameraNode.transform.forward;
        Vector3 deltaPosition = (toPosition - fromPosition) * 
            Time.fixedDeltaTime * maxPositionLerpMultiplier;
        Vector3 movementDirection = Vector3.Normalize(deltaPosition);
        
        // Attempts to slide along walls rather than clipping into them
        RaycastHit hit;
        if (Physics.Raycast(transform.position, movementDirection, out hit, 
            Time.fixedDeltaTime * maxPositionLerpMultiplier))
        {
            if (Vector3.Dot(deltaPosition, hit.normal) < 0)
            {
                deltaPosition -= Vector3.Project(deltaPosition, hit.normal);
            }
        }
        transform.position = transform.position + deltaPosition;

        // Rotates camera toward the camera node
        Quaternion fromRotation = transform.rotation;
        Quaternion toRotation = Quaternion.LookRotation(
            cameraNode.transform.position - 
            transform.position, cameraNode.transform.up);
        transform.rotation = Quaternion.Slerp(fromRotation, toRotation,
               Time.fixedDeltaTime * rotationLerpMultiplier);
    }
}
