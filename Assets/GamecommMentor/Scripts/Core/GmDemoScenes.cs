using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka
{
    public static class GmDemoScenes
    {
        private const string Folder = "Assets/GamecommMentor/Scenes_GamecommMentor/";
        public const string Title = Folder + "MainMenu.unity";
        public const string Capital = Folder + "IbuKota.unity";
        public const string Village = Folder + "DesaSorelia.unity";
        public const string Cave = Folder + "GoaLembanyung.unity";
        public const string Battle = Folder + "Battle_Encounter_Test.unity";
        private static bool loading;

        internal static bool IsLoading => loading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLoading() => loading = false;

        internal static bool TryStart() => TryLoad(Capital, freshRun: true);
        internal static bool TryReturnToTitle() => TryLoad(Title, freshRun: false);

        private static bool TryLoad(string path, bool freshRun)
        {
            if (loading || path == SceneManager.GetActiveScene().path) return false;
            int index = SceneUtility.GetBuildIndexByScenePath(path);
            if (index < 0) return false;

            loading = true;
            AsyncOperation operation;
            try
            {
                operation = SceneManager.LoadSceneAsync(index, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                loading = false;
                Debug.LogException(exception);
                return false;
            }
            if (operation == null)
            {
                loading = false;
                return false;
            }
            if (freshRun)
            {
                GmPartyRun.BeginPlaySession();
                GmMainQuestProgress.ResetWorldRun();
                GmQuestJournal.ResetRun();
                GmPartyBagView.ResetTab();
                GmWorldScene.ResetTransfer();
            }
            operation.completed += _ => loading = false;
            return true;
        }
    }
}
