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
        private string shownPrompt;
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
            string prompt = target != null ? target.Prompt : string.Empty;
            if (prompt == shownPrompt && shownGamepad == pad) return;
            shownPrompt = prompt;
            shownGamepad = pad;
            if (label != null)
            {
                label.text = pad ? prompt.Replace("[E]", "[A]") : prompt;
            }
        }
    }
}
