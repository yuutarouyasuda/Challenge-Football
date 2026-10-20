using UnityEngine;
using UnityEngine.AI;
public class TeamSetup : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private TeammateAI_New playerAI;
    [SerializeField] private TeammateAI_New[] teammates;

    [Header("position")]
    [SerializeField] private Transform fwPosition;
    [SerializeField]private Transform mfPosition;
    [SerializeField] private Transform dfPosition;
    void Start()
    {
        Debug.Log("Manager = " + TeamDataManager.Instance);

        if (TeamDataManager.Instance == null)
        {
            Debug.LogError("Manager‚ªnull");
            return;
        }

        player.SetPlayerData(TeamDataManager.Instance.forward);
        playerAI.SetPlayerData(TeamDataManager.Instance.forward);

        teammates[0].SetPlayerData(TeamDataManager.Instance.midfielder);
        teammates[1].SetPlayerData(TeamDataManager.Instance.defender);

        Debug.Log("Forward = " + TeamDataManager.Instance.forward);
        Debug.Log("Midfielder = " + TeamDataManager.Instance.midfielder);
        Debug.Log("Defender = " + TeamDataManager.Instance.defender);
    }
}

