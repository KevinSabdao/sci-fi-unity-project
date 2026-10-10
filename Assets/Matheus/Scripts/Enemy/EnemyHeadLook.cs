using UnityEngine;

namespace COMP602
{
    // turns the head towards the player with the Animator look-at IK
    // works on top of whatever clip is playing
    //
    // SETUP
    // goes on the enemy root, next to the Animator
    // the rig must be Humanoid, and IK Pass must be ticked on the Base Layer
    // EnemyAI is optional, and gates the look to chases
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

        // how much the chest follows
        [Range(0f, 1f)]
        [SerializeField] float bodyWeight = 0.25f;

        // ignored unless the avatar has eye bones mapped
        [Range(0f, 1f)]
        [SerializeField] float eyesWeight = 0f;

        // neck limit, 0.5 is about human
        [Range(0f, 1f)]
        [SerializeField] float clampWeight = 0.5f;

        [Header("Aim")]
        // raises the aim point from the feet to the head
        [SerializeField] float targetHeightOffset = 1.4f;

        // beyond this he stops looking
        [SerializeField] float maxLookDistance = 25f;

        // past this the look fades out
        [SerializeField] float maxLookAngle = 110f;

        // on, he only watches the player while chasing
        // off, he watches whenever the player is in range, which gives him
        // away and aims the EnemyAI view cone at the player
        [SerializeField] bool onlyWhileChasing = true;

        [Header("Smoothing")]
        // fade time, stops the head snapping at the edge of range
        [SerializeField] float fadeTime = 0.25f;

        Animator animator;

        // optional, only read when Only While Chasing is on
        EnemyAI ai;

        // eased towards the wanted weight each frame
        float currentWeight;

        void Awake()
        {
            animator = GetComponent<Animator>();
            ai = GetComponent<EnemyAI>();
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

        // called by the Animator, only when IK Pass is on
        void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || target == null)
                return;

            Vector3 lookPoint = target.position + Vector3.up * targetHeightOffset;

            float wanted = ShouldLook(lookPoint) ? weight : 0f;

            // drifts into the look
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
            if (onlyWhileChasing && ai != null && !ai.IsChasing)
                return false;

            Vector3 toTarget = lookPoint - transform.position;

            if (toTarget.sqrMagnitude > maxLookDistance * maxLookDistance)
                return false;

            // flattened, so looking up or down keeps the angle
            Vector3 flat = toTarget;
            flat.y = 0f;

            if (flat.sqrMagnitude < 0.001f)
                return false;

            return Vector3.Angle(transform.forward, flat) <= maxLookAngle * 0.5f;
        }
    }
}