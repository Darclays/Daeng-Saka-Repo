using System;
using System.Linq;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmWorldSceneSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Demo World";
        private const string CaveRepairMenu = "GamecommMentor/Daeng Saka/Repair Goa Finale";
        private const string SceneFolder = "Assets/GamecommMentor/Scenes_GamecommMentor/";
        private const string CorePrefabs = "Assets/GamecommMentor/Prefabs/Core/";
        private const string EnemyArt = "Assets/GamecommMentor/Resources/DaengSaka/BattleStage/Boss/";
        private const string WorldSpriteMaterial = "Assets/GamecommMentor/Art/2D/DialogueExamples/NPC_Unlit.mat";
        private const string Capital = SceneFolder + "IbuKota.unity";
        private const string Village = SceneFolder + "DesaSorelia.unity";
        private const string Cave = SceneFolder + "GoaLembanyung.unity";
        private const string Battle = SceneFolder + "Battle_Encounter_Test.unity";
        private const string WorldRoot = "Daeng Saka - World Gameplay";
        private const string FlowRoot = "Daeng Saka - Encounter Flow";
        private const string VillageStart = "PortalPoint_StartRespawnDesaSoralia";
        private const string VillageExit = "PortalPoint_GoGoaLembayung";
        private const string CaveEntrance = "PortalPoint_StartRespawnGoaLembayung";
        private const string CaveGuardName = "Musuh Goa";
        private const string PakandeName = "Nenek Pakande";
        private const string CaveGuideName = "Pemandu Goa (Uji)";
        private const string QuestHintName = "Quest Encounter Hint";

        private static readonly GmMainQuestStep[] CaveDialogueSteps =
        {
            GmMainQuestStep.MasukGuaLembayung,
            GmMainQuestStep.SelidikiGua,
            GmMainQuestStep.TemuiPakande,
            GmMainQuestStep.LawanPakande,
            GmMainQuestStep.Penutup
        };

        private sealed class WorldAssets
        {
            public readonly GameObject player = Load<GameObject>(CorePrefabs + "Dialogue/Saka_Interaction.prefab");
            public readonly GameObject camera = Load<GameObject>(CorePrefabs + "Camera/Horizontal_Orbit_Camera.prefab");
            public readonly GameObject manager = Load<GameObject>(CorePrefabs + "Dialogue/Dialogue_Manager.prefab");
            public readonly GameObject merchant = Load<GameObject>(CorePrefabs + "Dialogue/NPC_Pedagang.prefab");
            public readonly GameObject fisherman = Load<GameObject>(CorePrefabs + "Dialogue/NPC_Nelayan.prefab");
            public readonly GameObject prompt = Load<GameObject>("Assets/GamecommMentor/Prefabs/UI/Dialogue/Interaction_Prompt.prefab");
            public readonly Sprite poppo = Load<Sprite>(EnemyArt + "poppoWorld.png");
            public readonly Material spriteMaterial = Load<Material>(WorldSpriteMaterial);
        }

        [MenuItem(Menu)]
        private static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            bool openedWorld = false;
            try
            {
                foreach (string path in new[] { Capital, Village, Cave, Battle }) Load<SceneAsset>(path);
                openedWorld = true;
                Scene capital = OpenLevel(Capital, OpenSceneMode.Single);
                Scene village = OpenLevel(Village, OpenSceneMode.Additive);
                Scene cave = OpenLevel(Cave, OpenSceneMode.Additive);
                var assets = new WorldAssets();
                CharacterController body = Require<CharacterController>(assets.player);
                Physics.SyncTransforms();
                var capitalLayout = GmCapitalRouteSetup.CreateLayout(capital, body);
                var villageLayout = new GmWorldSceneLayout(village, VillageStart, VillageExit, body);
                var caveLayout = new GmWorldSceneLayout(cave, CaveEntrance, CaveEntrance, body);
                GmPixelCrushersMainQuestDialogue.StepDialogue[] dialogues = PrepareDialogueDatabase();

                ConfigureLevel(capital, capitalLayout, assets, dialogues);
                ConfigureLevel(village, villageLayout, assets, dialogues);
                ConfigureLevel(cave, caveLayout, assets, dialogues);
                VerifyCaveFinale(cave);
                foreach (Scene scene in new[] { capital, village, cave })
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException("[Daeng Saka] Gagal simpan " + scene.path);
                RegisterScenes();
                capital = OpenLevel(Capital, OpenSceneMode.Single);
                Selection.activeGameObject = capital.GetRootGameObjects().Single(root => root.name == WorldRoot);
                Debug.Log("[Daeng Saka 1C.4B-R1] Demo World siap. Mulai dari IbuKota untuk menguji rangkaian quest sampai penutup Goa.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (openedWorld)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    if (previous.Length > 0 && previous.All(scene => !string.IsNullOrEmpty(scene.path)))
                        EditorSceneManager.RestoreSceneManagerSetup(previous);
                }
            }
        }

        [MenuItem(CaveRepairMenu, true)]
        private static bool CanRepairCaveFinale() =>
            !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;

        [MenuItem(CaveRepairMenu)]
        private static void RepairCaveFinale()
        {
            if (!CanRepairCaveFinale() || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            bool opened = false;
            bool saved = false;
            try
            {
                Load<SceneAsset>(Cave);
                opened = true;
                Scene cave = OpenLevel(Cave, OpenSceneMode.Single);
                UpgradeCaveFinale(cave);
                VerifyCaveFinale(cave);
                saved = EditorSceneManager.SaveScene(cave);
                if (!saved) throw new InvalidOperationException("Gagal menyimpan " + cave.path);

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Scene reopened = OpenLevel(Cave, OpenSceneMode.Single);
                GameObject world = VerifyCaveFinale(reopened);
                Selection.activeGameObject = world;
                Debug.Log($"[Daeng Saka 1C.4B-R1] Goa finale tersimpan dan terverifikasi di {reopened.path}. " +
                    "Cek Musuh Goa, Nenek Pakande, dan Quest Encounter Hint di Hierarchy.", world);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (opened && !saved)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    if (previous.Length > 0 && previous.All(scene => !string.IsNullOrEmpty(scene.path)))
                        EditorSceneManager.RestoreSceneManagerSetup(previous);
                }
            }
        }

        private static void UpgradeCaveFinale(Scene cave)
        {
            GameObject world = RequireRoot(cave, WorldRoot);
            GameObject encounterObject = RequireRoot(cave, FlowRoot);
            Transform parent = world.transform;
            GameObject hero = RequireChild(parent, "Saka");
            var player = Require<GmInteractor>(hero);
            var gate = Require<GmControlGate>(hero);
            var camera = Require<Camera>(RequireChild(parent, "Saka - Orbit Camera"));
            GameObject manager = RequireChild(parent, "Dialogue Manager");
            var guide = Require<GmPixelCrushersConversation>(RequireChild(parent, CaveGuideName));
            Vector3 start = RequireChild(parent, "Spawn - Start").transform.position;
            Vector3 portal = RequireChild(parent, "Portal").transform.position;
            var flow = Require<GmEncounterFlow>(encounterObject);
            var quest = Require<GmMainQuestProgress>(encounterObject);
            var sequence = Require<GmPixelCrushersMainQuestDialogue>(encounterObject);
            var layout = new GmWorldSceneLayout(cave, CaveEntrance, CaveEntrance, Require<CharacterController>(hero));
            GmPixelCrushersMainQuestDialogue.StepDialogue[] dialogues = PrepareDialogueDatabase();

            (GmEnemyEncounter guard, GmEnemyEncounter boss) = SetUpCaveEncounters(parent, layout, camera, flow, start, portal);
            quest.Configure(null, guard, boss, shareAcrossScenes: true);
            var bridge = ConfigureDialogueManager(manager, player);
            sequence.Configure(bridge, dialogues.Where(dialogue => CaveDialogueSteps.Contains(dialogue.step)).ToArray(), guard);
            guide.SetMainQuestDialogue(sequence);
            Persist(quest);
            Persist(sequence);
            Persist(guide);
            SetUpQuestHint(parent, quest, gate, camera, showCompletion: true);
            EditorSceneManager.MarkSceneDirty(cave);
        }

        private static GameObject VerifyCaveFinale(Scene cave)
        {
            GameObject world = RequireRoot(cave, WorldRoot);
            GameObject encounter = RequireRoot(cave, FlowRoot);
            var guard = Require<GmEnemyEncounter>(RequireChild(world.transform, CaveGuardName));
            var boss = Require<GmEnemyEncounter>(RequireChild(world.transform, PakandeName));
            var hint = Require<GmMainQuestEncounterHint>(RequireChild(world.transform, QuestHintName));
            var guide = Require<GmPixelCrushersConversation>(RequireChild(world.transform, CaveGuideName));
            var gate = Require<GmControlGate>(RequireChild(world.transform, "Saka"));
            var bridge = Require<GmPixelCrushersDialogueBridge>(RequireChild(world.transform, "Dialogue Manager"));
            var quest = Require<GmMainQuestProgress>(encounter);
            var sequence = Require<GmPixelCrushersMainQuestDialogue>(encounter);
            if (!guard.UsePoppoBattle || boss.BossBattleProfile == null)
                throw new InvalidOperationException("Konfigurasi battle Goa belum lengkap.");

            VerifyReference(quest, "caveEncounter", guard);
            VerifyReference(quest, "pakandeEncounter", boss);
            VerifyReference(sequence, "bridge", bridge);
            VerifyReference(sequence, "revealAfterEncounter", guard);
            VerifyReference(guide, "mainQuestDialogue", sequence);
            VerifyReference(hint, "quest", quest);
            VerifyReference(hint, "controls", gate);
            DialogueDatabase database = Load<DialogueDatabase>(GmDialogueExampleData.DatabasePath);
            using (var sequenceData = new SerializedObject(sequence))
            {
                SerializedProperty dialogues = sequenceData.FindProperty("dialogues");
                foreach (GmMainQuestStep step in CaveDialogueSteps)
                {
                    bool hasDialogue = Enumerable.Range(0, dialogues.arraySize)
                        .Select(dialogues.GetArrayElementAtIndex)
                        .Any(entry => entry.FindPropertyRelative("step").enumValueIndex == (int)step &&
                            database.GetConversation(entry.FindPropertyRelative("conversationTitle").stringValue) != null);
                    if (!hasDialogue)
                        throw new InvalidOperationException($"Dialog {step} belum terpasang pada scene Goa.");
                }
            }
            return world;
        }

        private static void VerifyReference(UnityEngine.Object owner, string field, UnityEngine.Object expected)
        {
            using (var data = new SerializedObject(owner))
            {
                if (data.FindProperty(field)?.objectReferenceValue != expected)
                    throw new InvalidOperationException($"Referensi {field} pada {owner.name} belum terpasang.");
            }
        }

        private static GmPixelCrushersMainQuestDialogue.StepDialogue[] PrepareDialogueDatabase()
        {
            // Save generated conversations while this asset instance is loaded.
            DialogueDatabase database = Load<DialogueDatabase>(GmDialogueExampleData.DatabasePath);
            int countBefore = database.conversations.Count;
            var dialogues = GmMainQuestDialogueData.EnsureConversations(database);
            if (database.conversations.Count != countBefore)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
            }
            return dialogues;
        }

        private static Scene OpenLevel(string path, OpenSceneMode mode)
        {
            Scene scene = EditorSceneManager.OpenScene(path, mode);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("[Daeng Saka] Gagal membuka scene " + path);
            return scene;
        }

        private static void ConfigureLevel(Scene scene, GmWorldSceneLayout layout, WorldAssets assets,
            GmPixelCrushersMainQuestDialogue.StepDialogue[] dialogues)
        {
            bool isCapital = scene.path == Capital;
            bool isVillage = scene.path == Village;
            bool isCave = scene.path == Cave;
            GameObject world = GetRoot(scene, WorldRoot);
            GameObject encounterObject = GetRoot(scene, FlowRoot);
            ValidateExistingComponents(scene, world.transform, encounterObject);
            layout.HidePlaceholders();

            Transform start = GetObject(world.transform, "Spawn - Start", out bool newStart).transform;
            Vector3 inward = layout.IntoLevel;
            if (newStart)
                layout.Place(start, layout.StartGuide.position + inward * (isCapital ? GmCapitalRouteSetup.StartInset : 1.8f), inward);

            Transform portalPoint = GetObject(world.transform, "Portal", out bool newPortal).transform;
            if (newPortal)
            {
                float inset = isCave ? 0f : isCapital ? GmCapitalRouteSetup.ExitInset : .9f;
                layout.Place(portalPoint, layout.PortalGuide.position - inward * inset, isCave ? inward : -inward, start.position);
            }
            GmWorldPortal portal = GetOrAdd<GmWorldPortal>(portalPoint.gameObject);
            if (portal.GetComponent<GmWorldBorder>() == null)
            {
                BoxCollider trigger = Require<BoxCollider>(portalPoint.gameObject);
                trigger.isTrigger = true;
                trigger.size = new Vector3(1.45f, 1.6f, 1.05f);
                trigger.center = Vector3.up * 0.8f;
            }

            var entry = new GmWorldScene.Entry(GmWorldScene.DefaultEntry, start);
            GmWorldScene.Entry[] entries = { entry };
            Transform fromVillage = isCapital ? world.transform.Find("Spawn - Dari Sorelia") : null;
            if (fromVillage != null)
                entries = new[] { entry, new GmWorldScene.Entry(GmWorldScene.VillageReturnEntry, fromVillage) };
            else if (isVillage)
            {
                Transform fromCave = GetObject(world.transform, "Spawn - Dari Goa", out bool newReturn).transform;
                if (newReturn)
                    layout.Place(fromCave, portalPoint.position - inward * 2.9f, -inward, portalPoint.position);
                entries = new[] { entry, new GmWorldScene.Entry(GmWorldScene.CaveReturnEntry, fromCave) };
            }

            GameObject hero = GetPrefab(world.transform, "Saka", assets.player);
            var player = Require<GmSakaMotor>(hero);
            var gate = Require<GmControlGate>(hero);
            var interactor = Require<GmInteractor>(hero);
            hero.transform.SetPositionAndRotation(start.position, start.rotation * Quaternion.Euler(0f, 180f, 0f));
            Persist(hero.transform);

            Camera camera = SetUpCamera(scene, world.transform, assets.camera, player, gate, start.eulerAngles.y);
            var orbit = Require<GmOrbitFollowCamera>(camera.gameObject);
            player.SetCamera(camera);
            interactor.SetCamera(camera);
            Persist(player);
            Persist(interactor);
            FaceCamera(hero, camera);

            var flow = GetOrAdd<GmEncounterFlow>(encounterObject);
            var quest = GetOrAdd<GmMainQuestProgress>(encounterObject);
            var worldScene = GetOrAdd<GmWorldScene>(encounterObject);
            flow.SetBattleScene(Battle);
            worldScene.Configure(player, orbit, entries);
            if (isCapital)
            {
                portal.SetPresentation("Desa Sorelia", "Pergi");
                portal.Configure(worldScene, Village, GmWorldScene.DefaultEntry, quest,
                    GmMainQuestStep.KunjungiPenempaBesi, "Selesaikan persiapan di Kerajaan");
            }
            else if (isVillage)
            {
                portal.SetPresentation("Goa Lembayung", "Masuk");
                portal.Configure(worldScene, Cave, GmWorldScene.DefaultEntry, quest,
                    GmMainQuestStep.BicaraDenganNelayan, "Bicara dengan Nelayan setelah battle");
            }
            else
            {
                portal.SetPresentation("Desa Sorelia", "Kembali");
                portal.Configure(worldScene, Village, GmWorldScene.CaveReturnEntry, quest, null, string.Empty);
            }

            GmEnemyEncounter poppo = isVillage ? SetUpPoppo(world.transform, layout, assets, camera, flow, start.position) : null;
            GmEnemyEncounter caveGuard = null;
            GmEnemyEncounter pakande = null;
            if (isCave)
                (caveGuard, pakande) = SetUpCaveEncounters(world.transform, layout, camera, flow,
                    start.position, portalPoint.position);
            quest.Configure(poppo, caveGuard, pakande, shareAcrossScenes: true);
            SetUpDialogue(world.transform, encounterObject, layout, assets, dialogues, interactor,
                camera, start.position, portalPoint.position, isCapital, isVillage, caveGuard);
            var prompt = Require<GmInteractionPrompt>(GetPrefab(world.transform, "Interaction Prompt", assets.prompt));
            prompt.SetInteractor(interactor);
            Persist(prompt);
            SetUpQuestHint(world.transform, quest, gate, camera, isCave, isCave ? null : portal,
                isVillage ? "Goa Lembayung" : "Desa Sorelia");
            SetUpEventSystem(world.transform);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static Camera SetUpCamera(Scene scene, Transform parent, GameObject prefab, GmSakaMotor player,
            GmControlGate gate, float heading)
        {
            GameObject instance = GetPrefab(parent, "Saka - Orbit Camera", prefab, out bool created);
            Camera camera = Require<Camera>(instance);
            Camera authored = InScene<Camera>(scene).SingleOrDefault(value => value.CompareTag("MainCamera") &&
                value != camera && !value.transform.IsChildOf(parent));
            if (authored != null)
            {
                camera.clearFlags = authored.clearFlags;
                camera.backgroundColor = authored.backgroundColor;
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, authored.farClipPlane);
                authored.enabled = false;
                Persist(authored);
                var oldListener = authored.GetComponent<AudioListener>();
                if (oldListener != null)
                {
                    oldListener.enabled = false;
                    Persist(oldListener);
                }
                Persist(camera);
            }
            var orbit = Require<GmOrbitFollowCamera>(instance);
            orbit.SetTarget(player.transform, gate);
            orbit.SetStartingHeading(heading);
            if (created)
            {
                orbit.SetVerticalOrbit(true);
                orbit.SetIgnoredRoots(new[] { parent });
            }
            orbit.SnapToTarget();
            Persist(orbit);
            Persist(instance.transform);
            return camera;
        }

        private static void SetUpDialogue(Transform parent, GameObject encounterObject, GmWorldSceneLayout layout,
            WorldAssets assets, GmPixelCrushersMainQuestDialogue.StepDialogue[] dialogues, GmInteractor player,
            Camera camera, Vector3 start, Vector3 portal, bool isCapital, bool isVillage, GmEnemyEncounter caveGuard)
        {
            GameObject managerObject = GetPrefab(parent, "Dialogue Manager", assets.manager);
            var bridge = ConfigureDialogueManager(managerObject, player);

            GmMainQuestStep[] localSteps = isCapital
                ? new[] { GmMainQuestStep.PersiapanSebelumBertarung, GmMainQuestStep.KunjungiMerchant, GmMainQuestStep.KunjungiPenempaBesi }
                : isVillage
                    ? new[] { GmMainQuestStep.TemuiKeluargaNelayan, GmMainQuestStep.SelamatkanKeluargaNelayan, GmMainQuestStep.BicaraDenganNelayan }
                    : CaveDialogueSteps;
            var sequence = GetOrAdd<GmPixelCrushersMainQuestDialogue>(encounterObject);
            sequence.Configure(bridge, dialogues.Where(dialogue => localSteps.Contains(dialogue.step)).ToArray(), caveGuard);
            string guideName = isCapital ? "Pemandu Kerajaan (Uji)" : isVillage ? "Nelayan (Uji)" : CaveGuideName;
            GameObject guide = GetPrefab(parent, guideName, isVillage ? assets.fisherman : assets.merchant, out bool created);
            if (created)
            {
                Vector3 side = Vector3.Cross(Vector3.up, layout.IntoLevel);
                Vector3[] avoid = caveGuard != null ? new[] { start, portal, caveGuard.transform.position } : new[] { start, portal };
                layout.Place(guide.transform, start + 3f * layout.IntoLevel + side, -layout.IntoLevel, avoid);
            }
            var conversation = Require<GmPixelCrushersConversation>(guide);
            conversation.SetMainQuestDialogue(sequence);
            conversation.SetPresentation(guideName, "Bicara");
            Persist(conversation);
            FaceCamera(guide, camera);
        }

        private static GmPixelCrushersDialogueBridge ConfigureDialogueManager(GameObject managerObject, GmInteractor player)
        {
            var controller = Require<DialogueSystemController>(managerObject);
            controller.dontDestroyOnLoad = false;
            // The incoming scene's local controller can initialize while Unity finishes unloading the old scene.
            controller.allowOnlyOneInstance = false;
            var bridge = Require<GmPixelCrushersDialogueBridge>(managerObject);
            bridge.SetPlayer(player);
            DialogueDatabase database = Load<DialogueDatabase>(GmDialogueExampleData.DatabasePath);
            bridge.SetAdditionalDatabase(controller.initialDatabase == database ? null : database);
            Persist(controller);
            Persist(bridge);
            return bridge;
        }

        private static void SetUpQuestHint(Transform parent, GmMainQuestProgress quest, GmControlGate gate, Camera camera,
            bool showCompletion, GmWorldPortal portal = null, string destination = null)
        {
            var hint = GetOrAdd<GmMainQuestEncounterHint>(GetObject(parent, QuestHintName, out _, typeof(RectTransform)));
            hint.Configure(quest, gate, showQuestCompletion: showCompletion);
            hint.SetRoute(portal, destination, camera);
            Persist(hint);
        }

        private static GmEnemyEncounter SetUpPoppo(Transform parent, GmWorldSceneLayout layout, WorldAssets assets,
            Camera camera, GmEncounterFlow flow, Vector3 start)
        {
            GameObject marker = GetObject(parent, "Poppo (Uji)", out bool created);
            if (created)
                layout.Place(marker.transform, start + layout.IntoLevel * 8f, -layout.IntoLevel, start);
            return ConfigureEncounter(marker, flow, "common-poppo", "Poppo", assets.poppo, assets.spriteMaterial, camera, poppoBattle: true);
        }

        private static (GmEnemyEncounter guard, GmEnemyEncounter boss) SetUpCaveEncounters(Transform parent,
            GmWorldSceneLayout layout, Camera camera, GmEncounterFlow flow, Vector3 start, Vector3 portal)
        {
            Sprite poppo = Load<Sprite>(EnemyArt + "poppoWorld.png");
            Sprite pakande = Load<Sprite>(EnemyArt + "pakandeWorld.png");
            Material material = Load<Material>(WorldSpriteMaterial);
            var profile = Load<GmBossBattleProfile>("Assets/GamecommMentor/Data/NenekPakandePlaytest.asset");
            // The passage at this entrance runs east, between the two rows of authored props.
            Vector3 inward = Vector3.right;
            Vector3 entrance = layout.StartGuide.position;
            GameObject guardMarker = GetObject(parent, CaveGuardName, out bool newGuard);
            if (newGuard)
                layout.Place(guardMarker.transform, entrance + inward * 12f, -inward, start, portal);
            GmEnemyEncounter guard = ConfigureEncounter(guardMarker, flow, "world-cave-guard", CaveGuardName,
                poppo, material, camera, poppoBattle: true);

            Vector3 guardPosition = guardMarker.transform.position;
            GameObject bossMarker = GetObject(parent, PakandeName, out bool newBoss);
            if (newBoss)
                layout.Place(bossMarker.transform, guardPosition + inward * 9f, -inward, start, portal, guardPosition);
            GmEnemyEncounter boss = ConfigureEncounter(bossMarker, flow, "boss-nenek-pakande", PakandeName,
                pakande, material, camera, bossProfile: profile);
            return (guard, boss);
        }

        private static GmEnemyEncounter ConfigureEncounter(GameObject marker, GmEncounterFlow flow, string id,
            string displayName, Sprite sprite, Material material, Camera camera,
            GmBossBattleProfile bossProfile = null, bool poppoBattle = false)
        {
            var encounter = GetOrAdd<GmEnemyEncounter>(marker);
            encounter.Configure(flow, id, displayName, bossProfile, poppoBattle);
            var trigger = Require<SphereCollider>(marker);
            trigger.isTrigger = true;
            trigger.radius = 0.67f;
            trigger.center = Vector3.up * trigger.radius;
            var body = Require<Rigidbody>(marker);
            body.isKinematic = true;
            body.useGravity = false;
            GameObject visual = GetObject(marker.transform, "Visual", out _);
            visual.transform.localPosition = Vector3.up * -sprite.bounds.min.y;
            var renderer = GetOrAdd<SpriteRenderer>(visual);
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            GetOrAdd<GmSpriteBillboard>(visual).SetCamera(camera);
            return encounter;
        }

        private static void SetUpEventSystem(Transform parent)
        {
            GameObject root = GetObject(parent, "EventSystem", out _);
            GetOrAdd<EventSystem>(root);
#if ENABLE_INPUT_SYSTEM
            var input = GetOrAdd<InputSystemUIInputModule>(root);
            if (input.actionsAsset == null) input.AssignDefaultActions();
#else
            GetOrAdd<StandaloneInputModule>(root);
#endif
        }

        private static void ValidateExistingComponents(Scene scene, Transform world, GameObject flow)
        {
            if (InScene<GmSakaMotor>(scene).Any(c => !c.transform.IsChildOf(world)) ||
                InScene<DialogueSystemController>(scene).Any(c => !c.transform.IsChildOf(world)) ||
                InScene<EventSystem>(scene).Any(c => !c.transform.IsChildOf(world)) ||
                InScene<GmEncounterFlow>(scene).Any(c => c.gameObject != flow))
                throw new InvalidOperationException($"Gameplay setup sudah ada di luar '{WorldRoot}' pada {scene.path}.");
        }

        private static void RegisterScenes() => GmMainMenuSetup.RegisterScenes(GmMainMenuSetup.HasTitleScene);

        private static GameObject GetRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().SingleOrDefault(root => root.name == name) ?? CreateObject(scene, name);

        private static GameObject RequireRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().SingleOrDefault(root => root.name == name) ??
                throw new InvalidOperationException($"'{name}' belum ada di {scene.path}. Jalankan Set Up Demo World.");

        private static GameObject RequireChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null) throw new InvalidOperationException($"'{name}' belum ada di bawah '{parent.name}'.");
            return child.gameObject;
        }

        private static T[] InScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private static GameObject CreateObject(Scene scene, string name, params Type[] types)
        {
            var instance = new GameObject(name, types);
            if (instance.scene != scene)
                SceneManager.MoveGameObjectToScene(instance, scene);
            return instance;
        }

        private static GameObject GetObject(Transform parent, string name, out bool created, params Type[] types)
        {
            Transform existing = parent.Find(name);
            created = existing == null;
            if (!created) return existing.gameObject;
            var child = CreateObject(parent.gameObject.scene, name, types);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject GetPrefab(Transform parent, string name, GameObject prefab) =>
            GetPrefab(parent, name, prefab, out _);

        private static GameObject GetPrefab(Transform parent, string name, GameObject prefab, out bool created)
        {
            Transform existing = parent.Find(name);
            created = existing == null;
            if (!created) return existing.gameObject;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            instance.transform.SetParent(parent, false);
            instance.name = name;
            Persist(instance);
            return instance;
        }

        private static T GetOrAdd<T>(GameObject root) where T : Component =>
            root.TryGetComponent(out T component) ? component : root.AddComponent<T>();

        private static T Require<T>(GameObject root) where T : Component =>
            root.TryGetComponent(out T component) ? component :
                throw new InvalidOperationException($"'{root.name}' perlu komponen {typeof(T).Name}.");

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Asset belum tersedia: " + path);
            return asset;
        }

        private static void FaceCamera(GameObject root, Camera camera)
        {
            foreach (GmSpriteBillboard billboard in root.GetComponentsInChildren<GmSpriteBillboard>(true))
            {
                billboard.SetCamera(camera);
                Persist(billboard);
            }
        }

        private static void Persist(UnityEngine.Object value)
        {
            EditorUtility.SetDirty(value);
            if (PrefabUtility.IsPartOfPrefabInstance(value))
                PrefabUtility.RecordPrefabInstancePropertyModifications(value);
        }
    }
}
