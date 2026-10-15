using UnityEngine;

public class SideLineTrigger : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball"))
            return;

        Debug.Log("サイドラインを出ました");

        KickInManager.Instance.StartKickIn();
    }
}