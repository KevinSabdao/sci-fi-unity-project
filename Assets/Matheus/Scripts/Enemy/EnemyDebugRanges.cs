using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace COMP602
{
    // Debug only. Draws the EnemyAI detection ranges in the Game view, where
    // Scene gizmos do not show. Same colours as the gizmos: detection range
    // and view cone in red, lose sight in yellow, hearing in magenta, wander
    // area in cyan.
    //
    // Ranges are read off EnemyAI by reflection, so they always match the
    // Inspector without anything being wired up. The cone comes from the
    // public EyePosition and EyeForward, so it follows the head bone exactly
    // as the detection check does.
    //
    // SETUP
    // Drop on the enemy root next to EnemyAI. Nothing to configure.
    // Delete or disable before submitting.
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyDebugRanges : MonoBehaviour
    {
        [Header("Toggle")]
        // key name as the Input System spells it, all lowercase
        [SerializeField] string toggleKey = "g";

        [SerializeField] bool startVisible = true;

        [Header("Drawing")]
        // points per circle, higher is smoother and costs more
        [SerializeField] int segments = 48;

        // circles sit flat on the ground, which reads better up close
        [SerializeField] float groundOffset = 0.05f;

        // only read when the material is built, so it applies next play
        [SerializeField] bool drawThroughWalls = true;

        EnemyAI ai;
        Material lineMaterial;

        // resolved once in Awake
        FieldInfo detectionRangeField;
        FieldInfo viewAngleField;
        FieldInfo loseSightRangeField;
        FieldInfo hearingRangeField;
        FieldInfo wanderRadiusField;
        FieldInfo spawnPointField;

        bool visible;

        void Awake()
        {
            ai = GetComponent<EnemyAI>();

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            System.Type type = typeof(EnemyAI);

            detectionRangeField = type.GetField("detectionRange", flags);
            viewAngleField = type.GetField("viewAngle", flags);
            loseSightRangeField = type.GetField("loseSightRange", flags);
            hearingRangeField = type.GetField("hearingRange", flags);
            wanderRadiusField = type.GetField("wanderRadius", flags);
            spawnPointField = type.GetField("spawnPoint", flags);

            visible = startVisible;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            if (keyboard[GetKey()].wasPressedThisFrame)
                visible = !visible;
        }

        Key GetKey()
        {
            if (System.Enum.TryParse(toggleKey, true, out Key parsed))
                return parsed;

            return Key.G;
        }

        // GL drawing belongs here, after the camera has rendered
        void OnRenderObject()
        {
            if (!visible || ai == null)
                return;

            EnsureMaterial();

            float detectionRange = ReadFloat(detectionRangeField);
            float viewAngle = ReadFloat(viewAngleField);
            float loseSightRange = ReadFloat(loseSightRangeField);
            float hearingRange = ReadFloat(hearingRangeField);
            float wanderRadius = ReadFloat(wanderRadiusField);

            Vector3 spawnPoint = transform.position;

            if (spawnPointField != null && Application.isPlaying)
                spawnPoint = (Vector3)spawnPointField.GetValue(ai);

            Vector3 ground = transform.position + Vector3.up * groundOffset;

            lineMaterial.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.LINES);

            DrawCircle(ground, detectionRange, Color.red);
            DrawCircle(ground, loseSightRange, Color.yellow);
            DrawCircle(ground, hearingRange, Color.magenta);
            DrawCircle(spawnPoint + Vector3.up * groundOffset, wanderRadius, Color.cyan);

            DrawViewCone(viewAngle, detectionRange);

            GL.End();
            GL.PopMatrix();
        }

        // half the cone each side of the head, matching the EnemyAI check
        void DrawViewCone(float viewAngle, float range)
        {
            Vector3 eye = ai.EyePosition;
            Vector3 forward = ai.EyeForward;

            Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * forward;
            Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * forward;

            GL.Color(Color.red);

            GL.Vertex(eye);
            GL.Vertex(eye + left * range);

            GL.Vertex(eye);
            GL.Vertex(eye + right * range);

            // arc closing the cone at the edge of sight
            int arcSegments = Mathf.Max(segments / 4, 4);
            float step = viewAngle / arcSegments;

            for (int i = 0; i < arcSegments; i++)
            {
                Vector3 a = Quaternion.Euler(0f, -viewAngle * 0.5f + step * i, 0f) * forward;
                Vector3 b = Quaternion.Euler(0f, -viewAngle * 0.5f + step * (i + 1), 0f) * forward;

                GL.Vertex(eye + a * range);
                GL.Vertex(eye + b * range);
            }
        }

        void DrawCircle(Vector3 centre, float radius, Color color)
        {
            if (radius <= 0f)
                return;

            GL.Color(color);

            float step = Mathf.PI * 2f / segments;

            for (int i = 0; i < segments; i++)
            {
                float a = step * i;
                float b = step * (i + 1);

                Vector3 pointA = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                Vector3 pointB = centre + new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * radius;

                GL.Vertex(pointA);
                GL.Vertex(pointB);
            }
        }

        float ReadFloat(FieldInfo field)
        {
            if (field == null)
                return 0f;

            return (float)field.GetValue(ai);
        }

        // built from a stock Unity shader, so there is no asset to assign
        void EnsureMaterial()
        {
            if (lineMaterial != null)
                return;

            Shader shader = Shader.Find("Hidden/Internal-Colored");

            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;

            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);

            // 8 is Always, 4 is LessEqual
            lineMaterial.SetInt("_ZTest", drawThroughWalls ? 8 : 4);
        }

        void OnDestroy()
        {
            if (lineMaterial != null)
                Destroy(lineMaterial);
        }
    }
}
