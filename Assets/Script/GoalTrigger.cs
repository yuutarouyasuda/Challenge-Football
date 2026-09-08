using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    [SerializeField] private bool isHomeGoal;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball"))
            return;

        GameManager.Instance.Goal(isHomeGoal, other.GetComponent<Rigidbody>());
    }
}