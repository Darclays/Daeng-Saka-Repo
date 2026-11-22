using System;

namespace GamecommMentor.DaengSaka.Editor
{
    internal static class GmCapitalDialogueData
    {
        private const string Ambient = "DaengSaka/Dialog Ibu Kota/";
        private const string Saka = "Saka";
        private const string Pacca = "Pacca";
        private const string Daung = "Daung";
        internal const string FormerPua = "Pua’";
        internal const string Aco = "Daeng Aco";
        private const string Merchant = "Merchant";
        private const string Smith = "Penempa Besi";
        private const string Mother = "Warga Ibu";
        private const string Child = "Anak Warga";
        private const string Father = "Warga Bapak";
        private const string Fisher = "Nelayan Bapak";
        private const string Jamu = "Tukang Jamu";
        private const string Guard = "Penjaga Kerajaan";
        private const string GateGuard = "Penjaga Gerbang";
        private const string Yusuf = "Pak Yusuf";

        internal const string YusufTitle = "DaengSaka/Dialog Penjaga/Pak Yusuf";

        internal static readonly (string actor, string text)[] FormerOpening =
        {
            (FormerPua, "Jadi kalian akan pergi ke gua lembayung selanjutnya kan?"),
            (Saka, "Betul mangge. kata Acci artifact selanjutnya ada disana"),
            (FormerPua, "Berhati-hati lah nak. Gua tersebut sangat berbahaya"),
            (Pacca, "Tenang Pua’, Pacca tidak akan membiarkan Saka terluka"),
            (Daung, "Ngomong-ngomong, kita perlu menyetok ulang barang bawaan kita"),
            (Saka, "Iya nih.. jamu dan makanan kita sudah hampir habis"),
            (FormerPua, "Duh.. kalian ini gimana sih? masa kalian tidak memasok stok yang cukup sih?"),
            (FormerPua, "Nih sedikit uang sangu, pastikan stok barang kalian cukup hingga ke gua lembayung"),
            (Pacca, "Wah terima kasih banyak Pua’"),
            (Daung, "Yuk, kita mampir ke toko-toko dulu")
        };

        internal static readonly (string title, string partner, (string actor, string text)[] lines)[] Delivery =
        {
            (GmCapitalDeliveryDialogue.OfferTitle, Smith, new[]
            {
                (Smith, "Eh, Nak. Sebelum berangkat, boleh om titip sesuatu?"),
                (Daung, "Boleh, Om. Mau titip apa?"),
                (Smith, "Pedang Pak Yusuf sudah selesai. Bawa dulu ke Daeng Aco, ya. Dia yang pesan gagangnya dibungkus kain."),
                (Pacca, "Wah, akhirnya ada juga yang mau menitipkan pedang ke Pacca."),
                (Daung, "Menitipkan, Pacca. Pegang sarungnya yang benar."),
                (Smith, "Kalau Pak Yusuf sudah terima pedangnya, kembali ke sini. Om sudah siapkan upah kalian."),
                (Saka, "Siap, Om. Kita temui Daeng Aco dulu, yuk.")
            }),
            (GmCapitalDeliveryDialogue.InspectionTitle, Aco, new[]
            {
                (Aco, "Lho, kalian sudah kembali? Ada perbekalan yang kurang?"),
                (Saka, "Kami bawa pedang Pak Yusuf, Mangge. Kata penempa, gagangnya perlu diperiksa dulu."),
                (Aco, "Nah, pas. Waktu hujan kemarin pedangnya hampir lepas dari tangan. Begini enak digenggam."),
                (Pacca, "Kalau aku sih masih lebih percaya perisai."),
                (Aco, "Ya, bawa perisaimu baik-baik. Pak Yusuf sedang berjaga di kaki tangga istana. Bilang saja sudah kuperiksa.")
            }),
            (GmCapitalDeliveryDialogue.DeliveryTitle, Yusuf, new[]
            {
                (Daung, "Pak Yusuf, ini pedang Bapak. Daeng Aco sudah memeriksa gagangnya."),
                (Yusuf, "Wah, terima kasih, Tuan Putri. Sudah dua hari saya pinjam pedang penjaga sebelah."),
                (Pacca, "Berarti sekarang beliau bisa berhenti menagih, ya, Pak?"),
                (Yusuf, "Hahaha, betul. Ini tanda terimanya. Tolong bilang ke penempa, gagangnya sudah pas."),
                (Saka, "Siap, Pak. Kami sampaikan.")
            }),
            (GmCapitalDeliveryDialogue.PaymentTitle, Smith, new[]
            {
                (Saka, "Om, ini tanda terima dari Pak Yusuf. Katanya gagang pedangnya sudah pas."),
                (Smith, "Baguslah. Kemarin dia sampai tiga kali mampir buat menanyakan pedangnya."),
                (Smith, $"Ini upahnya, {GmQuestCatalog.CityDeliveryReward} koin. Kalau mau ganti senjata atau minta ditempa, tinggal bilang."),
                (Pacca, "Hmm, perisai baru boleh juga."),
                (Daung, "Lihat dulu harganya, Pacca. Kita masih perlu bekal."),
                (Saka, "Terima kasih, Om!")
            }),
            (GmCapitalDeliveryDialogue.AcoGreeting, Aco, new[]
            {
                (Aco, "Sudah cek perbekalan kalian, Nak? Kalau sudah siap, hati-hati di jalan ke Sorelia, ya."),
                (Saka, "Iya, Mangge. Kami akan berhati-hati.")
            })
        };

