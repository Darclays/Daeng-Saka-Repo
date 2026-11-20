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
        [SerializeField] private bool startWhenReadyWhileTouching;
        private SphereCollider trigger;
        private CharacterController suppressedPlayer;

        public string EncounterId => encounterId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        public bool IsDefeated { get; private set; }
        public GmBossBattleProfile BossBattleProfile => bossBattleProfile;
        public bool UsePoppoBattle => usePoppoBattle;

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

        private void Reset()
        {
            SphereCollider volume = GetComponent<SphereCollider>();
            volume.isTrigger = true;
            volume.radius = 0.65f;
            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void Awake() => trigger = GetComponent<SphereCollider>();

        private void OnTriggerEnter(Collider other) => TryStartEncounter(other);

        private void OnTriggerStay(Collider other)
        {
            if (startWhenReadyWhileTouching) TryStartEncounter(other);
        }

        private void TryStartEncounter(Collider other)
        {
            if (!isActiveAndEnabled || IsDefeated || flow == null) return;
            CharacterController body = other as CharacterController;
            if (body == null || body == suppressedPlayer) return;
            GmSakaMotor hero = body.GetComponent<GmSakaMotor>();
            if (hero != null) flow.TryBegin(hero, this);
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

        internal void MarkDefeated()
        {
            IsDefeated = true;
            gameObject.SetActive(false);
        }
    }
}
