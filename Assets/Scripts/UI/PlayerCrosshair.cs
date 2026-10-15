using System.Collections.Generic;
using UnityEngine;

// crosshair in the centre of the screen while firing. lingers and fades out after
// drawn in Color with a dark outline. the image only gives the shape
public class PlayerCrosshair : MonoBehaviour
{
    // needs Read/Write enabled in its import settings to be recoloured
    [SerializeField] Texture2D crosshair;

    // drawn size of the whole image in pixels
    [SerializeField] float size = 120f;

    [SerializeField] Color color = Color.white;

    [SerializeField] Color outlineColor = new Color(0f, 0f, 0f, 0.6f);

    // thickness of the outline in pixels
    [SerializeField] float outlineWidth = 1.5f;

    // off keeps it on screen at all times
    [SerializeField] bool onlyWhileFiring = true;

    // seconds it stays after the fire button is released
    [SerializeField] float lingerTime = 3f;

    // seconds to fade in and out
    [SerializeField] float fadeTime = 0.6f;

    // seconds between searches for the held weapons
    const float WeaponCheckInterval = 0.2f;

    // the crosshair shape in white, so Color can tint it
    Texture2D mask;

    // the active weapons, one per arm. their fire buttons are read without touching the weapon scripts
    readonly List<Item> weapons = new List<Item>();
    float nextWeaponCheck;

    float lastFireTime = float.NegativeInfinity;
    float alpha;

    void Awake()
    {
        mask = BuildMask(crosshair);
    }

    void OnDestroy()
    {
        if (mask != null && mask != crosshair)
            Destroy(mask);
    }

    void Update()
    {
        if (!onlyWhileFiring)
        {
            alpha = 1f;
            return;
        }

        if (Time.unscaledTime >= nextWeaponCheck)
        {
            nextWeaponCheck = Time.unscaledTime + WeaponCheckInterval;
            FindActiveWeapons();
        }

        if (!FirstPersonLook.UiHasCursor && FirePressed())
            lastFireTime = Time.unscaledTime;

        bool visible = Time.unscaledTime < lastFireTime + lingerTime;
        alpha = Mathf.MoveTowards(alpha, visible ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(fadeTime, 0.01f));
    }

    // the same button and check each weapon uses to fire: left and right arm
    bool FirePressed()
    {
        foreach (Item weapon in weapons)
        {
            if (weapon != null && weapon.isActive && Input.GetKey(weapon.keyPressRequired))
                return true;
        }

        return false;
    }

    // weapons that are active inventory items. one per arm at most
    void FindActiveWeapons()
    {
        weapons.Clear();

        foreach (Item item in FindObjectsByType<Item>())
        {
            if (item.isActive && (item is Weapon || item is MeleeWeapon))
                weapons.Add(item);
        }
    }

    void OnGUI()
    {
        if (mask == null)
            return;

        // inventory and pause free the cursor
        if (FirstPersonLook.UiHasCursor)
            return;

        if (alpha <= 0f)
            return;

        var area = new Rect((Screen.width - size) * 0.5f, (Screen.height - size) * 0.5f, size, size);

        Color previous = GUI.color;

        // the outline is the shape drawn a little off centre in every direction
        GUI.color = Faded(outlineColor);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4f;
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * outlineWidth;
            GUI.DrawTexture(new Rect(area.position + offset, area.size), mask);
        }

        GUI.color = Faded(color);
        GUI.DrawTexture(area, mask);

        GUI.color = previous;
    }

    Color Faded(Color c)
    {
        c.a *= alpha;
        return c;
    }

    // white copy that keeps the image's transparency. the original stays untouched
    static Texture2D BuildMask(Texture2D source)
    {
        if (source == null)
            return null;

        if (!source.isReadable)
        {
            Debug.LogWarning($"{nameof(PlayerCrosshair)}: turn on Read/Write in the crosshair's import " +
                             "settings so it can be recoloured. Drawing it as it is.", source);
            return source;
        }

        Color32[] pixels = source.GetPixels32();

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, pixels[i].a);

        var white = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        white.SetPixels32(pixels);
        white.Apply();

        return white;
    }
}
