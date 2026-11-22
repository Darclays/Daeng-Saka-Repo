using System;
using System.Collections.Generic;
using System.Linq;
using PixelCrushers.DialogueSystem;
using ProjectVN.Presentation.Actors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmCapitalDialogueSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Ibu Kota Dialogue";
        private const string UndoName = "Set Up Ibu Kota Dialogue";
        private const string Capital = "Assets/GamecommMentor/Scenes_GamecommMentor/IbuKota.unity";
        private const string Profiles = "Assets/GamecommMentor/Resources/DaengSaka/Dialogue/";

        [MenuItem(Menu, true)]
        private static bool CanSetUp() => CanInstall(Capital);

        [MenuItem(Menu)]
        private static void SetUp()
        {
            if (!CanSetUp()) return;
            Scene scene = SceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                InstallInScene(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
            }
        }

        internal static void InstallInScene(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform residents = RequireChild(world, "Warga Ibu Kota");
            Transform manager = RequireChild(world, "Dialogue Manager");
            Transform flow = RequireRoot(scene, "Daeng Saka - Encounter Flow");
            Transform smith = RequireChild(world, "Penempa Besi");
            Transform merchant = RequireChild(world, "Merchant");
            Transform guard = RequireChild(residents, "Penjaga Kerajaan 1");
            Transform herbalist = RequireChild(residents, "Tukang Jamu");
            Transform mother = RequireChild(residents, "Warga Ibu");
            var sequence = Require<GmPixelCrushersMainQuestDialogue>(flow);
            var journal = Require<GmQuestJournal>(RequireChild(world, "Quest Encounter Hint"));
            var bridge = Require<GmPixelCrushersDialogueBridge>(manager);
            var database = bridge.AdditionalDatabase;
            if (database == null) database = Require<DialogueSystemController>(manager).initialDatabase;
            if (database == null) throw new InvalidOperationException("Dialogue Manager perlu Dialogue Database.");

            var catalog = LoadProfile<GmVnDialogueCatalog>("DaftarDialogVN");
            var capitalProfile = LoadProfile<GmVnDialoguePilot>("ProfilDialogWargaIbuKota");
            var guardProfile = LoadProfile<GmVnDialoguePilot>("ProfilDialogPenjaga");
            var demoProfile = LoadProfile<GmVnDialoguePilot>("ProfilDialogDemo");
            var acoProfile = LoadProfile<VNActorVisualProfile>("Pua_Profil");
            var jamuProfile = LoadProfile<VNActorVisualProfile>("TukangJamu_Dialog_Profil");
            Transform aco = FindOrPlaceAco(scene, world, residents, smith, merchant);
            BindDeliveryRoute(world, flow, aco, smith, merchant, guard, herbalist, mother, bridge, sequence, journal);
            var supplies = GetOrAdd<GmSoreliaSuppliesDialogue>(merchant);
            Undo.RecordObject(supplies, UndoName);
            supplies.Configure(null, bridge, journal, merchant: true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(supplies);
            var template = Template.FromDefault();
            var kept = new List<string>();
            using var catalogData = new SerializedObject(catalog);
            SerializedProperty entries = catalogData.FindProperty("percakapan");
            Undo.RecordObject(database, UndoName);
            bool databaseChanged = GmMainQuestDialogueData.UpgradeCapitalStory(database, template, kept);
            foreach (var line in GmCapitalDialogueData.HerbalRevisions)
                databaseChanged |= GmMainQuestDialogueData.ReviseGeneratedLine(database, line.title, line.speaker,
                    line.previous, line.replacement);
            int bound = 0;

            foreach (var talk in GmCapitalDialogueData.Residents)
            {
                bool installed = false;
                foreach (string host in talk.hosts)
                {
                    Transform npc = residents.Find(host);
                    if (npc == null || npc.GetComponentInChildren<Collider>(true) == null)
                    {
                        kept.Add(host + " perlu NPC dengan collider");
                        continue;
                    }
                    if (!TryBind(npc, talk.title, talk.hosts.Length > 1 ? host : talk.partner, bridge))
                    {
                        kept.Add(host + " punya interaksi sendiri");
                        continue;
                    }
                    installed = true;
                    bound++;
                }
                if (!installed) continue;
                databaseChanged |= GmMainQuestDialogueData.AddConversation(database, template, talk.title, null, talk.partner, talk.lines);
                Register(entries, talk.title, talk.title == GmCapitalDialogueData.YusufTitle ? guardProfile : capitalProfile);
            }

            foreach (var talk in GmCapitalDialogueData.Delivery)
            {
                string terminal = talk.title switch
                {
                    GmCapitalDeliveryDialogue.AcoGreeting => null,
                    GmSoreliaSuppliesDialogue.DeliveryConversation => GmSoreliaSuppliesDialogue.CompletionEntry,
                    _ => GmCapitalDeliveryDialogue.CompletionEntry
                };
                databaseChanged |= GmMainQuestDialogueData.AddConversation(database, template, talk.title, terminal,
                    talk.partner, talk.lines);
                var profile = talk.title switch
                {
                    GmCapitalDeliveryDialogue.DeliveryTitle or GmCapitalDeliveryDialogue.HerbalOfferTitle => guardProfile,
                    GmCapitalDeliveryDialogue.HerbalPickupTitle or GmCapitalDeliveryDialogue.HerbalDropoffTitle => capitalProfile,
                    _ => demoProfile
                };
                Register(entries, talk.title, profile);
            }

            BindPortrait(aco, acoProfile);
            BindPortrait(residents.Find("Tukang Jamu"), jamuProfile);
            if (catalogData.ApplyModifiedProperties()) AssetDatabase.SaveAssetIfDirty(catalog);
            if (databaseChanged)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
            }
            if (kept.Count > 0) Debug.LogWarning("[Daeng Saka] Periksa penempatan atau dialog kustom berikut. " +
                string.Join(", ", kept));
            Debug.Log($"[Daeng Saka 1E.39B] Dialog Ibu Kota dan penyerahan Titipan Ibu Nelayan siap. {bound} dialog warga terhubung. Simpan scene IbuKota.");
        }

        private static Transform FindOrPlaceAco(Scene scene, Transform world, Transform residents,
            Transform smith, Transform merchant)
        {
            Transform existing = residents.Find(GmCapitalDialogueData.Aco);
            if (existing != null) return existing;

            Transform marker = RequireChild(RequireRoot(scene, "NPC_Places"), "NPCPlace_DaengAco");
            var player = Require<CharacterController>(RequireChild(world, "Saka"));
            var reference = Require<SpriteRenderer>(RequireChild(merchant, "Visual"));
            Vector3[] reserved = { player.transform.position, smith.position, merchant.position, RequireChild(world, "Portal").position };
            var placement = new GmAmbientNpcPlacement(world, residents.name, GmCapitalRouteSetup.CreateLayout(scene, player),
                reference, reserved, UndoName);
            var art = GmDialogueNpcSprites.Prepare(GmCapitalResidentsSetup.AcoSource, "IbuKota_DaengAco");
            placement.Add(GmCapitalDialogueData.Aco, marker.position, art, 1.90f, .24f);
            return RequireChild(residents, GmCapitalDialogueData.Aco);
        }

        private static void BindDeliveryRoute(Transform world, Transform flow, Transform aco, Transform smith,
            Transform merchant, Transform guard, Transform herbalist, Transform mother, GmPixelCrushersDialogueBridge bridge,
            GmPixelCrushersMainQuestDialogue sequence, GmQuestJournal journal)
        {
            var delivery = GetOrAdd<GmCapitalDeliveryDialogue>(flow);
            Undo.RecordObject(delivery, UndoName);
            delivery.Configure(journal, bridge, smith, aco, guard, herbalist, mother);

            var acoTalk = GetDeliveryInteraction(aco);
            acoTalk.SetPresentation(GmCapitalDialogueData.Aco, "Bicara");
            acoTalk.ConfigureAmbientDialogue(string.IsNullOrWhiteSpace(acoTalk.ConversationTitle)
                ? GmCapitalDeliveryDialogue.AcoGreeting : acoTalk.ConversationTitle, bridge, null, default);
            acoTalk.SetMainQuestDialogue(sequence);
            acoTalk.SetCityDelivery(delivery);
            PrefabUtility.RecordPrefabInstancePropertyModifications(acoTalk);

            var acoActor = GetOrAdd<DialogueActor>(aco);
            Undo.RecordObject(acoActor, UndoName);
            acoActor.actor = GmCapitalDialogueData.Aco;
            PrefabUtility.RecordPrefabInstancePropertyModifications(acoActor);

            var smithTalk = GetDeliveryInteraction(smith);
            smithTalk.SetMainQuestDialogue(sequence);
            smithTalk.SetCityDelivery(delivery);
            PrefabUtility.RecordPrefabInstancePropertyModifications(smithTalk);

            foreach (Transform actor in new[] { guard, herbalist, mother })
            {
                var talk = GetDeliveryInteraction(actor);
                talk.SetCityDelivery(delivery);
                PrefabUtility.RecordPrefabInstancePropertyModifications(talk);
            }

            Undo.RecordObject(sequence, UndoName);
            sequence.UseAssignedSpeakersOnly();
            sequence.SetSpeaker(GmMainQuestStep.PersiapanSebelumBertarung, aco);
            sequence.SetSpeaker(GmMainQuestStep.KunjungiMerchant, merchant);
            sequence.SetSpeaker(GmMainQuestStep.KunjungiPenempaBesi, smith);
            Undo.RecordObject(journal, UndoName);
            journal.SetLandmark(new GmQuestLandmark(GmQuestCatalog.Guide, GmCapitalDialogueData.Aco,
                "Di area permukiman Kerajaan.", aco));
            journal.SetLandmark(new GmQuestLandmark(GmQuestCatalog.Merchant, "Merchant",
                "Perbekalan dan ramuan.", merchant));
            journal.SetLandmark(new GmQuestLandmark(GmQuestCatalog.CastleGuard, "Pak Yusuf",
                "Penjaga di kaki tangga istana.", guard));
            journal.SetLandmark(new GmQuestLandmark(GmQuestCatalog.Blacksmith, "Penempa Besi",
                "Perlengkapan untuk perjalanan.", smith));
            journal.SetLandmark(new GmQuestLandmark(GmQuestCatalog.Herbalist, "Tukang Jamu",
                "Kedai di dekat tangga istana.", herbalist));
            journal.SetLandmark(new GmQuestLandmark(GmQuestCatalog.CityFamily, "Ibu dan Anak",
                "Warga di depan rumah putih.", mother));

            Transform oldGuide = world.Find(GmCapitalNpcSetup.PlaceholderGuide);
            if (oldGuide != null && oldGuide.gameObject.activeSelf)
            {
                Undo.RecordObject(oldGuide.gameObject, UndoName);
                oldGuide.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(oldGuide.gameObject);
            }
        }

        private static GmPixelCrushersConversation GetDeliveryInteraction(Transform npc)
        {
            npc.TryGetComponent(out GmPixelCrushersConversation talk);
            if (npc.GetComponentInChildren<Collider>(true) == null ||
                npc.GetComponents<GmInteractable>().Any(other => other != talk))
                throw new InvalidOperationException($"'{npc.name}' perlu satu interaksi dialog dan collider untuk Titipan Penempa.");
            if (talk == null) talk = AddComponent<GmPixelCrushersConversation>(npc);
            Undo.RecordObject(talk, UndoName);
            return talk;
        }

        private static T LoadProfile<T>(string name) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(Profiles + name + ".asset");
            if (asset == null) throw new InvalidOperationException($"Impor {name}.asset sebelum memasang dialog Ibu Kota.");
            return asset;
        }

        private static bool TryBind(Transform npc, string title, string actorName, GmPixelCrushersDialogueBridge bridge)
        {
            npc.TryGetComponent(out GmPixelCrushersConversation talk);
            if (npc.GetComponents<GmInteractable>().Any(other => other != talk) ||
                (talk != null && !string.IsNullOrEmpty(talk.ConversationTitle) && talk.ConversationTitle != title))
                return false;

            if (talk == null) talk = AddComponent<GmPixelCrushersConversation>(npc);
            Undo.RecordObject(talk, UndoName);
            talk.SetPresentation(actorName, "Bicara");
            talk.ConfigureAmbientDialogue(title, bridge, null, default);
            PrefabUtility.RecordPrefabInstancePropertyModifications(talk);

            var actor = GetOrAdd<DialogueActor>(npc);
            if (string.IsNullOrWhiteSpace(actor.actor))
            {
                Undo.RecordObject(actor, UndoName);
                actor.actor = actorName;
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
            }
            return true;
        }

        private static void Register(SerializedProperty entries, string title, GmVnDialoguePilot profile)
        {
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("judul").stringValue == title) return;

            int index = entries.arraySize++;
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("judul").stringValue = title;
            entry.FindPropertyRelative("profil").objectReferenceValue = profile;
            entry.FindPropertyRelative("kameraQuestUtama").boolValue = false;
        }

        private static void BindPortrait(Transform npc, VNActorVisualProfile profile)
        {
            if (npc == null) return;
            var renderer = npc.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer == null) return;
            Sprite sprite = renderer.sprite;
            if (sprite == null) return;
            using var data = new SerializedObject(profile);
            SerializedProperty portrait = data.FindProperty("defaultPortraitSprite");
            if (portrait.objectReferenceValue != null) return;
            Undo.RecordObject(profile, UndoName);
            portrait.objectReferenceValue = sprite;
            SerializedProperty stage = data.FindProperty("defaultStageSprite");
            if (stage.objectReferenceValue == null) stage.objectReferenceValue = sprite;
            if (data.ApplyModifiedProperties()) AssetDatabase.SaveAssetIfDirty(profile);
        }
    }
}
