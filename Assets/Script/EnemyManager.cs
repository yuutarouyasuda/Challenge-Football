using System.Linq;
using UnityEngine;
using static EnemyAI;

public class EnemyManager : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] public EnemyAI[] enemies;
    [SerializeField] public Transform ball;

    [Header("Attack")]
    public float shootPower = 20f;
    public float shootDistance = 8f;
    public float passPower = 12f;
    public float passCooldown = 1f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float dashSpeed = 8f;

    [Header("RotateSpeed")]
    public float rotateSpeed = 360f;
    private void Update()
    {
        BallController ballController = ball.GetComponent<BallController>();

        if (ballController == null)
            return;

        // ==========================
        // 味方がボールを持っている
        // ==========================
        if (ballController.Owner is EnemyAI owner)
        {
            EnemyAI[] others = enemies
                .Where(e => e != owner)
                .OrderBy(e => Vector3.Distance(e.transform.position, owner.transform.position))
                .ToArray();

            // ボール保持者
            owner.Role = DefenderRole.Press;

            // サポート
            if (others.Length > 0)
                others[0].Role = DefenderRole.Support;

            // 裏抜け
            if (others.Length > 1)
                others[1].Role = DefenderRole.RunBehind;
        }
        // ==========================
        // 相手がボールを持っている
        // ==========================
        else
        {
            Vector3 targetPos = ball.position;

            if (ballController.Owner != null)
                targetPos = ballController.Owner.transform.position;

            EnemyAI[] sortedEnemies = enemies
                .OrderBy(e => Vector3.Distance(e.transform.position, targetPos))
                .ToArray();

            if (sortedEnemies.Length > 0)
                sortedEnemies[0].Role = DefenderRole.Press;

            if (sortedEnemies.Length > 1)
                sortedEnemies[1].Role = DefenderRole.CutPass;

            if (sortedEnemies.Length > 2)
                sortedEnemies[2].Role = DefenderRole.CoverGoal;
        }
    }
}