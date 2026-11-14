using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka
{
    public enum GmEncounterOutcome { Retreat, Victory }

    [DisallowMultipleComponent]
    public sealed class GmEncounterFlow : MonoBehaviour
    {
        private enum Phase { Exploring, Loading, Battle, Returning }

        [SerializeField] private string battleScenePath;
        private readonly List<GameObject> suspendedRoots = new List<GameObject>();
        private Phase phase;
        private Scene explorationScene;
        private Scene battleScene;
        private string loadingPath;
        private AsyncOperation pendingLoad;
        private AsyncOperation pendingUnload;
        private GmControlGate gate;
        private GmEncounterBattleRoot battleRoot;
        private bool quitting;

        public string BattleScenePath => battleScenePath;
        public GmEnemyEncounter CurrentEncounter { get; private set; }
        public GmSakaMotor CurrentPlayer { get; private set; }

        public void SetBattleScene(string scenePath) => battleScenePath = scenePath;

        internal bool TryBegin(GmSakaMotor hero, GmEnemyEncounter enemy)
        {
            if (!isActiveAndEnabled || phase != Phase.Exploring || pendingLoad != null || pendingUnload != null ||
                hero == null || !hero.isActiveAndEnabled || enemy == null || enemy.IsDefeated) return false;

            Scene source = gameObject.scene;
            if (hero.gameObject.scene != source || enemy.gameObject.scene != source) return false;
            GmControlGate playerGate = hero.GetComponent<GmControlGate>();
            if (playerGate == null || !playerGate.CanExplore) return false;
            if (transform.parent != null)
            {
                Debug.LogError("[Daeng Saka] The encounter flow must be a root object in the exploration scene.", this);
                return false;
            }
            int buildIndex = string.IsNullOrWhiteSpace(battleScenePath)
                ? -1 : SceneUtility.GetBuildIndexByScenePath(battleScenePath);
            if (buildIndex < 0 || battleScenePath == source.path)
            {
                Debug.LogError("[Daeng Saka] Choose a separate battle scene and enable it in the active Scene List.", this);
                return false;
            }
            if (SceneManager.GetSceneByPath(battleScenePath).IsValid())
            {
                Debug.LogError("[Daeng Saka] The battle scene is already open. Start the test from exploration.", this);
                return false;
            }

            phase = Phase.Loading;
            explorationScene = source;
            loadingPath = battleScenePath;
            CurrentPlayer = hero;
            CurrentEncounter = enemy;
            gate = playerGate;
            gate.Block(this);
            try
            {
                pendingLoad = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                FinishReturn();
                return false;
            }
            if (pendingLoad == null)
            {
                Debug.LogError("[Daeng Saka] Could not start loading the battle scene.", this);
                FinishReturn();
                return false;
            }
            pendingLoad.completed += OnBattleLoaded;
            return true;
        }

        private void OnBattleLoaded(AsyncOperation operation)
        {
            if (operation != pendingLoad) return;
            pendingLoad = null;
            if (quitting) return;
            battleScene = SceneManager.GetSceneByPath(loadingPath);
            if (this == null || phase != Phase.Loading || !isActiveAndEnabled ||
                CurrentPlayer == null || CurrentEncounter == null || !explorationScene.isLoaded)
            {
                ReturnToExploration(GmEncounterOutcome.Retreat);
                return;
            }

            GameObject[] roots = battleScene.IsValid() && battleScene.isLoaded
                ? battleScene.GetRootGameObjects() : Array.Empty<GameObject>();
            battleRoot = roots.Length == 1 && !roots[0].activeSelf
                ? roots[0].GetComponent<GmEncounterBattleRoot>() : null;
            if (battleRoot == null || !SceneManager.SetActiveScene(battleScene))
            {
                Debug.LogError("[Daeng Saka] The battle scene needs one inactive GmEncounterBattleRoot.", this);
                ReturnToExploration(GmEncounterOutcome.Retreat);
                return;
            }

            foreach (GameObject root in explorationScene.GetRootGameObjects())
            {
                if (root == gameObject || !root.activeSelf) continue;
                suspendedRoots.Add(root);
                root.SetActive(false);
            }
            battleRoot.Bind(this);
            phase = Phase.Battle;
            battleRoot.gameObject.SetActive(true);
        }

        internal bool TryResolve(GmEncounterBattleRoot requester, GmEncounterOutcome outcome)
        {
            if (phase != Phase.Battle || requester == null || requester != battleRoot) return false;
            ReturnToExploration(outcome);
            return true;
        }

        private void ReturnToExploration(GmEncounterOutcome outcome)
        {
            phase = Phase.Returning;
            if (battleScene.IsValid() && battleScene.isLoaded)
                foreach (GameObject root in battleScene.GetRootGameObjects()) root.SetActive(false);

            if (explorationScene.IsValid() && explorationScene.isLoaded)
            {
                SceneManager.SetActiveScene(explorationScene);
                GameObject defeatedRoot = null;
                if (CurrentEncounter != null)
                {
                    if (outcome == GmEncounterOutcome.Victory)
                    {
                        defeatedRoot = CurrentEncounter.gameObject;
                        CurrentEncounter.MarkDefeated();
                    }
                    else CurrentEncounter.SuppressUntilSeparated(CurrentPlayer);
                }
                foreach (GameObject root in suspendedRoots)
                    if (root != null && root != defeatedRoot) root.SetActive(true);
            }
            suspendedRoots.Clear();

            // A pending load cannot be cancelled. Its completion will discard the inactive scene.
            if (pendingLoad != null)
            {
                ReleaseControls();
                return;
            }
            if (!battleScene.IsValid() || !battleScene.isLoaded)
            {
                FinishReturn();
                return;
            }
            try
            {
                pendingUnload = SceneManager.UnloadSceneAsync(battleScene);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                FinishReturn();
                return;
            }
            if (pendingUnload == null)
            {
                Debug.LogError("[Daeng Saka] Could not unload the battle scene.", this);
                FinishReturn();
                return;
            }
            pendingUnload.completed += OnBattleUnloaded;
        }

        private void OnBattleUnloaded(AsyncOperation operation)
        {
            if (operation == pendingUnload) FinishReturn();
        }

        private void ReleaseControls()
        {
            if (CurrentPlayer != null)
            {
                GmInteractor interactor = CurrentPlayer.GetComponent<GmInteractor>();
                if (interactor != null) interactor.SuppressInteraction();
            }
            if (gate != null) gate.Unblock(this);
            gate = null;
        }

        private void FinishReturn()
        {
            ReleaseControls();
            pendingLoad = null;
            pendingUnload = null;
            battleRoot = null;
            battleScene = default;
            explorationScene = default;
            loadingPath = null;
            CurrentPlayer = null;
            CurrentEncounter = null;
            suspendedRoots.Clear();
            phase = Phase.Exploring;
        }

        private void OnDisable()
        {
            if (Application.isPlaying && !quitting && (phase == Phase.Loading || phase == Phase.Battle))
                ReturnToExploration(GmEncounterOutcome.Retreat);
        }

        private void OnApplicationQuit() => quitting = true;
    }
}
