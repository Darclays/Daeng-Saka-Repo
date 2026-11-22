using System;
using UnityEngine;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;
using static GamecommMentor.DaengSaka.GmQuestJournalView;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmFishingView : IDisposable
    {
        private static readonly Color Water = new Color32(35, 66, 75, 255);
        private static readonly Color Mint = new Color32(144, 216, 155, 255);
        private static readonly Color Gold = new Color32(255, 204, 108, 255);
        private readonly RectTransform root, line, floatIcon, ripple, reel, bar, fish, fill, result, exit;
        private readonly Image curtain;
        private readonly bool hasRipple;
        private readonly Text heading, hint, action, progress, contact, bite, catchTitle, catchDetail, quest, closeLabel;
        private GmFishingPhase lastPhase;
        private int lastPercent;
        private bool lastContact;
        private bool gamepadHints;

        internal GmFishingView(RectTransform canvas)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root = Rect("Memancing di Sorelia", canvas, 0, 0, 1, 1);
            Fill(root, new Color(0, 0, 0, 0)).raycastTarget = true;

            ripple = Rect("Riak pelampung", root, .5f, .5f, .5f, .5f);
            RawImage waterRing = ripple.gameObject.AddComponent<RawImage>();
            waterRing.texture = Resources.Load<Texture2D>("DaengSaka/Fishing/FloatRipple");
            hasRipple = waterRing.texture != null;
            waterRing.raycastTarget = false;
            waterRing.color = new Color(.88f, 1f, .91f, .78f);
            line = Rect("Senar", root, .5f, .5f, .5f, .5f);
            line.pivot = new Vector2(0, .5f);
            Fill(line, new Color(1f, .96f, .79f, .88f));
            floatIcon = Rect("Pelampung", root, .5f, .5f, .5f, .5f);
            floatIcon.sizeDelta = new Vector2(11f, 16f);
            Fill(floatIcon, Ink);
            Fill(Rect("Putih", floatIcon, .17f,.11f,.83f,.90f), Paper);
            Fill(Rect("Merah", floatIcon, .17f,.48f,.83f,.90f), new Color32(215, 88, 51, 255));
            bite = AddText(font, floatIcon, "Gigitan", "!", 27, -1f,1f,2f,3.2f, TextAnchor.MiddleCenter, true);
            bite.color = Gold;
            bite.gameObject.AddComponent<Outline>().effectColor = Color.black;

            quest = AddText(font, root, "Objektif", "Tangkap 1 ikan   0 / 1", 19, .029f,.90f,.35f,.97f, bold:true);
            quest.color = Paper;
            quest.gameObject.AddComponent<Outline>().effectColor = Color.black;

            reel = Rect("Tarik pancing", root, .778f,.214f,.964f,.865f);
            Fill(reel, Paper);
            AddText(font, reel, "Judul", "Tarik pancing", 23, .07f,.86f,.94f,.98f, TextAnchor.MiddleCenter,true);
            AddText(font, reel, "Ikan", "Ikan", 15, .13f,.79f,.55f,.86f,TextAnchor.MiddleCenter);
            AddText(font, reel, "Progres", "Progres", 15, .55f,.79f,.91f,.86f,TextAnchor.MiddleCenter);
            RectTransform lane = Rect("Jalur ikan", reel, .18f,.19f,.51f,.78f);
            Fill(lane, Water);
            bar = Rect("Zona tangkap", lane, .04f,0f,.96f,GmFishingRound.BarHeight);
            Fill(bar, Mint);
            Fill(Rect("Tepi atas", bar, 0f,.96f,1f,1f), Paper);
            Fill(Rect("Tepi bawah", bar, 0f,0f,1f,.04f), Paper);
            fish = Rect("Ikan bergerak", lane, .50f,.2f,.50f,.2f);
            fish.sizeDelta = new Vector2(29f, 15f);
            Fill(Rect("Sirip",fish,0,.13f,.24f,.86f), Gold);
            Fill(Rect("Badan",fish,.22f,.10f,.89f,.91f), Ink);
            Fill(Rect("Sisik",fish,.26f,.22f,.96f,.78f), Gold);
            Fill(Rect("Mata",fish,.73f,.51f,.82f,.68f), Ink);
            RectTransform meter = Rect("Meter tangkapan", reel, .66f,.19f,.79f,.78f);
            Fill(meter, Water);
            fill = Rect("Isi", meter, .1f,0f,.9f,.3f);
            Fill(fill, Gold);
            progress = AddText(font,reel,"Angka","",16,.54f,.10f,.90f,.19f,TextAnchor.MiddleCenter,true);
            contact = AddText(font,reel,"Kontak","",15,.04f,.015f,.96f,.105f,TextAnchor.MiddleCenter);

            result = Rect("Hasil",root,.55f,.67f,.969f,.85f);
            Fill(result, Paper);
            catchTitle = AddText(font,result,"Tangkapan","",24,.07f,.48f,.95f,.92f,bold:true);
            catchDetail = AddText(font,result,"Status","",16,.07f,.08f,.95f,.48f);
            RectTransform controls = Rect("Kontrol",root,.026f,.028f,.974f,.16f);
            Fill(controls, Paper);
            heading = AddText(font,controls,"Judul","",23,.022f,.46f,.54f,.94f,bold:true);
            hint = AddText(font,controls,"Petunjuk","",16,.022f,.06f,.55f,.48f);
            RectTransform key = Rect("Aksi",controls,.557f,.16f,.806f,.84f);
            Fill(key, Leaf);
            action = AddText(font,key,"Aksi","",18,.035f,0,.965f,1,TextAnchor.MiddleCenter,true);
            action.color = Paper;
            exit = Rect("Keluar",controls,.825f,.16f,.98f,.84f);
            Fill(exit, Inset);
            closeLabel = AddText(font,exit,"Tutup","Keluar  [Esc]",17,.015f,0,.985f,1,TextAnchor.MiddleCenter,true);
            // Independent sibling so the scene can fade before and after the HUD is shown.
            curtain = Fill(Rect("Transisi memancing", canvas, 0f, 0f, 1f, 1f), Color.black);
            curtain.raycastTarget = true;
            curtain.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
        }

        internal void Show()
        {
            lastPhase = (GmFishingPhase)(-1);
            lastPercent = -1;
            ripple.gameObject.SetActive(false);
            line.gameObject.SetActive(false);
            floatIcon.gameObject.SetActive(false);
            root.SetAsLastSibling();
            if (curtain.gameObject.activeSelf) curtain.transform.SetAsLastSibling();
            root.gameObject.SetActive(true);
        }

        internal bool HitExit(Vector2 screenPoint) =>
            root.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(exit, screenPoint, null);

        internal void Render(GmFishingRound round, bool questOpen, bool added, bool completed)
        {
            bool changed = round.Phase != lastPhase;
            bool pad = GmGamepadInput.UsingGamepad;
            if (changed || gamepadHints != pad)
            {
                gamepadHints = pad;
                string key = pad ? "[A]" : "[Spasi / Klik]";
                closeLabel.text = $"Keluar  [{GmGamepadInput.BackLabel}]";
                lastPhase = round.Phase;
                reel.gameObject.SetActive(lastPhase == GmFishingPhase.Reeling);
                result.gameObject.SetActive(lastPhase == GmFishingPhase.Caught);
                quest.gameObject.SetActive(questOpen);
                bite.gameObject.SetActive(lastPhase == GmFishingPhase.Bite);
                switch (lastPhase)
                {
                    case GmFishingPhase.Ready:
                        heading.text = "Spot memancing"; hint.text = round.EarlyPull ? "Kail ditarik kembali." : "Alat pancing dan umpan siap.";
                        action.text = "Lempar  " + key; break;
                    case GmFishingPhase.Waiting:
                        heading.text = "Menunggu gigitan"; hint.text = "Tarik saat tanda ! muncul.";
                        action.text = "Tarik kembali  " + key; break;
                    case GmFishingPhase.Bite:
                        heading.text = "Ikan menggigit!"; hint.text = "Tarik sebelum ikan lepas.";
                        action.text = "Tarik  " + key; break;
                    case GmFishingPhase.Reeling:
                        heading.text = "Jaga ikan di dalam bar hijau"; hint.text = "Tahan untuk menaikkan bar. Lepas untuk menurunkannya.";
                        action.text = "Tahan  " + key; break;
                    case GmFishingPhase.Caught:
                        heading.text = added ? "Tangkapan berhasil" : "Tumpukan ikan penuh";
                        hint.text = "Lempar lagi atau kembali ke desa.";
                        action.text = "Mancing lagi  " + key; catchTitle.text = round.Species.Item.Name + "  ·  " + round.Species.Rarity;
                        catchDetail.text = !added ? "Tas penuh. Ikan dilepas." : completed ?
                            "Masuk Tas  ·  Memancing di Sorelia selesai" : "Masuk Tas  ·  +1 ikan";
                        break;
                    case GmFishingPhase.Escaped:
                        heading.text = "Ikan lepas"; hint.text = round.MissedBite ? "Gigitannya terlewat." : "Ikan terlalu lama di luar bar.";
                        action.text = "Coba lagi  " + key; break;
                }
            }
            if (round.Phase != GmFishingPhase.Reeling) return;
            SetHeight(bar, round.Bar, round.Bar + GmFishingRound.BarHeight);
            SetHeight(fish, round.Fish, round.Fish);
            SetHeight(fill, 0f, round.Progress);
            int percent = Mathf.RoundToInt(round.Progress * 100);
            if (changed || percent != lastPercent) { progress.text = percent + "%"; lastPercent = percent; }
            if (changed || lastContact != round.InBar)
            {
                lastContact = round.InBar;
                contact.text = lastContact ? "Ikan di dalam bar" : "Ikan di luar bar";
            }
        }

        private static void SetHeight(RectTransform rect, float min, float max)
        {
            rect.anchorMin = new Vector2(rect.anchorMin.x, min);
            rect.anchorMax = new Vector2(rect.anchorMax.x, max);
        }

        internal void DrawLine(Camera camera, Vector3 rodTip, Vector3 surface, GmFishingRound round)
        {
            bool casting = round.Phase == GmFishingPhase.Waiting;
            bool visible = (casting && round.Elapsed >= GmFishingPose.ReleaseTime) ||
                round.Phase == GmFishingPhase.Bite || round.Phase == GmFishingPhase.Reeling;
            float flight = casting ? Mathf.Clamp01((round.Elapsed - GmFishingPose.ReleaseTime) / .28f) : 1f;
            Vector3 bobber = surface + Vector3.up * (.06f + .035f *
                Mathf.Sin(round.Elapsed * (round.Phase == GmFishingPhase.Bite ? 14f : 4f)));
            if (flight < 1f)
                bobber = Vector3.Lerp(rodTip, bobber, flight) + Vector3.up * (.34f * 4f * flight * (1f - flight));
            Vector2 start = default, end = default;
            visible = visible && camera != null && Project(camera, rodTip, out start) && Project(camera, bobber, out end);
            line.gameObject.SetActive(visible);
            floatIcon.gameObject.SetActive(visible);
            bool onWater = visible && flight >= 1f && hasRipple;
            ripple.gameObject.SetActive(onWater);
            if (!visible) return;
            floatIcon.anchoredPosition = end;
            // Meet the top of the bobber once it is on the water. At release both ends coincide.
            end += Vector2.up * (floatIcon.sizeDelta.y * .35f * flight);
            Vector2 delta = end - start;
            line.anchoredPosition = start;
            line.sizeDelta = new Vector2(delta.magnitude, 1.8f);
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            if (!onWater) return;

            float radius = .24f + .024f * Mathf.Sin(round.Elapsed * 4f);
            Vector3 flatForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            Vector3 center = surface + Vector3.up * .015f;
            if (Project(camera, center, out Vector2 c) && Project(camera, center + camera.transform.right * radius, out Vector2 r) &&
                Project(camera, center + flatForward * radius, out Vector2 f))
            {
                ripple.anchoredPosition = c;
                ripple.sizeDelta = new Vector2(2f * Mathf.Abs(r.x - c.x), 2f * Mathf.Abs(f.y - c.y));
            }
            else ripple.gameObject.SetActive(false);
        }

        private bool Project(Camera camera, Vector3 world, out Vector2 local)
        {
            Vector3 screen = camera.WorldToScreenPoint(world);
            local = default;
            return screen.z > 0f && camera.pixelRect.Contains((Vector2)screen) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out local);
        }

        internal void SetFade(float alpha)
        {
            if (curtain == null) return;
            if (alpha <= 0f) { curtain.gameObject.SetActive(false); return; }
            if (!curtain.gameObject.activeSelf)
            {
                curtain.transform.SetAsLastSibling();
                curtain.gameObject.SetActive(true);
            }
            curtain.color = new Color(0f, 0f, 0f, alpha);
        }

        internal void Hide() { if (root != null) root.gameObject.SetActive(false); }

        public void Dispose()
        {
            if (curtain != null) UnityEngine.Object.Destroy(curtain.gameObject);
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
        }
    }
}
