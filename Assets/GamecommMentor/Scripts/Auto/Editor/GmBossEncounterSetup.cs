using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmBossEncounterSetup
    {
        private const string ProfilePath = "Assets/GamecommMentor/Data/NenekPakandePlaytest.asset";
        private const string ArtPath = "Assets/GamecommMentor/Resources/DaengSaka/BattleStage/Boss/";

        [MenuItem("GamecommMentor/Daeng Saka/Add Nenek Pakande Playtest")]
        private static void AddBossPlaytest() => AddPlaytest("boss-nenek-pakande", "Nenek Pakande",
            "pakandeWorld.png", 3.2f, 0f, false);

        [MenuItem("GamecommMentor/Daeng Saka/Add Poppo Playtest")]
        private static void AddPoppoPlaytest() => AddPlaytest("common-poppo", "Poppo",
            "poppoWorld.png", 2.2f, 3f, true);

        private static void AddPlaytest(string encounterId, string enemyName, string spriteFile,
            float rightOffset, float forwardOffset, bool isPoppo)
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
            GmEnemyEncounter existing = encounters.FirstOrDefault(enemy => enemy.EncounterId == encounterId);
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
                Debug.LogError("[Daeng Saka] Buka scene eksplorasi yang sudah dipakai untuk battle playtest.");
                return;
            }

            GmBossBattleProfile profile = isPoppo ? null : AssetDatabase.LoadAssetAtPath<GmBossBattleProfile>(ProfilePath);
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(ArtPath + spriteFile).OfType<Sprite>().FirstOrDefault();
            if ((!isPoppo && profile == null) || sprite == null)
            {
                Debug.LogError($"[Daeng Saka] Cek aset playtest dan sprite eksplorasi untuk {enemyName}.");
                return;
            }

            GmSakaMotor player = players[0];
            Vector3 right = Camera.main != null ? Camera.main.transform.right : player.transform.right;
            right = Vector3.ProjectOnPlane(right, Vector3.up);
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            Vector3 forward = Vector3.Cross(right.normalized, Vector3.up);
            Vector3 position = player.transform.position + right.normalized * rightOffset + forward * forwardOffset;
            if (Physics.Raycast(position + Vector3.up * 4f, Vector3.down, out RaycastHit ground, 8f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                position.y = ground.point.y;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Add {enemyName} Playtest");
            GameObject marker = new GameObject(isPoppo ? "Poppo (Uji Musuh)" : "Nenek Pakande (Uji Boss)");
            Undo.RegisterCreatedObjectUndo(marker, $"Add {enemyName}");
            Undo.RecordObject(marker.transform, $"Place {enemyName}");
            marker.transform.position = position;
            GmEnemyEncounter encounter = Undo.AddComponent<GmEnemyEncounter>(marker);
            SphereCollider trigger = marker.GetComponent<SphereCollider>();
            Rigidbody body = marker.GetComponent<Rigidbody>();
            Undo.RecordObjects(new Object[] { encounter, trigger, body }, $"Configure {enemyName}");
            encounter.Configure(flows[0], encounterId, enemyName, profile, isPoppo);
            trigger.center = Vector3.up * 0.84f;
            trigger.radius = 0.67f;
            trigger.isTrigger = true;
            body.isKinematic = true;
            body.useGravity = false;

            GameObject visual = new GameObject($"{enemyName} Sprite");
            Undo.RegisterCreatedObjectUndo(visual, $"Add {enemyName} Sprite");
            Undo.SetTransformParent(visual.transform, marker.transform, $"Parent {enemyName} Sprite");
            Undo.RecordObject(visual.transform, $"Place {enemyName} Sprite");
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            SpriteRenderer renderer = Undo.AddComponent<SpriteRenderer>(visual);
            GmSpriteBillboard billboard = Undo.AddComponent<GmSpriteBillboard>(visual);
            Undo.RecordObjects(new Object[] { renderer, billboard }, $"Set {enemyName} Sprite");
            renderer.sprite = sprite;
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/GamecommMentor/Art/2D/DialogueExamples/NPC_Unlit.mat");
            if (material != null) renderer.sharedMaterial = material;
            billboard.SetCamera(Camera.main);

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = marker;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Daeng Saka 1B.5B] {enemyName} siap. Simpan scene lalu sentuh sprite dari eksplorasi.", marker);
        }
    }
}
