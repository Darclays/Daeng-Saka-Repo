using System;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEngine;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmDialogueExampleData
    {
        internal const string DataRoot = "Assets/GamecommMentor/Data/PixelCrushersIntegration/DialogueDatabase";
        internal const string NelayanConversation = "DaengSaka/Examples/Nelayan";
        internal const string MerchantConversation = "DaengSaka/Examples/Pedagang";
        private const int Hero = 910001;
        private const int Fisher = 910002;
        private const int Merchant = 910003;

        internal static DialogueDatabase GetOrCreate()
        {
            EnsureFolder(DataRoot);
            string path = DataRoot + "/Exploration_Dialogue_Example.asset";
            DialogueDatabase existing = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(path);
            if (existing != null)
            {
                if (existing.GetConversation(NelayanConversation) == null || existing.GetConversation(MerchantConversation) == null)
                    throw new InvalidOperationException("Example database was edited. Restore its conversation titles or configure NPCs manually; the installer preserves existing content.");
                return existing;
            }
            Template template = Template.FromDefault();
            DialogueDatabase database = ScriptableObject.CreateInstance<DialogueDatabase>();
            database.name = "Exploration Dialogue Example";
            database.baseID = 910000;
            database.actors.Add(template.CreateActor(Hero, "Saka", true));
            database.actors.Add(template.CreateActor(Fisher, "Nelayan", false));
            database.actors.Add(template.CreateActor(Merchant, "Pedagang", false));
            database.conversations.Add(Create(template, 910101, Fisher, NelayanConversation,
                "Halo, Saka. Air sedang tenang hari ini.",
                "Di mana tempat yang bagus untuk memancing?", "Aku kembali nanti.",
                "Cobalah dekat dermaga. Ikan sering berkumpul di sana menjelang sore.", "Baik. Sampai jumpa, Saka."));
            database.conversations.Add(Create(template, 910102, Merchant, MerchantConversation,
                "Selamat datang, Saka. Ada yang ingin kamu tanyakan?",
                "Apa yang kamu jual?", "Aku hanya lewat.",
                "Perlengkapan perjalanan. Datanglah lagi sebelum kamu berangkat jauh.", "Hati-hati di jalan."));
            AssetDatabase.CreateAsset(database, path);
            return database;
        }

        private static Conversation Create(Template template, int id, int npc, string title,
            string greeting, string optionA, string optionB, string answerA, string answerB)
        {
            Conversation conversation = template.CreateConversation(id, title);
            conversation.ActorID = Hero;
            conversation.ConversantID = npc;
            DialogueEntry start = Node(template, id, 0, Hero, npc, "START", "", 250f, 0f);
            DialogueEntry hello = Node(template, id, 1, npc, Hero, "Sapaan", greeting, 250f, 140f);
            DialogueEntry a = Node(template, id, 2, Hero, npc, "Pilihan A", optionA, 50f, 300f);
            DialogueEntry b = Node(template, id, 3, Hero, npc, "Pilihan B", optionB, 450f, 300f);
            DialogueEntry replyA = Node(template, id, 4, npc, Hero, "Jawaban A", answerA, 50f, 460f);
            DialogueEntry replyB = Node(template, id, 5, npc, Hero, "Jawaban B", answerB, 450f, 460f);
            start.outgoingLinks.Add(new Link(id, 0, id, 1));
            hello.outgoingLinks.Add(new Link(id, 1, id, 2));
            hello.outgoingLinks.Add(new Link(id, 1, id, 3));
            a.outgoingLinks.Add(new Link(id, 2, id, 4));
            b.outgoingLinks.Add(new Link(id, 3, id, 5));
            conversation.dialogueEntries.AddRange(new[] { start, hello, a, b, replyA, replyB });
            return conversation;
        }

        private static DialogueEntry Node(Template template, int conversationID, int id, int actor,
            int conversant, string title, string text, float x, float y)
        {
            DialogueEntry node = template.CreateDialogueEntry(id, conversationID, title);
            node.ActorID = actor;
            node.ConversantID = conversant;
            node.DialogueText = text;
            node.canvasRect = new Rect(x, y, 180f, 90f);
            return node;
        }

        internal static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string child = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(child)) AssetDatabase.CreateFolder(parent, parts[i]);
                parent = child;
            }
        }
    }
}
