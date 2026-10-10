using System.Collections.Generic;
using UnityEngine;

namespace COMP602
{
    // effects that can show on the player screen
    // add a value at the end and a matching entry in Profiles to create a new one
    public enum ScreenEffect
    {
        Grab,
        Infect,
        Infected
    }

    // screen tint, screen border and camera shake on the player
    // switched on and off from outside, never decides on its own
    public class PlayerScreenEffects : MonoBehaviour
    {
        public enum Style
        {
            // covers the whole screen
            Tint,

            // fades in from the screen edges
            Border
        }

        [System.Serializable]
        public class Profile
        {
            // list label in the inspector, filled in by OnValidate
            [HideInInspector] public string name;

            public ScreenEffect effect;

            // a tint hides any border while it is active
            public Style style = Style.Tint;

            [Header("Color")]
            public Color tintColor = new Color(0.7f, 0f, 0f, 1f);

            [Range(0f, 1f)]
            public float maxAlpha = 0.35f;

            // pulses per second
            public float pulseSpeed = 3f;

            // Border only, how far the border reaches in, as a share of the screen
            [Range(0.01f, 0.5f)]
            public float borderWidth = 0.15f;

            [Header("Shake")]
            // 0 for no shake
            public float shakeAmount = 6f;

            // shakes per second
            public float shakeRate = 8f;
        }

        [SerializeField] Profile[] profiles =
        {
            new Profile
            {
                effect = ScreenEffect.Grab,
                tintColor = new Color(0.7f, 0f, 0f, 1f)
            },
            new Profile
            {
                effect = ScreenEffect.Infect,
                tintColor = new Color(0.35f, 0.75f, 0f, 1f)
            },
            new Profile
            {
                effect = ScreenEffect.Infected,
                style = Style.Border,
                tintColor = new Color(0.35f, 0.75f, 0f, 1f),
                maxAlpha = 0.6f,
                pulseSpeed = 0.5f,
                shakeAmount = 0f
            }
        };

        [Header("Shake")]
        // how quickly each kick settles back to centre
        [SerializeField] float returnSpeed = 5f;

        Transform cameraTransform;

        // active effects, the most recent of each style is the one shown
        readonly List<ScreenEffect> active = new List<ScreenEffect>();

        // one border texture per width, built on first use
        readonly Dictionary<float, Texture2D> borderTextures = new Dictionary<float, Texture2D>();

        float nextShakeTime;
        float currentPitch;

        // the offset applied last frame, removed at the start of this one so
        // the shake never accumulates, the look script is disabled meanwhile
        Quaternion lastOffset = Quaternion.identity;

        // each call to true needs a matching false, so two grabs at once stack
        public void SetActive(ScreenEffect effect, bool value)
        {
            if (value)
                active.Add(effect);
            else
                active.Remove(effect);
        }

        void Start()
        {
            Camera camera = GetComponentInChildren<Camera>();

            // falls back to the main camera for a different player setup
            if (camera == null)
                camera = Camera.main;

            if (camera != null)
                cameraTransform = camera.transform;
        }

        // names each list entry after its effect and style
        void OnValidate()
        {
            if (profiles == null)
                return;

            foreach (Profile profile in profiles)
            {
                if (profile != null)
                    profile.name = $"{profile.effect} ({profile.style})";
            }
        }

        void OnDestroy()
        {
            foreach (Texture2D texture in borderTextures.Values)
                Destroy(texture);
        }

        void Update()
        {
            Profile profile = Latest(p => p.shakeAmount > 0f);

            if (profile == null)
                return;

            if (Time.time < nextShakeTime)
                return;

            nextShakeTime = Time.time + 1f / profile.shakeRate;

            currentPitch -= Random.Range(-profile.shakeAmount, profile.shakeAmount);
        }

        // runs after the look script has set the pitch, so the kick stacks
        // on top
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
            Profile tint = Latest(p => p.style == Style.Tint);

            // a tint takes over the screen, borders come back once it ends
            if (tint != null)
            {
                Draw(tint, Texture2D.whiteTexture);
                return;
            }

            Profile border = Latest(p => p.style == Style.Border);

            if (border != null)
                Draw(border, BorderTexture(border.borderWidth));
        }

        void Draw(Profile profile, Texture2D texture)
        {
            // sine wave gives a heartbeat
            float pulse = (Mathf.Sin(Time.time * profile.pulseSpeed * Mathf.PI) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(profile.maxAlpha * 0.4f, profile.maxAlpha, pulse);

            Color previous = GUI.color;
            GUI.color = new Color(profile.tintColor.r, profile.tintColor.g, profile.tintColor.b, alpha);

            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), texture);

            GUI.color = previous;
        }

        // white at the edges fading to clear towards the centre, tinted when drawn
        Texture2D BorderTexture(float width)
        {
            if (borderTextures.TryGetValue(width, out Texture2D texture))
                return texture;

            const int size = 128;

            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;

                    // 0 at the nearest edge, 0.5 in the centre
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));

                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, edge / width);

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            borderTextures[width] = texture;

            return texture;
        }

        // profile of the most recent active effect that matches, null when none
        Profile Latest(System.Predicate<Profile> match)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                foreach (Profile profile in profiles)
                {
                    if (profile.effect == active[i] && match(profile))
                        return profile;
                }
            }

            return null;
        }
    }
}