        // The pairs use the two placed NPCs. For Random NPC 2, the child near the mother starts the talk.
        internal static readonly (string title, string partner, string[] hosts, (string actor, string text)[] lines)[] Residents =
        {
            (Ambient + "Ibu dan Anak", Mother, new[] { Mother }, new[]
            {
                (Child, "Ma, Kepala ku pusing....."),
                (Mother, "Iya nak... yuk kita ke toko Jamu")
            }),
            (Ambient + "Main Petak Umpet", Child, new[] { Child }, new[]
            {
                (Child, "Ka Saka, Main lagi yuk"),
                (Saka, "Nanti ya sepulang aku dari Gua Lembayung"),
                (Child, "Oke deh ka, kita main petak umpet lagi nanti")
            }),
            (Ambient + "Ikan dari Sorelia", Father, new[] { Father, Fisher }, new[]
            {
                (Father, "Kayaknya makan ikan goreng enak deh...."),
                (Fisher, "Ke Desa Sorelia yuk, ikan dari danau mereka enak-enak")
            }),
            (Ambient + "Peringatan Poppo", Guard, new[] { "Penjaga Kerajaan 2" }, new[]
            {
                (Guard, "Kalian mau ke Desa Sorelia?"),
                (Guard, "Berhati-hatilah, lagi banyak poppo berkeliaran")
            }),
            (Ambient + "Salam dari Sorelia", GateGuard, new[] { GateGuard }, new[]
            {
                (GateGuard, "Ku dengan dari Daeng Aco, kalian akan pergi ke Gua Lembayung ya?"),
                (Pacca, "Betul, kita akan menyelesaikan sumber masalah disana"),
                (GateGuard, "Sebagai warga desa Sorelia, saya ucapkan terima kasih dan selamat berjuang disana")
            }),
            (Ambient + Jamu, Jamu, new[] { Jamu }, new[]
            {
                (Daung, "Siang bu, kita mau beli Jamu bu"),
                (Jamu, "Waduh.. Maaf nak, stok jamu ibu habis, langsung ke toko saja nak, ibu tadi juga tinggalin beberapa botol disana"),
                (Daung, "Yah.. baiklah ibu, terima kasih")
            }),
            (YusufTitle, Yusuf, new[] { "Penjaga Kerajaan 1" }, new[]
            {
                (Yusuf, "Maaf Tuan Putri, Sang Paduka Raja sedang ada Tamu special"),
                (Yusuf, "Kondisi kastil kerajaan harus steril dari orang"),
                (Daung, "ahh baiklah pak Yusuf, aku akan kembali nanti"),
                (Yusuf, "Hati-hati diperjalanan anda tuan putri"),
                (Yusuf, "{Uhhh senangnya, tuan putri mengingat namaku)")
            })
        };

