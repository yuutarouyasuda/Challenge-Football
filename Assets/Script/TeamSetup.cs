using UnityEngine;
using UnityEngine.AI;
public class TeamSetup : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private TeammateAI_New[] teammates;

    [Header("position")]
    [SerializeField] private Transform fwPosition;
    [SerializeField]private Transform mfPosition;
    [SerializeField] private Transform dfPosition;
    void Start()
    {
        // FW
        player.SetPlayerData(
            TeamDataManager.Instance.forward
        );

        player.transform.position = fwPosition.position;


        // MF
        teammates[0].SetPlayerData(
            TeamDataManager.Instance.midfielder
        );

        NavMeshAgent mfagent =
     teammates[0].GetComponent<NavMeshAgent>();

        mfagent.Warp(mfPosition.position);


        // DF
        teammates[1].SetPlayerData(
            TeamDataManager.Instance.defender
        );

        NavMeshAgent dfagent =
    teammates[1].GetComponent<NavMeshAgent>();

        dfagent.Warp(dfPosition.position);

        teammates[0].Initialize();
        teammates[1].Initialize();
        Debug.Log("”z’uŠ®—¹");
    }
}

