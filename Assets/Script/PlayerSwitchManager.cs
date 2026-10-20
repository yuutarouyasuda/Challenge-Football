using UnityEngine;
using UnityEngine.UIElements;

public class PlayerSwitchManager : MonoBehaviour
{
    [SerializeField] private PlayerController[] fieldPlayers;
    [SerializeField] private PlayerController goalkeeper;
    [SerializeField] private BallController ball;
    [SerializeField] private GameObject cursor;
    [SerializeField] private float cursorHeight = 2f;
    [SerializeField] private GameObject nextCursor;
    private int currentIndex;
    private PlayerController currentCursorPlayer;
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
        }
        else
        {
            previousOwner = null;

            if (switchPressed)
            {
                SwitchPlayer();
                switchPressed = false;
            }
        }

        if (currentCursorPlayer != null)
        {
            cursor.transform.position =
                currentCursorPlayer.transform.position +
                Vector3.up * cursorHeight;
        }

        int nextIndex = GetNearestPlayerIndex();

        nextCursor.transform.position =
            fieldPlayers[nextIndex].transform.position +
            Vector3.up * cursorHeight;
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
        int nextIndex = GetNearestPlayerIndex();
        SetControlPlayer(nextIndex);
    }
    private void SetControlPlayer(int index)
    {
        GameManager.Instance.ChangePlayer(fieldPlayers[index]);

        currentIndex = index;
        currentCursorPlayer = fieldPlayers[index];

        cursor.transform.position =
            currentCursorPlayer.transform.position +
            Vector3.up * cursorHeight;
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


        // キーパーの場合
        if (player == goalkeeper)
        {
            SetGoalkeeperControl();
            return;
        }


        // フィールド選手の場合
        for (int i = 0; i < fieldPlayers.Length; i++)
        {
            if (fieldPlayers[i] == player)
            {
                SetControlPlayer(i);
                return;
            }
        }

        Debug.Log("fieldPlayersに見つからない : " + player.name);
    }
    private void SetGoalkeeperControl()
    {
        GameManager.Instance.ChangePlayer(goalkeeper);

        currentCursorPlayer = goalkeeper;

        cursor.transform.position =
            goalkeeper.transform.position +
            Vector3.up * cursorHeight;

        Debug.Log("操作選手 : Goalkeeper");
    }
    private int GetNearestPlayerIndex()
    {
        float nearestDistance = Mathf.Infinity;
        int nearestIndex = currentIndex;

        for (int i = 0; i < fieldPlayers.Length; i++)
        {
            // 今操作している選手は除外
            if (i == currentIndex)
                continue;

            float distance = Vector3.Distance(
                fieldPlayers[i].transform.position,
                ball.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }
    public void SetCurrentPlayer(PlayerController player)
    {
        currentCursorPlayer = player;

        if (player == goalkeeper)
        {
            return;
        }

        for (int i = 0; i < fieldPlayers.Length; i++)
        {
            if (fieldPlayers[i] == player)
            {
                currentIndex = i;
                return;
            }
        }
    }
}
