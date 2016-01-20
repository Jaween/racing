using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MovementScript : MonoBehaviour {

    public float forwardAcceleration;
    public float maxForwardSpeed;
    public float maxUpwardSpeed;
    public float friction;
    public float maxTurnAngle;
    public float maxRealignmentAngle;
    public float jumpSpeed;
    public float gravity;
    public float hoverHeight;

    public bool jumpEnabled;
    public bool smoothingEnabled;

    public GameObject visualNode;
    public MeshRenderer meshRenderer;
    public Text debugText;

    private float forwardVelocity_;
    private float upwardVelocity_;
    private Vector3 gravityDirection_;
    bool isTurning_ = false;
    float turnStartTime_ = 0;
    private int shipLayerMask_;
    private int groundLayerMask_;
    private int barrierLayerMask_;
    private Rigidbody rigidbody_;
    private Vector3 startingPosition_;
    private Quaternion startingRotation_;

    void Start()
    {
        rigidbody_ = GetComponent<Rigidbody>();
        if (rigidbody_ == null)
        {
            Debug.Log("MovementScript Start(): Rigidbody was null");
        }

        startingPosition_ = transform.position;
        startingRotation_ = transform.rotation;

        shipLayerMask_ = LayerMask.NameToLayer("Ship");
        groundLayerMask_ = LayerMask.NameToLayer("TrackGround");
        barrierLayerMask_ = LayerMask.NameToLayer("TrackBarrier");

        ResetShip();
    }

    void Update()
    {
        // Velocity debug UI
        debugText.text = "Speed: " + forwardVelocity_;
        Debug.DrawLine(transform.position, 
            transform.position + rigidbody_.velocity, Color.red);

        // Hover debug UI
        Debug.DrawLine(transform.position, 
            transform.position - transform.up * hoverHeight * 2, 
            Color.magenta);

        AnimateMesh();
    }

    void FixedUpdate()
    {
        // Input
        float horizontalInputAxis = Input.GetAxis("Horizontal");
        float verticalInputAxis = Input.GetAxis("Vertical");
        bool jump = false;
        if (Input.GetButtonDown("Fire1"))
        {
            jump = true;
        }
        if (Input.GetButtonDown("Fire2"))
        {
            ResetShip();
        }

        // Tilting in the yaw
        ShipTurning(horizontalInputAxis);

        ShipForwardMovement(verticalInputAxis);

        ShipVerticalMovement(jump);

        // Updates the |RigidBody|'s velocity with our computed values
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

    private void ShipTurning(float axisValue)
    {
        const float superTurnDelay = 0.5f;
        const float turnMultiplier = 40.0f;
        const float superTurnActivationAxisThreshold = 0.9f;
        const float superTurnMultiplier = 1.5f;

        // The 'super turn' increases the sharpness of a turn by holding the 
        // input axis to one side or the other for a brief amount of time
        float turn = axisValue;
        if (Mathf.Abs(axisValue) >= superTurnActivationAxisThreshold)
        {
            if (!isTurning_)
            {
                isTurning_ = true;
                turnStartTime_ = Time.time;
            }

            if (Time.time - turnStartTime_ >= superTurnDelay)
            {
                turn = axisValue * superTurnMultiplier;
            }
        }
        else
        {
            isTurning_ = false;
        }

        // Applies the turn on the ship
        transform.rotation *= Quaternion.Euler(
                0.0f, turnMultiplier * turn * Time.fixedDeltaTime, 0.0f);
        //AnimateTurn(turn);
    }

    private void ShipForwardMovement(float axisAmount)
    {
        forwardVelocity_ = forwardVelocity_ + axisAmount * 
            forwardAcceleration * Time.fixedDeltaTime;

        const float minVelocityForFriction = 0.05f;
        if (Mathf.Abs(forwardVelocity_) > minVelocityForFriction)
        {
            // Friction applied opposite to the direction of velocity
            forwardVelocity_ += -1 * Mathf.Sign(forwardVelocity_) * friction *
                Time.fixedDeltaTime;
        }

        // Enforces the max speed
        forwardVelocity_ = Mathf.Clamp(forwardVelocity_, -maxForwardSpeed, 
            maxForwardSpeed);
    }

    private void ShipVerticalMovement(bool jump)
    {
        // Turns off barrier collisions so they don't impact the sweep test
        Physics.IgnoreLayerCollision(shipLayerMask_, barrierLayerMask_, true);

        // Checks for a platform under the ship
        // To avoid 'jumpiness' |rayLength| should be larger than |hoverHeight|
        float rayLength = 5.0f;

        RaycastHit hit = new RaycastHit();
        if (rigidbody_.SweepTest(-transform.up, out hit, rayLength))
        {
            // Sweep test rays originate from arbitrary points on the 
            // collider's surface, we calculate a ray originating from the 
            // transform's position
            Vector3 projectionVector = Vector3.Project(
                hit.point - transform.position, -hit.distance * transform.up);
            float distanceFromCenterToColliderSurface =
                Mathf.Abs(projectionVector.magnitude - hit.distance);
            Vector3 colliderEdgePoint = transform.position -
                transform.up * distanceFromCenterToColliderSurface;
            Vector3 newHitPoint = transform.position - transform.up *
                projectionVector.magnitude;

            // TODO(jaween): Do we really need a ray cast here? 
            // Could we remove it and rely solely on the sweep test above?
            if (Physics.Raycast(colliderEdgePoint, -transform.up, out hit, 
                rayLength))
            {
                upwardVelocity_ = 0.0f;

                float angle = Vector3.Angle(transform.up, hit.normal);
                if (angle <= maxRealignmentAngle)
                {
                    // Realigns the ship
                    transform.position = 
                        newHitPoint + hit.normal * hoverHeight;
                    transform.rotation = Quaternion.LookRotation(
                        Vector3.Cross(transform.right, hit.normal), 
                        hit.normal);

                    // Sets the gravity direction based on the new orientation
                    gravityDirection_ = -hit.normal;
                }
            }

            // Jumping
            if (jumpEnabled && jump)
            {
                rigidbody_.MovePosition(
                    transform.position + transform.up * 0.5f);
                upwardVelocity_ += jumpSpeed * Time.fixedDeltaTime;
            }
        }
        else
        {
            // Applies gravity
            upwardVelocity_ -= gravity * Time.fixedDeltaTime;

            // Enforces the max gravity/jumping speeds
            upwardVelocity_ = Mathf.Clamp(upwardVelocity_, -maxUpwardSpeed, 
                maxUpwardSpeed);
        }

        // Turns barrier collisions back on
        Physics.IgnoreLayerCollision(shipLayerMask_, barrierLayerMask_, false);
    }

    void AnimateMesh()
    {
        if (smoothingEnabled)
        {
            // Interpolates the mesh position to the collider position
            Vector3 fromPosition = visualNode.transform.position;
            Vector3 toPosition = transform.position;
            float velocityRatio = forwardVelocity_ / maxForwardSpeed;
            const float multiplier = 5.0f;
            visualNode.transform.position = Vector3.Lerp(
                fromPosition, toPosition, 
                Time.deltaTime * velocityRatio * multiplier);
        }
        else
        {
            visualNode.transform.position = transform.position;
        }

        // Interpolates the mesh rotation to the collider rotation
        Quaternion fromRotation = visualNode.transform.rotation;
        Quaternion toRotation = transform.rotation;
        visualNode.transform.rotation = Quaternion.Slerp(
            fromRotation, toRotation, Time.deltaTime * 3.0f);
    }

    private void ApplyVelocity()
    {
        rigidbody_.velocity = transform.forward * forwardVelocity_ + 
            transform.up * upwardVelocity_;
    }

    // Reduces the speed of the ship based on the incident angle to the wall 
    // So brushing the wall reduces speed slightly, while ramming head on into
    // a wall slows speed considerably
    private void SlowDownAlongWall(Collision collision)
    {
        // Squaring (or absolute value) needed to make dot product positive
        const float slowDownMultiplier = 3.0f;
        float dotProduct = 
            Vector3.Dot(-collision.contacts[0].normal, transform.forward);
        float dotProductSquared = dotProduct * dotProduct;
        forwardVelocity_ *= 1 - 
            (slowDownMultiplier * dotProductSquared * Time.fixedDeltaTime);
        ApplyVelocity();

        Debug.DrawLine(collision.contacts[0].point, 
            collision.contacts[0].point + 2 * collision.contacts[0].normal, 
            Color.cyan);
    }

    // Rotates the mesh slightly without rolling the ship's collider
    private void AnimateTurn(float angle)
    {
        // TODO(jaween): When transform goes upside down, this doesn't work
        const float tiltSmoothing = 5.0f;
        Quaternion from = meshRenderer.transform.rotation;
        Quaternion to = Quaternion.Euler(
            meshRenderer.transform.rotation.eulerAngles.x, 
            meshRenderer.transform.rotation.eulerAngles.y, 
            angle * -maxTurnAngle);
        meshRenderer.transform.rotation = 
            Quaternion.Slerp(from, to, Time.fixedDeltaTime * tiltSmoothing);

        //Debug.Log("Euler angle z is " + transform.rotation.eulerAngles.z);
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
}
