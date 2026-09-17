using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using Unity.Multiplayer.PlayMode;
using UnityEngine.AI;
public class GameManager : MonoBehaviour
{
    public PlayerController CurrentPlayer {  get; private set; }

    public static GameManager Instance;
    [Header("Score")]
    public int homeScore;
    public int awayScore;

    [Header("Match")]
    public float matchTime = 300f;

    [SerializeField] private Transform ballSpawnPoint;

    [SerializeField] private TMP_Text homeScoreText;
    [SerializeField] private TMP_Text awayScoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private PlayerController firstPlayer;
    private float currentTime;

    private void Awake()
    {
        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        currentTime = matchTime;

        UpdateTimerUI();
        UpdateScoreUI();

        ChangePlayer(firstPlayer);
    }

    // Update is called once per frame
    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerUI();
        }
        else
        {
            currentTime = 0;
            UpdateTimerUI();
            EndMatch();
        }
    }
    public void ChangePlayer(PlayerController player)
    {
        if (CurrentPlayer != null)
        {
            CurrentPlayer.IsControlled = false;
            CurrentPlayer.DisableInput();
            CurrentPlayer.IsControlled = false;

            NavMeshAgent oldAgent = CurrentPlayer.GetComponent<NavMeshAgent>();
            if (oldAgent != null)
                oldAgent.enabled = true;

            TeammateAI oldAI = CurrentPlayer.GetComponent<TeammateAI>();
            if (oldAI != null)
                oldAI.enabled = true;
        }

        CurrentPlayer = player;
        CurrentPlayer.IsControlled = true;
        CurrentPlayer.EnableInput();

        NavMeshAgent newAgent = CurrentPlayer.GetComponent<NavMeshAgent>();
        if (newAgent != null)
            newAgent.enabled = false;

        TeammateAI newAI = CurrentPlayer.GetComponent<TeammateAI>();
        if (newAI != null)
            newAI.enabled = false;
    }
    public void Goal(bool homeGoal, Rigidbody ballRb)
    {
        if (homeGoal)
            awayScore++;
        else
            homeScore++;

        UpdateScoreUI();
        Debug.Log($"{homeScore}-{awayScore}");

        //ボールを中央へ戻す
        ballRb.transform.position = ballSpawnPoint.position;
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        //プレイヤーを初期位置へ戻す
        PlayerController[] Players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in Players)
        {
            player.ResetPosition();
        }
    }
    public void OnBallOwnerChanged(MonoBehaviour owner)
    {
        PlayerController player = owner.GetComponent<PlayerController>();

        if (player != null)
        {
            ChangePlayer(player);
        }
    }
    private void UpdateTimerUI()
    {
        int minutes=Mathf.FloorToInt(currentTime/60);
        int seconds=Mathf.FloorToInt(currentTime%60);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }
    private void UpdateScoreUI()
    {
        homeScoreText.text = $"Home : {homeScore}";
        awayScoreText.text = $"Away : {awayScore}";
    }
    private void EndMatch()
    {
        Debug.Log("試合終了");
    }
}
