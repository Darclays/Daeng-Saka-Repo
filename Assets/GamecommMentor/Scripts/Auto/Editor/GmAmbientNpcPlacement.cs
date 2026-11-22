using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal sealed class GmAmbientNpcPlacement
    {
        internal const string NpcArt = "Assets/Asset2D_Pixel/NPC/";

        private readonly GmWorldSceneLayout layout;
        private readonly SpriteRenderer reference;
        private readonly Camera camera;
        private readonly Vector3[] reserved;
        private readonly string undoName;

        internal Transform Group { get; }

        internal GmAmbientNpcPlacement(Transform world, string groupName, GmWorldSceneLayout layout,
            SpriteRenderer reference, Vector3[] reserved, string undoName)
        {
            this.layout = layout;
            this.reference = reference;
            this.reserved = reserved;
            this.undoName = undoName;
            camera = Require<Camera>(RequireChild(world, "Saka - Orbit Camera"));
            Group = world.Find(groupName);
            if (Group == null) Group = CreateChild(world, groupName);
        }

        internal void Add(string name, Vector3 marker, GmDialogueNpcSprites.Prepared art, float height, float radius)
        {
            // Keep the placement of any actor already adjusted in the scene.
            if (Group.Find(name) != null) return;

            Transform actor = CreateChild(Group, name);
            Undo.RecordObject(actor, undoName);
            layout.Place(actor, marker, -layout.IntoLevel, reserved);
            var body = AddComponent<CapsuleCollider>(actor);
            Undo.RecordObject(body, undoName);
            body.height = height;
            body.radius = radius;
            body.center = Vector3.up * (height * 0.5f);

            Transform visual = CreateChild(actor, "Visual");
            var renderer = AddComponent<SpriteRenderer>(visual);
            var animator = AddComponent<Animator>(visual);
            var billboard = AddComponent<GmSpriteBillboard>(visual);
            Undo.RecordObject(renderer, undoName);
            Undo.RecordObject(animator, undoName);
            Undo.RecordObject(billboard, undoName);
            renderer.sprite = art.First;
            if (reference.sharedMaterial != null) renderer.sharedMaterial = reference.sharedMaterial;
            renderer.sortingLayerID = reference.sortingLayerID;
            renderer.sortingOrder = reference.sortingOrder;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            animator.runtimeAnimatorController = art.Controller;
            animator.applyRootMotion = false;
            billboard.SetCamera(camera);
        }

        internal static GmDialogueNpcSprites.Prepared PrepareGuard() => GmDialogueNpcSprites.Prepare(
            NpcArt + "NPC_RoyalGuard/NPC_RoyalGuard_Idle/NPC_RoyalGuard_Idle_Front.png", "IbuKota_Penjaga");

        internal static GmDialogueNpcSprites.Prepared PrepareCommonerMother() => GmDialogueNpcSprites.Prepare(
            NpcArt + "NPC_Commoner/NPC_Commoner_Ibu/NPC_Commoner_Ibu_Norm/" +
            "NPC_Commoner_Ibu_Norm_Idle/NPC_Commoner_Ibu_Norm_Idle_Front.png", "IbuKota_WargaIbu");

        internal static GmDialogueNpcSprites.Prepared PrepareCommonerFather() => GmDialogueNpcSprites.Prepare(
            NpcArt + "NPC_Commoner/NPC_Commoner_Ayah/NPC_Commoner_Ayah_Norm/" +
            "NPC_Commoner_Ayah_Norm_Idle/NPC_Commoner_Ayah_Norm_Idle_Front.png", "IbuKota_Pua");

        internal static bool CanInstall(string path) => !EditorApplication.isPlayingOrWillChangePlaymode &&
            !EditorApplication.isCompiling && SceneManager.GetActiveScene().path == path;

        internal static void Run(string undoName, Func<Scene, Transform> install)
        {
            Scene scene = SceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            try
            {
                Transform group = install(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeGameObject = group.gameObject;
                Debug.Log($"[Daeng Saka 1E.12B R1] {group.childCount} NPC siap di {scene.name}. Simpan scene.", group);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
            }
        }

        internal static Transform RequireRoot(Scene scene, string name)
        {
            GameObject root = scene.GetRootGameObjects().SingleOrDefault(candidate => candidate.name == name);
            if (root == null) throw new InvalidOperationException($"'{name}' belum ada di {scene.name}.");
            return root.transform;
        }

        internal static Transform RequireChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null) throw new InvalidOperationException($"'{name}' belum ada di bawah '{parent.name}'.");
            return child;
        }

        internal static T Require<T>(Transform owner) where T : Component
        {
            if (owner.TryGetComponent(out T component)) return component;
            throw new InvalidOperationException($"'{owner.name}' memerlukan {typeof(T).Name}.");
        }

        internal static T GetOrAdd<T>(Transform owner) where T : Component
        {
            if (owner.TryGetComponent(out T component)) return component;
            return AddComponent<T>(owner);
        }

        internal static T AddComponent<T>(Transform owner) where T : Component
        {
            T component = Undo.AddComponent<T>(owner.gameObject);
            if (component == null)
                throw new InvalidOperationException($"Gagal menambahkan {typeof(T).Name} ke '{owner.name}'.");
            return component;
        }

        private Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            SceneManager.MoveGameObjectToScene(child, parent.gameObject.scene);
            child.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(child, undoName);
            return child.transform;
        }
    }
}
