using System;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmSoreliaQuestDialogueSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Sorelia Quest Dialogue";
        private const string UndoName = "Set Up Sorelia Quest Dialogue";
        private const string Village = "Assets/GamecommMentor/Scenes_GamecommMentor/DesaSorelia.unity";
        private const string Profiles = "Assets/GamecommMentor/Resources/DaengSaka/Dialogue/";
        private const string CatalogPath = Profiles + "DaftarDialogVN.asset";
        private const string ProfilePath = Profiles + "ProfilDialogQuestSorelia.asset";
        private const string Title = GmMainQuestDialogueData.SoreliaAfterPoppoTitle;

        [MenuItem(Menu, true)]
        private static bool CanSetUp() => CanInstall(Village);

        [MenuItem(Menu)]
        private static void SetUp()
        {
            if (!CanSetUp()) return;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                InstallInScene(SceneManager.GetActiveScene());
                Undo.CollapseUndoOperations(group);
                Debug.Log("[Daeng Saka 1E.23B R1] Dialog quest Sorelia setelah Poppo siap.");
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
            Transform manager = RequireChild(world, "Dialogue Manager");
            var bridge = Require<GmPixelCrushersDialogueBridge>(manager);
            var database = Require<DialogueSystemController>(manager).initialDatabase;
            var conversation = database != null ? database.GetConversation(Title) : null;
            var additional = bridge.AdditionalDatabase;
            if (additional != null && additional != database)
            {
                var extraConversation = additional.GetConversation(Title);
                if (extraConversation != null)
                {
                    if (conversation != null)
                        throw new InvalidOperationException($"Dialog '{Title}' terdaftar di kedua Dialogue Database.");
                    database = additional;
                    conversation = extraConversation;
                }
            }
            if (conversation == null)
                throw new InvalidOperationException($"Dialog '{Title}' belum ada di Dialogue Manager DesaSorelia.");

            var catalog = AssetDatabase.LoadAssetAtPath<GmVnDialogueCatalog>(CatalogPath);
            if (catalog == null)
                throw new InvalidOperationException($"Daftar Dialog VN gagal dimuat. Cek aset di '{CatalogPath}'.");
            var profile = AssetDatabase.LoadAssetAtPath<GmVnDialoguePilot>(ProfilePath);
            if (profile == null)
                throw new InvalidOperationException($"Profil Dialog Quest Sorelia gagal dimuat. Cek aset dan file .meta di '{ProfilePath}'.");

            using var catalogData = new SerializedObject(catalog);
            var entries = catalogData.FindProperty("percakapan");
            int index = 0;
            for (; index < entries.arraySize; index++)
                if (entries.GetArrayElementAtIndex(index).FindPropertyRelative("judul").stringValue == Title) break;

            Undo.RecordObject(database, UndoName);
            bool updated = GmMainQuestDialogueData.UpgradeSoreliaAfterPoppo(database, conversation);
            bool added = index == entries.arraySize;
            if (added) entries.arraySize++;
            var entry = entries.GetArrayElementAtIndex(index);
            if (added)
            {
                entry.FindPropertyRelative("judul").stringValue = Title;
                entry.FindPropertyRelative("kameraQuestUtama").boolValue = true;
            }
            entry.FindPropertyRelative("profil").objectReferenceValue = profile;
            bool catalogUpdated = catalogData.ApplyModifiedProperties();

            if (updated)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
            }
            if (catalogUpdated) AssetDatabase.SaveAssetIfDirty(catalog);
        }
    }
}
