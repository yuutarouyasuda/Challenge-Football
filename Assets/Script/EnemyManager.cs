using System.Linq;
using UnityEngine;
using static EnemyAI;

public class EnemyManager : MonoBehaviour
{
   [SerializeField] public EnemyAI[] enemies;
   [SerializeField] public Transform ball;

    private void Update()
    {
        // ƒ{[ƒ‹‚É‹ß‚¢‡‚É•À‚×‚é
        EnemyAI[] sortedEnemies = enemies
            .OrderBy(e => Vector3.Distance(e.transform.position, ball.position))
            .ToArray();

        if (sortedEnemies.Length >= 1)
            sortedEnemies[0].Role = DefenderRole.Press;

        if (sortedEnemies.Length >= 2)
            sortedEnemies[1].Role = DefenderRole.CutPass;

        if (sortedEnemies.Length >= 3)
            sortedEnemies[2].Role = DefenderRole.CoverGoal;
    }
}