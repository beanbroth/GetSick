using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

/// <summary>
/// A floating-capsule oriented physics based character controller. Based on the approach devised by Toyful Games for Very Very Valet.
/// </summary>
public class PhysicsBasedEnemy : MonoBehaviour
{
    private Rigidbody _rb;
    private Vector3 _gravitationalForce;
    private Vector3 _rayDir = Vector3.down;
    private Vector3 _previousVelocity = Vector3.zero;
    private Vector2 _moveContext;
    private ParticleSystem.EmissionModule _emission;

    [Header("Other:")]
    [SerializeField] private ParticleSystem _dustParticleSystem;

    private bool _shouldMaintainHeight = true;

    [Header("Height Spring:")]
    [SerializeField] private float _rideHeight = 1.75f; // rideHeight: desired distance to ground (Note, this is distance from the original raycast position (currently centre of transform)). 
    [SerializeField] private float _rayToGroundLength = 3f; // rayToGroundLength: max distance of raycast to ground (Note, this should be greater than the rideHeight).
    [SerializeField] public float _rideSpringStrength = 50f; // rideSpringStrength: strength of spring. (?)
    [SerializeField] private float _rideSpringDamper = 5f; // rideSpringDampener: dampener of spring. (?)


    private enum lookDirectionOptions { velocity, acceleration, moveInput };
    private Quaternion _uprightTargetRot = Quaternion.identity; // Adjust y value to match the desired direction to face.
    private Quaternion _lastTargetRot;
    private bool didLastRayHit;

    [Header("Upright Spring:")]
    [SerializeField] private lookDirectionOptions _characterLookDirection = lookDirectionOptions.velocity;
    [SerializeField] private float _uprightSpringStrength = 40f;
    [SerializeField] private float _uprightSpringDamper = 5f;


    private Vector3 _moveInput;
    private float _speedFactor = 1f;
    private float _maxAccelForceFactor = 1f;
    private Vector3 _m_GoalVel = Vector3.zero;

    [Header("Movement:")]
    [SerializeField] private float _maxSpeed = 8f;
    [SerializeField] private float _acceleration = 200f;
    [SerializeField] private float _maxAccelForce = 150f;
    [SerializeField] private float _leanFactor = 0.25f;
    [SerializeField] private AnimationCurve _accelerationFactorFromDot;
    [SerializeField] private AnimationCurve _maxAccelerationForceFactorFromDot;
    [SerializeField] private Vector3 _moveForceScale = new Vector3(1f, 0f, 1f);

    [Header("Navmesh")]
    public Transform target;
    private NavMeshPath path;
    private float elapsed = 0.0f;

    private void Start()
    {
        path = new NavMeshPath();
        elapsed = 0.0f;
        NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path);

        _rb = GetComponent<Rigidbody>();
        _gravitationalForce = Physics.gravity * _rb.mass;

