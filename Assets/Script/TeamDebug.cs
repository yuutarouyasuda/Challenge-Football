using UnityEngine;

public class TeamDebug : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("FW : " + TeamDataManager.Instance.forward.playerName);
        Debug.Log("MF : " + TeamDataManager.Instance.midfielder.playerName);
        Debug.Log("DF : " + TeamDataManager.Instance.defender.playerName);
    }
}