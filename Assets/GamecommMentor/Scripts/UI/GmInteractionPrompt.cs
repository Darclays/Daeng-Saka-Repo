using UnityEngine;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmInteractionPrompt : MonoBehaviour
    {
        [SerializeField] private GmInteractor interactor;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text label;
        private GmInteractable shownTarget;
        private bool shownGamepad;

        public void SetInteractor(GmInteractor playerInteractor) => interactor = playerInteractor;
        public void SetView(GameObject promptPanel, Text promptLabel) { panel = promptPanel; label = promptLabel; }

        private void Awake()
        {
            if (interactor == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) interactor = player.GetComponent<GmInteractor>();
            }
        }

        private void LateUpdate()
        {
            GmInteractable target = interactor != null && interactor.ControlGate.CanExplore
                ? interactor.CurrentTarget : null;
            if (panel != null && panel.activeSelf != (target != null)) panel.SetActive(target != null);
            bool pad = GmGamepadInput.UsingGamepad;
            if (target == shownTarget && shownGamepad == pad) return;
            shownTarget = target;
            shownGamepad = pad;
            if (label != null)
            {
                string prompt = target != null ? target.Prompt : string.Empty;
                label.text = pad ? prompt.Replace("[E]", "[A]") : prompt;
            }
        }
    }
}
