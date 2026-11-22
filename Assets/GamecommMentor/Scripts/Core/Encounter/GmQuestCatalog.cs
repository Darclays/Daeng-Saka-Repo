using System;
using System.Collections.Generic;

namespace GamecommMentor.DaengSaka
{
    public enum GmQuestArea { Kerajaan, DesaSorelia, GoaLembayung }

    public readonly struct GmQuestGoal
    {
        public readonly string Text;
        public readonly GmQuestArea Area;
        public readonly string Landmark;
        public readonly string Progress;
        public readonly string CompletedProgress;

        public GmQuestGoal(string text, GmQuestArea area, string landmark,
            string progress = null, string completedProgress = null)
        {
            Text = text;
            Area = area;
            Landmark = landmark;
            Progress = progress;
            CompletedProgress = completedProgress;
        }
    }

    public sealed class GmSideQuestDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public IReadOnlyList<GmQuestGoal> Goals { get; }

        public GmSideQuestDefinition(string id, string title, string description, params GmQuestGoal[] goals)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Quest needs a stable ID.", nameof(id));
            if (goals == null || goals.Length == 0) throw new ArgumentException("Quest needs goals.", nameof(goals));
            Id = id;
            Title = title;
            Description = description;
            Goals = Array.AsReadOnly((GmQuestGoal[])goals.Clone());
        }
    }

    internal sealed class GmQuestEntry
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Description;
        public readonly bool IsSideQuest;
        public readonly IReadOnlyList<GmQuestGoal> Goals;
        public readonly int CompletedSteps;
        public bool IsComplete => CompletedSteps == Goals.Count;
        public GmQuestGoal CurrentGoal => Goals[CompletedSteps];

        public GmQuestEntry(string id, string title, string description, bool isSideQuest,
            IReadOnlyList<GmQuestGoal> goals, int completedSteps)
        {
            Id = id;
            Title = title;
            Description = description;
            IsSideQuest = isSideQuest;
            Goals = goals;
            CompletedSteps = completedSteps;
        }
    }

    public static class GmQuestCatalog
    {
        public const string Guide = "guide";
        public const string Merchant = "merchant";
        public const string Blacksmith = "blacksmith";
        public const string CastleGuard = "castle-guard";
        public const string Exit = "exit";
        public const string Encounter = "encounter";
        public const string Pakande = "pakande";
        public const string FishingSpot = "fishing-spot";

        public static readonly GmSideQuestDefinition Fishing = new GmSideQuestDefinition(
            "side.fishing.sorelia", "Memancing di Sorelia",
            "Pinjam perlengkapan dari Nelayan, lalu coba memancing di tepi sungai Sorelia.",
            new GmQuestGoal("Pinjam alat pancing dan umpan", GmQuestArea.DesaSorelia, Guide),
            new GmQuestGoal("Tangkap 1 ikan", GmQuestArea.DesaSorelia, FishingSpot, "0 / 1", "1 / 1"));

        public const int CityDeliveryReward = 70;
        public static readonly GmSideQuestDefinition CityDelivery = new GmSideQuestDefinition(
            "side.delivery.capital", "Titipan Penempa Besi",
            $"Antarkan pedang Pak Yusuf keliling Kerajaan. Upah {CityDeliveryReward} koin dari Penempa Besi.",
            new GmQuestGoal("Minta Daeng Aco memeriksa pedang", GmQuestArea.Kerajaan, Guide),
            new GmQuestGoal("Antarkan pedang ke Pak Yusuf", GmQuestArea.Kerajaan, CastleGuard),
            new GmQuestGoal("Ambil upah dari Penempa Besi", GmQuestArea.Kerajaan, Blacksmith));

        private sealed class Chapter
        {
            public readonly string Id;
            public readonly string Title;
            public readonly string Description;
            public readonly GmMainQuestStep[] Steps;
            public readonly GmQuestGoal[] Goals;

            public Chapter(string id, string title, string description,
                params (GmMainQuestStep step, GmQuestGoal goal)[] sequence)
            {
                Id = id;
                Title = title;
                Description = description;
                Steps = new GmMainQuestStep[sequence.Length];
                Goals = new GmQuestGoal[sequence.Length];
                for (int i = 0; i < sequence.Length; i++)
                    (Steps[i], Goals[i]) = sequence[i];
            }
        }

        private static readonly Chapter[] MainChapters =
        {
            new Chapter("main.preparation", "Persiapan Sebelum Bertarung",
                "Siapkan perbekalan dan perlengkapan di Kerajaan sebelum berangkat.",
                (GmMainQuestStep.PersiapanSebelumBertarung, new GmQuestGoal("Bicara dengan Daeng Aco", GmQuestArea.Kerajaan, Guide)),
                (GmMainQuestStep.KunjungiMerchant, new GmQuestGoal("Kunjungi toko Merchant", GmQuestArea.Kerajaan, Merchant)),
                (GmMainQuestStep.KunjungiPenempaBesi, new GmQuestGoal("Kunjungi Penempa Besi", GmQuestArea.Kerajaan, Blacksmith))),
            new Chapter("main.sorelia", "Pergi ke Goa Lembayung",
                "Lewati Desa Sorelia dan cari jalan menuju sumber serangan monster.",
                (GmMainQuestStep.TemuiKeluargaNelayan, new GmQuestGoal("Temui keluarga Nelayan", GmQuestArea.DesaSorelia, Guide)),
                (GmMainQuestStep.SelamatkanKeluargaNelayan, new GmQuestGoal("Selamatkan keluarga Nelayan", GmQuestArea.DesaSorelia, Encounter)),
                (GmMainQuestStep.BicaraDenganNelayan, new GmQuestGoal("Bicara dengan Nelayan", GmQuestArea.DesaSorelia, Guide)),
                (GmMainQuestStep.MasukGuaLembayung, new GmQuestGoal("Masuk ke Goa Lembayung", GmQuestArea.GoaLembayung, Guide))),
            new Chapter("main.cave", "Selidiki Goa Lembayung",
                "Selidiki goa dan hadapi Nenek Pakande.",
                (GmMainQuestStep.SelidikiGua, new GmQuestGoal("Kalahkan Musuh Goa", GmQuestArea.GoaLembayung, Encounter)),
                (GmMainQuestStep.TemuiPakande, new GmQuestGoal("Temui Nenek Pakande", GmQuestArea.GoaLembayung, Pakande)),
                (GmMainQuestStep.LawanPakande, new GmQuestGoal("Kalahkan Nenek Pakande", GmQuestArea.GoaLembayung, Pakande)),
                (GmMainQuestStep.Penutup, new GmQuestGoal("Selesaikan percakapan penutup", GmQuestArea.GoaLembayung, Guide)))
        };

        internal static void AddMainEntries(GmMainQuestProgress progress, List<GmQuestEntry> entries)
        {
            foreach (Chapter chapter in MainChapters)
            {
                int completed = 0;
                while (completed < chapter.Steps.Length && progress.HasCompleted(chapter.Steps[completed]))
                    completed++;
                if (completed == chapter.Steps.Length || chapter.Steps[completed] == progress.Step)
                    entries.Add(new GmQuestEntry(chapter.Id, chapter.Title, chapter.Description,
                        false, chapter.Goals, completed));
            }
        }

        internal static bool IsMainQuestId(string id) => Array.Exists(MainChapters, chapter => chapter.Id == id);

        internal static string ChapterStartingAt(GmMainQuestStep step)
        {
            foreach (Chapter chapter in MainChapters)
                if (chapter.Steps[0] == step) return chapter.Title;
            return null;
        }

        public static string AreaName(GmQuestArea area) => area switch
        {
            GmQuestArea.Kerajaan => "Kerajaan",
            GmQuestArea.DesaSorelia => "Desa Sorelia",
            GmQuestArea.GoaLembayung => "Goa Lembayung",
            _ => area.ToString()
        };
    }
}
