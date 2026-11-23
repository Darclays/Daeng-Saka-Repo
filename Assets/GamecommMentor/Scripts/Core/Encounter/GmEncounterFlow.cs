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
        private const int GroupLimit = 3;
        private const float JoinRadius = 5f;
        private const float JoinHeight = 1.6f;
        private const float ReturnSpaceRadius = 7f;

        [SerializeField] private string battleScenePath;
        private readonly List<GameObject> suspendedRoots = new List<GameObject>();
        private readonly List<GmEnemyEncounter> joinedEncounters = new List<GmEnemyEncounter>(GroupLimit - 1);
        private readonly RaycastHit[] groupSightHits = new RaycastHit[16];
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
        internal string OriginSceneName => explorationScene.IsValid() ? explorationScene.name : null;

        public event Action<GmSakaMotor, GmEnemyEncounter> VictoryReturned;

        public void SetBattleScene(string scenePath) => battleScenePath = scenePath;

        internal bool TryBegin(GmSakaMotor hero, GmEnemyEncounter enemy)
        {
            if (!isActiveAndEnabled || phase != Phase.Exploring || pendingLoad != null || pendingUnload != null ||
                hero == null || !hero.isActiveAndEnabled || enemy == null || enemy.IsDefeated) return false;
            GmMainQuestProgress quest = GetComponent<GmMainQuestProgress>();
            if (quest != null && !quest.CanStartEncounter(enemy)) return false;

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
            GatherGroup(hero, enemy, quest);
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

        private void GatherGroup(GmSakaMotor hero, GmEnemyEncounter lead, GmMainQuestProgress quest)
        {
            joinedEncounters.Clear();
            var body = hero.GetComponent<CharacterController>();
            if (!lead.CanJoinGroup(body) || (quest != null && quest.IsScriptedEncounter(lead))) return;
            int slots = GroupLimit - lead.Roster.Enemies.Length;
            if (slots <= 0) return;

            var candidates = new List<(GmEnemyEncounter enemy, float distance)>();
            foreach (GmEnemyEncounter enemy in SceneEncounters(explorationScene))
            {
                if (enemy == lead || !enemy.UsesFlow(this) || !enemy.CanJoinGroup(body) ||
                    enemy.Roster.Enemies.Length != 1 || (quest != null && quest.IsScriptedEncounter(enemy))) continue;
                Vector3 separation = enemy.transform.position - lead.transform.position;
                float distance = Vector3.ProjectOnPlane(separation, Vector3.up).sqrMagnitude;
                if (distance > JoinRadius * JoinRadius || Mathf.Abs(separation.y) > JoinHeight ||
                    !GmCavePatrol.HasClearSight(enemy.transform, lead.transform, groupSightHits) ||
                    !GmCavePatrol.HasClearSight(enemy.transform, hero.transform, groupSightHits)) continue;
                candidates.Add((enemy, distance));
            }
            candidates.Sort((a, b) => a.distance != b.distance ? a.distance.CompareTo(b.distance) :
                string.CompareOrdinal(a.enemy.EncounterId, b.enemy.EncounterId));
            for (int index = 0; index < Math.Min(slots, candidates.Count); index++)
                joinedEncounters.Add(candidates[index].enemy);
        }

        internal GmBattleStats[] WithNearbyEnemies(GmBattleStats[] leadRoster)
        {
            if (joinedEncounters.Count == 0) return leadRoster;
            var combined = new GmBattleStats[leadRoster.Length + joinedEncounters.Count];
            Array.Copy(leadRoster, combined, leadRoster.Length);
            for (int index = 0; index < joinedEncounters.Count; index++)
                combined[leadRoster.Length + index] = joinedEncounters[index].Roster.Enemies[0];
            return combined;
        }

        private static IEnumerable<GmEnemyEncounter> SceneEncounters(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root != null)
                    foreach (var enemy in root.GetComponentsInChildren<GmEnemyEncounter>(true))
                        yield return enemy;
        }

        private void GiveSpaceAroundPlayer()
        {
            if (CurrentPlayer == null) return;
            var quest = GetComponent<GmMainQuestProgress>();
            Vector3 position = CurrentPlayer.transform.position;
            foreach (var enemy in SceneEncounters(explorationScene))
                if (enemy != null && !enemy.IsDefeated && enemy.IsPlacedOrPatrolling && enemy.UsesFlow(this) &&
                    (quest == null || !quest.IsScriptedEncounter(enemy)) &&
                    (enemy.transform.position - position).sqrMagnitude <= ReturnSpaceRadius * ReturnSpaceRadius)
                    enemy.GiveSpaceOnReturn(CurrentPlayer);
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
                if (CurrentEncounter != null)
                {
                    if (outcome == GmEncounterOutcome.Victory) Defeat(CurrentEncounter);
                    else CurrentEncounter.SuppressUntilSeparated(CurrentPlayer);
                }
                foreach (var enemy in joinedEncounters)
                {
                    if (enemy == null) continue;
                    if (outcome == GmEncounterOutcome.Victory) Defeat(enemy);
                    else enemy.GiveSpaceOnReturn(CurrentPlayer);
                }
                GiveSpaceAroundPlayer();
                foreach (GameObject root in suspendedRoots)
                    if (root != null) root.SetActive(true);
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

        private void Defeat(GmEnemyEncounter enemy)
        {
            suspendedRoots.Remove(enemy.gameObject);
            enemy.MarkDefeated();
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
            GmSakaMotor hero = CurrentPlayer;
            GmEnemyEncounter enemy = CurrentEncounter;
            ReleaseControls();
            pendingLoad = null;
            pendingUnload = null;
            battleRoot = null;
            battleScene = default;
            explorationScene = default;
            loadingPath = null;
            CurrentPlayer = null;
            CurrentEncounter = null;
            joinedEncounters.Clear();
            suspendedRoots.Clear();
            phase = Phase.Exploring;

            if (quitting || !isActiveAndEnabled || hero == null || !hero.isActiveAndEnabled ||
                enemy == null || !enemy.IsDefeated) return;
            GmMainQuestProgress quest = GetComponent<GmMainQuestProgress>();
            if (quest != null) quest.OnEncounterWon(enemy);
            VictoryReturned?.Invoke(hero, enemy);
        }

        private void OnDisable()
        {
            if (Application.isPlaying && !quitting && (phase == Phase.Loading || phase == Phase.Battle))
                ReturnToExploration(GmEncounterOutcome.Retreat);
        }

        private void OnApplicationQuit() => quitting = true;
    }
}
