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

    public GameObject visualNode;
    public MeshRenderer meshRenderer;
    public Text debugText;

    private int trackLayerMask_;
    private float upwardVelocity_ = 0;
    bool isTurning = false;
    float turnStartTime = 0;

    private Rigidbody rigidbody_;
    private Vector3 startingPosition_;
    private Quaternion startingRotation_;
    private Vector3 velocity_ = Vector3.zero;

    void Start() {
        rigidbody_ = GetComponent<Rigidbody>();
        if (rigidbody_ == null)
        {
            Debug.Log("MovementScript Start(): Rigidbody was null");
        }

        startingPosition_ = transform.position;
        startingRotation_ = transform.rotation;

        float gravityMultiplier = 1.0f;
        Physics.gravity = -transform.up * Physics.gravity.magnitude * gravityMultiplier;

        trackLayerMask_ = LayerMask.NameToLayer("Track");
    }

    void Update()
    {
        // Debug UI
        debugText.text = "Velocity: " + velocity_ + "\nSpeed: " + velocity_.magnitude;
        Debug.DrawLine(transform.position, transform.position + velocity_, Color.red);
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

        AnimateMesh();

        // Applies the computed velocity to the RigidBody
        rigidbody_.velocity = velocity_;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        SlowDown(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        SlowDown(collision);
    }

    void Turning(float axisAmount)
    {
        const float superTurnDelay = 0.5f;
        const float turnMultiplier = 40.0f;
        const float maxAxisAmount = 1.0f;
        const float superTurnMultiplier = 1.5f;

        float turn = axisAmount;
        if (Mathf.Abs(axisAmount) == maxAxisAmount)
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

    void ForwardMovement(float axisAmount)
    {
        float sign = Mathf.Sign(Vector3.Dot(transform.forward, velocity_));
        velocity_ = sign * transform.forward * velocity_.magnitude + transform.forward * axisAmount * forwardAcceleration * Time.fixedDeltaTime;

        // TODO(jaween): Rethink friction implementation
        const float minVelocityForFriction = 0.05f;
        if (velocity_.magnitude > minVelocityForFriction)
        {
            velocity_ -= Vector3.Normalize(velocity_) * (friction * Time.fixedDeltaTime);
        }

        // Enforces the max speed
        velocity_ = Vector3.ClampMagnitude(velocity_, maxForwardSpeed);
    }

    void ShipHover()
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

            // Jumping
            if (Input.GetButtonDown("Fire1") && jumpEnabled)
            {
                rigidbody_.MovePosition(transform.position + transform.up * 0.5f);
                velocity_ += transform.up * 4.0f;
            }
        }
        else if (!rigidbody_.SweepTest(-transform.up, out hit, 1.0f))
        {
            // Applies gravity
            //velocity_ += Physics.gravity * Time.fixedDeltaTime;
        }
    }

    void AnimateMesh()
    {
        // Smoothly interpolates the position of the mesh to that of the ship's collider
        visualNode.transform.position = transform.position;
        //visualNode.transform.position = Vector3.Lerp(visualNode.transform.position, transform.position, Time.fixedDeltaTime * velocity_);

        // Smoothly interpolates the rotation of the mesh to that of the ship's collider
        Quaternion fromRotation = visualNode.transform.rotation;
        Quaternion toRotation = transform.rotation;
        visualNode.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, Time.fixedDeltaTime * 6.0f);
    }

    void SlowDown(Collision collision)
    {
        const float slowDownMultiplier = 3.0f;
        Vector3 clippingComponent = Vector3.Project(velocity_, collision.contacts[0].normal);

        // Reduces the speed of the ship proportionally to the incident angle to the wall squared
        // Sliding parallel to a wall slows velocity slightly, ramming perpendicular to a wall slows velocity considerably
        float dotProduct = Vector3.Dot(Vector3.Normalize(collision.contacts[0].normal), Vector3.Normalize(velocity_));
        float dotProductSquared = dotProduct * dotProduct;
        velocity_ *= 1 - slowDownMultiplier * dotProductSquared * Time.fixedDeltaTime;
        rigidbody_.velocity = velocity_;

        Debug.DrawLine(collision.contacts[0].point, collision.contacts[0].point + 2 * collision.contacts[0].normal, Color.cyan);
    }

    // Rotates the mesh slightly without rolling the ship's collider
    private void AnimateTurn(float angle)
    {
        // TODO(jaween): When y-axis rotation isn't locked and the ship's nose is pointed upwards, this rotation code erronously flips the ship so its belly faces the camera
        const float tiltSmoothing = 10.0f;
        Quaternion from = meshRenderer.transform.rotation;
        Quaternion to = Quaternion.Euler(meshRenderer.transform.rotation.eulerAngles.x, meshRenderer.transform.rotation.eulerAngles.y, angle * -maxTurnAngle);
        meshRenderer.transform.rotation = Quaternion.Slerp(from, to, Time.fixedDeltaTime * tiltSmoothing);
    }

    private void ResetShip()
    {
        transform.position = startingPosition_;
        transform.rotation = startingRotation_;

        visualNode.transform.position = transform.position;
        visualNode.transform.rotation = transform.rotation;

        rigidbody_.velocity = Vector3.zero;
        velocity_ = Vector3.zero;
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
