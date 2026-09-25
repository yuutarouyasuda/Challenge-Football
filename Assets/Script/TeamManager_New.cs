using UnityEngine;

public class TeamManager_New : MonoBehaviour
{
    [SerializeField] private TeammateAI_New[] teammates;
    [SerializeField] private BallController ball;

    private void Update()
    {
        if (ball == null)
            return;

        bool attack =
            ball.Owner is PlayerController ||
            ball.Owner is TeammateAI_New;

        if (attack)
            AttackRole();
        else
            DefenceRole();
    }

    private void AttackRole()
    {
        bool supportAssigned = false;

        foreach (TeammateAI_New mate in teammates)
        {
            PlayerController player = mate.GetComponent<PlayerController>();

            // Ž©•ª‚ÍAI‚É‚µ‚È‚¢
            if (player != null && player.IsControlled)
                continue;

            if (!supportAssigned)
            {
                mate.Role = TeammateAI_New.TeammateRole.Support;
                supportAssigned = true;
            }
            else
            {
                mate.Role = TeammateAI_New.TeammateRole.RunBehind;
            }
        }
    }

    private void DefenceRole()
    {
        bool pressAssigned = false;

        foreach (TeammateAI_New mate in teammates)
        {
            PlayerController player = mate.GetComponent<PlayerController>();

            if (player != null && player.IsControlled)
                continue;

            if (!pressAssigned)
            {
                mate.Role = TeammateAI_New.TeammateRole.Press;
                pressAssigned = true;
            }
            else
            {
                mate.Role = TeammateAI_New.TeammateRole.Cover;
            }
        }
    }
}