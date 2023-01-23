using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerController : MonoBehaviour
{
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask groundLayer;
    public Transform head;
    public Transform orientation;

    public bool isGrounded;
    public bool isMoving;

    [SerializeField]
    private Vector3 _moveInput;
    private Vector3 _lookInput;
    [SerializeField]
    private Vector3 _groundHitNormal;

    private Rigidbody _rb;

    [SerializeField]
    private Vector3 _adjustedVelocity;
    
    [SerializeField]
    private float _speed;
    [SerializeField]
    private float _acceleration;
    [SerializeField]
    private float _deceleration;
    [SerializeField]
    private float _velPower;
    [SerializeField]
    private float _frictionAmount;
    [SerializeField]
    private float _lookSensX;
    [SerializeField]
    private float _lookSensY;
    [SerializeField]
    private float _jumpPower;
    [SerializeField]
    private float _airSpeed;
    [SerializeField]
    private float _gravity;
    [SerializeField]
    private float _dampingAmount;
    [SerializeField]
    private float _maxWallRunTime;
    [SerializeField]
    private float _minWallRunStartHeight;
    [SerializeField]
    private float _wallRunSpeedThreshold;
    [SerializeField]
    private float _wallRunCameraTilt;
    [SerializeField]
    private float _wallRunBaseSpeed;
    [SerializeField]
    private float _cameraTiltSpeed;
    [SerializeField]
    private float _wallRunCooldown;
    [SerializeField]
    private float _wallJumpHeight;
    [SerializeField]
    private float _wallJumpDistance;
    [SerializeField]
    private float _doubleJumpPower;
    [SerializeField]
    private float _bunnyHopWindow;
    [SerializeField]
    private float _groundCheckDistance;
    [SerializeField]
    private float _rampSlideSpeedThreshold;
    [SerializeField]
    private float _topSpeed;

    private float _xRotation;
    private float _yRotation;
    private float _xDampingMultiplier;
    private float _zDampingMultiplier;
    private float _wallRunTimer;
    private float _zRot;
    private float _lastWallDismountTime;
    private float _groundedTimer;
    private float _wallRunSpeed;

    private bool _jumpKeyHeld;
    private bool _touchingLeftWall;
    private bool _touchingRightWall;
    private bool _touchingLevelGeom;
    private bool _highEnoughToWallrun;
    private bool _wallRunning;
    private bool _canDoubleJump;

    private RaycastHit _rightWallHit;
    private RaycastHit _leftWallhit;
    private RaycastHit _groundHit;


    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _rb = GetComponent<Rigidbody>();
        _lastWallDismountTime = -_wallRunCooldown;
        _groundedTimer = 0;
    }

    private void Update()
    {
        #region CAMERA TILT WHEN WALL RUNNING
        if (_wallRunning)
        {
            if (_touchingLeftWall)
            {
                if (_zRot > -_wallRunCameraTilt)
                {
                    _zRot -= _cameraTiltSpeed * Time.deltaTime;
                }
            }
            else
            {
                if (_zRot < _wallRunCameraTilt)
                {
                    _zRot += _cameraTiltSpeed * Time.deltaTime;
                }
            }
        }
        else
        {
            if (_zRot < -0.5)
            {
                _zRot += _cameraTiltSpeed * Time.deltaTime;
            }
            else if (_zRot > 0.5)
            {
                _zRot -= _cameraTiltSpeed * Time.deltaTime;
            }
            else if (_zRot >= -0.5 && _zRot <= 0.5)
            {
                _zRot = 0;
            }
        }
        #endregion

        #region LOOKING AROUND
        _yRotation += _lookInput.x * Time.deltaTime * _lookSensX;
        _xRotation -= _lookInput.z * Time.deltaTime * _lookSensY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        head.rotation = Quaternion.Euler(_xRotation, _yRotation, _zRot);
        orientation.rotation = Quaternion.Euler(0, _yRotation, 0);
        #endregion
    }
    void FixedUpdate()
    {
        float moveInputAsFloat = Mathf.Sqrt(_moveInput.x * _moveInput.x + _moveInput.z * _moveInput.z);
        float velocityAsFloat = Mathf.Sqrt(_rb.velocity.x * _rb.velocity.x + _rb.velocity.z * _rb.velocity.z);

        Vector3 moveDirection = orientation.forward * _moveInput.z * _zDampingMultiplier + orientation.right * _moveInput.x * _xDampingMultiplier;

        float adjustedVelocityZ = Vector3.Dot(_rb.velocity, orientation.forward);
        float adjustedVelocityX = Vector3.Dot(_rb.velocity, orientation.right);
        _adjustedVelocity = new Vector3(adjustedVelocityX, 0, adjustedVelocityZ);

        #region ACCELERATION
        float targetSpeed = moveInputAsFloat * _speed;
        float speedDiff = targetSpeed - velocityAsFloat;
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? _acceleration : _deceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, _velPower) * Mathf.Sign(speedDiff);
        #endregion

        #region GROUND MOVEMENT
        if (isGrounded == true)
        {
            _rb.AddForce(Vector3.Cross(Quaternion.AngleAxis(90, Vector3.up) * moveDirection.normalized, _groundHitNormal) * movement);
        }
        #endregion

        #region AIR MOVEMENT
        if(isGrounded == false && _wallRunning == false)
        {
            _rb.AddForce(moveDirection * _airSpeed);
            _rb.AddForce(Vector3.down * _gravity);
        }
        #endregion

        #region CHANGING DIRECTION IN AIR
        if(isGrounded == false)
        {
            if(_adjustedVelocity.x > 0)
            {
                if(_moveInput.x < 0)
                {
                    _xDampingMultiplier = 1 + _dampingAmount;
                }
                else
                {
                    _xDampingMultiplier = 1;
                }
            }
            else if(_adjustedVelocity.x < 0)
            {
                if(_moveInput.x > 0)
                {
                    _xDampingMultiplier = 1 + _dampingAmount;
                }
                else
                {
                    _xDampingMultiplier = 1;
                }
            }
            else
            {
                _xDampingMultiplier = 1;
            }
            if(_adjustedVelocity.z > 0)
            {
                if(_moveInput.z < 0)
                {
                    _zDampingMultiplier = 1 + _dampingAmount;
                }
                else
                {
                    _zDampingMultiplier = 1;
                }
            }
            else if(_adjustedVelocity.z < 0)
            {
                if(_moveInput.z > 0)
                {
                    _zDampingMultiplier = 1 + _dampingAmount;
                }
                else
                {
                    _zDampingMultiplier = 1;
                }
            }
            else
            {
                _zDampingMultiplier = 1;
            }
        }
        else
        {
            _xDampingMultiplier = 1;
            _zDampingMultiplier = 1;
        }
        #endregion

        #region GROUND DETECTION
        if (Physics.Raycast(groundCheck.position, Vector3.down, _groundCheckDistance, groundLayer) && _touchingLevelGeom == true)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
        #endregion

        #region SLOPE DETECTION
        Physics.Raycast(groundCheck.position, Vector3.down, out _groundHit, _groundCheckDistance, groundLayer);

        _groundHitNormal = _groundHit.normal;
        #endregion

        #region FRICTION
        if (!isGrounded)
        {
            _groundedTimer = _bunnyHopWindow;
        }
        else if(isGrounded)
        {
            _groundedTimer -= Time.deltaTime;
        }
        
        if(_wallRunning)
        {
            _rb.drag = _frictionAmount;
        }
        else if(_groundedTimer <= 0)
        {
            if(velocityAsFloat > _rampSlideSpeedThreshold && _groundHitNormal.y < 1)
            {
                _rb.drag = 0;
            }
            else
            {
                _rb.drag = _frictionAmount;
            }
        }
        else
        {
            _rb.drag = 0;
        }
        #endregion

        #region JUMPING
        if (_jumpKeyHeld)
        {
            if(isGrounded)
            {
                _rb.AddForce(_jumpPower * Vector3.up, ForceMode.Impulse);
            }
        }

        if (isGrounded || _wallRunning)
        {
            _canDoubleJump = true;
        }
        #endregion

        #region WALL RUNNING

        _touchingLeftWall = Physics.Raycast(wallCheck.position, -orientation.right, out _leftWallhit, 1f, groundLayer);
        _touchingRightWall = Physics.Raycast(wallCheck.position, orientation.right, out _rightWallHit, 1f, groundLayer);

        _highEnoughToWallrun = !Physics.Raycast(groundCheck.position, Vector3.down, _minWallRunStartHeight, groundLayer);

        if((_touchingLeftWall || _touchingRightWall) && _moveInput.z > 0 && _highEnoughToWallrun == true)
        {
            if(!_wallRunning && (Time.time > _lastWallDismountTime + _wallRunCooldown))
            {
                StartWallRun();
            }
        }

        if(_wallRunning)
        {
            
            _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
            Vector3 wallNormal = _touchingRightWall ? _rightWallHit.normal : _leftWallhit.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
            if(_touchingLeftWall)
            {
                wallForward *= 1;
                if(_moveInput.x > 0)
                {
                    StopWallRun();
                }
            }
            else
            {
                wallForward *= -1;
                if (_moveInput.x < 0)
                {
                    StopWallRun();
                }
            }

            if (_moveInput.z > 0)
            {
                _rb.AddForce(wallForward * _wallRunSpeed);
            }
            
            if(_adjustedVelocity.z < _wallRunSpeedThreshold)
            {
                StopWallRun();
            }
        }
        #endregion

        #region SPEED LIMIT

        if (velocityAsFloat > _topSpeed)
        {
            //_rb.AddForce()
        }
        #endregion
    }

    public void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer == 3)
        {
            _touchingLevelGeom = true;
        }
    }

    public void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == 3)
        {
            _touchingLevelGeom = true;
        }
    }

    public void OnCollisionExit(Collision collision)
    {
        if(collision.gameObject.layer == 3)
        {
            _touchingLevelGeom = false;
        }
    }

    public void Movement(InputAction.CallbackContext context)
    {
        _moveInput = new Vector3(context.ReadValue<Vector2>().x, 0, context.ReadValue<Vector2>().y);
        if(_moveInput != Vector3.zero)
        {
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
    }

    public void Look(InputAction.CallbackContext context)
    {
        _lookInput = new Vector3(context.ReadValue<Vector2>().x, 0, context.ReadValue<Vector2>().y);
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _jumpKeyHeld = true;
        }
        else
        {
            _jumpKeyHeld = false;
        }

        if(_wallRunning && context.started)
        {
            WallJump();
        }

        if(!isGrounded && !_wallRunning && _canDoubleJump && context.started && (Time.time > _lastWallDismountTime + _wallRunCooldown))
        {
            _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
            _rb.AddForce((_doubleJumpPower) * Vector3.up, ForceMode.Impulse);
            _canDoubleJump = false;
        }
    }

    public void StartWallRun()
    {
        //Debug.Log("Start");
        _wallRunning = true;
        if(_adjustedVelocity.z < 14.9)
        {
            _wallRunSpeed = _wallRunBaseSpeed;
        }
        else
        {
            _wallRunSpeed = _adjustedVelocity.z * 13.5f;
        }
    }
    
    public void StopWallRun()
    {
        //Debug.Log("Stop");
        _wallRunning = false;
        _lastWallDismountTime = Time.time;
    }

    public void WallJump()
    {
        StopWallRun();
        _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        if(_touchingLeftWall)
        {
            _rb.AddForce(_wallJumpHeight * Vector3.up + _wallJumpDistance * _leftWallhit.normal, ForceMode.Impulse);
        }
        else if(_touchingRightWall)
        {
            _rb.AddForce(_wallJumpHeight * Vector3.up + _wallJumpDistance * _rightWallHit.normal, ForceMode.Impulse);
        }
    }
}