        if (_dustParticleSystem)
        {
            _emission = _dustParticleSystem.emission; // Stores the module in a local variable
            _emission.enabled = false; // Applies the new value directly to the Particle System
        }
    }

    private bool CheckIfGrounded(bool rayHitGround, RaycastHit rayHit)
    {
        bool grounded;
        if (rayHitGround == true)
        {
            grounded = rayHit.distance <= _rideHeight * 1.3f; // 1.3f allows for greater leniancy (as the value will oscillate about the rideHeight).
        }
        else
        {
            grounded = false;
        }
        return grounded;
    }


    private Vector3 GetLookDirection(lookDirectionOptions lookDirectionOption)
    {
        Vector3 lookDirection = Vector3.zero;
        if (lookDirectionOption == lookDirectionOptions.velocity || lookDirectionOption == lookDirectionOptions.acceleration)
        {
            Vector3 velocity = _rb.velocity;
            velocity.y = 0f;
            if (lookDirectionOption == lookDirectionOptions.velocity)
            {
                lookDirection = velocity;
            }
            else if (lookDirectionOption == lookDirectionOptions.acceleration)
            {
                Vector3 deltaVelocity = velocity - _previousVelocity;
                _previousVelocity = velocity;
                Vector3 acceleration = deltaVelocity / Time.fixedDeltaTime;
                lookDirection = acceleration;
            }
        }
        else if (lookDirectionOption == lookDirectionOptions.moveInput)
        {
            lookDirection = _moveInput;
        }
        return lookDirection;
    }

    private bool _prevGrounded = false;

    private void FixedUpdate()
    {

        // Update the way to the goal every second.
        elapsed += Time.fixedDeltaTime;
        if (elapsed > 1.0f)
        {
            elapsed -= 1.0f;
            NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path);
        }

        //_moveInput = new Vector3(1, 0, 0);
        _moveInput = Vector3.Normalize(path.corners[1] - transform.position);

        (bool rayHitGround, RaycastHit rayHit) = RaycastToGround();

        bool grounded = CheckIfGrounded(rayHitGround, rayHit);
        if (grounded == true)
        {
            if (_prevGrounded == false)
            {
                //if (!FindObjectOfType<AudioManager>().IsPlaying("Land"))
                //{
                //    FindObjectOfType<AudioManager>().Play("Land");
                //}
            }

            if (_moveInput.magnitude != 0)
            {
                //if (!FindObjectOfType<AudioManager>().IsPlaying("Walking"))
                //{
                //    FindObjectOfType<AudioManager>().Play("Walking");
                //}
            }
            else
            {
                //FindObjectOfType<AudioManager>().Stop("Walking");
            }

            if (_dustParticleSystem)
            {
                if (_emission.enabled == false)
                {
                    _emission.enabled = true; // Applies the new value directly to the Particle System                  
                }
            }
        }
        else
        {
            //FindObjectOfType<AudioManager>().Stop("Walking");

            if (_dustParticleSystem)
            {
                if (_emission.enabled == true)
                {
                    _emission.enabled = false; // Applies the new value directly to the Particle System
                }
            }
        }

        CharacterMove(_moveInput, rayHit);

        if (rayHitGround && _shouldMaintainHeight)
        {
            MaintainHeight(rayHit);
        }

        Vector3 lookDirection = GetLookDirection(_characterLookDirection);
        //Vector3 lookDirection = new Vector3(-1, 0, 0);
        MaintainUpright(lookDirection, rayHit);

        _prevGrounded = grounded;
    }

    /// <summary>
    /// Perfom raycast towards the ground.
    /// </summary>
    /// <returns>Whether the ray hit the ground, and information about the ray.</returns>
    private (bool, RaycastHit) RaycastToGround()
    {
        RaycastHit rayHit;
        Ray rayToGround = new Ray(transform.position, _rayDir);
        bool rayHitGround = Physics.Raycast(rayToGround, out rayHit, _rayToGroundLength);
        //Debug.DrawRay(transform.position, _rayDir * _rayToGroundLength, Color.blue);
        return (rayHitGround, rayHit);
    }

    /// <summary>
    /// Determines the relative velocity of the character to the ground beneath,
    /// Calculates and applies the oscillator force to bring the character towards the desired ride height.
    /// Additionally applies the oscillator force to the squash and stretch oscillator, and any object beneath.
    /// </summary>
    /// <param name="rayHit">Information about the RaycastToGround.</param>
    private void MaintainHeight(RaycastHit rayHit)
    {
        Vector3 vel = _rb.velocity;
        Vector3 otherVel = Vector3.zero;
        Rigidbody hitBody = rayHit.rigidbody;
        if (hitBody != null)
        {
            otherVel = hitBody.velocity;
        }
        float rayDirVel = Vector3.Dot(_rayDir, vel);
        float otherDirVel = Vector3.Dot(_rayDir, otherVel);

        float relVel = rayDirVel - otherDirVel;
        float currHeight = rayHit.distance - _rideHeight;
        float springForce = (currHeight * _rideSpringStrength) - (relVel * _rideSpringDamper);
        Vector3 maintainHeightForce = -_gravitationalForce + springForce * Vector3.down;
        Vector3 oscillationForce = springForce * Vector3.down;
        _rb.AddForce(maintainHeightForce);
        //Debug.DrawLine(transform.position, transform.position + (_rayDir * springForce), Color.yellow);

        // Apply force to objects beneath
        if (hitBody != null)
        {
            hitBody.AddForceAtPosition(-maintainHeightForce, rayHit.point);
        }
    }

    /// <summary>
    /// Determines the desired y rotation for the character, with account for platform rotation.
    /// </summary>
    /// <param name="yLookAt">The input look rotation.</param>
    /// <param name="rayHit">The rayHit towards the platform.</param>
    private void CalculateTargetRotation(Vector3 yLookAt, RaycastHit rayHit = new RaycastHit())
    {
        if (yLookAt != Vector3.zero)
        {
            _uprightTargetRot = Quaternion.LookRotation(yLookAt, Vector3.up);
            _lastTargetRot = _uprightTargetRot;
        }
        else
        {
            float yAngle = _lastTargetRot.eulerAngles.y;
            _uprightTargetRot = Quaternion.Euler(new Vector3(0f, yAngle, 0f));
        }
    }

    /// <summary>
    /// Adds torque to the character to keep the character upright, acting as a torsional oscillator (i.e. vertically flipped pendulum).
    /// </summary>
    /// <param name="yLookAt">The input look rotation.</param>
    /// <param name="rayHit">The rayHit towards the platform.</param>
    private void MaintainUpright(Vector3 yLookAt, RaycastHit rayHit = new RaycastHit())
    {
        CalculateTargetRotation(yLookAt, rayHit);

        Quaternion currentRot = transform.rotation;
        Quaternion toGoal = MathsUtils.ShortestRotation(_uprightTargetRot, currentRot);

        Vector3 rotAxis;
        float rotDegrees;

        toGoal.ToAngleAxis(out rotDegrees, out rotAxis);
        rotAxis.Normalize();

        float rotRadians = rotDegrees * Mathf.Deg2Rad;

        _rb.AddTorque((rotAxis * (rotRadians * _uprightSpringStrength)) - (_rb.angularVelocity * _uprightSpringDamper));
    }

    /// <summary>
    /// Reads the player movement input.
    /// </summary>
    /// <param name="context">The move input's context.</param>
    public void MoveInputAction(InputAction.CallbackContext context)
    {
        _moveContext = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Apply forces to move the character up to a maximum acceleration, with consideration to acceleration graphs.
    /// </summary>
    /// <param name="moveInput">The player movement input.</param>
    /// <param name="rayHit">The rayHit towards the platform.</param>
    private void CharacterMove(Vector3 moveInput, RaycastHit rayHit)
    {
        Vector3 m_UnitGoal = moveInput;
        Vector3 unitVel = _m_GoalVel.normalized;
        float velDot = Vector3.Dot(m_UnitGoal, unitVel);
        float accel = _acceleration * _accelerationFactorFromDot.Evaluate(velDot);
        Vector3 goalVel = m_UnitGoal * _maxSpeed * _speedFactor;
        Vector3 otherVel = Vector3.zero;
        Rigidbody hitBody = rayHit.rigidbody;
        _m_GoalVel = Vector3.MoveTowards(_m_GoalVel,
                                        goalVel,
                                        accel * Time.fixedDeltaTime);
        Vector3 neededAccel = (_m_GoalVel - _rb.velocity) / Time.fixedDeltaTime;
        float maxAccel = _maxAccelForce * _maxAccelerationForceFactorFromDot.Evaluate(velDot) * _maxAccelForceFactor;
        neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);
        _rb.AddForceAtPosition(Vector3.Scale(neededAccel * _rb.mass, _moveForceScale), transform.position + new Vector3(0f, transform.localScale.y * _leanFactor, 0f)); // Using AddForceAtPosition in order to both move the player and cause the play to lean in the direction of input.
    }
}
