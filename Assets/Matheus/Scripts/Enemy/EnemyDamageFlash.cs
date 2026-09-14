using System.Collections.Generic;
using UnityEngine;

namespace COMP602
{
    // Tints every material on the enemy for a moment when it takes damage, so
    // hits read clearly without an animation.
    public class EnemyDamageFlash : MonoBehaviour
    {
        [Header("Flash")]
        [SerializeField] Color flashColor = Color.red;
        [SerializeField] float flashDuration = 0.12f;

        EnemyHealth health;
        Renderer[] renderers;

        // original colours, put back when the flash ends
        readonly List<Material> materials = new List<Material>();
        readonly List<Color> originalColors = new List<Color>();

        float flashEndTime;
        bool flashing;

        void Awake()
        {
            health = GetComponentInParent<EnemyHealth>();
            renderers = GetComponentsInChildren<Renderer>();

            foreach (Renderer r in renderers)
            {
                foreach (Material m in r.materials)
                {
                    if (!m.HasProperty("_BaseColor"))
                        continue;

                    materials.Add(m);
                    originalColors.Add(m.GetColor("_BaseColor"));
                }
            }
        }

        void OnEnable()
        {
            if (health != null)
                health.OnDamaged.AddListener(Flash);
        }

        void OnDisable()
        {
            if (health != null)
                health.OnDamaged.RemoveListener(Flash);
        }

        void Update()
        {
            if (!flashing)
                return;

            if (Time.time < flashEndTime)
                return;

            Restore();
        }

        void Flash()
        {
            for (int i = 0; i < materials.Count; i++)
                materials[i].SetColor("_BaseColor", flashColor);

            flashEndTime = Time.time + flashDuration;
            flashing = true;
        }

        void Restore()
        {
            for (int i = 0; i < materials.Count; i++)
                materials[i].SetColor("_BaseColor", originalColors[i]);

            flashing = false;
        }
    }
}
