using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

    public float forwardAcceleration;
    public float maxForwardSpeed;
    public float maxHorizontalSpeed;
    public float jumpSpeed;
    public float minJumpSpeed;
    public float roll;
    public float tiltSmoothing;
    public float horizontalSmoothing;
    public float resetY;
    public bool boostEnabled;
    public float boostSpeed;
    public float boostDelay;
    public float gravityMultiplier;
    public MeshRenderer meshRenderer;
    public Light leftThrustLight;
    public Light rightThrustLight;
    public bool rotatable = false;
    
    [HideInInspector]
    public Vector3 accelerationSum;

    private Rigidbody rigidbody_;
    private Vector3 startPosition_;
    private Quaternion startRotation_;
    private float lastJumpTime_;
    private float lastBoostTime_;
    private Transform meshTransform_;
    public LayerMask mask_;
    private bool jumping_ = false;
    private const float analogStickDeadzone_ = 0.02f;

    void Awake() {
        rigidbody_ = GetComponent<Rigidbody>();
        startPosition_ = transform.position;
        startRotation_ = transform.rotation;

        // TODO(jaween): Place in a level manager?
        Vector3 gravity = Physics.gravity;
        gravity *= gravityMultiplier;
        Physics.gravity = gravity;

        // Enables immediate boosting
        lastBoostTime_ = Time.time - boostDelay;

        meshTransform_ = meshRenderer.transform;
        if (meshTransform_ == null)
        {
            Debug.Log("PlayerController: Error, couldn't retrieve MeshRenderer's Transform");
        }
    }

    void Update()
    {
        // Thruster light intensity
        const float maxLightIntensity = 8.0f;
        float speedRatio = rigidbody_.velocity.magnitude / maxForwardSpeed;
        float thrusterLightIntensity = speedRatio * maxLightIntensity;
        leftThrustLight.intensity = thrusterLightIntensity;
        rightThrustLight.intensity = thrusterLightIntensity;

        // Thruster light spread
        const float minLightAngle = 40.0f;
        const float maxLightAngle = 100.0f;
        float thrusterLightAngle = minLightAngle + speedRatio * (maxLightAngle - minLightAngle);
        leftThrustLight.spotAngle = thrusterLightAngle;
        rightThrustLight.spotAngle = thrusterLightAngle;
    }

    void FixedUpdate() {
        float horizontalInputAxis = Input.GetAxis("Horizontal");
        float verticalInputAxis = Input.GetAxis("Vertical");

        // Calculates velocity in local space coordinates, at the end of FixedUpdate() we convert it
        // back to world space and apply it to the Rigidbody
        // TODO(jaween): Alternatively calculate velocity using floats and apply it to the Rigidybody's transform directions
        Vector3 localSpaceVelocity = transform.InverseTransformDirection(rigidbody_.velocity);

        float friction = 0.2f;
        localSpaceVelocity.x *= 1 - friction * Time.fixedDeltaTime;
        localSpaceVelocity.z *= 1 - friction * Time.fixedDeltaTime;

        // Side-to-side momevement
        float lateralVelocityTarget = horizontalInputAxis * maxHorizontalSpeed;
        localSpaceVelocity.x = lateralVelocityTarget;
        RollShip(localSpaceVelocity.x);

        // Aiming the ship (yaw)
        if (rotatable)
        {
            float rotation = transform.rotation.eulerAngles.y + horizontalInputAxis * 80;
            Quaternion from = transform.rotation;
            Quaternion to = Quaternion.Euler(0.0f, rotation, 0.0f);
            rigidbody_.rotation = Quaternion.Slerp(from, to, Time.fixedDeltaTime * 1.0f);
        }

        // Stops the ship from clipping into a wall and getting stuck
        Vector3 newVelocityDirection = new Vector3(Mathf.Sign(localSpaceVelocity.x), 0.0f, 0.0f);
        RaycastHit hit;
        float sweepTestLength = Mathf.Abs(localSpaceVelocity.x) * Time.deltaTime;
        if (rigidbody_.SweepTest(newVelocityDirection, out hit, sweepTestLength))
        {
            // Stops the ship moving horizontally when it hits a wall
            localSpaceVelocity.x = 0.0f;
        }

        // Forward movement
        accelerationSum = Vector3.zero;
        accelerationSum += transform.InverseTransformDirection(transform.forward) * verticalInputAxis * forwardAcceleration;

        // Hovering and Gravity
        sweepTestLength = 0.8f;
        Debug.DrawLine(transform.position, transform.position - transform.up * sweepTestLength, Color.magenta);
        if (rigidbody_.SweepTest(-transform.up, out hit, sweepTestLength))
        {
            // Hovering
            Vector3 newPosition = transform.position;
            newPosition.y = hit.point.y + sweepTestLength;
            rigidbody_.MovePosition(newPosition);
            localSpaceVelocity.y = 0.0f;
            jumping_ = false;

            if (Input.GetButton("Fire1"))
            {
                // Temp move up to stop immediate gravity
                rigidbody_.MovePosition(transform.position + transform.up * 0.1f);
                localSpaceVelocity.y = jumpSpeed;
                jumping_ = true;
            }
        }
        else
        {
            // Gravity
            float gravity = -Physics.gravity.magnitude * Time.fixedDeltaTime;
            localSpaceVelocity.y += gravity;
        }
        
        // Jumping
        float now = Time.time;
        /*if (Input.GetButton("Fire1"))
        {
            // Can only jump when the ship is on a platform
            RaycastHit closestCollision;
            //if (rigidbody_.SweepTest(Vector3.down, out closestCollision, 0.7f))
            {
                localSpaceVelocity.y = jumpSpeed * Time.fixedDeltaTime;
                lastJumpTime_ = now;
                jumping_ = true;
            } 
        }*/

        // Variable jump height
        if (Input.GetButtonUp("Fire1") && jumping_)
        {
            if (localSpaceVelocity.y > minJumpSpeed)
            {
                localSpaceVelocity.y = minJumpSpeed;
            }
            jumping_ = false;
        }

        // Forward boost
        if (boostEnabled && Input.GetButton("Fire2") && now - lastBoostTime_ > boostDelay)
        {
            accelerationSum += transform.forward * boostSpeed * Time.fixedDeltaTime;
            lastBoostTime_ = now;
        }

        // Applies the forward/backward acceleration
        localSpaceVelocity += rigidbody_.mass * accelerationSum * Time.fixedDeltaTime;

        // Enforces the max speed both forward and backwards
        if (Mathf.Abs(localSpaceVelocity.z) > maxForwardSpeed)
        {
            localSpaceVelocity.z = Mathf.Sign(localSpaceVelocity.z) * maxForwardSpeed;
        }

        // Applies the velocity
        rigidbody_.velocity = transform.TransformDirection(localSpaceVelocity);

        // Resets the ship
        if (rigidbody_.position.y <= resetY || Input.GetButton("Fire2"))
        {
            Reset();
        }
    }

    // Rotates the mesh slightly without rolling the ship's collider
    private void RollShip(float angle)
    {
        // TODO(jaween): When y-axis rotation isn't locked and the ship's nose is pointed upwards, this rotation code erronously flips the ship so its belly faces the camera
        Quaternion from = meshTransform_.rotation;
        Quaternion to = Quaternion.Euler(meshTransform_.rotation.eulerAngles.x, meshTransform_.rotation.eulerAngles.y, angle * -roll);
        meshTransform_.rotation = Quaternion.Slerp(from, to, Time.fixedDeltaTime * tiltSmoothing);
    }

    private void Reset()
    {
        rigidbody_.MovePosition(startPosition_);
        rigidbody_.MoveRotation(startRotation_);

        rigidbody_.velocity = Vector3.zero;
        rigidbody_.angularVelocity = Vector3.zero;
        accelerationSum = Vector3.zero;
    }
}
