using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;
using ActorCue = GamecommMentor.DaengSaka.GmSoreliaRescueCutscene.ActorCue;
using Shot = GamecommMentor.DaengSaka.GmSoreliaRescueCutscene.Shot;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static partial class GmSoreliaRescueCutsceneSetup
    {
        private const string TimberModel = "3D_DesaSorelia_Rumah_1";
        private const string RedModel = "3D_DesaSorelia_Rumah_2";

        private readonly struct Stairs
        {
            internal readonly float X, DeckY, DeckEdge, Start, Toe, ToeY, Road, Width;

            internal Stairs(float x, float deckY, float deckEdge, float start, float toe, float toeY,
                float road, float width)
            {
                X = x;
                DeckY = deckY;
                DeckEdge = deckEdge;
                Start = start;
                Toe = toe;
                ToeY = toeY;
                Road = road;
                Width = width;
            }
        }

        // House-local coordinates measured from the FBX porch floor, the clear opening and the treads.
        private static readonly Stairs TimberStairs = new Stairs(-4.19f, 1.513f, 1.095f, 1.55f, 3.54f, .225f, 4.50f, 1.36f);
        private static readonly Stairs RedStairs = new Stairs(3.067f, 1.050f, 4.503f, 4.57f, 6.53f, .14f, 7.35f, 1.72f);

        private readonly struct Terrace
        {
            internal readonly Transform House, Top, Foot, Road;
            private readonly float deckY;

            internal Terrace(Transform house, float deckY, Transform top, Transform foot, Transform road)
            {
                House = house;
                this.deckY = deckY;
                Top = top;
                Foot = foot;
                Road = road;
            }

            internal Vector3 OnDeck(float x, float z) => House.TransformPoint(new Vector3(x, deckY + .025f, z));
        }

        private static (Terrace Timber, Terrace Red) SetUpHouseAccess(Transform houses, TerrainCollider terrain)
        {
            Transform timber = RequireChild(houses, TimberModel);
            Transform red = RequireChild(houses, RedModel);
            for (int i = 0; i < houses.childCount; i++)
            {
                Transform other = houses.GetChild(i);
                if (other.name.StartsWith(TimberModel + " (", StringComparison.Ordinal))
                    SetUpHouse(other, TimberStairs, terrain);
                else if (other.name.StartsWith(RedModel + " (", StringComparison.Ordinal))
                    SetUpHouse(other, RedStairs, terrain);
            }
            var usedInCutscene = (SetUpHouse(timber, TimberStairs, terrain), SetUpHouse(red, RedStairs, terrain));
            Physics.SyncTransforms();
            return usedInCutscene;
        }

        private static Terrace SetUpHouse(Transform house, Stairs stairs, TerrainCollider terrain)
        {
            MeshFilter[] models = house.GetComponentsInChildren<MeshFilter>()
                .Where(filter => filter.sharedMesh != null).ToArray();
            if (models.Length == 0)
                throw new InvalidOperationException($"Mesh rumah '{house.name}' belum tersedia.");

            foreach (MeshFilter model in models)
            {
                bool hasSurface = model.GetComponents<MeshCollider>().Any(collider =>
                    collider.sharedMesh == model.sharedMesh && collider.enabled && !collider.convex && !collider.isTrigger);
                if (hasSurface) continue;
                var collider = Undo.AddComponent<MeshCollider>(model.gameObject);
                if (collider == null)
                    throw new InvalidOperationException($"Gagal menambahkan MeshCollider pada '{model.name}'.");
                Undo.RecordObject(collider, UndoName);
                collider.sharedMesh = model.sharedMesh;
            }

            Transform access = Child(house, "Akses Teras");
            var deck = new Vector3(stairs.X, stairs.DeckY, stairs.DeckEdge - .07f);
            var top = new Vector3(stairs.X, stairs.DeckY, stairs.Start);
            var foot = new Vector3(stairs.X, stairs.ToeY, stairs.Toe);
            Vector3 roadWorld = Ground(terrain, house.TransformPoint(new Vector3(stairs.X, stairs.ToeY, stairs.Road)));
            Vector3 road = house.InverseTransformPoint(roadWorld - Vector3.up * .025f);

            AddWalkSurface(access, "Sambungan Teras", deck, top, stairs.Width);
            AddWalkSurface(access, "Ramp Tangga", top, foot, stairs.Width);
            AddWalkSurface(access, "Sambungan ke Tanah", foot, road, stairs.Width);
            Transform At(string name, Vector3 point) =>
                Mark(access, name, house.TransformPoint(point) + Vector3.up * .025f);
            return new Terrace(house, stairs.DeckY, At("Tangga Atas", top),
                At("Tangga Bawah", foot), At("Kaki Tangga", road));
        }

        private static void AddWalkSurface(Transform parent, string name, Vector3 start, Vector3 end, float width)
        {
            Transform existing = parent.Find(name);
            if (existing != null && existing.GetComponent<BoxCollider>() != null) return;
            const float thickness = .12f;
            Vector3 slope = end - start;
            Quaternion angle = Quaternion.LookRotation(slope, Vector3.up);
            Transform walkSurface = Child(parent, name);
            Undo.RecordObject(walkSurface, UndoName);
            walkSurface.localPosition = (start + end) * .5f - angle * Vector3.up * (thickness * .5f);
            walkSurface.localRotation = angle;
            var box = Component<BoxCollider>(walkSurface);
            Undo.RecordObject(box, UndoName);
            box.center = Vector3.zero;
            // A small seam overlap keeps the CharacterController supported across adjacent surfaces.
            box.size = new Vector3(width, thickness, slope.magnitude + .035f);
        }

        private static (Shot Wide, Shot Close) BuildTerraceShots(Transform root, Terrace timber, Terrace red,
            TerrainCollider terrain)
        {
            Transform actors = RequireChild(root, "Aktor Cutscene");
            Transform panel3 = Child(root, "Panel 3 - Warga Keluar");
            Transform panel4 = Child(root, "Panel 4 - Turun Teras");

            GmCutsceneSprite Actor(string name) => Require<GmCutsceneSprite>(RequireChild(actors, name));
            Transform Path(Transform panel, GmCutsceneSprite actor, string name, Vector3 point) =>
                SetMark(Child(panel, actor.gameObject.name), name, point, Quaternion.identity);
            Transform CameraAt(Transform panel, string name, Vector3 eye, Vector3 target) =>
                SetMark(panel, name, eye, Quaternion.LookRotation(target - eye, Vector3.up));

            var bapak = Actor("Bapak Commoner");
            var ibu = Actor("Ibu Commoner");
            var child = Actor("Anak Nelayan");
            var fisher = Actor("Ibu Nelayan");

            Transform bapakStart = Path(panel3, bapak, "Awal", timber.OnDeck(-3.05f, -.63f));
            Transform bapakTurn = Path(panel3, bapak, "Belok Teras", timber.OnDeck(-4.19f, -.32f));
            Transform bapakStop = Path(panel3, bapak, "Akhir",
                Vector3.Lerp(timber.Top.position, timber.Foot.position, .24f));
            Transform ibuStart = Path(panel3, ibu, "Awal", timber.OnDeck(-3.05f, -1.64f));
            Transform ibuTurn = Path(panel3, ibu, "Belok Teras", timber.OnDeck(-4.19f, -1.17f));
            Transform ibuStop = Path(panel3, ibu, "Akhir", timber.OnDeck(-4.19f, .89f));

            Transform childStart = Path(panel3, child, "Awal", red.OnDeck(3.35f, 1.75f));
            Transform childStop = Path(panel3, child, "Akhir", red.OnDeck(3.067f, 4.13f));
            Transform fisherStart = Path(panel3, fisher, "Awal", red.OnDeck(3.55f, .66f));
            Transform fisherStop = Path(panel3, fisher, "Akhir", red.OnDeck(3.067f, 3.10f));
            Vector3 clearOfStairs = Ground(terrain, red.House.TransformPoint(new Vector3(3.77f, .13f, 8.30f)));
            Transform childDown = Path(panel4, child, "Akhir", clearOfStairs);
            Transform fisherDown = Path(panel4, fisher, "Akhir", Vector3.Lerp(red.Foot.position, red.Road.position, .72f));
            Transform childLook = SetMark(panel4, "Arah ke Jalan",
                red.House.TransformPoint(new Vector3(3.067f, .15f, 10f)), Quaternion.identity);

            Vector3 front = Flat(timber.House.forward + red.House.forward).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, front);
            Vector3 center = (timber.House.position + red.House.position) * .5f;
            Vector3 wideEye = center + front * 17.2f + Vector3.up * 6.4f;
            Vector3 wideFocus = center + front * 1.50f + Vector3.up * 2.55f;
            Transform wideStart = CameraAt(panel3, "Kamera Awal", wideEye - side * 1.4f, wideFocus - side * .55f);
            Transform wideEnd = CameraAt(panel3, "Kamera Akhir", wideEye + side * 1.5f, wideFocus + side * .55f);
            Vector3 closeEye = red.House.TransformPoint(new Vector3(5.15f, 4.12f, 12.65f));
            Vector3 closeTarget = red.House.TransformPoint(new Vector3(3.05f, 1.36f, 4.70f));
            Transform closeStart = CameraAt(panel4, "Kamera Awal", closeEye, closeTarget);
            Transform closeEnd = CameraAt(panel4, "Kamera Akhir",
                red.House.TransformPoint(new Vector3(4.62f, 3.98f, 13.12f)),
                red.House.TransformPoint(new Vector3(3.12f, .96f, 5.60f)));

            return (
                new Shot(wideStart, wideEnd, 2.65f, 53f, new[]
                {
                    new ActorCue(child, childStart, childStop, red.Road, .28f, 1.55f, false),
                    new ActorCue(fisher, fisherStart, fisherStop, red.Road, .63f, 1.72f, false),
                    new ActorCue(bapak, bapakStart, bapakStop, timber.Road, .40f, 1.91f, false, bapakTurn, timber.Top),
                    new ActorCue(ibu, ibuStart, ibuStop, timber.Road, .71f, 1.85f, false, ibuTurn)
                }),
                new Shot(closeStart, closeEnd, 2.53f, 51f, new[]
                {
                    new ActorCue(child, childStop, childDown, childLook, .03f, 1.72f, false, red.Top, red.Foot),
                    new ActorCue(fisher, fisherStop, fisherDown, childLook, .34f, 2.04f, false, red.Top, red.Foot)
                }));
        }

        private static Transform SetMark(Transform parent, string name, Vector3 point, Quaternion rotation)
        {
            Transform marker = Child(parent, name);
            Undo.RecordObject(marker, UndoName);
            marker.SetPositionAndRotation(point, rotation);
            return marker;
        }
    }
}
