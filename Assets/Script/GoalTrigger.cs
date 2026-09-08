using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public int score = 0;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Ball"))
        {
            score++;

            Debug.Log("GOAL!!");
            Debug.Log("Score : " + score);

            //ƒ{[ƒ‹‚ğ‰ŠúˆÊ’u‚Ö–ß‚·
            other.transform.position = new Vector3(0, 1, -10);

            Rigidbody rb=other.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
}
