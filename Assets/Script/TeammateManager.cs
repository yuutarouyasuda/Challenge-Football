using UnityEngine;

public class TeammateManager : MonoBehaviour
{
    [SerializeField] private TeammateAI[] teammates;
    [SerializeField] private Transform ball;

    private void Update()
    {
        if (ball == null)
            return;

        // ˆê”Ô‹ß‚¢–¡•û‚ð’T‚·
        float nearestDistance = float.MaxValue;
        int nearestIndex = -1;

        for (int i = 0; i < teammates.Length; i++)
        {
            float distance =
                Vector3.Distance(
                    teammates[i].transform.position,
                    ball.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        // ‘Sˆõ‚ðCoverGoal‚É‚·‚é
        foreach (TeammateAI mate in teammates)
        {
            mate.Role = TeammateAI.TeammateRole.CoverGoal;
        }

        // Žc‚èˆêl‚ðCutPass
        foreach (TeammateAI mate in teammates)
        {
            if (mate.Role == TeammateAI.TeammateRole.CoverGoal)
            {
                mate.Role = TeammateAI.TeammateRole.CutPass;
                break;
            }
        }
    }
}