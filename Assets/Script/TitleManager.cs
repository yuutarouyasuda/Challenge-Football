using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    // ゲーム開始
    public void StartGame()
    {
        SceneManager.LoadScene("PlayerSelectScene");
    }

    // ゲーム終了
    public void ExitGame()
    {
        Application.Quit();
    }
}