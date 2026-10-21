using UnityEngine;

public class SideLineTrigger : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball"))
            return;

        if (GameManager.Instance.IskickIn) return;

        Debug.Log($"サイドラインを出ました frame={Time.frameCount}");

        KickInManager.Instance.StartKickIn();
    }
}