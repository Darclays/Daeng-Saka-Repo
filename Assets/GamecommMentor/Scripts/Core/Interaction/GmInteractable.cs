using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    public abstract class GmInteractable : MonoBehaviour
    {
        [SerializeField] private string displayName = "Objek";
        [SerializeField] private string actionLabel = "Interaksi";
        [SerializeField] private Transform focusPoint;
        [SerializeField] private Vector3 focusOffset = new Vector3(0f, 0.8f, 0f);

        public virtual string Prompt => "[E] " + actionLabel + " — " + displayName;
        public Vector3 FocusPosition => focusPoint != null ? focusPoint.position : transform.TransformPoint(focusOffset);

        public void SetPresentation(string label, string action)
        {
            displayName = label;
            actionLabel = action;
        }

        public virtual bool CanInteract(GmInteractor requester) => isActiveAndEnabled && requester != null;
        public abstract bool TryInteract(GmInteractor requester);
    }
}
