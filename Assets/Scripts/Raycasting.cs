using UnityEngine;

public class Raycasting : MonoBehaviour
{
    public static float distanceFromTarget;
    [SerializeField] float toTarget;
    public static GameObject target;

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit))
        {

            toTarget = hit.distance;
            distanceFromTarget = hit.distance;
            target = hit.transform.gameObject;
        }

    }
}
