using System;
using System.Collections.Generic;
using System.Linq;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;
using ActorCue = GamecommMentor.DaengSaka.GmSoreliaRescueCutscene.ActorCue;
using Shot = GamecommMentor.DaengSaka.GmSoreliaRescueCutscene.Shot;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static partial class GmSoreliaRescueCutsceneSetup
    {
        private const string PakandeMenu = "GamecommMentor/Daeng Saka/Set Up Nenek Pakande Cutscene";
        private const string PakandeUndo = "Set Up Nenek Pakande Cutscene";
        private const string PakandeArt = "Assets/GamecommMentor/Art/2D/Cutscenes/Pakande/";
        private const string GuardsName = "Rombongan Monster";
        private const string GuardsRoster = "Assets/GamecommMentor/Data/RombonganGoa.asset";
        private const string VnCatalogPath = "Assets/GamecommMentor/Resources/DaengSaka/Dialogue/DaftarDialogVN.asset";
        private const string OldRevealTitle = "DaengSaka/Main Quest/Temui Nenek Pakande";

        [MenuItem(PakandeMenu, true)]
        private static bool CanSetUpPakande() => CanInstall(GoaScene);

        [MenuItem(PakandeMenu)]
        private static void SetUpPakande()
        {
            if (!CanSetUpPakande()) return;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(PakandeUndo);
            try
            {
                var finale = InstallPakande(SceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(finale.gameObject.scene);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = finale.gameObject;
                Debug.Log("[Daeng Saka 1E.17B] Cutscene dan rute battle Nenek Pakande siap. Simpan GoaLembanyung.", finale);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
            }
        }

        private static GmGoaPakandeFinale InstallPakande(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform flowRoot = RequireRoot(scene, "Daeng Saka - Encounter Flow");
            Transform hero = RequireChild(world, "Saka");
            Transform boss = RequireChild(world, "Nenek Pakande");
            var quest = Require<GmMainQuestProgress>(flowRoot);
            if (quest.PakandeEncounter != Require<GmEnemyEncounter>(boss) || quest.PakandeEncounter.BossBattleProfile == null)
                throw new InvalidOperationException("Jalankan setup rute Goa sebelum memasang cutscene Nenek Pakande.");

            Transform places = RequireRoot(scene, "NPCPlaces");
            Transform babiMarker = RequireChild(places, "EE_BabiRusa");
            Transform laMarker = RequireChild(places, "EE_Lawuna");
            Vector3 outward = Flat((babiMarker.position + laMarker.position) * .5f - boss.position).normalized;
            if (outward.sqrMagnitude < .01f)
                throw new InvalidOperationException("Posisi Nenek Pakande, Babi Rusa, dan La Wuna perlu titik terpisah.");
            Vector3 side = Vector3.Cross(Vector3.up, outward);

            var terrain = Require<TerrainCollider>(RequireChild(RequireRoot(scene, "LevelDesign"), "Terrain"));
            Vector3 Road(float distance, float lane) => Ground(terrain, boss.position + outward * distance + side * lane);
            var stageRoot = Child(world, "Cutscene - Nenek Pakande");
            Transform castRoot = Child(stageRoot, "Aktor Cutscene");
            var heroSprite = hero.GetComponentInChildren<SpriteRenderer>(true);
            if (heroSprite == null) throw new InvalidOperationException("Visual Saka belum tersedia.");
            var actors = CreateCast(castRoot, heroSprite, "Saka", "Daung", "Pacca");
            AddPakandeActor("Pakande", "Nenek Pakande");
            AddPakandeActor("BabiRusa", "Babi Rusa");
            AddPakandeActor("Manguni", "Manguni");
            AddPakandeActor("LaWuna", "La Wuna");

            void AddPakandeActor(string key, string label)
            {
                string path = PakandeArt + key + ".png";
                Sprite[] art = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                var idle = ReadDirections(art, "Idle", hero: false, standing: false, path: path);
                actors.Add(key, CreateActor(castRoot, label, heroSprite, idle, default));
            }

            var cameraObject = RequireChild(world, "Saka - Orbit Camera");
            var camera = Require<Camera>(cameraObject);
            var orbit = Require<GmOrbitFollowCamera>(cameraObject);
            var hud = Require<Canvas>(RequireChild(world, "Quest Encounter Hint"));
            var bridge = Require<GmPixelCrushersDialogueBridge>(RequireChild(world, "Dialogue Manager"));
            var sequence = Require<GmPixelCrushersMainQuestDialogue>(flowRoot);
            var roster = AssetDatabase.LoadAssetAtPath<GmEncounterRoster>(GuardsRoster);
            if (roster == null || !roster.HasEnemies)
                throw new InvalidOperationException("Aset RombonganGoa belum tersedia. Impor folder Data dari patch.");

            Transform guardPoint = Mark(places, "Encounter_RombonganPakande", Road(8.20f, .03f), Quaternion.LookRotation(-outward, Vector3.up));
            Transform guardObject = Child(world, GuardsName);
            Undo.RecordObject(guardObject, PakandeUndo);
            guardObject.SetPositionAndRotation(guardPoint.position, guardPoint.rotation);
            var guards = Component<GmEnemyEncounter>(guardObject);
            Undo.RecordObject(guards, PakandeUndo);
            guards.Configure(Require<GmEncounterFlow>(flowRoot), "world-cave-pack", GuardsName);
            guards.SetRoster(roster);
            // The encounter uses the approach marker so the framing is already visible when the cutscene takes over.
            var trigger = Component<SphereCollider>(guardObject);
            Undo.RecordObject(trigger, PakandeUndo);
            trigger.isTrigger = true;
            trigger.enabled = false;
            var body = Component<Rigidbody>(guardObject);
            Undo.RecordObject(body, PakandeUndo);
            body.isKinematic = true;
            body.useGravity = false;

            var monsterPositions = new Dictionary<string, Transform>
            {
                ["BabiRusa"] = Mark(stageRoot, "Posisi Babi Rusa",
                    Ground(terrain, Vector3.Lerp(boss.position, babiMarker.position, .55f))),
                ["Manguni"] = Mark(stageRoot, "Posisi Manguni", Road(-.75f, -1.76f) + Vector3.up * .48f),
                ["LaWuna"] = Mark(stageRoot, "Posisi La Wuna",
                    Ground(terrain, Vector3.Lerp(boss.position, laMarker.position, .55f))),
            };
            foreach (var monster in monsterPositions)
                SetUpGuardPreview(guardObject, actors[monster.Key], monster.Value, camera);

            GmSoreliaRescueCutscene MakeDirector(Transform parent, Shot[] shots)
            {
                var director = Component<GmSoreliaRescueCutscene>(parent);
                var overlay = MakeOverlay(parent, out var fade, out var caption);
                var hidden = new[] { hero, boss, guardObject }.SelectMany(t => t.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();
                Undo.RecordObject(director, PakandeUndo);
                director.Configure(camera, orbit, terrain, hud, overlay.gameObject, fade, caption,
                    actors.Values.ToArray(), hidden, shots);
                if (!director.IsConfigured) throw new InvalidOperationException("Aktor atau kamera cutscene Pakande belum lengkap.");
                return director;
            }

            Transform approach = Child(stageRoot, "01. Sebelum Battle Rombongan");
            Transform discovered = Child(approach, "Melihat Rombongan");
            Transform protect = Child(approach, "Menghampiri Pakande");
            Transform reaction = Child(stageRoot, "02. Setelah Battle Rombongan");
            Transform reunion = Child(reaction, "Kembali dari Battle");
            Transform address = Child(reaction, "Berbicara dengan Pakande");
            Transform anger = Child(reaction, "Pakande Mengamuk");

            var team = new[]
            {
                (Key: "Saka", Start: 8.10f, End: 5.08f, Lane: .00f, Lag: 0f),
                (Key: "Daung", Start: 9.04f, End: 6.22f, Lane: -1.06f, Lag: .10f),
                (Key: "Pacca", Start: 9.28f, End: 6.46f, Lane: 1.25f, Lag: .18f),
            };
            ActorCue[] Stage(Transform panel, bool move, bool advanced, bool withMonsters)
            {
                var cues = new List<ActorCue>();
                foreach (var member in team)
                {
                    Transform track = Child(panel, actors[member.Key].gameObject.name);
                    Transform first = Mark(track, "Awal", Road(advanced ? member.End : member.Start, member.Lane));
                    Transform last = move ? Mark(track, "Akhir", Road(member.End, member.Lane)) : first;
                    cues.Add(new ActorCue(actors[member.Key], first, last, boss, member.Lag, move ? 2.10f : 0f, true));
                }
                Transform saka = Mark(panel, "Arah ke Saka", Road(advanced || move ? team[0].End : team[0].Start, 0f));
                cues.Add(new ActorCue(actors["Pakande"], boss, boss, saka, 0f, 0f, true));
                if (withMonsters)
                    foreach (var monster in monsterPositions)
                        cues.Add(new ActorCue(actors[monster.Key], monster.Value, monster.Value, boss, 0f, 0f, false));
                return cues.ToArray();
            }

            Transform Frame(Transform panel, string label, float d, float lane, float height, float focusD, float focusY) =>
                CameraMark(panel, label, Road(d, lane) + Vector3.up * height, Road(focusD, 0f) + Vector3.up * focusY);
            Transform establishing = Frame(discovered, "Kamera Awal", 7.55f, 5.70f, 4.12f, 5.96f, 1.01f);
            Transform establishingEnd = Frame(discovered, "Kamera Akhir", 7.42f, 5.62f, 4.08f, 5.81f, 1.00f);
            Transform rushFrom = Frame(protect, "Kamera Awal", 7.42f, 5.62f, 4.08f, 5.81f, 1.00f);
            Transform rushTo = Frame(protect, "Kamera Akhir", 5.62f, 5.42f, 3.25f, 4.47f, .88f);
            var beforeBattle = MakeDirector(approach, new[]
            {
                new Shot(establishing, establishingEnd, 2.55f, 58f, Stage(discovered, false, false, true),
                    "Saka   ·   Lihat itu, ada monster besar!", .26f),
                new Shot(rushFrom, rushTo, 2.75f, 56f, Stage(protect, true, false, true),
                    "Daung   ·   Ada nenek di sana. Nek, menjauh!", .26f),
            });

            Transform highFrom = Frame(reunion, "Kamera Awal", 5.40f, 5.52f, 4.68f, 3.95f, .37f);
            Transform highTo = Frame(reunion, "Kamera Akhir", 5.16f, 5.44f, 4.48f, 3.94f, .40f);
            Transform talk = Frame(address, "Kamera Akhir", 4.20f, 5.20f, 2.46f, 3.90f, .75f);
            Transform glare = Frame(anger, "Kamera Akhir", 4.20f, 5.20f, 2.12f, 3.90f, .75f);
            var roar = Component<AudioSource>(Mark(anger, "Auman", boss.position + Vector3.up * 1.10f));
            Undo.RecordObject(roar, PakandeUndo);
            roar.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GoaRoar);
            roar.playOnAwake = false;
            roar.spatialBlend = .35f;
            roar.volume = .63f;
            var afterBattle = MakeDirector(reaction, new[]
            {
                new Shot(highFrom, highTo, 2.30f, 54f, Stage(reunion, false, true, false),
                    "Daung   ·   Kita berhasil mengalahkan monster itu!", .25f),
                new Shot(highTo, talk, 1.82f, 56f, Stage(address, false, true, false),
                    "Saka   ·   Apakah Nenek tidak apa-apa?", .21f),
                new Shot(talk, glare, 2.01f, 56f, Stage(anger, false, true, false),
                    "Nenek Pakande   ·   Apa yang kalian lakukan?!", .28f, roar.clip != null ? roar : null, .11f),
            });

            InstallPakandeDialogues(bridge);
            Undo.RecordObject(sequence, PakandeUndo);
            sequence.SetCaveFinaleDialogues(GmGoaPakandeFinale.RouteTitle, GmGoaPakandeFinale.RevealTitle);
            Undo.RecordObject(quest, PakandeUndo);
            quest.SetCaveFinaleGuard(guards);
            var journal = hud.GetComponent<GmQuestJournal>();
            if (journal != null)
            {
                Undo.RecordObject(journal, PakandeUndo);
                journal.SetLandmark(new GmQuestLandmark(GmQuestCatalog.Encounter, GuardsName,
                    "Suara gaduh terdengar dari ruang dalam Goa Lembayung.", guardObject));
            }
            var finale = Component<GmGoaPakandeFinale>(flowRoot);
            Undo.RecordObject(finale, PakandeUndo);
            finale.Configure(Require<GmSakaMotor>(hero), guards, beforeBattle, afterBattle, bridge);
            return finale;
        }

        private static void SetUpGuardPreview(Transform guards, GmCutsceneSprite actor, Transform point, Camera camera)
        {
            Transform visual = Child(guards, actor.gameObject.name);
            var source = Require<SpriteRenderer>(actor.transform);
            Undo.RecordObject(visual, PakandeUndo);
            visual.position = point.position;
            var sprite = Component<SpriteRenderer>(visual);
            Undo.RecordObject(sprite, PakandeUndo);
            sprite.sprite = source.sprite;
            sprite.sharedMaterial = source.sharedMaterial;
            sprite.sortingLayerID = source.sortingLayerID;
            sprite.sortingOrder = source.sortingOrder;
            sprite.spriteSortPoint = source.spriteSortPoint;
            var billboard = Component<GmSpriteBillboard>(visual);
            Undo.RecordObject(billboard, PakandeUndo);
            billboard.SetCamera(camera);
        }

        private static void InstallPakandeDialogues(GmPixelCrushersDialogueBridge bridge)
        {
            var database = bridge.AdditionalDatabase ?? Require<DialogueSystemController>(bridge.transform).initialDatabase;
            if (database == null) throw new InvalidOperationException("Dialogue Manager di Goa perlu Dialogue Database.");
            Undo.RecordObject(database, PakandeUndo);
            var template = Template.FromDefault();
            bool changed = GmMainQuestDialogueData.AddConversation(database, template, GmGoaPakandeFinale.RouteTitle, null,
                ("Petunjuk", "Ikuti jalan di sela bebatuan sampai ke ruang dalam. Suara gemuruhnya datang dari sana."));
            changed |= GmMainQuestDialogueData.AddConversation(database, template, GmGoaPakandeFinale.AmbushTitle, null,
                ("Pacca", "Langsung aja kita serang dan jaga neneknya."),
                ("Saka", "Semua, siap menyerang!"),
                ("Nenek Pakande", "Hei, hei, siapa kalian? Apa yang kalian lakukan di sini?"),
                ("Daung", "Tenang, Nek. Kita akan menyelamatkanmu."));
            changed |= GmMainQuestDialogueData.AddConversation(database, template, GmGoaPakandeFinale.RevealTitle,
                GmPixelCrushersMainQuestDialogue.CompletionEntryTitle,
                ("Pacca", "Hei! Bersyukur kita di sini telah menyelamatkanmu."),
                ("Nenek Pakande", "Siapa yang meminta diselamatkan, hah? Monster itu hasil karyaku!"),
                ("Saka", "Ma-maksudnya...?"),
                ("Nenek Pakande", "Dasar anak-anak tidak tahu diri. Kalian akan bayar atas kelakuan kalian!"),
                ("Daung", "Kok nenek itu marah-marah, ya?"),
                ("Pacca", "Awas! Nenek itu akan menyerang!"));
            if (changed) EditorUtility.SetDirty(database);

            var catalog = AssetDatabase.LoadAssetAtPath<GmVnDialogueCatalog>(VnCatalogPath);
            if (catalog == null) throw new InvalidOperationException("Daftar Dialog VN belum tersedia.");
            using var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("percakapan");
            GmVnDialoguePilot profile = null;
            int FindTitle(string title)
            {
                for (int i = 0; i < entries.arraySize; i++)
                    if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("judul").stringValue == title) return i;
                return -1;
            }
            int original = FindTitle(OldRevealTitle);
            if (original >= 0)
                profile = entries.GetArrayElementAtIndex(original).FindPropertyRelative("profil").objectReferenceValue as GmVnDialoguePilot;
            if (profile == null) throw new InvalidOperationException("Profil VN Nenek Pakande perlu ditautkan di Daftar Dialog VN.");

            Undo.RecordObject(catalog, PakandeUndo);
            foreach (string title in new[] { GmGoaPakandeFinale.AmbushTitle, GmGoaPakandeFinale.RevealTitle })
            {
                if (FindTitle(title) >= 0) continue;
                int index = entries.arraySize;
                entries.arraySize++;
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("judul").stringValue = title;
                entry.FindPropertyRelative("profil").objectReferenceValue = profile;
                entry.FindPropertyRelative("kameraQuestUtama").boolValue = false;
            }
            serialized.ApplyModifiedProperties();
        }
    }
}
