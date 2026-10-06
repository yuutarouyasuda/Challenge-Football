using UnityEngine;

public class PlayerSwitchManager : MonoBehaviour
{
    [SerializeField] private PlayerController[] fieldPlayers;
    [SerializeField] private PlayerController goalkeeper;
    [SerializeField] private BallController ball;
    [SerializeField] private GameObject cursor;
    [SerializeField] private float cursorHeight = 2f;
    private int currentIndex;
    private PlayerInputActions inputActions;
    private bool switchPressed;

    void Start()
    {
        SetControlPlayer(0);
    }

    // Update is called once per frame
    private bool previousDefence;
    private PlayerController previousOwner;
    void Update()
    {
        bool defence = IsDefence();

        if (!defence)
        {
            PlayerController owner =
                ball.Owner != null ?
                ball.Owner.GetComponent<PlayerController>() :
                null;

            if (owner != null && owner != previousOwner)
            {
                previousOwner = owner;
                ChangeBallOwnerPlayer();
            }

            return;
        }

        previousOwner = null;

        if (switchPressed)
        {
            SwitchPlayer();
            switchPressed = false;
        }
    }
    private void Awake()
    {
        inputActions = new PlayerInputActions();

        inputActions.Player.SwitchPlayer.performed += ctx =>
        {
            switchPressed = true;
        };
    }
    private void OnEnable()
    {
        inputActions.Enable();
    }
    private void OnDisable()
    {
        inputActions.Disable();
    }
    private void SwitchPlayer()
    {
        currentIndex++;

        if(currentIndex>=fieldPlayers.Length)
        {
            currentIndex = 0;
        }
        SetControlPlayer(currentIndex);
    }
    private void SetControlPlayer(int index)
    {
        for (int i = 0; i < fieldPlayers.Length; i++)
        {
            fieldPlayers[i].SetControl(false);
        }
        fieldPlayers[index].SetControl(true);

        currentIndex = index;

        //カーソル移動
        cursor.transform.position = fieldPlayers[index].transform.position+Vector3.up*cursorHeight;
        Debug.Log("操作選手 : "+fieldPlayers[index].name);
    }
    private bool IsDefence()
    {
        if(ball.Owner==null)
        {
            return false;
        }

        //味方がボールを持っている
        if (ball.Owner.GetComponent<PlayerController>() != null||
            ball.Owner.GetComponent<TeammateAI_New>()!=null)
        {
            return false;
        }
        return true;
    }
    private void ChangeBallOwnerPlayer()
    {
        Debug.Log("ChangeBallOwnerPlayer");
        if (ball.Owner == null)
            return;


        PlayerController player =
            ball.Owner.GetComponent<PlayerController>();


        if (player == null)
            return;


        for (int i = 0; i < fieldPlayers.Length; i++)
        {
            if (fieldPlayers[i] == player)
            {
                SetControlPlayer(i);
                return;
            }
        }
    }
}
