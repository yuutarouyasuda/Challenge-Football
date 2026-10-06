using TMPro;
using UnityEngine;

public class PlayerSelectButtun : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TeamSelectManager teamSelectManager;
    void Start()
    {
        playerNameText.text = playerData.playerName;
    }
    public void OnClick()
    {
        teamSelectManager.SelectPlayer(playerData);
    }
}

