using System.Collections.Generic;
using UnityEngine;

namespace COMP602
{
    // takes the weapons away while an enemy holds the player
    // hides them, stops them firing and blocks the inventory key
    // counted, so two grabs at once only release on the last Unlock
    public static class PlayerHands
    {
        static int lockCount;

        // what Lock switched off, so Unlock only turns those back on
        static readonly List<Behaviour> disabledBehaviours = new List<Behaviour>();
        static readonly List<Renderer> hiddenRenderers = new List<Renderer>();

        public static bool IsLocked => lockCount > 0;

        // statics survive between play sessions when domain reload is off
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            lockCount = 0;
            disabledBehaviours.Clear();
            hiddenRenderers.Clear();
        }

        public static void Lock()
        {
            lockCount++;

            if (lockCount == 1)
                TakeAway();
        }

        public static void Unlock()
        {
            if (lockCount == 0)
                return;

            lockCount--;

            if (lockCount == 0)
                GiveBack();
        }

        static void TakeAway()
        {
            Inventory inventory = Inventory.Instance;

            if (inventory == null)
                return;

            // the inventory only reads its open key in Update
            Disable(inventory);

            // weapons sit under InventoryStorage, the menu is a sibling
            Transform storage = inventory.transform.Find("InventoryStorage");

            if (storage == null)
                storage = inventory.transform;

            // a disabled weapon keeps its pending ResetShot, so it can fire again on release
            foreach (Weapon weapon in storage.GetComponentsInChildren<Weapon>(true))
                Disable(weapon);

            foreach (Renderer renderer in storage.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled)
                    continue;

                renderer.enabled = false;
                hiddenRenderers.Add(renderer);
            }
        }

        static void Disable(Behaviour behaviour)
        {
            if (!behaviour.enabled)
                return;

            behaviour.enabled = false;
            disabledBehaviours.Add(behaviour);
        }

        static void GiveBack()
        {
            // null checks cover anything destroyed during the grab
            foreach (Behaviour behaviour in disabledBehaviours)
            {
                if (behaviour != null)
                    behaviour.enabled = true;
            }

            foreach (Renderer renderer in hiddenRenderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }

            disabledBehaviours.Clear();
            hiddenRenderers.Clear();
        }
    }
}
