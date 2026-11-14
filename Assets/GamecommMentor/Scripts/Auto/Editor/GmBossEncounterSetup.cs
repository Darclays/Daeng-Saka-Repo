using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmBossEncounterSetup
    {
        private const string ProfilePath = "Assets/GamecommMentor/Data/NenekPakandePlaytest.asset";
        private const string SpritePath = "Assets/GamecommMentor/Resources/DaengSaka/BattleStage/Boss/pakandeWorld.png";
        private const string EncounterId = "boss-nenek-pakande";

        [MenuItem("GamecommMentor/Daeng Saka/Add Nenek Pakande Playtest")]
        private static void AddPlaytest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
            {
                Debug.LogError("[Daeng Saka] Buka scene eksplorasi yang sudah tersimpan.");
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            GmEnemyEncounter[] encounters = roots.SelectMany(root => root.GetComponentsInChildren<GmEnemyEncounter>(true)).ToArray();
            GmEnemyEncounter existing = encounters.FirstOrDefault(enemy => enemy.EncounterId == EncounterId);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            GmEncounterFlow[] flows = roots.SelectMany(root => root.GetComponentsInChildren<GmEncounterFlow>(true)).ToArray();
            GmSakaMotor[] players = roots.SelectMany(root => root.GetComponentsInChildren<GmSakaMotor>(false))
                .Where(player => player.isActiveAndEnabled).ToArray();
            if (flows.Length != 1 || players.Length != 1 ||
                SceneUtility.GetBuildIndexByScenePath(flows[0].BattleScenePath) < 0)
            {
                Debug.LogError("[Daeng Saka] Buka scene eksplorasi yang sudah dipakai untuk tes 1B.2B.");
                return;
            }

            GmBossBattleProfile profile = AssetDatabase.LoadAssetAtPath<GmBossBattleProfile>(ProfilePath);
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(SpritePath).OfType<Sprite>().FirstOrDefault();
            if (profile == null || sprite == null)
            {
                Debug.LogError("[Daeng Saka] Profile Nenek Pakande atau sprite eksplorasinya belum terimpor.");
                return;
            }

            GmSakaMotor player = players[0];
            Vector3 right = Camera.main != null ? Camera.main.transform.right : player.transform.right;
            right = Vector3.ProjectOnPlane(right, Vector3.up);
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            Vector3 position = player.transform.position + right.normalized * 3.2f;
            if (Physics.Raycast(position + Vector3.up * 4f, Vector3.down, out RaycastHit ground, 8f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                position.y = ground.point.y;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add Nenek Pakande Playtest");
            GameObject marker = new GameObject("Nenek Pakande (Uji Boss)");
            Undo.RegisterCreatedObjectUndo(marker, "Add Nenek Pakande");
            Undo.RecordObject(marker.transform, "Place Nenek Pakande");
            marker.transform.position = position;
            GmEnemyEncounter encounter = Undo.AddComponent<GmEnemyEncounter>(marker);
            SphereCollider trigger = marker.GetComponent<SphereCollider>();
            Rigidbody body = marker.GetComponent<Rigidbody>();
            Undo.RecordObjects(new Object[] { encounter, trigger, body }, "Configure Nenek Pakande");
            encounter.Configure(flows[0], EncounterId, "Nenek Pakande", profile);
            trigger.center = Vector3.up * 0.84f;
            trigger.radius = 0.67f;
            trigger.isTrigger = true;
            body.isKinematic = true;
            body.useGravity = false;

            GameObject visual = new GameObject("Nenek Pakande Sprite");
            Undo.RegisterCreatedObjectUndo(visual, "Add Nenek Pakande Sprite");
            Undo.SetTransformParent(visual.transform, marker.transform, "Parent Nenek Pakande Sprite");
            Undo.RecordObject(visual.transform, "Place Nenek Pakande Sprite");
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            SpriteRenderer renderer = Undo.AddComponent<SpriteRenderer>(visual);
            GmSpriteBillboard billboard = Undo.AddComponent<GmSpriteBillboard>(visual);
            Undo.RecordObjects(new Object[] { renderer, billboard }, "Set Nenek Pakande Sprite");
            renderer.sprite = sprite;
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/GamecommMentor/Art/2D/DialogueExamples/NPC_Unlit.mat");
            if (material != null) renderer.sharedMaterial = material;
            billboard.SetCamera(Camera.main);

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = marker;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Daeng Saka 1B.3B] Nenek Pakande siap. Simpan scene lalu sentuh sprite boss dari eksplorasi.", marker);
        }
    }
}
