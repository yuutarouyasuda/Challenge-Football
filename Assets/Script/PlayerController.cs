using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeed = 8f;
    [Header("Gorund Kick")]
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float maxGroundKickPower = 15f;
    [SerializeField] private float GroundKickchargeSpeed = 10f;
    [Header("Lob Kick")]
    [SerializeField] private float maxLobKickPower = 30f;
    [SerializeField] private float LobKickChargeSpeed = 20f;
    [SerializeField] private float pickupCooldown = 0.3f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float pickupTimer = 0f;
    private float groundKickPower;
    private bool chargingGroundKick;

    private float lobKickPower;
    private bool chargingLobKick;
    private Rigidbody rb;
    private BallController currentBall;
    private PlayerInputActions inputActions;
    private TeammateAI teammateAI;
    private Vector2 moveInput;
    private bool isDash;

    public bool IsControlled = false;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        inputActions = new PlayerInputActions();

        teammateAI = GetComponent<TeammateAI>();

        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;

        inputActions.Player.Dash.performed += OnDash;
        inputActions.Player.Dash.canceled += OnDash;

        inputActions.Player.GroundKick.started += OnGroundKickStarted;
        inputActions.Player.GroundKick.canceled += OnGroundKickCanceled;

        inputActions.Player.LobKick.started += OnLobKickStarted;
        inputActions.Player.LobKick.canceled += OnLobKickCanceled;
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }
    public void EnableInput()
    {
        inputActions.Enable();
    }
    public void DisableInput()
    {
        inputActions.Disable();

        moveInput = Vector2.zero;
        isDash = false;

        chargingGroundKick = false;
        chargingLobKick = false;
    }
    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        isDash = context.ReadValueAsButton();
    }
    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }
    private void Update()
    {
        if (!IsControlled) return;
        if (currentBall != null)
        {
            if (currentBall.Owner != this &&
                currentBall.Owner != GetComponent<TeammateAI>())
            {
                currentBall = null;
            }
        }
        if (chargingGroundKick)
        {
            groundKickPower += GroundKickchargeSpeed * Time.deltaTime;
            groundKickPower = Mathf.Clamp(groundKickPower, 5f, maxGroundKickPower);
        }
        if (pickupTimer > 0)
        {
            pickupTimer -= Time.deltaTime;
        }
        if (chargingLobKick)
        {
            lobKickPower += LobKickChargeSpeed * Time.deltaTime;
            lobKickPower = Mathf.Clamp(lobKickPower, 10f, maxLobKickPower);
        }
    }
    private void FixedUpdate()
    {
        if (!IsControlled) return;

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
        {
            return;
        }

        BallController ball = collision.gameObject.GetComponent<BallController>();

        if (ball == null)
        {
            return;
        }


        if (ball.CanSteal && ball.Owner != this)
        {
            ball.SetOwner(this);
            currentBall = ball;
        }
    }
    private Vector3 GetMouseDirection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 dir = hit.point - transform.position;
            dir.y = 0;
            return dir.normalized;
        }

        return transform.forward;
    }
    private void OnGroundKickStarted(InputAction.CallbackContext ctx)
    {
        chargingGroundKick = true;
        groundKickPower = 0;
    }

    private void OnGroundKickCanceled(InputAction.CallbackContext ctx)
    {
        if (currentBall == null)
            return;
        Vector3 dir = GetMouseDirection();

        transform.forward = dir;

        TeammateAI[] mates = FindObjectsByType<TeammateAI>(FindObjectsSortMode.None);

        float best = float.MaxValue;
        TeammateAI receiver = null;

        foreach (TeammateAI mate in mates)
        {
            mate.isReceiver = false;

            float d = Vector3.Distance(mate.transform.position, transform.position);

            if (d < best)
            {
                best = d;
                receiver = mate;
            }
        }

        if (receiver != null)
        {
            receiver.isReceiver = true;
        }
        currentBall.Kick(dir, groundKickPower, false);

        currentBall = null;
        pickupTimer = pickupCooldown;
    }
    private void OnLobKickStarted(InputAction.CallbackContext ctx)
    {
        chargingLobKick = true;
        lobKickPower = 0;
    }

    private void OnLobKickCanceled(InputAction.CallbackContext ctx)
    {
        chargingLobKick = false;

        if (currentBall == null)
            return;

        Vector3 dir = GetMouseDirection();
        dir.y = 0.5f;

        currentBall.Kick(dir, lobKickPower, true);

        currentBall = null;
        pickupTimer = pickupCooldown;
    }
    public void ResetPosition()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = startPosition;
        transform.rotation = startRotation;
    }
    public void SetCurrentBall(BallController ball)
    {
        currentBall = ball;
    }
}