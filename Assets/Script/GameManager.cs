using UnityEngine;
using TMPro;
public class GameManager : MonoBehaviour
{
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
