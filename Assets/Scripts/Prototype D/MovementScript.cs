using UnityEngine;
using System.Collections;

public class MovementScript : MonoBehaviour {

    public float forwardAcceleration;
    public float maxForwardSpeed;
    public float lateralSpeed;
    public float maxRealignmentAngle;
    public bool jumpEnabled;
    public GameObject visualNode;
    public MeshRenderer meshRenderer;
    public float maxRollAngle;

    public float forwardFrictionRate;
    public float lateralFrictionRate;

    private Rigidbody rigidbody_;
    private Vector3 startingPosition_;
    private Quaternion startingRotation_;

    //private Vector3 gravityDirection_ = Vector3.down;
    private float forwardVelocity_ = 0.0f;
    private float lateralVelocity_ = 0.0f;
    private float upwardVelocity_ = 0.0f;

    void Start() {
        rigidbody_ = GetComponent<Rigidbody>();
        if (rigidbody_ == null)
        {
            Debug.Log("MovementScript Start(): Rigidbody was null");
        }

        startingPosition_ = transform.position;
        startingRotation_ = transform.rotation;
    }

    void FixedUpdate() {
        // Input
        if (Input.GetButtonDown("Fire2"))
        {
            ResetShip();
        }

        float horizontalInputAxis = Input.GetAxis("Horizontal");
        float verticalInputAxis = Input.GetAxis("Vertical");

        // Movement and yaw aiming
        transform.rotation *= Quaternion.Euler(0.0f, horizontalInputAxis, 0.0f);
        //lateralVelocity_ += horizontalInputAxis * Time.fixedDeltaTime;
        forwardVelocity_ += verticalInputAxis * forwardAcceleration * Time.fixedDeltaTime;
        //RollShip(horizontalInputAxis);

        // Applies friction
        // TODO(jaween): Some other form of applying friction might be better (ex. multiplication velocity by a friction factor)
        //lateralVelocity_ -= lateralVelocity_ * Mathf.Sign(lateralVelocity_) * lateralFrictionRate * Time.fixedDeltaTime;
        forwardVelocity_ -= forwardVelocity_ * Mathf.Sign(forwardVelocity_) * forwardFrictionRate * Time.fixedDeltaTime;

        // Enforces the max speed
        forwardVelocity_ = Mathf.Clamp(forwardVelocity_, -maxForwardSpeed, maxForwardSpeed);

        // Aligns the ship to the platform immediately below
        RaycastHit hit = new RaycastHit();
        Debug.DrawLine(transform.position, transform.position + transform.forward * 4.0f, Color.red);
        if (Physics.Raycast(transform.position, -transform.up, out hit, 1.5f))
        {
            // Avoids aligning the ship too sharply
            float angle = Vector3.Angle(transform.up, hit.normal);
            if (angle <= maxRealignmentAngle)
            {
                transform.position = hit.point + hit.normal;
                transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.right, hit.normal), hit.normal);

                //gravityDirection_ = -hit.normal;
                upwardVelocity_ = 0.0f;
            }

            // Jump
            if (Input.GetButtonDown("Fire1") && jumpEnabled)
            {
                rigidbody_.MovePosition(transform.position + transform.up * 0.5f);
                upwardVelocity_ = 4.0f;
            }
        }
        else if (!rigidbody_.SweepTest(-transform.up, out hit, 1.0f))
        {
            // Applies gravity
            upwardVelocity_ -= Physics.gravity.magnitude * Time.fixedDeltaTime;
        }

        // Applies the velocity to the rigidbody
        rigidbody_.velocity = transform.forward * forwardVelocity_ + transform.right * lateralVelocity_ - transform.up * upwardVelocity_;

        // Smoothly interpolates the position of the mesh to that of the ship's collider
        visualNode.transform.position = transform.position;
        //visualNode.transform.position = Vector3.Lerp(visualNode.transform.position, transform.position, Time.fixedDeltaTime * forwardVelocity_);

        // Smoothly interpolates the rotation of the mesh to that of the ship's collider
        Quaternion fromRotation = visualNode.transform.rotation;
        Quaternion toRotation = transform.rotation;
        visualNode.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, Time.fixedDeltaTime * 8.0f);
	}

    void OnCollisionStay(Collision collision)
    {
        // Removes the component of veolcity that clips inside a wall during a collision
        if (Vector3.Dot(rigidbody_.velocity, collision.contacts[0].normal) < 0)
        {
            Vector3 newVelocity = rigidbody_.velocity - Vector3.Project(rigidbody_.velocity, collision.contacts[0].normal);
            //lateralVelocity_ = Vector3.Dot(newVelocity, transform.right);
            //forwardVelocity_ = 0;// Vector3.Dot(newVelocity, transform.forward);
            //rigidbody_.velocity = transform.forward * forwardVelocity_ + transform.right * lateralVelocity_ - transform.up * upwardVelocity_;
            rigidbody_.velocity = Vector3.zero;

            rigidbody_.MovePosition(rigidbody_.position + newVelocity * Time.fixedDeltaTime);

        }
    }

    // Rotates the mesh slightly without rolling the ship's collider
    private void RollShip(float angle)
    {
        // TODO(jaween): When y-axis rotation isn't locked and the ship's nose is pointed upwards, this rotation code erronously flips the ship so its belly faces the camera
        const float tiltSmoothing = 10.0f;
        Quaternion from = meshRenderer.transform.rotation;
        Quaternion to = Quaternion.Euler(meshRenderer.transform.rotation.eulerAngles.x, meshRenderer.transform.rotation.eulerAngles.y, angle * -maxRollAngle);
        meshRenderer.transform.rotation = Quaternion.Slerp(from, to, Time.fixedDeltaTime * tiltSmoothing);
    }

    private void ResetShip()
    {
        transform.position = startingPosition_;
        transform.rotation = startingRotation_;

        visualNode.transform.position = transform.position;
        visualNode.transform.rotation = transform.rotation;

        forwardVelocity_ = 0.0f;
        lateralVelocity_ = 0.0f;
        upwardVelocity_ = 0.0f;
        
        rigidbody_.velocity = Vector3.zero;
        rigidbody_.angularVelocity = Vector3.zero;
    }
}
