using UnityEngine;

namespace COMP602
{
    // Screen tint and camera rattle while the player is held by a grab.
    // Switched on and off from outside, this script never decides on its own.
    public class PlayerGrabEffect : MonoBehaviour
    {
        [Header("Tint")]
        [SerializeField] Color tintColor = new Color(0.7f, 0f, 0f, 1f);

        // maximum opacity of the red filter
        [SerializeField, Range(0f, 1f)] float maxAlpha = 0.35f;

        // pulses per second
        [SerializeField] float pulseSpeed = 3f;

        [Header("Shake")]
        [SerializeField] float shakeAmount = 0.6f;

        // shakes per second
        [SerializeField] float shakeRate = 12f;

        // how quickly each kick settles back to centre
        [SerializeField] float returnSpeed = 10f;

        Transform cameraTransform;
        Texture2D tint;

        bool active;
        float nextShakeTime;
        float currentPitch;

        // the offset applied last frame, removed at the start of this one so
        // the shake never accumulates, the look script is disabled meanwhile
        Quaternion lastOffset = Quaternion.identity;

        public void SetActive(bool value) => active = value;

        void Start()
        {
            Camera camera = GetComponentInChildren<Camera>();

            // falls back to the main camera for a different player setup
            if (camera == null)
                camera = Camera.main;

            if (camera != null)
                cameraTransform = camera.transform;
        }

        void Update()
        {
            if (!active)
                return;

            if (Time.time < nextShakeTime)
                return;

            nextShakeTime = Time.time + 1f / shakeRate;

            // alternating sign keeps the view rattling instead of drifting
            currentPitch -= Random.Range(-shakeAmount, shakeAmount);
        }

        // runs after the look script has set the pitch, so the kick stacks
        // on top instead of being overwritten
        void LateUpdate()
        {
            if (cameraTransform == null)
                return;

            // undo last frame before anything else
            cameraTransform.localRotation *= Quaternion.Inverse(lastOffset);

            currentPitch = Mathf.Lerp(currentPitch, 0f, returnSpeed * Time.deltaTime);

            // below this, zeroing leaves the camera where the look script put it
            if (Mathf.Abs(currentPitch) < 0.01f)
            {
                currentPitch = 0f;
                lastOffset = Quaternion.identity;
                return;
            }

            lastOffset = Quaternion.Euler(currentPitch, 0f, 0f);

            cameraTransform.localRotation *= lastOffset;
        }

        void OnGUI()
        {
            if (!active)
                return;

            EnsureTexture();

            // sine wave gives a heartbeat rather than a constant wash
            float pulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(maxAlpha * 0.4f, maxAlpha, pulse);

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), tint);

            GUI.color = previous;
        }

        void EnsureTexture()
        {
            if (tint != null)
                return;

            tint = new Texture2D(1, 1);
            tint.SetPixel(0, 0, tintColor);
            tint.Apply();
        }
    }
}
