using System.Collections.Generic;
using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    /// <summary>Independent systems block exploration using their own component as owner.</summary>
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    public sealed class GmControlGate : MonoBehaviour
    {
        [SerializeField] private bool blockControls;
        private readonly HashSet<Object> owners = new HashSet<Object>();
        private readonly List<Object> destroyedOwners = new List<Object>();

        public bool CanExplore => isActiveAndEnabled && !blockControls && owners.Count == 0;

        public void Block(Object owner)
        {
            if (owner != null) owners.Add(owner);
        }

        public void Unblock(Object owner)
        {
            // ReferenceEquals also permits removing a destroyed Unity object.
            if (!ReferenceEquals(owner, null)) owners.Remove(owner);
        }

        private void Update()
        {
            destroyedOwners.Clear();
            foreach (Object owner in owners)
                if (owner == null) destroyedOwners.Add(owner);
            foreach (Object owner in destroyedOwners) owners.Remove(owner);
        }
    }
}
