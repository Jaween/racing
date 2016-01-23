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
    public float minFov;

    void Update()
    {
        Vector3 fromPosition = transform.position;
        Vector3 toPosition = cameraNode.transform.position -
            distanceFromNode * cameraNode.transform.forward;
        transform.position = Vector3.Lerp(fromPosition, toPosition,
            Time.deltaTime * maxPositionLerpMultiplier);

        Quaternion fromRotation = transform.rotation;
        Quaternion toRotation = cameraNode.transform.rotation;
        transform.rotation = Quaternion.Slerp(fromRotation, toRotation,
              Time.deltaTime * rotationLerpMultiplier);
    }
}
