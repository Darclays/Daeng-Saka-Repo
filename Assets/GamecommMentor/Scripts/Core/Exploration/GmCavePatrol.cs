using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmEnemyEncounter))]
    public sealed class GmCavePatrol : MonoBehaviour
    {
        private enum Mode { Return, Patrol, Alert, Chase }

        private const float FootLift = .035f;
        private const float MoveRadius = .29f;
        private const float DetectionRadius = 3.8f;
        private const float PursuitRadius = 5.2f;
        private const float AlertSeconds = .65f;
        private const float ReturnGrace = 3.0f;
        private const float PauseSeconds = .75f;

        [SerializeField] private GmSakaMotor player;
        [SerializeField] private GmMainQuestProgress quest;
        [SerializeField] private TerrainCollider terrain;
        [SerializeField] private Transform home, farPoint, visual;
        [SerializeField] private GameObject alert;
        [SerializeField] private bool usePlacedRoute;
        [SerializeField, Min(.5f)] private float patrolDistance = 3.8f;
        [SerializeField] private bool floatWhilePatrolling;
        [SerializeField, Min(.1f)] private float warningHeight = 1.34f;
        private readonly RaycastHit[] sightHits = new RaycastHit[16];
        private SphereCollider contact;
        private Rigidbody body;
        private GmControlGate playerGate;
        private Mode mode;
        private bool towardFar;
        private float graceUntil, resumeAfter, alertUntil;
        private float hover = GmGoaArrivalIntro.PreviewHeight;
        private Vector3? previousContact;
        private Collider placedFloor;
        private Vector3 placedHome, placedDestination, visualOffset;
        private bool placementReady;

        public void Configure(GmSakaMotor hero, GmMainQuestProgress progress, TerrainCollider floor,
            Transform near, Transform far, Transform sprite, GameObject warning)
        {
            player = hero;
            quest = progress;
            terrain = floor;
            home = near;
            farPoint = far;
            visual = sprite;
            alert = warning;
        }

        internal bool DrivesMouthVisual => !usePlacedRoute && isActiveAndEnabled && quest != null && quest.isActiveAndEnabled &&
            quest.Step == GmMainQuestStep.SelidikiGua;

        private bool HasPlacedFloor => usePlacedRoute && placementReady && placedFloor != null;
        private bool UsesStationaryFallback => usePlacedRoute && placementReady && placedFloor == null;
        private bool CanPatrol => isActiveAndEnabled && body != null && (usePlacedRoute ? HasPlacedFloor :
            DrivesMouthVisual && home != null && farPoint != null && terrain != null);
        private Vector3 HomePosition => usePlacedRoute ? placedHome : home.position;
        private Vector3 Destination => usePlacedRoute ? placedDestination : farPoint.position;

        internal bool IsFollowing(GmSakaMotor hero) => hero == player && CanPatrol &&
            (mode == Mode.Alert || mode == Mode.Chase) && CanSeePlayer(PursuitRadius, requireFacing: false);

        internal bool CanStartContact(GmSakaMotor hero) =>
            UsesStationaryFallback || (mode == Mode.Chase && IsFollowing(hero));

        internal void RequireNewApproach(Vector3 contactPosition) => previousContact = contactPosition;

        private void Awake()
        {
            contact = GetComponent<SphereCollider>();
            body = GetComponent<Rigidbody>();
            if (player != null) playerGate = player.GetComponent<GmControlGate>();
        }

        private void Start()
        {
            if (!usePlacedRoute) return;
            placementReady = true;
            if (player == null)
                foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                {
                    player = root.transform.GetComponentInChildren<GmSakaMotor>();
                    if (player != null) break;
                }
            if (player != null) playerGate = player.GetComponent<GmControlGate>();

            placedFloor = FindPlacedFloor();
            if (playerGate == null || placedFloor == null || !TryGroundOn(placedFloor, body.position, out placedHome))
            {
                placedFloor = null;
                SetContact(true);
                return;
            }

            placedDestination = placedHome + Flat(transform.forward).normalized * Mathf.Max(.5f, patrolDistance);
            body.position = placedHome;
            if (visual == null) return;
            visualOffset = visual.localPosition;
            if (alert == null) alert = MakeWarning(visual, "Waspada", warningHeight);
        }

        private Collider FindPlacedFloor()
        {
            Vector3 origin = body.position + Vector3.up * .6f;
            Collider floor = null;
            float nearest = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(origin, Vector3.down, 1.8f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == null || hit.collider is CharacterController || hit.collider.transform.IsChildOf(transform) ||
                    hit.normal.y < .74f || hit.distance >= nearest) continue;
                floor = hit.collider;
                nearest = hit.distance;
            }
            return floor;
        }

        private void OnEnable()
        {
            mode = Mode.Return;
            graceUntil = Time.time + ReturnGrace;
            SetWarning(false);
            SetContact(UsesStationaryFallback);
        }

        private void OnDisable()
        {
            SetWarning(false);
            SetContact(false);
        }

        private void LateUpdate()
        {
            if (visual == null) return;
            if (usePlacedRoute)
            {
                if (HasPlacedFloor && floatWhilePatrolling)
                    visual.localPosition = visualOffset + Vector3.up * (Mathf.Sin(Time.time * 4.8f) * .10f);
                return;
            }
            if (!DrivesMouthVisual)
            {
                hover = GmGoaArrivalIntro.PreviewHeight;
                return;
            }

            if (!visual.gameObject.activeSelf) visual.gameObject.SetActive(true);
            hover = Mathf.MoveTowards(hover, .50f, Time.deltaTime * 2.7f);
            visual.localPosition = Vector3.up * (hover + Mathf.Sin(Time.time * 4.8f) * .11f);
        }

        private void FixedUpdate()
        {
            if (!CanPatrol ||
                player == null || !player.isActiveAndEnabled || playerGate == null || !playerGate.CanExplore ||
                gameObject.scene != SceneManager.GetActiveScene())
            {
                if (mode == Mode.Alert || mode == Mode.Chase) ReturnToRoute();
                return;
            }

            float now = Time.time;
            if (previousContact.HasValue && Flat(player.transform.position - previousContact.Value).sqrMagnitude >= 2.25f)
                previousContact = null;
            if (mode == Mode.Return)
            {
                if (MoveToward(HomePosition, 1.22f) && Flat(HomePosition - body.position).sqrMagnitude < .04f)
                {
                    mode = Mode.Patrol;
                    towardFar = true;
                    resumeAfter = now + PauseSeconds;
                }
                return;
            }

            if (mode == Mode.Patrol)
            {
                if (!previousContact.HasValue && now >= graceUntil && CanSeePlayer(DetectionRadius, requireFacing: true))
                {
                    mode = Mode.Alert;
                    alertUntil = now + AlertSeconds;
                    Face(player.transform.position - body.position);
                    SetWarning(true);
                    return;
                }

                if (now < resumeAfter) return;
                Vector3 target = towardFar ? Destination : HomePosition;
                if (!MoveToward(target, 1.22f) || Flat(target - body.position).sqrMagnitude < .04f)
                {
                    towardFar = !towardFar;
                    resumeAfter = now + PauseSeconds;
                }
                return;
            }

            if (!CanSeePlayer(PursuitRadius, requireFacing: false))
            {
                ReturnToRoute();
                return;
            }

            Vector3 towardPlayer = player.transform.position - body.position;
            if (mode == Mode.Alert)
            {
                Face(towardPlayer);
                if (now < alertUntil) return;
                mode = Mode.Chase;
                SetContact(true);
            }

            if (Flat(towardPlayer).sqrMagnitude > .29f && !MoveToward(player.transform.position, 2.45f))
                ReturnToRoute();
        }

        private bool CanSeePlayer(float range, bool requireFacing)
        {
            Vector3 playerPosition = player.transform.position;
            Vector3 span = playerPosition - body.position;
            Vector3 flat = Flat(span);
            if (flat.sqrMagnitude > range * range || Mathf.Abs(span.y) > 1.6f) return false;
            // The chase is confined to the same stretch of road, even if the target keeps running.
            Vector3 route = Flat(Destination - HomePosition);
            float along = route.sqrMagnitude > .01f
                ? Mathf.Clamp01(Vector3.Dot(Flat(playerPosition - HomePosition), route) / route.sqrMagnitude) : 0f;
            if (Flat(playerPosition - (HomePosition + route * along)).sqrMagnitude > PursuitRadius * PursuitRadius)
                return false;
            if (requireFacing && flat.sqrMagnitude > .36f && Vector3.Dot(transform.forward, flat.normalized) < .42f)
                return false;

            Vector3 from = body.position + Vector3.up * .84f;
            Vector3 delta = playerPosition + Vector3.up * .84f - from;
            int count = Physics.RaycastNonAlloc(from, delta.normalized, sightHits, delta.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == sightHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Transform hit = sightHits[i].collider.transform;
                if (!hit.IsChildOf(transform) && !hit.IsChildOf(player.transform)) return false;
            }
            return true;
        }

        private bool MoveToward(Vector3 target, float speed)
        {
            Vector3 travel = Flat(target - body.position);
            if (travel.sqrMagnitude < .0144f) return true;
            Vector3 step = Vector3.ClampMagnitude(travel, speed * Time.fixedDeltaTime);
            if (!TryGroundOn(usePlacedRoute ? placedFloor : terrain, body.position + step, out Vector3 feet) ||
                Mathf.Abs(feet.y - body.position.y) > .32f) return false;
            Face(travel);
            body.MovePosition(feet);
            return true;
        }

        public static bool TryGround(TerrainCollider terrain, Vector3 sample, out Vector3 feet) =>
            TryGroundOn(terrain, sample, out feet);

        private static bool TryGroundOn(Collider floor, Vector3 sample, out Vector3 feet)
        {
            feet = default;
            if (!floor.Raycast(new Ray(sample + Vector3.up * 1.6f, Vector3.down), out var hit, 3.4f) ||
                hit.normal.y < .74f) return false;
            feet = hit.point + Vector3.up * FootLift;
            return !Physics.CheckCapsule(feet + Vector3.up * .34f, feet + Vector3.up * 1.04f, MoveRadius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        public static GameObject MakeWarning(Transform visual, string name, float height)
        {
            Transform warning = visual.Find(name);
            if (warning != null) return warning.gameObject;
            var marker = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(marker, visual.gameObject.scene);
            var rect = (RectTransform)marker.transform;
            rect.SetParent(visual, false);
            rect.localPosition = Vector3.up * height;
            rect.localScale = Vector3.one * .009f;
            rect.sizeDelta = new Vector2(56, 56);
            var canvas = marker.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 6;

            var label = new GameObject("Tanda waspada", typeof(RectTransform), typeof(Text));
            var labelRect = (RectTransform)label.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var symbol = label.GetComponent<Text>();
            symbol.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            symbol.text = "!";
            symbol.alignment = TextAnchor.MiddleCenter;
            symbol.fontSize = 49;
            symbol.raycastTarget = false;
            symbol.supportRichText = false;
            symbol.resizeTextForBestFit = true;
            symbol.resizeTextMinSize = 12;
            symbol.resizeTextMaxSize = symbol.fontSize;
            symbol.color = new Color32(255, 198, 82, 255);
            symbol.fontStyle = FontStyle.Bold;
            var stroke = symbol.gameObject.AddComponent<Outline>();
            stroke.effectColor = new Color32(41, 26, 20, 255);
            stroke.effectDistance = new Vector2(2, -2);
            marker.SetActive(false);
            return marker;
        }

        private void ReturnToRoute()
        {
            mode = Mode.Return;
            graceUntil = Time.time + ReturnGrace;
            SetWarning(false);
            SetContact(false);
        }

        private void Face(Vector3 direction)
        {
            Vector3 forward = Flat(direction);
            if (forward.sqrMagnitude > .001f) body.MoveRotation(Quaternion.LookRotation(forward, Vector3.up));
        }

        private void SetWarning(bool visible)
        {
            if (alert != null && alert.activeSelf != visible) alert.SetActive(visible);
        }

        private void SetContact(bool enabled)
        {
            if (contact != null) contact.enabled = enabled;
        }

        private static Vector3 Flat(Vector3 vector) => Vector3.ProjectOnPlane(vector, Vector3.up);
    }
}
