using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private CapsuleCollider _collider;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Transform _orientation;
    
    [Header("Ground Check Settings")]
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float extraDistanceGroundCheck;
    [SerializeField] private bool isGrounded;
    [SerializeField] private float lastTimeGrounded;
    public bool IsGrounded(float coyoteTime) => isGrounded || Time.deltaTime - lastTimeGrounded <= coyoteTime;
    private RaycastHit _groundHit;
    
    [Header("Slope Check Settings")]
    [SerializeField] private float extraDistanceSlopeCheck;
    [SerializeField] private bool onSlope;
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private bool exitingSlope;
    [SerializeField] private float correctionForce;
    [SerializeField] private float changeTerrainCorrectionForce;
    [SerializeField] private float baseTerrainAngle;
    [SerializeField] private Vector3 previousSlopeNormal;
    public bool OnSlope => onSlope && !exitingSlope;
    private RaycastHit _slopeHit;
    
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float groundFriction;
    [SerializeField] private float airFriction;
    [SerializeField] private Vector3 cachedMoveDirection;
    public Vector3 MoveDirection(Vector3 moveDirection) => _orientation.right * moveDirection.x + _orientation.forward * moveDirection.z;
    public bool IsTurningInDirection(Vector3 direction) => Vector3.Dot(_rigidbody.linearVelocity,  direction) * Vector3.Dot(MoveDirection(cachedMoveDirection), direction) <= 0;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float jumpCooldownTimer;
    [SerializeField] private float coyoteJumpTime;
    [SerializeField] private float fallGravityMultiplier;
    [SerializeField] private float lowJumpGravityMultiplier;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchScaleMultiplier;
    [SerializeField] private float coyoteCrouchTime;
    [SerializeField] private Vector3 crouchScale;
    [SerializeField] private Vector3 initialScale;
    [SerializeField] private float crouchCooldown;
    [SerializeField] private float crouchCooldownTimer;
    
    [Header("Sprint Settings")]
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float speedSmoothingThreshold;
    [SerializeField] private float speedSmoothing;
    [SerializeField] private float currentSpeed;

    private void Start()
    {
        currentSpeed = maxSpeed;
        previousSlopeNormal = Vector3.up;
    }

    private void OnMove(InputValue value)
    {
        Vector2 moveDirection = value.Get<Vector2>();
        cachedMoveDirection = new Vector3(moveDirection.x, 0f, moveDirection.y);
    }

    private void Update()
    {
        GroundCheck();
        SlopeCheck();
        ConditionalGravity();
    }

    private void FixedUpdate()
    {
        Move();
        LimitSpeed();
        Friction();
    }

    private void GroundCheck()
    {
        float halfHeight = _collider.bounds.extents.y;
        Vector3 footBox = new Vector3(_collider.bounds.extents.x, extraDistanceGroundCheck, _collider.bounds.extents.z);
        isGrounded = Physics.BoxCast(_playerTransform.position, footBox, Vector3.down, out _groundHit, Quaternion.identity, halfHeight, whatIsGround);
        if (isGrounded)
        {
            lastTimeGrounded = Time.time;
        }
    }

    private void SlopeCheck()
    {
        float halfHeight = _collider.bounds.extents.y;
        Vector3 footBox = new Vector3(_collider.bounds.extents.x, extraDistanceSlopeCheck, _collider.bounds.extents.z);
        if (Physics.BoxCast(_playerTransform.position, footBox, Vector3.down, out _slopeHit, Quaternion.identity, halfHeight, whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            onSlope = angle <= maxSlopeAngle && angle != 0f;
        }
        else
        {
            onSlope = false;
            previousSlopeNormal = Vector3.up;
        }

        TurnMovement();
        if(_slopeHit.normal != Vector3.zero) previousSlopeNormal = _slopeHit.normal;
    }

    private Vector3 ProjectOnTerrain(Vector3 direction)
    {
        if(!OnSlope) return direction;
        return Vector3.ProjectOnPlane(direction, _slopeHit.normal);
    }

    private void Move()
    {
        Vector3 moveDirection = ProjectOnTerrain(MoveDirection(cachedMoveDirection));
        Vector3 correction = Vector3.zero;
        if (OnSlope)
        {
            correction = -_slopeHit.normal * correctionForce; //TODO Evaluate this implementation of correction force on slopes
        }
        _rigidbody.AddForce(moveDirection.normalized * acceleration + correction);
    }

    private void LimitSpeed()
    {
        Vector3 planarSpeed = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        float speedSquareMagnitude = OnSlope ? _rigidbody.linearVelocity.sqrMagnitude : planarSpeed.sqrMagnitude;
        if (speedSquareMagnitude > currentSpeed * currentSpeed)
        {
            if(OnSlope) _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * currentSpeed;
            else _rigidbody.linearVelocity = planarSpeed.normalized * currentSpeed + Vector3.up * _rigidbody.linearVelocity.y;
        }
    }

    private void Friction()
    {
        float friction = isGrounded ? groundFriction : airFriction;
        AddFrictionAlongDirection(_orientation.right, friction);
        AddFrictionAlongDirection(_orientation.forward, friction);
    }

    private void AddFrictionAlongDirection(Vector3 direction, float friction)
    {
        if (!IsTurningInDirection(direction)) return; //TODO Evaluate this implementation of turning direction check function
        
        Vector3 normalizedDirection = direction.normalized;
        float speedAlongDirection = Vector3.Dot(_rigidbody.linearVelocity, normalizedDirection);
        _rigidbody.AddForce(-normalizedDirection * (speedAlongDirection * friction * Time.fixedDeltaTime), ForceMode.VelocityChange);
    }

    private void ConditionalGravity()
    {
        _rigidbody.useGravity = !OnSlope;
    }

    private void TurnMovement()
    {
        if (previousSlopeNormal == _slopeHit.normal) return;
        
        Debug.Log($"Change in normal: {previousSlopeNormal} to {_slopeHit.normal}");
        
        Vector3 velocity = _rigidbody.linearVelocity;
        float turnSign = Mathf.Sign(DirectionAngle(previousSlopeNormal, _slopeHit.normal));
        float velocitySign = Mathf.Sign(velocity.x) * Mathf.Sign(velocity.z);
        float snap = Vector3.Angle(previousSlopeNormal, _slopeHit.normal) * changeTerrainCorrectionForce / baseTerrainAngle; 
        Vector2 force = Vector2.up * (velocitySign * turnSign * snap);
        _rigidbody.AddForce(force, ForceMode.VelocityChange);
        Debug.Log($"Correction Force Applied: {force}");
    }

    private float DirectionAngle(Vector2 from, Vector2 to)
    {
        float angleFrom = -Vector2.SignedAngle(Vector2.up, from);
        float angleTo = -Vector2.SignedAngle(Vector2.up, to);
        return angleTo - angleFrom;
    }
}