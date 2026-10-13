using TMPro;
using UnityEngine;

public class PlayerSelectButtun : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TeamSelectManager teamSelectManager;
    [SerializeField] private TextMeshProUGUI statusText;
    private void Start()
    {
        playerNameText.text = playerData.playerName;

        statusText.text =
            "Speed " + playerData.speed +
            " Kick " + playerData.kick +
            " Dribble " + playerData.dribble +
            " Defence " + playerData.defence;
    }
    public void OnClick()
    {
        teamSelectManager.SelectPlayer(playerData);
    }
}

