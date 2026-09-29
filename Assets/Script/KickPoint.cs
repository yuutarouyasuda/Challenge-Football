using UnityEngine;

public class KickPoint : MonoBehaviour
{
    [SerializeField] private Transform kickPoint;

    public Transform Point => kickPoint;
}