using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace COMP602
{
    // Grab attack. Holds the player in place, turns their view towards this
    // enemy, then damages and shoves them. Once it starts, the grab always
    // runs to completion.
    //
    // SETUP
    // Goes on the enemy root, alongside EnemyAttackState, which decides when
    // this runs. This script never attacks on its own.
    // On the enemy: Animator with a Trigger parameter named "GrabAttack", and
    // a NavMeshAgent.
    // In the scene: the player tagged "Player", carrying PlayerHealth, with a
    // Camera somewhere below it.
    //
    // INTEGRATION
    // PlayerHealth is the only hard dependency, referenced by type.
    // FirstPersonMovement, FirstPersonLook, FirstPersonSprintJump and
    // PlayerGrabEffect are found by type name at runtime instead, so their
    // scripts can be absent from the project entirely and this still compiles.
    // The catch is that everything about them fails quietly: with no
    // FirstPersonMovement the player is never actually held and the grab
    // becomes decoration, and a renamed method produces no error either.
    //
    // TROUBLESHOOTING
    // Nothing happens: EnemyAttackState has no Option pointing here, or its
    // weight is 0.
    // "no target" in Console: the player object is not tagged "Player".
    // "no PlayerHealth": the tagged object has none on it or on a parent. The
    // script disables itself, by design.
    // Damage lands but no animation: the Animator parameter is missing or
    // spelled differently. Must be a Trigger named "GrabAttack" exactly.
    // Player keeps walking while held: FirstPersonMovement was not found, or
    // it no longer has a SpeedMultiplier property.
    // Camera jumps on release: SyncFromTransform is not being reached.
    // Enemy freezes after a grab: Recovery Time should be slightly longer than
    // the grab clip, so the Animator switches before the body is freed.
    public class EnemyGrabAttack : MonoBehaviour, IEnemyAttack
    {
        // who to grab, empty finds the object tagged Player
        [SerializeField] Transform target;

        [Header("Grab")]
        [SerializeField] float damage = 20f;

        // how long the player stays held, in seconds
        [ReadOnly]
        [SerializeField] float grabDuration = 2f;

        // seconds spent turning the player to face this enemy
        [SerializeField] float faceTurnDuration = 0.3f;

        // throw applied on release, in metres per second
        [SerializeField] float pushForce = 8f;

        // extra seconds planted for the recovery animation, the player is
        // already free by then
        [ReadOnly]
        [SerializeField] float recoveryTime = 3.1f;

        PlayerHealth targetHealth;
        Transform targetCamera;

        // held as MonoBehaviour and looked up by type name, so the project
        // still compiles if any of these scripts is removed
        MonoBehaviour targetMovement;
        MonoBehaviour targetSprintJump;
        MonoBehaviour targetLook;
        MonoBehaviour targetEffect;

        // resolved once in Start, not per grab
        MethodInfo addImpulse;
        PropertyInfo speedMultiplier;
        MethodInfo syncFromTransform;
        MethodInfo setEffectActive;

        EnemyAttackState attackState;
        NavMeshAgent agent;
        Animator animator;

        bool grabbing;

        // true from the start of the grab until the recovery ends, which is
        // when the gate is handed back
        bool holdingGate;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            attackState = GetComponent<EnemyAttackState>();

            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");

                if (player != null)
                    target = player.transform;
            }

            if (target == null)
            {
                Debug.LogError($"{nameof(EnemyGrabAttack)}: no target. Assign one, or tag " +
                               "the player object as Player.", this);
                enabled = false;
                return;
            }

            targetHealth = target.GetComponentInParent<PlayerHealth>();

            targetMovement = FindByTypeName("FirstPersonMovement");
            targetSprintJump = FindByTypeName("FirstPersonSprintJump");
            targetLook = FindByTypeName("FirstPersonLook");
            targetEffect = FindByTypeName("PlayerGrabEffect");

            if (targetMovement != null)
            {
                System.Type type = targetMovement.GetType();

                addImpulse = type.GetMethod("AddImpulse", new[] { typeof(Vector3) });
                speedMultiplier = type.GetProperty("SpeedMultiplier");
            }

            if (targetLook != null)
                syncFromTransform = targetLook.GetType().GetMethod("SyncFromTransform");

            if (targetEffect != null)
                setEffectActive = targetEffect.GetType().GetMethod("SetActive");

            Camera camera = target.GetComponentInChildren<Camera>();

            if (camera != null)
                targetCamera = camera.transform;

            if (targetHealth == null)
            {
                Debug.LogError($"{nameof(EnemyGrabAttack)}: target has no PlayerHealth.", this);
                enabled = false;
            }
        }

        // searches the player and its parents for a component by class name
        MonoBehaviour FindByTypeName(string typeName)
        {
            foreach (MonoBehaviour script in target.GetComponentsInParent<MonoBehaviour>())
            {
                if (script == null)
                    continue;

                if (script.GetType().Name == typeName)
                    return script;
            }

            return null;
        }

        // entry point called by EnemyAttackState, this attack has one variant
        public void BeginAttack(int variantIndex)
        {
            if (grabbing)
                return;

            holdingGate = true;

            StartCoroutine(Grab());
        }

        IEnumerator Grab()
        {
            grabbing = true;

            SetPlayerHeld(true);

            if (agent != null)
                agent.isStopped = true;

            if (animator != null)
                animator.SetTrigger("GrabAttack");

            yield return StartCoroutine(TurnPlayerToFaceMe());

            // the turn already consumed part of the hold
            float remaining = Mathf.Max(grabDuration - faceTurnDuration, 0f);

            yield return new WaitForSeconds(remaining);

            targetHealth.TakeDamage(damage);

            Release();

            // the recovery animation needs the body to stay still
            yield return new WaitForSeconds(recoveryTime);

            if (agent != null)
                agent.isStopped = false;

            ReleaseGate();
        }

        // swings the player view around to look at this enemy
        IEnumerator TurnPlayerToFaceMe()
        {
            if (targetCamera == null || faceTurnDuration <= 0f)
                yield break;

            Vector3 flatDirection = transform.position - target.position;
            flatDirection.y = 0f;

            if (flatDirection.sqrMagnitude < 0.001f)
                yield break;

            Quaternion startBody = target.rotation;
            Quaternion endBody = Quaternion.LookRotation(flatDirection);

            Quaternion startCamera = targetCamera.localRotation;

            // aimed slightly down, the enemy is close and below eye line
            Quaternion endCamera = Quaternion.Euler(10f, 0f, 0f);

            float elapsed = 0f;

            while (elapsed < faceTurnDuration)
            {
                elapsed += Time.deltaTime;

                // SmoothStep eases in and out, so the swing is not mechanical
                float t = Mathf.SmoothStep(0f, 1f, elapsed / faceTurnDuration);

                target.rotation = Quaternion.Slerp(startBody, endBody, t);
                targetCamera.localRotation = Quaternion.Slerp(startCamera, endCamera, t);

                yield return null;
            }

            // without this the look script snaps back to its own stored pitch
            SyncLook();
        }

        // lets go of the player, the enemy stays stopped past this point
        void Release()
        {
            PushPlayer();

            SetPlayerHeld(false);

            grabbing = false;
        }

        void ReleaseGate()
        {
            if (!holdingGate)
                return;

            holdingGate = false;

            if (attackState != null)
                attackState.End(this);
        }

        void PushPlayer()
        {
            if (targetMovement == null || addImpulse == null || pushForce <= 0f)
                return;

            // flattened before normalising, the push stays horizontal
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            addImpulse.Invoke(targetMovement, new object[] { direction.normalized * pushForce });
        }

        void SetPlayerHeld(bool held)
        {
            // sprint writes the multiplier every frame, so it goes off first
            if (targetSprintJump != null)
                targetSprintJump.enabled = !held;

            if (speedMultiplier != null)
                speedMultiplier.SetValue(targetMovement, held ? 0f : 1f);

            if (targetLook != null)
                targetLook.enabled = !held;

            if (setEffectActive != null)
                setEffectActive.Invoke(targetEffect, new object[] { held });
        }

        void SyncLook()
        {
            if (syncFromTransform != null)
                syncFromTransform.Invoke(targetLook, null);
        }

        // frees the player if this enemy is destroyed mid-grab
        void OnDisable()
        {
            if (grabbing)
            {
                SetPlayerHeld(false);

                SyncLook();

                grabbing = false;
            }

            ReleaseGate();
        }
    }
}
