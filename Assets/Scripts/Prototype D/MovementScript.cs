using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MovementScript : MonoBehaviour {

    public float forwardAcceleration;
    public float maxForwardSpeed;
    public float friction;
    public float maxTurnAngle;
    public float maxRealignmentAngle;

    public bool jumpEnabled;
    public bool smoothingEnabled;

    public GameObject visualNode;
    public MeshRenderer meshRenderer;
    public Text debugText;

    private int trackLayerMask_;
    private float forwardVelocity_ = 0;
    private float upwardVelocity_ = 0;
    bool isTurning = false;
    float turnStartTime = 0;

    private Rigidbody rigidbody_;
    private Vector3 startingPosition_;
    private Quaternion startingRotation_;

    void Start() {
        rigidbody_ = GetComponent<Rigidbody>();
        if (rigidbody_ == null)
        {
            Debug.Log("MovementScript Start(): Rigidbody was null");
        }

        startingPosition_ = transform.position;
        startingRotation_ = transform.rotation;

        float gravityMultiplier = 2.0f;
        Physics.gravity *= gravityMultiplier;

        trackLayerMask_ = LayerMask.NameToLayer("Track");
    }

    void Update()
    {
        // Debug UI
        debugText.text = "Speed: " + forwardVelocity_;
        Debug.DrawLine(transform.position, transform.position + rigidbody_.velocity, Color.red);

        AnimateMesh();
    }

    void FixedUpdate() {
        // Input
        float horizontalInputAxis = Input.GetAxis("Horizontal");
        float verticalInputAxis = Input.GetAxis("Vertical");
        if (Input.GetButtonDown("Fire2"))
        {
            ResetShip();
        }

        // Tilting in the yaw
        Turning(horizontalInputAxis);

        ForwardMovement(verticalInputAxis);

        ShipHover();

        // Applies the computed velocity to the RigidBody
        ApplyVelocity();
    }

    void OnCollisionEnter(Collision collision)
    {
        SlowDownAlongWall(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        SlowDownAlongWall(collision);
    }

    private void Turning(float axisAmount)
    {
        const float superTurnDelay = 0.5f;
        const float turnMultiplier = 40.0f;
        const float superTurnActivationAxisThreshold = 0.9f;
        const float superTurnMultiplier = 1.5f;

        float turn = axisAmount;
        if (Mathf.Abs(axisAmount) >= superTurnActivationAxisThreshold)
        {
            if (!isTurning)
            {
                isTurning = true;
                turnStartTime = Time.time;
            }

            if (Time.time - turnStartTime >= superTurnDelay)
            {
                turn = axisAmount * superTurnMultiplier;
            }
        }
        else
        {
            isTurning = false;
        }

        // Turns the ship
        transform.rotation *= Quaternion.Euler(0.0f, turnMultiplier * turn * Time.fixedDeltaTime, 0.0f);
        //AnimateTurn(turn);
    }

    private void ForwardMovement(float axisAmount)
    {
        forwardVelocity_ = forwardVelocity_ + (axisAmount * forwardAcceleration) * Time.fixedDeltaTime;

        // TODO(jaween): Rethink friction implementation
        const float minVelocityForFriction = 0.05f;
        if (Mathf.Abs(forwardVelocity_) > minVelocityForFriction)
        {
            // TODO(jaween): Reenable friction when slowing down along walls is fixed
            forwardVelocity_ -= friction * Time.fixedDeltaTime;
        }

        // Enforces the max speed
        forwardVelocity_ = Mathf.Clamp(forwardVelocity_, -maxForwardSpeed, maxForwardSpeed);
    }

    private void ShipHover()
    {
        // Aligns the ship to the platform immediately below
        const float rayLength = 1.5f;
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(transform.position, -transform.up, out hit, rayLength, trackLayerMask_))
        {
            float angle = Vector3.Angle(transform.up, hit.normal);
            if (angle <= maxRealignmentAngle)
            {
                // Realigns the ship
                transform.position = hit.point + hit.normal;
                transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.right, hit.normal), hit.normal);

                // Sets the new gravity direction
                Physics.gravity = -hit.normal * Physics.gravity.magnitude;
            }

            upwardVelocity_ = 0.0f;

            // Jumping
            if (Input.GetButtonDown("Fire1") && jumpEnabled)
            {
                rigidbody_.MovePosition(transform.position + transform.up * 0.5f);
                upwardVelocity_ += 4.0f * Time.fixedDeltaTime;
            }
        }
        else if (!rigidbody_.SweepTest(-transform.up, out hit, 1.0f))
        {
            // Applies gravity
            // TODO(jaween): Combine test for hovering and gravity
            upwardVelocity_ += Mathf.Sign(Vector3.Dot(transform.up, Physics.gravity)) * Physics.gravity.magnitude * Time.fixedDeltaTime;
        }
    }

    void AnimateMesh()
    {
        // Smoothly interpolates the position of the mesh to that of the ship's collider
        if (smoothingEnabled)
        {
            visualNode.transform.position = Vector3.Lerp(visualNode.transform.position, transform.position, Time.deltaTime * (rigidbody_.velocity.magnitude / maxForwardSpeed * 5.0f + 4.0f));
        }
        else
        {
            visualNode.transform.position = transform.position;
        }

        // Smoothly interpolates the rotation of the mesh to that of the ship's collider
        Quaternion fromRotation = visualNode.transform.rotation;
        Quaternion toRotation = transform.rotation;
        visualNode.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, Time.deltaTime * 3.0f);
    }

    private void ApplyVelocity()
    {
        rigidbody_.velocity = transform.forward * forwardVelocity_ + transform.up * upwardVelocity_;
    }

    // Reduces the speed of the ship proportionally to the incident angle to the wall squared
    // So brushing the wall reduces speed slightly, while ramming head on into a wall slows speed considerably
    private void SlowDownAlongWall(Collision collision)
    {
        // Squaring or absolute value is needed to make the dot product positive
        // TODO(jaween): Fix dot product
        const float slowDownMultiplier = 3.0f;

        float dotProduct = Vector3.Dot(-collision.contacts[0].normal, transform.forward);
        float dotProductSquared = dotProduct * dotProduct;
        forwardVelocity_ *= 1 - (slowDownMultiplier * dotProductSquared * Time.fixedDeltaTime);
        ApplyVelocity();

        Debug.DrawLine(collision.contacts[0].point, collision.contacts[0].point + 2 * collision.contacts[0].normal, Color.cyan);
    }

    // Rotates the mesh slightly without rolling the ship's collider
    private void AnimateTurn(float angle)
    {
        // TODO(jaween): When y-axis rotation isn't locked and the ship's nose is pointed upwards, this rotation code erronously flips the ship so its belly faces the camera
        const float tiltSmoothing = 10.0f;
        //Debug.Log("Euler angle z is " + transform.rotation.eulerAngles.z);
        Quaternion from = meshRenderer.transform.rotation;
        Quaternion to = Quaternion.Euler(meshRenderer.transform.rotation.eulerAngles.x, meshRenderer.transform.rotation.eulerAngles.y, angle * -maxTurnAngle);
        meshRenderer.transform.rotation = Quaternion.Slerp(from, to, Time.fixedDeltaTime * tiltSmoothing);
    }
    
    private void ResetShip()
    {
        transform.position = startingPosition_;
        transform.rotation = startingRotation_;
        Physics.gravity = -transform.up * Physics.gravity.magnitude;

        visualNode.transform.position = transform.position;
        visualNode.transform.rotation = transform.rotation;

        rigidbody_.velocity = Vector3.zero;
        forwardVelocity_ = 0;
        upwardVelocity_ = 0;
        rigidbody_.angularVelocity = Vector3.zero;
    }

    // To be removed when collisions work stop being a big problem
    /*void ClipVelocity(Collision collision)
    {
        // Removes the component of veolcity that clips inside a wall during a collision
        if (Vector3.Dot(velocity_, collision.contacts[0].normal) < 0)
        {
            float oldMagnitude = velocity_.magnitude;
            Vector3 clippingComponent = Vector3.zero;// Project(velocity_, collision.contacts[0].normal);
            velocity_ = Vector3.Normalize(velocity_ - clippingComponent) * oldMagnitude;
            rigidbody_.velocity = velocity_;

            //rigidbody_.MovePosition(collision.contacts[0].point + 2 * collision.contacts[0].normal
        }
    }*/
}
