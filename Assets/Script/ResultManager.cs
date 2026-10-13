using TMPro;
using UnityEngine;

public class ResultManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text winnerText;

    private void Start()
    {
        // スコア表示
        scoreText.text =
            $"HOME {GameManager.ResultHomeScore} - {GameManager.ResultAwayScore} AWAY";

        // 勝者表示
        winnerText.text = GameManager.ResultText;
    }
}