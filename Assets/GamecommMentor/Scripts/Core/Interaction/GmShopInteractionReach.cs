using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmShopInteractionReach : MonoBehaviour
    {
        [SerializeField] private SphereCollider volume;
        [SerializeField] private Transform[] counters;

        public void Configure(SphereCollider sensor, params Transform[] allowedCounters)
        {
            volume = sensor;
            counters = allowedCounters;
        }

        internal bool Contains(Vector3 origin)
        {
            if (!isActiveAndEnabled || volume == null || !volume.enabled || !volume.gameObject.activeInHierarchy) return false;
            Vector3 point = volume.transform.InverseTransformPoint(origin) - volume.center;
            return point.sqrMagnitude <= volume.radius * volume.radius;
        }

        internal bool IsCounter(Transform obstacle)
        {
            if (counters == null) return false;
            foreach (Transform counter in counters)
                if (counter != null && obstacle.IsChildOf(counter)) return true;
            return false;
        }
    }
}
