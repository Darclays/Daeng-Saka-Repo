using UnityEditor;
using UnityEngine;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmPartyInventoryPlaytest
    {
        private const string AddFish = "GamecommMentor/Daeng Saka/Playtest/Tambah 1 Ikan Wader";

        [MenuItem(AddFish, true)]
        private static bool CanAddFish() => Application.isPlaying;

        [MenuItem(AddFish)]
        private static void AddOneFish()
        {
            if (!CanAddFish()) return;
            GmPartyInventory inventory = GmPartyRun.Current.Inventory;
            if (inventory.TryAdd(GmItemDefinition.Wader))
                Debug.Log($"[Daeng Saka] Ikan Wader ×{inventory.Count(GmItemDefinition.Wader)} di tas party.");
        }
    }
}
