using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float messageTime = 2f;

    private void Start()
    {
        messageText.text = "";    
    }
    public void GoToRoleSelect()
    {
        if (TeamSelectManager.SelectedPlayers == null ||
            TeamSelectManager.SelectedPlayers.Count != 3)
        {
            StartCoroutine(ShowMessage("Please Select 3 Players"));
            return;
        }

        SceneManager.LoadScene("RoleScene");
    }

    private IEnumerator ShowMessage(string message)
    {
        messageText.text = message;

        yield return new WaitForSeconds(messageTime);

        messageText.text = "";
    }
}