using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    [SerializeField] private Slider groundKickSlider;
    [SerializeField] private Slider lobKickSlider;
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
    private Vector2 moveInput;
    private bool isDash;
    public bool IsControlled = false;
    private TeammateAI_New teammateAI;
    private NavMeshAgent agent;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        teammateAI = GetComponent<TeammateAI_New>();
        agent = GetComponent<NavMeshAgent>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        inputActions = new PlayerInputActions();


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
    public float MoveSpeed
    {
        get
        {
            if (playerData == null)
                return moveSpeed;

            return playerData.speed;
        }
    }

    public float KickPower
    {
        get
        {
            if (playerData == null)
                return maxGroundKickPower;

            return playerData.kick;
        }
    }
    public float DashSpeed
    {
        get
        {
            if (playerData == null)
                return dashSpeed;

            return playerData.speed * 1.5f;
        }
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
                currentBall.Owner != GetComponent<TeammateAI_New>())
            {
                currentBall = null;
            }
        }
        if (chargingGroundKick)
        {
            groundKickPower += GroundKickchargeSpeed * Time.deltaTime;
            groundKickPower = Mathf.Clamp(groundKickPower, 5f, KickPower);

            groundKickSlider.value = groundKickPower / KickPower;
        }
        if (pickupTimer > 0)
        {
            pickupTimer -= Time.deltaTime;
        }
        if (chargingLobKick)
        {
            lobKickPower += LobKickChargeSpeed * Time.deltaTime;
            lobKickPower = Mathf.Clamp(lobKickPower, 10f, maxLobKickPower);

            lobKickSlider.value = lobKickPower / maxLobKickPower;
        }
    }
    private void FixedUpdate()
    {
        if (!IsControlled)
            return;
        Debug.Log(moveInput);

        float speed = isDash ? DashSpeed : MoveSpeed*0.8f;

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
    private PlayerData playerData;
    private PlayerData PlayerData => playerData;
    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
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
    public void GroundPass(Vector3 dir, float power)
    {

        if (currentBall == null)
        {
            return;
        }

        transform.forward = dir;

        currentBall.Kick(dir, power, false);

        currentBall = null;
        if(GameManager.Instance.IsKickOff)
        {
            GameManager.Instance.EndKickOffState();
        }
        GoalkeeperAI gk=GetComponent<GoalkeeperAI>();
        if(gk != null)
        {
            gk.enabled = true;
        }
        pickupTimer = pickupCooldown;
    }
    public void LobPass(Vector3 dir, float power)
    {
        if (currentBall == null)
            return;

        dir.y = 0.5f;

        currentBall.Kick(dir, power, true);
       
        currentBall = null;
        if (GameManager.Instance.IsKickOff)
        {
            GameManager.Instance.EndKickOffState();
        }
        GoalkeeperAI gk = GetComponent<GoalkeeperAI>();
        if (gk != null)
        {
            gk.enabled = true;
        }
        pickupTimer = pickupCooldown;
    }
    private void OnGroundKickStarted(InputAction.CallbackContext ctx)
    {
        chargingGroundKick = true;
        groundKickPower = 0;
    }

    private void OnGroundKickCanceled(InputAction.CallbackContext ctx)
    {
        chargingGroundKick = false;

        groundKickSlider.value = 0;
        if (currentBall == null)
            return;

        Vector3 dir = GetMouseDirection();

        // ‘Sˆõ‚ÌŽó‚¯Žèƒtƒ‰ƒO‚ðOFF
        TeammateAI_New[] mates =
            FindObjectsByType<TeammateAI_New>(FindObjectsSortMode.None);

        foreach (TeammateAI_New mate in mates)
        {
            mate.isReceiver = false;
        }

        // ˆê”Ô‹ß‚¢–¡•û‚ðŽó‚¯Žè‚É‚·‚é
        float best = float.MaxValue;
        TeammateAI_New receiver = null;

        foreach (TeammateAI_New mate in mates)
        {
            if (mate.GetComponent<PlayerController>() == this)
                continue;

            float d = Vector3.Distance(
                transform.position,
                mate.transform.position);

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

        GroundPass(dir, groundKickPower);
    }
    private void OnLobKickStarted(InputAction.CallbackContext ctx)
    {
        chargingLobKick = true;
        lobKickPower = 0;
    }

    private void OnLobKickCanceled(InputAction.CallbackContext ctx)
    {
        chargingLobKick = false;
        lobKickSlider.value = 0;
        LobPass(GetMouseDirection(), lobKickPower);
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

    public void SetControl(bool value)
    {
        Debug.Log(name + " SetControl : " + value);

        IsControlled = value;

        if (value)
            EnableInput();
        else
            DisableInput();

        if (teammateAI != null)
            teammateAI.enabled = !value;

        if (agent != null)
            agent.enabled = !value;
    }

}