using UnityEngine;

// cures the infection when the player walks over it
// stays in the world while the player has nothing to cure
[RequireComponent(typeof(Collider))]
public class VaccinePickup : MonoBehaviour
{
    // the visible ball, spun on its own so the label stays still
    [SerializeField] Transform model;

    // degrees per second around the vertical axis
    [SerializeField] float spinSpeed = 30f;

    // name shown above the ball, always turned to the camera
    [SerializeField] Transform label;

    void Awake()
    {
        // walked through, never blocks the player
        GetComponent<Collider>().isTrigger = true;
    }

    void Update()
    {
        if (model != null)
            model.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;

        // after the camera moved, so the text never lags behind
        if (label != null && cam != null)
            label.rotation = cam.transform.rotation;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerStatusEffects status = other.GetComponentInParent<PlayerStatusEffects>();

        if (status == null)
            return;

        // false when there is no infection to cure, the vaccine stays
        if (!status.Vaccinate())
            return;

        Destroy(gameObject);
    }
}
