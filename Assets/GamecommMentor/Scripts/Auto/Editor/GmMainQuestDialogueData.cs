using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmMainQuestDialogueData
    {
        private const string Prefix = "DaengSaka/Main Quest/";
        private const string Saka = "Saka";
        private const string Pacca = "Pacca";
        private const string Daung = "Daung";
        private const string Nelayan = "Nelayan";
        private const string Pakande = "Nenek Pakande";
        private const string Petunjuk = "Petunjuk";
        private const string SoreliaAfterPoppo = "Petunjuk Gua Lembayung";
        internal const string SoreliaAfterPoppoTitle = Prefix + SoreliaAfterPoppo;

        private static readonly (string actor, string text)[] SoreliaAfterPoppoLines =
        {
            (Nelayan, "Terima kasih, Nak, sudah menyelamatkan keluarga saya."),
            (Daung, "Apakah Bapak dan keluarga aman? Kalian tidak terluka, kan?"),
            ("Anak Nelayan", "Kami tidak apa-apa, Kak. Kalian datang sangat tepat waktu."),
            (Saka, "Monster itu datang dari mana, ya? Aku kira desa ini aman dari monster."),
            (Nelayan, "Mereka sepertinya datang dari Gua Lembayung, Nak. Belakangan ini aura gelap mulai terlihat dari mulut guanya."),
            (Pacca, "Sepertinya pemikiranmu benar, Saka."),
            (Saka, "Sudah kutebak. Baiklah, kita harus segera ke sana untuk menghentikan serangan monster ke desa ini."),
            (Nelayan, "Kalian berencana mau ke gua itu? Lebih baik jangan, Nak. Di sana sangat berbahaya."),
            (Daung, "Tenang, Pak. Kami utusan dari kerajaan untuk menghentikan masalah ini."),
            ("Ibu Nelayan", "Baiklah, kalau begitu. Kalian boleh istirahat dulu sebelum pergi ke sana."),
            (Nelayan, "Kalau mau, kalian bisa memancing di sungai dekat sini untuk istirahat sejenak."),
            (Saka, "Baiklah, kita pikir-pikir dulu, Pak.")
        };

        private static readonly (string actor, string text)[] PreviousSoreliaAfterPoppoLines =
        {
            (Nelayan, "Terima kasih nak sudah menyelamatkan keluarga saya"),
            (Nelayan, "Mereka sepertinya datang dari Gua Lembayung nak, belakangan ini aura gelap mulai terlihat dari mulut Guanya"),
            (Saka, "Sudah ku tebak... baiklah kalau begitu, kita harus segera kesana untuk menghentikan serangan monster ke desa ini")
        };

        internal static GmPixelCrushersMainQuestDialogue.StepDialogue[] EnsureConversations(DialogueDatabase database)
        {
            var dialogues = new List<GmPixelCrushersMainQuestDialogue.StepDialogue>();
            Template template = Template.FromDefault();
            foreach (var story in GmCapitalDialogueData.Story)
            {
                string title = Prefix + story.suffix;
                dialogues.Add(new GmPixelCrushersMainQuestDialogue.StepDialogue(story.step, title));
                AddConversation(database, template, title,
                    GmPixelCrushersMainQuestDialogue.CompletionEntryTitle, story.partner, story.lines);
            }
            Add(database, template, dialogues, GmMainQuestStep.TemuiKeluargaNelayan, "Keluarga Nelayan di Sorelia",
                (Nelayan, "Tolooooong!!!!!!!"),
                (Saka, "Cepat, lari kerumah! akan kita tahan mereka disini"));
            bool createSoreliaDialogue = database.GetConversation(SoreliaAfterPoppoTitle) == null;
            Add(database, template, dialogues, GmMainQuestStep.BicaraDenganNelayan, SoreliaAfterPoppo, PreviousSoreliaAfterPoppoLines);
            Add(database, template, dialogues, GmMainQuestStep.MasukGuaLembayung, "Masuk Gua Lembayung",
                (Pacca, "Wah.... Gelap"),
                (Daung, "Iyalah.. namanya juga Gua.. disini tidak banyak sumber cahaya"),
                (Saka, "Baiklah ayo kita selidiki Gua ini"));
            Add(database, template, dialogues, GmMainQuestStep.TemuiPakande, "Temui Nenek Pakande",
                (Daung, "Yeaay kita berhasil mengalahkan monster itu"),
                (Pakande, "grr.... APA YANG KALIAN TELAH LAKUKAN???"),
                (Pakande, "MONSTER TERSEBUT ADALAH HASIL KARYAKU"),
                (Pacca, "Awas nenek itu akan menyerang"));
            Add(database, template, dialogues, GmMainQuestStep.Penutup, "Ravi dan Pusaka Acci",
                (Pakande, "TIDAAAAAAAAAAK TUAN RAVI... MAAF AKU DIKALAHKAN OLEH ANAK.ANAK INGUSAN..........."),
                (Pacca, "Hei kau dengar itu? dia menyebut Ravi juga"),
                (Saka, "ini ketiga kalinya lawan kuat seperti dia menyebut nama Ravi"),
                (Daung, "Jangan-jangan.. Ravi ravi ini ada kaitannya dengan monster yang menjadi sering bermunculan ya.."),
                (Saka, "Melihat nenek-nenek tersebut bisa memanggil monster tersebut dengan mudah... aku tidak kaget.."),
                (Pacca, "Dia juga mengeluarkan aura gelap sama seperti kedua lawan sebelumnya"),
                (Daung, "Mana keduanya berada ditempat yang ada bagian dari Acci lagi..."),
                (Pacca, "Saka. sepertinya pusaka Acci ini juga memiliki kaitan dengan peristiwa aneh ini"),
                (Saka, "Aku setuju, semakin besar alasanku untuk melanjutkan petualangan kita ini"),
                (Daung, "Sepertinya kita harus pergi kepulau lain untuk mencari petunjuk"),
                (Pacca, "Hahaha, kemanapun itu aku akan selalu ikut untuk menjaga kalian"),
                (Saka, "Terima kasih teman-teman, untuk sekarang, ayo kita ambil bagian pusaa Acci terlebih dulu"));
            Add(database, template, dialogues, GmMainQuestStep.SelamatkanKeluargaNelayan, "Petunjuk Battle Poppo",
                (Petunjuk, "Cari penanda Poppo. Sentuh marker untuk memulai battle, lalu kembali ke sini setelah menang."));
            Add(database, template, dialogues, GmMainQuestStep.SelidikiGua, "Petunjuk Battle Gua",
                (Petunjuk, "Kalahkan musuh gua yang ditandai di layar. Setelah menang, kembali ke sini untuk bertemu Nenek Pakande."));
            Add(database, template, dialogues, GmMainQuestStep.LawanPakande, "Petunjuk Battle Pakande",
                (Petunjuk, "Dekati dan sentuh penanda Nenek Pakande untuk memulai battle."));
            // Assign the existing cast's IDs before adding the two new speakers.
            if (createSoreliaDialogue)
                UpgradeSoreliaAfterPoppo(database, database.GetConversation(SoreliaAfterPoppoTitle));
            return dialogues.ToArray();
        }

        private static void Add(DialogueDatabase database, Template template,
            List<GmPixelCrushersMainQuestDialogue.StepDialogue> dialogues, GmMainQuestStep step,
            string title, params (string actor, string text)[] lines)
        {
            title = Prefix + title;
            dialogues.Add(new GmPixelCrushersMainQuestDialogue.StepDialogue(step, title));
            string completionEntry = GmMainQuestProgress.IsEncounterStep(step)
                ? null : GmPixelCrushersMainQuestDialogue.CompletionEntryTitle;
            AddConversation(database, template, title, completionEntry, lines);
        }

        internal static bool EnsureFishingLoanConversation(DialogueDatabase database)
        {
            return AddConversation(database, Template.FromDefault(), GmFishingQuestDialogue.LoanConversation,
                GmFishingQuestDialogue.LoanCompletionEntry,
                (Nelayan, "Jadi mau mencoba memancing, Nak?"),
                (Saka, "Iya, Pak. Boleh pinjam alat pancing? Punyaku rusak."),
                (Nelayan, "Baiklah, ini alat pancing dan umpannya."),
                (Nelayan, "Coba memancing dari tepi air, di tempat ikan berkumpul."));
        }

        internal static bool UpgradeSoreliaAfterPoppo(DialogueDatabase database, Conversation conversation)
        {
            if (MatchesGeneratedQuest(database, conversation, Nelayan, SoreliaAfterPoppoLines)) return false;
            if (!MatchesGeneratedQuest(database, conversation, Nelayan, PreviousSoreliaAfterPoppoLines))
                throw new InvalidOperationException(
                    $"Dialog '{conversation.Title}' sudah diedit. Cadangkan versi itu sebelum memasukkan revisi.");

            var updated = CreateLinearEntries(database, Template.FromDefault(), conversation,
                GmPixelCrushersMainQuestDialogue.CompletionEntryTitle, SoreliaAfterPoppoLines);
            conversation.dialogueEntries.Clear();
            conversation.dialogueEntries.AddRange(updated);
            return true;
        }

        internal static bool UpgradeCapitalStory(DialogueDatabase database, Template template, ICollection<string> kept)
        {
            bool changed = false;
            const string completion = GmPixelCrushersMainQuestDialogue.CompletionEntryTitle;
            foreach (var story in GmCapitalDialogueData.Story)
            {
                string title = Prefix + story.suffix;
                Conversation conversation = database.GetConversation(title);
                if (conversation == null)
                {
                    changed |= AddConversation(database, template, title, completion, story.partner, story.lines);
                    continue;
                }
                if (MatchesGeneratedQuest(database, conversation, story.partner, story.lines)) continue;
                bool fromFullOpening = story.step == GmMainQuestStep.PersiapanSebelumBertarung &&
                    MatchesGeneratedQuest(database, conversation, GmCapitalDialogueData.FormerPua,
                        GmCapitalDialogueData.FormerOpening, checkPresentation: true);
                if (!fromFullOpening &&
                    !MatchesGeneratedQuest(database, conversation, story.previous[0].actor, story.previous, checkPresentation: true))
                {
                    kept.Add(title);
                    continue;
                }
                conversation.ConversantID = GetOrCreateActor(database, template, story.partner).id;
                var updated = CreateLinearEntries(database, template, conversation, completion, story.lines);
                conversation.dialogueEntries.Clear();
                conversation.dialogueEntries.AddRange(updated);
                changed = true;
            }
            return changed;
        }

        private static bool MatchesGeneratedQuest(DialogueDatabase database, Conversation conversation,
            string partner, (string actor, string text)[] lines, bool checkPresentation = false)
        {
            var entries = conversation.dialogueEntries;
            int end = lines.Length + 1;
            if (entries.Count != end + 1 || entries[0].Title != "START" ||
                entries[end].Title != GmPixelCrushersMainQuestDialogue.CompletionEntryTitle ||
                !string.IsNullOrEmpty(entries[0].DialogueText) || !string.IsNullOrEmpty(entries[end].DialogueText) ||
                database.GetActor(conversation.ActorID)?.Name != Saka ||
                database.GetActor(conversation.ConversantID)?.Name != partner) return false;

            for (int i = 0; i <= end; i++)
            {
                DialogueEntry entry = entries[i];
                if (entry.id != i || entry.conversationID != conversation.id ||
                    !string.IsNullOrEmpty(entry.conditionsString) || !string.IsNullOrEmpty(entry.userScript) ||
                    entry.outgoingLinks.Count != (i == end ? 0 : 1)) return false;
                if (checkPresentation)
                {
                    bool terminal = i == 0 || i == end;
                    if (terminal ? entry.Sequence != "None()" : !string.IsNullOrEmpty(entry.Sequence)) return false;
                    if (!terminal)
                    {
                        var speaker = database.GetActor(entry.ActorID);
                        string menu = speaker?.IsPlayer == true ? "[auto] " + lines[i - 1].text : "";
                        if (entry.Title != lines[i - 1].actor || (entry.MenuText ?? "") != menu ||
                            entry.ConversantID != (entry.ActorID == conversation.ActorID
                                ? conversation.ConversantID : conversation.ActorID)) return false;
                    }
                }
                if (i == end) break;

                Link next = entry.outgoingLinks[0];
                if (next.originConversationID != conversation.id || next.destinationConversationID != conversation.id ||
                    next.originDialogueID != i || next.destinationDialogueID != i + 1) return false;
                if (i > 0 && (entry.DialogueText != lines[i - 1].text ||
                    database.GetActor(entry.ActorID)?.Name != lines[i - 1].actor)) return false;
            }
            return true;
        }

        internal static bool AddConversation(DialogueDatabase database, Template template,
            string title, string completionEntry, params (string actor, string text)[] lines) =>
            AddConversation(database, template, title, completionEntry, null, lines);

        internal static bool ReviseGeneratedLine(DialogueDatabase database, string title, string speaker,
            string previous, string replacement)
        {
            var conversation = database.GetConversation(title);
            if (conversation == null) return false;
            foreach (var entry in conversation.dialogueEntries)
            {
                if (entry.Title != speaker || entry.DialogueText != previous ||
                    database.GetActor(entry.ActorID)?.Name != speaker) continue;
                entry.DialogueText = replacement;
                if (entry.MenuText == "[auto] " + previous) entry.MenuText = "[auto] " + replacement;
                return true;
            }
            return false;
        }

        internal static bool AddConversation(DialogueDatabase database, Template template,
            string title, string completionEntry, string partnerName, params (string actor, string text)[] lines)
        {
            if (database.GetConversation(title) != null) return false;

            Actor hero = GetOrCreateActor(database, template, Saka);
            Actor partner = GetOrCreateActor(database, template, partnerName ?? lines[0].actor);
            int id = 910150;
            while (database.GetConversation(id) != null) id++;
            Conversation conversation = template.CreateConversation(id, title);
            conversation.ActorID = hero.id;
            conversation.ConversantID = partner.id;
            conversation.overrideSettings.useOverrides = true;
            conversation.overrideSettings.overrideSubtitleSettings = true;
            conversation.overrideSettings.showPCSubtitlesDuringLine = true;
            conversation.overrideSettings.continueButton = DisplaySettings.SubtitleSettings.ContinueButtonMode.Always;
            conversation.dialogueEntries.AddRange(CreateLinearEntries(database, template, conversation, completionEntry, lines));
            database.conversations.Add(conversation);
            return true;
        }

        private static List<DialogueEntry> CreateLinearEntries(DialogueDatabase database, Template template,
            Conversation conversation, string completionEntry, (string actor, string text)[] lines)
        {
            int id = conversation.id;
            int hero = conversation.ActorID;
            int partner = conversation.ConversantID;
            var entries = new List<DialogueEntry>(lines.Length + 2);
            DialogueEntry previous = GmDialogueExampleData.Node(template, id, 0, hero, partner, "START", "", 250f, 0f);
            previous.Sequence = "None()";
            entries.Add(previous);
            for (int index = 0; index < lines.Length; index++)
            {
                Actor speaker = GetOrCreateActor(database, template, lines[index].actor);
                int listener = speaker.id == hero ? partner : hero;
                int entryId = index + 1;
                DialogueEntry entry = GmDialogueExampleData.Node(template, id, entryId, speaker.id, listener,
                    speaker.Name, lines[index].text, 250f, entryId * 140f);
                // PC history is authored as linear dialogue, even when regular NPCs show response menus.
                if (speaker.IsPlayer) entry.MenuText = "[auto] " + lines[index].text;
                previous.outgoingLinks.Add(new Link(id, previous.id, id, entryId));
                entries.Add(entry);
                previous = entry;
            }

            if (completionEntry != null)
            {
                int completionId = lines.Length + 1;
                DialogueEntry completion = GmDialogueExampleData.Node(template, id, completionId, partner, hero,
                    completionEntry, "", 250f, completionId * 140f);
                completion.Sequence = "None()";
                previous.outgoingLinks.Add(new Link(id, previous.id, id, completionId));
                entries.Add(completion);
            }
            return entries;
        }

        private static Actor GetOrCreateActor(DialogueDatabase database, Template template, string name)
        {
            Actor actor = database.GetActor(name);
            if (actor != null) return actor;

            int id = 910050;
            while (database.GetActor(id) != null) id++;
            actor = template.CreateActor(id, name, name == Saka);
            database.actors.Add(actor);
            return actor;
        }
    }
}
