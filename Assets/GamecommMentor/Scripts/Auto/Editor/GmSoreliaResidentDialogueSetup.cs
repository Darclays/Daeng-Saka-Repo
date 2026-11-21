using System;
using System.Linq;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GamecommMentor.DaengSaka.Editor.GmAmbientNpcPlacement;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmSoreliaResidentDialogueSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Sorelia Resident Dialogue";
        private const string UndoName = "Set Up Sorelia Resident Dialogue";
        private const string Village = "Assets/GamecommMentor/Scenes_GamecommMentor/DesaSorelia.unity";
        private const string Profiles = "Assets/GamecommMentor/Resources/DaengSaka/Dialogue/";
        internal const string FamilyConversation = "DaengSaka/Dialog Warga Sorelia/Keluarga Warga";
        internal const string ChildrenConversation = "DaengSaka/Dialog Warga Sorelia/Anak-Anak Desa";

        [MenuItem(Menu, true)]
        private static bool CanSetUp() => CanInstall(Village);

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
                Debug.Log("[Daeng Saka 1E.22B] Dialog warga siap. Simpan scene DesaSorelia.");
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
            Transform family = RequireChild(world, "Keluarga Nelayan");
            Transform residents = RequireChild(world, "Warga Sorelia Setelah Ditolong");
            Transform fisherMother = RequireChild(family, "Ibu Nelayan");
            Transform fisherChild = RequireChild(family, "Anak Nelayan");
            Transform villagerMother = RequireChild(residents, "Warga Ibu");
            Transform villagerFather = RequireChild(residents, "Warga Bapak");
            Transform villagerChild = RequireChild(residents, "Anak Warga");
            var quest = Require<GmMainQuestProgress>(RequireRoot(scene, "Daeng Saka - Encounter Flow"));
            Transform manager = RequireChild(world, "Dialogue Manager");
            var bridge = Require<GmPixelCrushersDialogueBridge>(manager);
            var database = bridge.AdditionalDatabase ?? Require<DialogueSystemController>(manager).initialDatabase;
            if (database == null) throw new InvalidOperationException("Dialogue Manager perlu Dialogue Database.");

            var catalog = AssetDatabase.LoadAssetAtPath<GmVnDialogueCatalog>(Profiles + "DaftarDialogVN.asset");
            var profile = AssetDatabase.LoadAssetAtPath<GmVnDialoguePilot>(Profiles + "ProfilDialogWargaSorelia.asset");
            if (catalog == null || profile == null)
                throw new InvalidOperationException("Daftar Dialog VN dan Profil Dialog Warga Sorelia diperlukan.");

            BindResident(fisherMother, GmSoreliaSuppliesDialogue.FirstConversation, 1.65f, bridge, quest);
            BindResident(fisherChild, ChildrenConversation, 1.4f, bridge, quest);
            BindResident(villagerMother, FamilyConversation, 1.65f, bridge, quest);
            BindResident(villagerFather, FamilyConversation, 1.85f, bridge, quest);
            BindResident(villagerChild, ChildrenConversation, 1.5f, bridge, quest);
            var supplies = fisherMother.GetComponent<GmSoreliaSuppliesDialogue>();
            if (supplies == null) supplies = Undo.AddComponent<GmSoreliaSuppliesDialogue>(fisherMother.gameObject);
            Undo.RecordObject(supplies, UndoName);
            supplies.Configure(quest, bridge);
            PrefabUtility.RecordPrefabInstancePropertyModifications(supplies);

            using var catalogData = new SerializedObject(catalog);
            var entries = catalogData.FindProperty("percakapan");
            var template = Template.FromDefault();
            Undo.RecordObject(database, UndoName);
            bool databaseChanged = false;

            void Add(string title, string partner, string completion, params (string actor, string text)[] lines)
            {
                databaseChanged |= GmMainQuestDialogueData.AddConversation(database, template, title, completion, partner, lines);
                for (int i = 0; i < entries.arraySize; i++)
                    if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("judul").stringValue == title) return;

                int index = entries.arraySize++;
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("judul").stringValue = title;
                entry.FindPropertyRelative("profil").objectReferenceValue = profile;
                entry.FindPropertyRelative("kameraQuestUtama").boolValue = false;
            }

            const string mother = "Ibu Nelayan";
            Add(GmSoreliaSuppliesDialogue.FirstConversation, mother, GmSoreliaSuppliesDialogue.CompletionEntry,
                (mother, "Kalian datang tepat waktu. Terima kasih banyak, ya, Nak."),
                ("Daung", "Sama-sama, Bu. Syukurlah kami kebetulan lewat sini."),
                ("Saka", "Betul, tapi ini masalah besar kalau monster mulai datang ke desa."),
                (mother, "Belakangan ini monster memang mulai sering terlihat di sekitar luar desa."),
                (mother, "Semoga perjalanan kalian ke Gua Lembayung bisa menyelesaikan masalah ini."),
                ("Pacca", "Tenang saja, Bu. Akan kami basmi siapa pun yang mengancam desa ini."),
                (mother, "Terima kasih, Nak. Ini ada lima Ramuan HP dan lima Ramuan MP dari Ibu untuk bekal."),
                ("Daung", "Wah, terima kasih banyak, Bu!"),
                (mother, "Seharusnya Ibu yang berterima kasih."));
            Add(GmSoreliaSuppliesDialogue.RepeatConversation, mother, null,
                (mother, "Terima kasih sudah menolong keluarga Ibu, Nak. Hati-hati di jalan, ya."));
            Add(FamilyConversation, "Warga Ibu", null,
                ("Warga Ibu", "Monsternya sudah dikalahkan, ya?"),
                ("Warga Bapak", "Sepertinya sudah, Bu."),
                ("Pacca", "Tenang saja, semuanya sudah dikalahkan."),
                ("Warga Bapak", "Terima kasih, anak muda, sudah membasmi monster itu."));
            Add(ChildrenConversation, "Anak Nelayan", null,
                ("Anak Nelayan", "Terima kasih, Kak, sudah menyelamatkan kami."),
                ("Anak Warga", "Kakak semua keren! Terutama hiunya Kakak Biru!"),
                ("Saka", "Hahaha, sama-sama. Kalian hati-hati, ya, kalau bermain di sini."));

            if (catalogData.ApplyModifiedProperties()) AssetDatabase.SaveAssetIfDirty(catalog);
            if (databaseChanged)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
            }
        }

        private static void BindResident(Transform resident, string title, float height,
            GmPixelCrushersDialogueBridge bridge, GmMainQuestProgress quest)
        {
            var interaction = resident.GetComponent<GmPixelCrushersConversation>();
            if (resident.GetComponents<GmInteractable>().Any(item => item != interaction) ||
                (interaction != null && interaction.ConversationTitle != title))
                throw new InvalidOperationException($"'{resident.name}' sudah memiliki interaksi lain.");

            if (resident.GetComponentInChildren<Collider>(true) == null)
            {
                var trigger = Undo.AddComponent<CapsuleCollider>(resident.gameObject);
                trigger.isTrigger = true;
                trigger.height = height;
                trigger.radius = .23f;
                trigger.center = Vector3.up * (height * .5f);
            }
            if (interaction == null) interaction = Undo.AddComponent<GmPixelCrushersConversation>(resident.gameObject);
            Undo.RecordObject(interaction, UndoName);
            interaction.SetPresentation(resident.name, "Bicara");
            interaction.ConfigureAmbientDialogue(title, bridge, quest, GmMainQuestStep.SelamatkanKeluargaNelayan);
            PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);
        }
    }
}
