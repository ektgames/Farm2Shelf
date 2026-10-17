using UnityEngine;
using Farm2Shelf.UI;

namespace Farm2Shelf.Utils
{
    /// <summary>Visibility policy only. Never reads or resets a gesture or camera state.</summary>
    public static class WorldInteractionGate
    {
        public static bool IsBlocked(Vector2 point) => BlockReason(point) != null;

        public static string BlockReason(Vector2 point)
        {
            if (MainMenuUI.IsMenuVisible) return "Main menu";
            if (EKTReklamIntroManager.IsIntroVisible) return "Intro";
            if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen) return "Pause menu";
            if (EKTPhoneManager.IsTabletOpen) return "Tablet";
            if (ModalManager.IsModalOpen) return "Visible modal";
            if (TouchInputHelper.IsPointerOverUI(point)) return "UI / tutorial";
            return null;
        }
    }
}
