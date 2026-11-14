using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    /// <summary>Maps a world heading to four existing sprite views, with a small boundary deadband.</summary>
    public static class GmCardinalFacing
    {
        public static Vector2 FromWorld(Vector3 worldHeading, Camera viewCamera,
            Vector2 previous, float hysteresisDegrees = 3f)
        {
            Vector3 forward = viewCamera != null
                ? Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up) : Vector3.forward;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector2 screen = new Vector2(Vector3.Dot(worldHeading, right), Vector3.Dot(worldHeading, forward));
            if (screen.sqrMagnitude < 0.0001f) return previous.sqrMagnitude > 0f ? previous : Vector2.down;

            float angle = Mathf.Atan2(screen.x, screen.y) * Mathf.Rad2Deg;
            if (previous.sqrMagnitude > 0.5f)
            {
                float oldAngle = Mathf.Atan2(previous.x, previous.y) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(oldAngle, angle)) <= 45f + Mathf.Clamp(hysteresisDegrees, 0f, 10f))
                    return previous;
            }
            return Mathf.Abs(screen.x) > Mathf.Abs(screen.y)
                ? new Vector2(Mathf.Sign(screen.x), 0f) : new Vector2(0f, Mathf.Sign(screen.y));
        }
    }
}