        internal static readonly (GmMainQuestStep step, string suffix, string partner,
            (string actor, string text)[] previous, (string actor, string text)[] lines)[] Story =
        {
            (GmMainQuestStep.PersiapanSebelumBertarung, "Persiapan Sebelum Bertarung", Aco, new[]
            {
                (FormerPua, "Jadi kalian akan pergi ke gua lembayung selanjutnya kan?"),
                (Saka, "Betul mangge. kata Acci artifact selanjutnya ada disana"),
                (FormerPua, "Nih sedikit uang sangu, pastikan stok barang kalian cukup hingga ke gua lembayung"),
                (Saka, "Yuk, kita mampir ke toko-toko dulu")
            }, Array.ConvertAll(FormerOpening, line =>
                (line.actor == FormerPua ? Aco : line.actor, line.text.Replace(FormerPua, Aco)))),
            (GmMainQuestStep.KunjungiMerchant, "Kunjungi Merchant", Merchant, new[]
            {
                (Merchant, "Selamat siang anak anak, ada yang bisa saya bantu?"),
                (Saka, "Siang bu, kita perlu membeli beberapa jamu dan makanan untuk perjalanan kami"),
                (Merchant, "Ini dia nak, sudah ibu kemas untuk kalian"),
                (Merchant, "Kalian sepertinya akan menempuh perjalanan jauh ya? bawakan ini ke bapak penempa")
            }, new[]
            {
                (Merchant, "Selamat siang anak anak, ada yang bisa saya bantu?"),
                (Daung, "Siang bu, kita perlu membeli beberapa jamu dan makanan untuk perjalanan kami"),
                (Merchant, "Baiklah akan ibu persiapkan sebentar ya"),
                (Merchant, "Ini dia nak, sudah ibu kemas untuk kalian"),
                (Saka, "Wah terima kasih banyak bu"),
                (Merchant, "Kalian sepertinya akan menempuh perjalanan jauh ya? bawakan ini ke bapak penempa"),
                (Merchant, "Beliau akan membuatkan aksesoris yang bisa memperkuat kalian"),
                (Pacca, "Aksesoris ya, semangatku jadi membara! Ayo segera kita kesana")
            }),
            (GmMainQuestStep.KunjungiPenempaBesi, "Kunjungi Penempa Besi", Smith, new[]
            {
                (Pacca, "Pak tua, kami ingin membuat aksesoris dari bahan ini"),
                (Daung, "Heh Pacca, yang sopan dikit kenapa sih"),
                (Smith, "Ini dia anak-anak, Anting untuk si gadis, kalung untuk si merah, dan cincin untuk nak Saka"),
                (Smith, "Dengan senang hati anak-anak, semangat dengan misi kalian")
            }, new[]
            {
                (Pacca, "Pak tua, kami ingin membuat aksesoris dari bahan ini"),
                (Daung, "Heh Pacca, yang sopan dikit kenapa sih"),
                (Smith, "Hahaha, semangat sekali kalian, ini bahan pasti dari ibu penjaga toko ya?"),
                (Smith, "Baiklah berikan om sedikit waktu untuk membuatnya ya"),
                (Saka, "Aku tidak sabar untuk aksesoris barunya"),
                (Pacca, "Sama nih, kita akan menjadi lebih kuat dengan aksesoris tersebut"),
                (Smith, "Ini dia anak-anak, Anting untuk si gadis, kalung untuk si merah, dan cincin untuk nak Saka"),
                (Daung, "Wah.... cantiknya"),
                (Saka, "Aku merasakan kekuatan yang hebat dari aksesoris ini"),
                (Smith, "Tentu saja, batu akik ini memiliki properti sihir yang memperkuat pengguna jika digunakan dengan tepat"),
                (Pacca, "Dengan ini, para monster tidak akan bisa berkutik melawan kita"),
                (Daung, "Iya-iya.. tapi tetap berhati-hati saat melawan mereka"),
                (Saka, "Terima kasih banyak om"),
                (Smith, "Dengan senang hati anak-anak, semangat dengan misi kalian")
            })
        };
    }
}
