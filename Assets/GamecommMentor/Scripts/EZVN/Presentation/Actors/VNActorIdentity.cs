using System;
using UnityEngine;

namespace ProjectVN.Presentation.Actors
{
    [Serializable]
    public struct VNActorIdentity : IEquatable<VNActorIdentity>
    {
        [SerializeField] private int pixelActorId;
        [SerializeField] private string pixelActorName;
        [SerializeField] private string displayName;
        [SerializeField] private string actorKey;

        public int PixelActorId => pixelActorId;
        public string PixelActorName => pixelActorName ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public string ActorKey => actorKey ?? string.Empty;

        public bool IsEmpty =>
            pixelActorId <= 0 &&
            string.IsNullOrWhiteSpace(pixelActorName) &&
            string.IsNullOrWhiteSpace(displayName) &&
            string.IsNullOrWhiteSpace(actorKey);

        public VNActorIdentity(
            int pixelActorId,
            string pixelActorName,
            string displayName,
            string actorKey = "")
        {
            this.pixelActorId = pixelActorId;
            this.pixelActorName = Sanitize(pixelActorName);
            this.displayName = Sanitize(displayName);
            this.actorKey = NormalizeKey(actorKey);
        }

        public VNActorIdentity WithActorKey(string value)
        {
            return new VNActorIdentity(
                pixelActorId,
                pixelActorName,
                displayName,
                value);
        }

        public VNActorIdentity WithDisplayName(string value)
        {
            return new VNActorIdentity(
                pixelActorId,
                pixelActorName,
                value,
                actorKey);
        }

        public bool Equals(VNActorIdentity other)
        {
            return pixelActorId == other.pixelActorId &&
                   string.Equals(
                       NormalizeLookup(pixelActorName),
                       NormalizeLookup(other.pixelActorName),
                       StringComparison.Ordinal) &&
                   string.Equals(
                       NormalizeLookup(displayName),
                       NormalizeLookup(other.displayName),
                       StringComparison.Ordinal) &&
                   string.Equals(
                       NormalizeKey(actorKey),
                       NormalizeKey(other.actorKey),
                       StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is VNActorIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = pixelActorId;
                hash = (hash * 397) ^ NormalizeLookup(pixelActorName).GetHashCode();
                hash = (hash * 397) ^ NormalizeLookup(displayName).GetHashCode();
                hash = (hash * 397) ^ NormalizeKey(actorKey).GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"ID={pixelActorId}, PixelName='{PixelActorName}', DisplayName='{DisplayName}', ActorKey='{ActorKey}'";
        }

        public static bool operator ==(VNActorIdentity left, VNActorIdentity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VNActorIdentity left, VNActorIdentity right)
        {
            return !left.Equals(right);
        }

        public static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().ToLowerInvariant().Replace(" ", string.Empty);
        }

        public static string NormalizeLookup(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}
