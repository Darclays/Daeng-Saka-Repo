using System;
using System.Collections;
using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DialogueSystemController))]
    public sealed class GmPixelCrushersDialogueBridge : MonoBehaviour
    {
        [SerializeField] private GmInteractor player;
        [Tooltip("Optional additional database, used by the example scene when a manager already has its own database.")]
        [SerializeField] private DialogueDatabase additionalDatabase;
        private DialogueSystemController controller;
        private GmControlGate gate;
        private bool subscribed;
        private bool databaseAdded;
        private bool starting;
        private int activityVersion;

        public DialogueDatabase AdditionalDatabase => additionalDatabase;

        public void SetPlayer(GmInteractor interactor)
        {
            if (gate != null && player != interactor) gate.Unblock(this);
            player = interactor;
            gate = interactor != null ? interactor.GetComponent<GmControlGate>() : null;
        }

        public void SetAdditionalDatabase(DialogueDatabase database) => additionalDatabase = database;

        private void Awake()
        {
            controller = GetComponent<DialogueSystemController>();
            if (player == null)
            {
                GameObject hero = GameObject.FindGameObjectWithTag("Player");
                if (hero != null) player = hero.GetComponent<GmInteractor>();
            }
            SetPlayer(player);
        }

        private void OnEnable()
        {
            if (controller == null) controller = GetComponent<DialogueSystemController>();
            if (controller == null || subscribed) return;
            controller.conversationStarted += OnConversationStarted;
            controller.conversationEnded += OnConversationEnded;
            subscribed = true;
            if (controller.isConversationActive) OnConversationStarted(controller.currentActor);
        }

        private void Start() => EnsureAdditionalDatabase();

        private void EnsureAdditionalDatabase()
        {
            if (databaseAdded || additionalDatabase == null || controller == null
                || controller.initialDatabase == additionalDatabase) return;
            controller.AddDatabase(additionalDatabase);
            databaseAdded = true;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (controller != null && subscribed)
            {
                controller.conversationStarted -= OnConversationStarted;
                controller.conversationEnded -= OnConversationEnded;
            }
            subscribed = false;
            starting = false;
            if (gate != null) gate.Unblock(this);
            if (player != null) player.SuppressInteraction();
        }

        public bool TryStartConversation(string title, GmInteractor requester, Transform conversant)
        {
            if (!isActiveAndEnabled || starting || requester == null || conversant == null
                || controller == null || !controller.isActiveAndEnabled || controller.isConversationActive
                || !requester.ControlGate.CanExplore) return false;
            EnsureAdditionalDatabase();
            if (controller.masterDatabase == null || controller.masterDatabase.GetConversation(title) == null)
            {
                Debug.LogError("[Daeng Saka 1A.2B] Conversation not found: " + title, this);
                return false;
            }
            SetPlayer(requester);
            starting = true;
            gate.Block(this);
            player.SuppressInteraction();
            StartCoroutine(StartOnNextFrame(title, requester.transform, conversant));
            return true;
        }

        private IEnumerator StartOnNextFrame(string title, Transform actor, Transform conversant)
        {
            // The E key used to open dialogue must not also advance its first subtitle.
            yield return null;
            int before = activityVersion;
            bool failed = false;
            if (actor == null || conversant == null || controller == null)
                failed = true;
            else
            {
                try { controller.StartConversation(title, actor, conversant); }
                catch (Exception exception) { failed = true; Debug.LogException(exception, this); }
                if (!failed && !controller.isConversationActive && activityVersion == before)
                {
                    Debug.LogError("[Daeng Saka 1A.2B] Conversation could not start. Check its links and Dialogue UI: " + title, this);
                    failed = true;
                }
            }
            if (failed && controller != null && controller.isConversationActive)
            {
                try { controller.StopConversation(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
            starting = false;
            if (failed && gate != null && (controller == null || !controller.isConversationActive)) gate.Unblock(this);
        }

        private void OnConversationStarted(Transform actor)
        {
            activityVersion++;
            if (gate != null) gate.Block(this);
            if (player != null) player.SuppressInteraction();
        }

        private void OnConversationEnded(Transform actor)
        {
            if (player != null) player.SuppressInteraction();
            StartCoroutine(ReleaseAfterDialogue(activityVersion));
        }

        private IEnumerator ReleaseAfterDialogue(int version)
        {
            yield return null;
            if (version != activityVersion || starting || (controller != null && controller.isConversationActive)) yield break;
            if (gate != null) gate.Unblock(this);
            if (player != null) player.SuppressInteraction();
        }
    }
}
