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
        private const string Pua = "Pua’";
        private const string Merchant = "Merchant";
        private const string Penempa = "Penempa Besi";
        private const string Nelayan = "Nelayan";
        private const string Pakande = "Nenek Pakande";
        private const string Petunjuk = "Petunjuk";

        internal static GmPixelCrushersMainQuestDialogue.StepDialogue[] EnsureConversations(DialogueDatabase database)
        {
            var dialogues = new List<GmPixelCrushersMainQuestDialogue.StepDialogue>();
            Template template = Template.FromDefault();
            Add(database, template, dialogues, GmMainQuestStep.PersiapanSebelumBertarung, "Persiapan Sebelum Bertarung",
                (Pua, "Jadi kalian akan pergi ke gua lembayung selanjutnya kan?"),
                (Saka, "Betul mangge. kata Acci artifact selanjutnya ada disana"),
                (Pua, "Nih sedikit uang sangu, pastikan stok barang kalian cukup hingga ke gua lembayung"),
                (Saka, "Yuk, kita mampir ke toko-toko dulu"));
            Add(database, template, dialogues, GmMainQuestStep.KunjungiMerchant, "Kunjungi Merchant",
                (Merchant, "Selamat siang anak anak, ada yang bisa saya bantu?"),
                (Saka, "Siang bu, kita perlu membeli beberapa jamu dan makanan untuk perjalanan kami"),
                (Merchant, "Ini dia nak, sudah ibu kemas untuk kalian"),
                (Merchant, "Kalian sepertinya akan menempuh perjalanan jauh ya? bawakan ini ke bapak penempa"));
            Add(database, template, dialogues, GmMainQuestStep.KunjungiPenempaBesi, "Kunjungi Penempa Besi",
                (Pacca, "Pak tua, kami ingin membuat aksesoris dari bahan ini"),
                (Daung, "Heh Pacca, yang sopan dikit kenapa sih"),
                (Penempa, "Ini dia anak-anak, Anting untuk si gadis, kalung untuk si merah, dan cincin untuk nak Saka"),
                (Penempa, "Dengan senang hati anak-anak, semangat dengan misi kalian"));
            Add(database, template, dialogues, GmMainQuestStep.TemuiKeluargaNelayan, "Keluarga Nelayan di Sorelia",
                (Nelayan, "Tolooooong!!!!!!!"),
                (Saka, "Cepat, lari kerumah! akan kita tahan mereka disini"));
            Add(database, template, dialogues, GmMainQuestStep.BicaraDenganNelayan, "Petunjuk Gua Lembayung",
                (Nelayan, "Terima kasih nak sudah menyelamatkan keluarga saya"),
                (Nelayan, "Mereka sepertinya datang dari Gua Lembayung nak, belakangan ini aura gelap mulai terlihat dari mulut Guanya"),
                (Saka, "Sudah ku tebak... baiklah kalau begitu, kita harus segera kesana untuk menghentikan serangan monster ke desa ini"));
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

        internal static bool AddConversation(DialogueDatabase database, Template template,
            string title, string completionEntry, params (string actor, string text)[] lines)
        {
            if (database.GetConversation(title) != null) return false;

            Actor hero = GetOrCreateActor(database, template, Saka);
            Actor firstSpeaker = GetOrCreateActor(database, template, lines[0].actor);
            int id = 910150;
            while (database.GetConversation(id) != null) id++;
            Conversation conversation = template.CreateConversation(id, title);
            conversation.ActorID = hero.id;
            conversation.ConversantID = firstSpeaker.id;
            conversation.overrideSettings.useOverrides = true;
            conversation.overrideSettings.overrideSubtitleSettings = true;
            conversation.overrideSettings.showPCSubtitlesDuringLine = true;
            conversation.overrideSettings.continueButton = DisplaySettings.SubtitleSettings.ContinueButtonMode.Always;

            DialogueEntry previous = GmDialogueExampleData.Node(template, id, 0, hero.id, firstSpeaker.id, "START", "", 250f, 0f);
            previous.Sequence = "None()";
            conversation.dialogueEntries.Add(previous);
            for (int index = 0; index < lines.Length; index++)
            {
                Actor speaker = GetOrCreateActor(database, template, lines[index].actor);
                int listener = speaker.id == hero.id ? firstSpeaker.id : hero.id;
                int entryId = index + 1;
                DialogueEntry entry = GmDialogueExampleData.Node(template, id, entryId, speaker.id, listener,
                    speaker.Name, lines[index].text, 250f, entryId * 140f);
                // PC history is authored as linear dialogue, even when regular NPCs show response menus.
                if (speaker.IsPlayer) entry.MenuText = "[auto] " + lines[index].text;
                previous.outgoingLinks.Add(new Link(id, previous.id, id, entryId));
                conversation.dialogueEntries.Add(entry);
                previous = entry;
            }

            if (completionEntry != null)
            {
                int completionId = lines.Length + 1;
                DialogueEntry completion = GmDialogueExampleData.Node(template, id, completionId, firstSpeaker.id, hero.id,
                    completionEntry, "", 250f, completionId * 140f);
                completion.Sequence = "None()";
                previous.outgoingLinks.Add(new Link(id, previous.id, id, completionId));
                conversation.dialogueEntries.Add(completion);
            }
            database.conversations.Add(conversation);
            return true;
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
