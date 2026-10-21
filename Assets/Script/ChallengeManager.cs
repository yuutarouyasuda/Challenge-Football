using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class ChallengeManager : MonoBehaviour
{
   public static ChallengeManager Instance;

    [SerializeField] private TextMeshProUGUI challengeText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private int minPassCount = 2;
    [SerializeField] private int maxPassCount = 5;

    [SerializeField] private float minDribbleDistance = 5f;
    [SerializeField] private float maxDribbleDistance = 15f;

    private int targetPassCount;

    private ChallengeType currentChallenge;

    private float targetdribbleDistance = 10f;

    // 味方
    private int homeCurrentPassCount;
    private float homeDribbleDistance;
    private bool homeDribbled;
    private bool homePassed;
    private bool homePassStarted;

    // 敵
    private int awayCurrentPassCount;
    private float awayDribbleDistance;
    private bool awayDribbled;
    private bool awayPassed;
    private bool awayPassStarted;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartChallenge();
    }
    private void StartChallenge()
    {
        // 味方
        homeDribbled = false;
        homePassed = false;
        homePassStarted = false;
        homeDribbleDistance = 0f;
        homeCurrentPassCount = 0;

        // 敵
        awayDribbled = false;
        awayPassed = false;
        awayPassStarted = false;
        awayDribbleDistance = 0f;
        awayCurrentPassCount = 0;


        currentChallenge = ((ChallengeType)Random.Range(0, 2));

        if(currentChallenge==ChallengeType.DribbleGoal)
        {
            targetdribbleDistance=Random.Range(minDribbleDistance,maxDribbleDistance);
        }
        else
        {
            targetPassCount=Random.Range(minPassCount,maxPassCount);
        }
            Debug.Log("今回のチャレンジ : " + currentChallenge);

        UpdateChallengeUI();
        UpdateProgressUI();
    }

   

    public int GetGoalPoint(bool homeGoal)
    {
        int point = 1;   // 普通のゴールは1点

        switch (currentChallenge)
        {
            case ChallengeType.DribbleGoal:

                if (homeGoal) // 敵が得点
                {
                    if (awayDribbled)
                    {
                        point = 2;
                        Debug.Log("敵チャレンジ成功");
                    }
                }
                else // 味方が得点
                {
                    if (homeDribbled)
                    {
                        point = 2;
                        Debug.Log("味方チャレンジ成功");
                    }
                }

                break;

            case ChallengeType.PassGoal:

                if (homeGoal)
                {
                    if (awayPassed)
                    {
                        point = 3;
                        Debug.Log("敵チャレンジ成功");
                    }
                }
                else
                {
                    if (homePassed)
                    {
                        point = 3;
                        Debug.Log("味方チャレンジ成功");
                    }
                }

                break;
        }

        StartChallenge();
        return point;
    }
    private void UpdateChallengeUI()
    {
        switch (currentChallenge)
        {
            case ChallengeType.DribbleGoal:
                challengeText.text =
                    "Challenge Dribble  2pt";
                break;

            case ChallengeType.PassGoal:
                challengeText.text =
                    "Challenge Pass  3pt";
                break;
        }
    }
    public void OnDribble(bool isHome)
    {
        if (isHome)
        {
            homeDribbled = true;
        }
        else
        {
            awayDribbled = true;
        }
    }

    public void AddDribbleDistance(float distance, bool isHome)
    {
        if (GameManager.Instance.IsKickOff) return;
        if (isHome)
        {
            if (homeDribbled)
                return;

            homeDribbleDistance += distance;

            if (homeDribbleDistance >= targetdribbleDistance)
            {
                homeDribbled = true;
                Debug.Log("味方ドリブルチャレンジ達成");
            }
            UpdateProgressUI();
        }
        else
        {
            if (awayDribbled)
                return;

            awayDribbleDistance += distance;

            if (awayDribbleDistance >= targetdribbleDistance)
            {
                awayDribbled = true;
                Debug.Log("敵ドリブルチャレンジ達成");
            }
        }
    }

    public void OnPassKick(bool isHome)
    {
        if(GameManager.Instance.IsKickOff) return;

        if (isHome)
            homePassStarted = true;
        else
            awayPassStarted = true;
    }
    public void OnPassReceive(bool isHome)
    {
        if(GameManager.Instance.IsKickOff) return;

        if (isHome)
        {
            if (!homePassStarted)
                return;

            homeCurrentPassCount++;

            UpdateProgressUI();

            if (homeCurrentPassCount >= targetPassCount)
                homePassed = true;
            Debug.Log("味方パスチャレンジ成功");

            homePassStarted = false;
        }
        else
        {
            if (!awayPassStarted)
                return;

            awayCurrentPassCount++;

            if (awayCurrentPassCount >= targetPassCount)
                awayPassed = true;
            Debug.Log("敵パスチャレンジ成功");

            awayPassStarted = false;
        }
    }
    public void ResetProgress(bool isHome)
    {
        Debug.Log("チャレンジリセット");
        if (isHome)
        {
            UpdateProgressUI();
            homeDribbleDistance = 0f;
            homeCurrentPassCount = 0;
            homeDribbled = false;
            homePassed = false;
            homePassStarted = false;
        }
        else
        {
            awayDribbleDistance = 0f;
            awayCurrentPassCount = 0;
            awayDribbled = false;
            awayPassed = false;
            awayPassStarted = false;
        }
    }
    private void UpdateProgressUI()
    {
        switch (currentChallenge)
        {
            case ChallengeType.DribbleGoal:

                progressText.text =
                    $"{homeDribbleDistance:F1} / {targetdribbleDistance:F1} m";

                break;

            case ChallengeType.PassGoal:

                progressText.text =
                    $"{homeCurrentPassCount} / {targetPassCount}";

                break;
        }
    }
}

