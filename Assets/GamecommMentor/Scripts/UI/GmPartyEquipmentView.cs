using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;
using static GamecommMentor.DaengSaka.GmQuestJournalView;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmPartyEquipmentView
    {
        private readonly Font font;
        private readonly RectTransform tabs, list, detail;
        private readonly Text listTitle, empty, name, status, attack, skill, vitals, equipLabel;
        private readonly Image icon;
        private readonly Button equip;
        private readonly List<Button> heroButtons = new List<Button>();
        private readonly List<(GmOwnedWeapon weapon, Image background, Text state)> rows =
            new List<(GmOwnedWeapon, Image, Text)>();
        private IReadOnlyList<GmPartyVitals> roster;
        private GmPartyVitals member;
        private GmOwnedWeapon selected;
        private int lastRevision = -1;
        internal RectTransform Panel { get; }

        internal GmPartyEquipmentView(RectTransform parent, Font font)
        {
            this.font = font;
            Panel = Rect("Perlengkapan Party", parent, 0f, 0f, 1f, 1f);
            tabs = Rect("Pilihan Karakter", Panel, 0f, .875f, 1f, 1f);
            RectTransform stock = Rect("Koleksi Senjata", Panel, 0f, 0f, .435f, .845f);
            Fill(stock, Inset);
            listTitle = AddText(font, stock, "Pemilik", "", 23, .055f, .83f, .95f, .97f, bold: true);
            list = CreateScrollContent(Rect("Daftar Senjata", stock, .035f, .055f, .965f, .81f), out _);

            RectTransform slot = Rect("Slot Senjata", Panel, .455f, 0f, 1f, .845f);
            Fill(slot, Inset);
            empty = AddText(font, slot, "Slot Kosong", "Belum ada senjata untuk karakter ini.", 20,
                .06f, .34f, .94f, .66f, TextAnchor.MiddleCenter);
            detail = Rect("Detail Senjata", slot, .04f, .035f, .96f, .965f);
            icon = Fill(Rect("Ikon Senjata", detail, .02f, .70f, .23f, .99f), Color.white);
            icon.preserveAspect = true;
            name = AddText(font, detail, "Nama", "", 27, .26f, .80f, .98f, .985f, bold: true);
            status = AddText(font, detail, "Status Senjata", "", 17, .26f, .685f, .98f, .81f);
            status.color = Muted;
            Fill(Rect("Garis", detail, .02f, .655f, .98f, .66f), Rule);
            attack = AddText(font, detail, "Attack Dasar", "", 23, .025f, .49f, .98f, .64f, bold: true);
            skill = AddText(font, detail, "Contoh Skill", "", 18, .025f, .345f, .98f, .485f);
            vitals = AddText(font, detail, "HP dan MP", "", 17, .025f, .225f, .98f, .34f);
            equip = AddButton(font, detail, "Pasang senjata", .025f, .015f, .98f, .185f, Equip, primary: true);
            equipLabel = equip.GetComponentInChildren<Text>(true);
        }

        internal void RefreshIfChanged()
        {
            GmPartyRun run = GmPartyRun.Current;
            if (roster != run.Members || lastRevision != run.Equipment.Revision) Render();
        }

        internal void Render()
        {
            GmPartyRun run = GmPartyRun.Current;
            if (roster != run.Members) BuildTabs(run.Members);
            lastRevision = run.Equipment.Revision;
            for (int i = 0; i < heroButtons.Count; i++) SetSelected(heroButtons[i], roster[i] == member);

            ClearChildren(list);
            rows.Clear();
            bool found = false;
            foreach (var weapon in run.Equipment.Options(member))
            {
                var row = Rect(weapon.Definition.Name, list, 0f, 0f, 1f, 1f);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 66f;
                var background = Fill(row, Paper);
                var thumbnail = Fill(Rect("Ikon", row, .035f, .12f, .18f, .88f), Color.white);
                SetIcon(thumbnail, weapon);
                AddText(font, row, "Nama", weapon.Definition.Name, 19, .215f, .45f, .98f, .96f, bold: true);
                var label = AddText(font, row, "Status", "", 15, .215f, .02f, .98f, .48f);
                var owner = member;
                Clickable(background, () =>
                {
                    if (!Panel.gameObject.activeInHierarchy || owner != member ||
                        !row.gameObject.activeInHierarchy) return;
                    selected = weapon;
                    RenderSelection();
                });
                rows.Add((weapon, background, label));
                if (weapon == selected) found = true;
            }
            if (!found) selected = run.Equipment.Equipped(member) ?? (rows.Count > 0 ? rows[0].weapon : null);
            RenderSelection();
        }

        private void BuildTabs(IReadOnlyList<GmPartyVitals> members)
        {
            roster = members;
            ClearChildren(tabs);
            heroButtons.Clear();
            bool found = false;
            for (int i = 0; i < roster.Count; i++)
            {
                GmPartyVitals hero = roster[i];
                var button = AddButton(font, tabs, hero.Name, (i + .02f) / roster.Count, .02f,
                    (i + .98f) / roster.Count, .98f, () =>
                    {
                        if (!Panel.gameObject.activeInHierarchy || hero == member) return;
                        member = hero;
                        selected = null;
                        Render();
                    });
                heroButtons.Add(button);
                if (hero == member) found = true;
            }
            if (!found) member = roster.Count == 0 ? null : roster[0];
        }

        private void RenderSelection()
        {
            GmOwnedWeapon worn = GmPartyRun.Current.Equipment.Equipped(member);
            foreach (var row in rows)
            {
                row.background.color = row.weapon == selected ? LeafLight : Paper;
                string state = row.weapon == worn ? "Terpasang" : "Tersimpan";
                row.state.text = $"{state}  ·  +{row.weapon.Bonus} daya";
            }

            listTitle.text = member == null ? "Senjata" : "Senjata " + member.Name;
            empty.gameObject.SetActive(selected == null);
            detail.gameObject.SetActive(selected != null);
            if (selected == null) return;
            SetIcon(icon, selected);
            name.text = selected.Definition.Name;
            status.text = $"Tingkat +{selected.Rank}  ·  +{selected.Bonus} daya";
            attack.text = "Attack dasar   " + Compare(GmBattleMove.BasicAttack, worn);
            var skills = GmBattleMove.ForCharacter(member.Character);
            skill.text = "";
            if (skills.Count > 0)
            {
                var move = skills[0];
                if (move.IsDamaging) skill.text = move.Name + "   " + Compare(move, worn) + " DMG dasar";
                else if (move.Effect == GmBattleEffect.Heal) skill.text = $"{move.Name}   hingga {move.Amount} HP";
            }
            vitals.text = $"HP  {member.Hp}/{member.MaxHp}     MP  {member.Mp}/{member.MaxMp}";
            equip.interactable = selected != worn;
            equip.targetGraphic.color = equip.interactable ? Leaf : Paper;
            equipLabel.color = equip.interactable ? Paper : Ink;
            equipLabel.text = equip.interactable ? "Pasang ke " + member.Name : "Sedang dipakai";
        }

        private string Compare(GmBattleMove move, GmOwnedWeapon worn)
        {
            int before = move.PowerWithWeapon(member.BaseAttack, worn?.Bonus ?? 0);
            int after = move.PowerWithWeapon(member.BaseAttack, selected.Bonus);
            return before == after ? after.ToString() : $"{before} → {after}";
        }

        private static void SetIcon(Image image, GmOwnedWeapon weapon)
        {
            image.preserveAspect = true;
            image.sprite = weapon.Definition.Icon;
            image.enabled = image.sprite != null;
        }

        private void Equip()
        {
            if (!Panel.gameObject.activeInHierarchy) return;
            if (GmPartyRun.Current.Equipment.TryEquip(member, selected?.InstanceId)) Render();
        }
    }
}
