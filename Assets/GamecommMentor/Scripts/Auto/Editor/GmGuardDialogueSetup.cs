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
    internal static class GmGuardDialogueSetup
    {
        private const string Menu = "GamecommMentor/Daeng Saka/Set Up Guard Dialogue";
        private const string UndoName = "Set Up Guard Dialogue";
        private const string Capital = "Assets/GamecommMentor/Scenes_GamecommMentor/IbuKota.unity";
        private const string Village = "Assets/GamecommMentor/Scenes_GamecommMentor/DesaSorelia.unity";
        private const string Profiles = "Assets/GamecommMentor/Resources/DaengSaka/Dialogue/";
        private const string Prefix = "DaengSaka/Dialog Penjaga/";
        private const string Yusuf = "Pak Yusuf";
        private const string GuardOne = "Penjaga 1";
        private const string GuardTwo = "Penjaga 2";
        private const string Daung = "Daung";
        private const string Saka = "Saka";

        [MenuItem(Menu, true)]
        private static bool CanSetUp() => CanInstall(Capital) || CanInstall(Village);

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
                Debug.Log($"[Daeng Saka 1E.20B] Dialog penjaga siap di {scene.name}. Simpan scene.");
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
            bool capital = scene.path == Capital;
            Transform world = RequireRoot(scene, "Daeng Saka - World Gameplay");
            Transform residents = RequireChild(world, capital ? "Warga Ibu Kota" : "Warga Sorelia Setelah Ditolong");
            Transform manager = RequireChild(world, "Dialogue Manager");
            var bridge = Require<GmPixelCrushersDialogueBridge>(manager);
            var database = bridge.AdditionalDatabase ?? Require<DialogueSystemController>(manager).initialDatabase;
            if (database == null) throw new InvalidOperationException("Dialogue Manager perlu Dialogue Database.");

            var catalog = AssetDatabase.LoadAssetAtPath<GmVnDialogueCatalog>(Profiles + "DaftarDialogVN.asset");
            var profile = AssetDatabase.LoadAssetAtPath<GmVnDialoguePilot>(Profiles + "ProfilDialogPenjaga.asset");
            if (catalog == null || profile == null)
                throw new InvalidOperationException("Daftar Dialog VN dan Profil Dialog Penjaga diperlukan.");

            using var catalogData = new SerializedObject(catalog);
            var entries = catalogData.FindProperty("percakapan");
            var template = Template.FromDefault();
            Undo.RecordObject(database, UndoName);
            bool databaseChanged = false;

            void AddGuard(string objectName, string name, string suffix, params (string actor, string text)[] lines)
            {
                Transform guard = RequireChild(residents, objectName);
                Require<Collider>(guard);
                string title = Prefix + suffix;
                var interaction = guard.GetComponent<GmPixelCrushersConversation>();
                if (guard.GetComponents<GmInteractable>().Any(item => item != interaction) ||
                    (interaction != null && interaction.ConversationTitle != title))
                    throw new InvalidOperationException($"'{objectName}' sudah memiliki interaksi lain.");

                databaseChanged |= GmMainQuestDialogueData.AddConversation(database, template, title, null, name, lines);
                if (interaction == null) interaction = Undo.AddComponent<GmPixelCrushersConversation>(guard.gameObject);
                Undo.RecordObject(interaction, UndoName);
                interaction.SetConversation(title);
                interaction.SetPresentation(name, "Bicara");
                using (var binding = new SerializedObject(interaction))
                {
                    binding.FindProperty("dialogueBridge").objectReferenceValue = bridge;
                    binding.ApplyModifiedProperties();
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);

                var actor = guard.GetComponent<DialogueActor>() ?? Undo.AddComponent<DialogueActor>(guard.gameObject);
                Undo.RecordObject(actor, UndoName);
                actor.actor = name;
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor);

                for (int i = 0; i < entries.arraySize; i++)
                    if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("judul").stringValue == title) return;
                int index = entries.arraySize;
                entries.arraySize++;
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("judul").stringValue = title;
                entry.FindPropertyRelative("profil").objectReferenceValue = profile;
                entry.FindPropertyRelative("kameraQuestUtama").boolValue = false;
            }

            if (capital)
            {
                AddGuard("Penjaga Kerajaan 1", Yusuf, "Pak Yusuf",
                    (Yusuf, "Maaf, Tuan Putri. Sang Paduka Raja sedang menerima tamu istimewa."),
                    (Yusuf, "Untuk sementara, kastil harus dikosongkan dari pengunjung."),
                    (Daung, "Ah, baiklah, Pak Yusuf. Aku akan kembali nanti."),
                    (Yusuf, "Hati-hati di perjalanan, Tuan Putri."),
                    (Yusuf, "(Senangnya, Tuan Putri mengingat namaku.)"));
            }
            else
            {
                AddGuard(GuardOne, GuardOne, "Sorelia 1",
                    (Daung, "Ke mana aja kamu? Tadi ada serangan Poppo, tahu!"),
                    (GuardOne, "Ma-maaf, Tuan Putri. Aku tadi lagi... lagi, anu..."),
                    (Daung, "Anu, anu? Anu apaan, hei?"),
                    (GuardOne, "Anu... aku tadi sakit perut, jadi aku bergegas ke kamar mandi, Tuan Putri."),
                    (Daung, "Sungguh... tidak bisa dipercaya. Akan aku laporkan kepada Yang Mulia Raja."),
                    (GuardOne, "Ti-tidaaaak!"));
                AddGuard(GuardTwo, GuardTwo, "Sorelia 2",
                    (Daung, "Ke mana aja kamu? Tadi ada serangan Poppo, tahu!"),
                    (GuardTwo, "Mohon maaf sebesar-besarnya, Tuan Putri. Kami tidak sadar ada yang berhasil melewati kami."),
                    (Saka, "Melewati?"),
                    (GuardTwo, "Betul. Tadi kami menemukan beberapa monster mendekat, jadi kami meninggalkan pos untuk menahan mereka."),
                    (Daung, "Baiklah, aku maafkan kalian. Akan aku beri laporan kepada Yang Mulia."),
                    (GuardTwo, "Terima kasih atas kebaikannya, Tuan Putri. Kami berjanji untuk lebih waspada."));
            }

            if (catalogData.ApplyModifiedProperties()) AssetDatabase.SaveAssetIfDirty(catalog);
            if (databaseChanged)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
            }
        }
    }
}
