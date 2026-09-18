using UnityEngine;

namespace COMP602
{
    // Turns the enemy's head towards the player using the Animator's built-in
    // look-at IK. Works on top of whatever clip is playing, so the zombie
    // keeps watching the player through walking, attacking and screaming.
    //
    // SETUP
    // Goes on the enemy root, next to the Animator. The rig must be Humanoid,
    // and IK Pass must be ticked on the Base Layer (Animator window, Layers
    // tab, gear icon on the layer).
    public class EnemyHeadLook : MonoBehaviour
    {
        // who to look at, empty finds the object tagged Player
        [SerializeField] Transform target;

        [Header("Strength")]
        // overall amount of look-at, 0 off and 1 full
        [Range(0f, 1f)]
        [SerializeField] float weight = 0.8f;

        // how much the head itself contributes
        [Range(0f, 1f)]
        [SerializeField] float headWeight = 1f;

        // how much the chest follows, small values read better
        [Range(0f, 1f)]
        [SerializeField] float bodyWeight = 0.25f;

        // ignored unless the avatar has eye bones mapped
        [Range(0f, 1f)]
        [SerializeField] float eyesWeight = 0f;

        // keeps the neck inside a believable range, 0.5 is about human
        [Range(0f, 1f)]
        [SerializeField] float clampWeight = 0.5f;

        [Header("Aim")]
        // raised to the head rather than the feet
        [SerializeField] float targetHeightOffset = 1.4f;

        // beyond this the enemy stops bothering to look
        [SerializeField] float maxLookDistance = 25f;

        // past this the look fades instead of twisting the neck
        [SerializeField] float maxLookAngle = 110f;

        [Header("Smoothing")]
        // fade time, stops the head snapping at the edge of range
        [SerializeField] float fadeTime = 0.25f;

        Animator animator;

        // eased towards the wanted weight each frame
        float currentWeight;

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");

                if (player != null)
                    target = player.transform;
            }

            if (target == null)
            {
                Debug.LogError($"{nameof(EnemyHeadLook)}: no target. Assign one, or tag " +
                               "the player object as Player.", this);
                enabled = false;
            }
        }

        // called by the Animator, but only when IK Pass is on
        void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || target == null)
                return;

            Vector3 lookPoint = target.position + Vector3.up * targetHeightOffset;

            float wanted = ShouldLook(lookPoint) ? weight : 0f;

            // drifts into the look instead of popping to it
            currentWeight = Mathf.MoveTowards(currentWeight, wanted,
                                              Time.deltaTime / Mathf.Max(fadeTime, 0.0001f));

            if (currentWeight <= 0f)
            {
                animator.SetLookAtWeight(0f);
                return;
            }

            animator.SetLookAtWeight(currentWeight, bodyWeight, headWeight,
                                     eyesWeight, clampWeight);

            animator.SetLookAtPosition(lookPoint);
        }

        bool ShouldLook(Vector3 lookPoint)
        {
            Vector3 toTarget = lookPoint - transform.position;

            if (toTarget.sqrMagnitude > maxLookDistance * maxLookDistance)
                return false;

            // flattened, looking up or down must not count against the angle
            Vector3 flat = toTarget;
            flat.y = 0f;

            if (flat.sqrMagnitude < 0.001f)
                return false;

            return Vector3.Angle(transform.forward, flat) <= maxLookAngle * 0.5f;
        }
    }
}
