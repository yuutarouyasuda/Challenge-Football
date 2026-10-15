using UnityEngine;

public class GoalKickTrigger : MonoBehaviour
{
    [SerializeField] private bool isHomeSide;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball"))
            return;

        Debug.Log("ƒS[ƒ‹ƒ‰ƒCƒ“‚ğ’Ê‰ß");

        GoalKickManager.Instance.StartGoalKick(isHomeSide);
    }
}