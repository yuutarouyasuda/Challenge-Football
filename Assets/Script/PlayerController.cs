using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeed = 8f;
    [Header("Pass")]
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float maxPassPower = 15f;
    [SerializeField] private float chargeSpeed = 10f;
    [Header("Shoot")]
    [SerializeField] private float maxShootPower = 30f;
    [SerializeField] private float shootChargeSpeed = 20f;
    [SerializeField] private float pickupCooldown = 0.3f;

    private float shootPower;
    private bool chargingShoot;
    private float pickupTimer = 0f;
    private float passPower;
    private bool chargingPass;
    private Rigidbody rb;
    private BallController currentBall;
    private PlayerInputActions inputActions;

    private Vector2 moveInput;
    private bool isDash;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;

        inputActions.Player.Dash.performed += OnDash;
        inputActions.Player.Dash.canceled += OnDash;

        inputActions.Player.Pass.started += OnPassStarted;
        inputActions.Player.Pass.canceled += OnPassCanceled;

        inputActions.Player.Shoot.started += OnShootStarted;
        inputActions.Player.Shoot.canceled += OnShootCanceled;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;

        inputActions.Player.Dash.performed -= OnDash;
        inputActions.Player.Dash.canceled -= OnDash;

        inputActions.Player.Pass.started -= OnPassStarted;
        inputActions.Player.Pass.canceled -= OnPassCanceled;

        inputActions.Player.Shoot.started -= OnShootStarted;
        inputActions.Player.Shoot.canceled -= OnShootCanceled;

        inputActions.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        isDash = context.ReadValueAsButton();
    }
    private void Update()
    {
        if(chargingPass )
        {
            passPower += chargeSpeed * Time.deltaTime;
            passPower=Mathf.Clamp(passPower,5f,maxPassPower);
        }
        if(pickupTimer>0)
        {
            pickupTimer-=Time.deltaTime;
        }
        if(chargingShoot)
        {
            shootPower += shootChargeSpeed * Time.deltaTime;
            shootPower=Mathf.Clamp(shootPower,10f,maxShootPower);
        }
    }
    private void FixedUpdate()
    {
        float speed = isDash ? dashSpeed : moveSpeed;

        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);

        Vector3 velocity = move * speed;
        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;

        if (move != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);

            rb.MoveRotation(
                Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    rotateSpeed * Time.fixedDeltaTime));
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (pickupTimer > 0)
            return;

        BallController ball =collision.gameObject.GetComponent<BallController>();

        if (ball == null) return;

        //ëºÇÃëIéËÇ™éùÇ¡ÇƒÇ¢Ç»ÇØÇÍÇŒï€éù
        if(ball.Owner == null)
        {
            ball.SetOwner(this);
            currentBall = ball;
        }
    }
    private void OnPassStarted(InputAction.CallbackContext ctx)
    {
        chargingPass = true;
        passPower = 0;
    }

    private void OnPassCanceled(InputAction.CallbackContext ctx)
    {
        chargingPass = false;

        if(currentBall==null) return;

        currentBall.Kick(transform.forward, passPower);
        currentBall = null;

        pickupTimer = pickupCooldown;
    }
    private void OnShootStarted(InputAction.CallbackContext ctx)
    {
        chargingShoot = true;
        shootPower = 0f;
    }

    private void OnShootCanceled(InputAction.CallbackContext ctx)
    {
        chargingShoot = false;

        if (currentBall == null)
            return;

        currentBall.Kick(transform.forward, shootPower);

        currentBall = null;

        pickupTimer = pickupCooldown;
    }
}