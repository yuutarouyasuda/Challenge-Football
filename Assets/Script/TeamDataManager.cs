using UnityEngine;

public class TeamDataManager : MonoBehaviour
{
    public static TeamDataManager Instance;

    public PlayerData forward;
    public PlayerData midfielder;
    public PlayerData defender;

    public void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
