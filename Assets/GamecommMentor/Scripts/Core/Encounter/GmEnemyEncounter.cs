using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public sealed class GmEnemyEncounter : MonoBehaviour
    {
        [SerializeField] private GmEncounterFlow flow;
        [SerializeField] private string encounterId = "enemy-test-01";
        [SerializeField] private string displayName = "Musuh Uji";
        [SerializeField] private GmBossBattleProfile bossBattleProfile;
        [SerializeField] private bool usePoppoBattle;
        [SerializeField] private GmEncounterRoster roster;
        [SerializeField] private bool startWhenReadyWhileTouching;
        [SerializeField] private bool rememberVictory;
        [SerializeField] private bool findSceneFlow;
        private SphereCollider trigger;
        private CharacterController suppressedPlayer;
        private GmCavePatrol patrol;
        private string placedEncounterId;
        private float canStartAt;

        public string EncounterId => placedEncounterId ?? encounterId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        public bool IsDefeated { get; private set; }
        public GmBossBattleProfile BossBattleProfile => bossBattleProfile;
        public bool UsePoppoBattle => usePoppoBattle;
        internal GmEncounterRoster Roster => roster;
        internal bool IsPlacedOrPatrolling => findSceneFlow || patrol != null;

        internal bool CanJoinGroup(CharacterController player) =>
            isActiveAndEnabled && !IsDefeated && BossBattleProfile == null && !UsePoppoBattle &&
            IsPlacedOrPatrolling && Roster != null && Roster.HasEnemies &&
            Time.time >= canStartAt && (suppressedPlayer == null || suppressedPlayer != player);

        internal bool UsesFlow(GmEncounterFlow encounterFlow) => flow == encounterFlow;

        public void SetRoster(GmEncounterRoster value) => roster = value;

        public void Configure(GmEncounterFlow encounterFlow, string id, string enemyName,
            GmBossBattleProfile bossProfile = null, bool poppoBattle = false)
        {
            flow = encounterFlow;
            encounterId = id;
            displayName = enemyName;
            bossBattleProfile = bossProfile;
            usePoppoBattle = poppoBattle;
        }

        public void SetStartWhenReadyWhileTouching(bool enabled) => startWhenReadyWhileTouching = enabled;
        public void SetRememberVictory(bool enabled) => rememberVictory = enabled;

        private void Reset()
        {
            SphereCollider volume = GetComponent<SphereCollider>();
            volume.isTrigger = true;
            volume.radius = 0.65f;
            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void Awake()
        {
            trigger = GetComponent<SphereCollider>();
            patrol = GetComponent<GmCavePatrol>();
            if (findSceneFlow)
            {
                if (flow == null)
                    foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                        if (root.TryGetComponent(out flow)) break;

                // A dragged prefab or its duplicate gets a separate key in the current run.
                string address = transform.GetSiblingIndex().ToString();
                for (Transform parent = transform.parent; parent != null; parent = parent.parent)
                    address = $"{parent.GetSiblingIndex()}/{address}";
                placedEncounterId = $"{gameObject.scene.path}:{encounterId}:{address}";
            }
            if (rememberVictory && GmPartyRun.Current.HasDefeatedEncounter(EncounterId)) MarkDefeated();
        }

        private void OnTriggerEnter(Collider other) => TryStartEncounter(other);

        private void OnTriggerStay(Collider other)
        {
            if (startWhenReadyWhileTouching) TryStartEncounter(other);
        }

        private void TryStartEncounter(Collider other)
        {
            if (!isActiveAndEnabled || IsDefeated || flow == null || Time.time < canStartAt) return;
            CharacterController body = other as CharacterController;
            if (body == null || body == suppressedPlayer) return;
            GmSakaMotor hero = body.GetComponent<GmSakaMotor>();
            if (hero == null || (patrol != null && patrol.isActiveAndEnabled && !patrol.CanStartContact(hero))) return;
            if (flow.TryBegin(hero, this) && patrol != null) patrol.RequireNewApproach(hero.transform.position);
        }

        private void FixedUpdate()
        {
            if (suppressedPlayer == null || !suppressedPlayer.enabled ||
                !suppressedPlayer.gameObject.activeInHierarchy) return;
            // Disabling and re-enabling scene roots can generate fresh trigger events.
            // Rearm only after the player has physically left this encounter volume.
            if (!trigger.bounds.Intersects(suppressedPlayer.bounds)) suppressedPlayer = null;
        }

        internal void SuppressUntilSeparated(GmSakaMotor hero)
        {
            suppressedPlayer = hero != null ? hero.GetComponent<CharacterController>() : null;
        }

        internal void GiveSpaceOnReturn(GmSakaMotor hero)
        {
            SuppressUntilSeparated(hero);
            canStartAt = Time.time + 1.5f;
            if (patrol != null && hero != null) patrol.RequireNewApproach(hero.transform.position);
        }

        internal void MarkDefeated()
        {
            IsDefeated = true;
            if (rememberVictory) GmPartyRun.Current.RememberDefeatedEncounter(EncounterId);
            gameObject.SetActive(false);
        }
    }
}
