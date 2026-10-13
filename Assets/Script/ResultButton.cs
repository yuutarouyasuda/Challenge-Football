using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultButton : MonoBehaviour
{
    // もう一度遊ぶ
    public void Retry()
    {
        SceneManager.LoadScene("Football");
    }

    // タイトルへ戻る
    public void BackTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
}