using UnityEngine;

namespace CGD.Core
{
    public static class CursorLock
    {
        // Locked = hidden and captured for gameplay; unlocked = free and visible for menus.
        public static void Set(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible   = !locked;
        }
    }
}
