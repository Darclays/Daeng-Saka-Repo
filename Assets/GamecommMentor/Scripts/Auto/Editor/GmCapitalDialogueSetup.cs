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

        private static void InstallInScene(Scene scene)
        {
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform residents = RequireChild(world, "Warga Ibu Kota");
            Transform manager = RequireChild(world, "Dialogue Manager");
            var bridge = Require<GmPixelCrushersDialogueBridge>(manager);
            var database = bridge.AdditionalDatabase ?? Require<DialogueSystemController>(manager).initialDatabase;
            if (database == null) throw new InvalidOperationException("Dialogue Manager perlu Dialogue Database.");

            var catalog = LoadProfile<GmVnDialogueCatalog>("DaftarDialogVN");
            var capitalProfile = LoadProfile<GmVnDialoguePilot>("ProfilDialogWargaIbuKota");
            var guardProfile = LoadProfile<GmVnDialoguePilot>("ProfilDialogPenjaga");
            var jamuProfile = LoadProfile<VNActorVisualProfile>("TukangJamu_Dialog_Profil");
            var template = Template.FromDefault();
            var kept = new List<string>();
            using var catalogData = new SerializedObject(catalog);
            SerializedProperty entries = catalogData.FindProperty("percakapan");
            Undo.RecordObject(database, UndoName);
            bool databaseChanged = GmMainQuestDialogueData.UpgradeCapitalStory(database, template, kept);
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

            BindJamuPortrait(residents.Find("Tukang Jamu"), jamuProfile);
            if (catalogData.ApplyModifiedProperties()) AssetDatabase.SaveAssetIfDirty(catalog);
            if (databaseChanged)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
            }
            if (kept.Count > 0) Debug.LogWarning("[Daeng Saka 1E.36B] Periksa penempatan atau dialog kustom berikut. " +
                string.Join(", ", kept));
            Debug.Log($"[Daeng Saka 1E.36B] {bound} NPC Ibu Kota terhubung ke dialog. Simpan scene IbuKota.");
        }

        private static T LoadProfile<T>(string name) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(Profiles + name + ".asset");
            if (asset == null) throw new InvalidOperationException($"Impor {name}.asset sebelum memasang dialog Ibu Kota.");
            return asset;
        }

        private static bool TryBind(Transform npc, string title, string actorName, GmPixelCrushersDialogueBridge bridge)
        {
            GmPixelCrushersConversation talk = npc.GetComponent<GmPixelCrushersConversation>();
            if (npc.GetComponents<GmInteractable>().Any(other => other != talk) ||
                (talk != null && !string.IsNullOrEmpty(talk.ConversationTitle) && talk.ConversationTitle != title))
                return false;

            talk ??= Undo.AddComponent<GmPixelCrushersConversation>(npc.gameObject);
            Undo.RecordObject(talk, UndoName);
            talk.SetPresentation(actorName, "Bicara");
            talk.ConfigureAmbientDialogue(title, bridge, null, default);
            PrefabUtility.RecordPrefabInstancePropertyModifications(talk);

            var actor = npc.GetComponent<DialogueActor>() ?? Undo.AddComponent<DialogueActor>(npc.gameObject);
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

        private static void BindJamuPortrait(Transform npc, VNActorVisualProfile profile)
        {
            Sprite sprite = npc != null ? npc.GetComponentInChildren<SpriteRenderer>(true)?.sprite : null;
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
