using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TeamSelectManager : MonoBehaviour
{
    [SerializeField]private List<PlayerData>selectedPlayers=new List<PlayerData>();
    [SerializeField] private TextMeshProUGUI player1Text;
    [SerializeField] private TextMeshProUGUI player2Text;
    [SerializeField] private TextMeshProUGUI player3Text;
    public static List<PlayerData> SelectedPlayers { get; private set; }

    private void Awake()
    {
        SelectedPlayers = selectedPlayers;

        DontDestroyOnLoad(gameObject);
    }
    private void UpdatePlayerText()
    {
        player1Text.text = selectedPlayers.Count > 0 ? "1 " + selectedPlayers[0].playerName : "1 ";
        player2Text.text = selectedPlayers.Count > 1 ? "2 " + selectedPlayers[1].playerName : "2 ";
        player3Text.text = selectedPlayers.Count > 2 ? "3 " + selectedPlayers[2].playerName : "3 ";
    }
    public void SelectPlayer(PlayerData player)
    {
        //すでに選ばれていたら解除
        if (selectedPlayers.Contains(player))
        {
            selectedPlayers.Remove(player);
            UpdatePlayerText();
            Debug.Log(player.playerName + "を選択解除");
            return;
        }
        //三人まで
        if(selectedPlayers.Count>=3)
        {
            Debug.Log("選べるのは三人までです");
            return;
        }
        selectedPlayers.Add(player);
        UpdatePlayerText();

        Debug.Log(player.playerName + "を選択しました");
    }
   
}
