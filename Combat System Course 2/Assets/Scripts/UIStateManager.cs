using UnityEngine;

public static class UIStateManager
{
    public static System.Action<bool> OnUIActiveStateChanged;
    public static bool IsAnyUIActive { get; private set; }

    public static void SetUIActive(bool active)
    {
        IsAnyUIActive = active;
        OnUIActiveStateChanged?.Invoke(active);

        UpdateCursorState(active);
    }

    private static void UpdateCursorState(bool isUIActive)
    {
        Cursor.visible = isUIActive;
        Cursor.lockState = isUIActive ? CursorLockMode.None : CursorLockMode.Locked;
    }
}