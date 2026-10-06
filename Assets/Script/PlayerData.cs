using UnityEngine;

[CreateAssetMenu(menuName = "Soccer/PlayerData")]
public class PlayerData : ScriptableObject
{
    public string playerName;

    [Range(1, 100)] public int speed;
    [Range(1, 100)] public int kick;
    [Range(1, 100)] public int dribble;
    [Range(1, 100)] public int defence;
}
