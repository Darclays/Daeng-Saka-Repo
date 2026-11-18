using UnityEngine;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;
using static GamecommMentor.DaengSaka.GmQuestJournalView;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmPartyBagView
    {
        private readonly Font font;
        private readonly RectTransform items;
        private readonly RectTransform members;
        private readonly Button recover;
        private int lastInventoryRevision = -1;
        public RectTransform Panel { get; }

        internal GmPartyBagView(RectTransform window, Font font)
        {
            this.font = font;
            Panel = Rect("Tas Party", window, .026f, .045f, .974f, .833f);
            RectTransform stock = Rect("Isi Tas", Panel, 0f, 0f, .46f, 1f);
            Fill(stock, Inset);
            AddText(font, stock, "Title", "Isi tas", 26, .055f, .835f, .95f, .98f, bold: true);
            items = CreateScrollContent(Rect("Item List", stock, .035f, .03f, .965f, .81f), out _);

            RectTransform party = Rect("Status Party", Panel, .48f, 0f, 1f, 1f);
            Fill(party, Inset);
            AddText(font, party, "Title", "Status party", 26, .055f, .835f, .95f, .98f, bold: true);
            members = CreateScrollContent(Rect("Party List", party, .035f, .18f, .965f, .81f), out _);
            recover = AddButton(font, party, $"Pulihkan party ke {GmPartyRun.WipeRecoveryPercent}% HP",
                .045f, .03f, .955f, .15f, Recover);
        }

        internal void Render()
        {
            RenderItems();
            ClearChildren(members);
            foreach (GmPartyVitals member in GmPartyRun.Current.Members)
            {
                RectTransform row = Row(members, member.Name, 77f);
                string name = member.Hp == 0 ? member.Name + "  ·  KO" : member.Name;
                AddText(font, row, "Name", name, 20, .05f, .49f, .95f, .98f, bold: true);
                AddText(font, row, "HP", $"HP   {member.Hp} / {member.MaxHp}", 17, .05f, .03f, .50f, .5f);
                AddText(font, row, "MP", $"MP   {member.Mp} / {member.MaxMp}", 17, .52f, .03f, .95f, .5f);
            }
            recover.gameObject.SetActive(GmPartyRun.Current.PartyWiped);
        }

        internal void RefreshStock()
        {
            if (lastInventoryRevision != GmPartyRun.Current.Inventory.Revision) RenderItems();
        }

        private void RenderItems()
        {
            GmPartyInventory bag = GmPartyRun.Current.Inventory;
            lastInventoryRevision = bag.Revision;
            ClearChildren(items);
            bool any = false;
            foreach (GmItemDefinition item in GmItemDefinition.All)
            {
                int count = bag.Count(item);
                if (count == 0) continue;
                any = true;
                RectTransform row = Row(items, item.Name, 54f);
                AddText(font, row, "Name", item.Name, 19, .05f, 0f, .69f, 1f, bold: true);
                AddText(font, row, "Quantity", $"×{count}", 19, .69f, 0f, .95f, 1f, TextAnchor.MiddleRight);
            }
            if (!any)
                AddText(font, Row(items, "Empty", 54f), "Empty Label", "Tas kosong", 18, .05f, 0f, .95f, 1f);
        }

        private static RectTransform Row(RectTransform parent, string name, float height)
        {
            RectTransform row = Rect(name, parent, 0f, 0f, 1f, 1f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            Fill(row, Paper);
            return row;
        }

        private void Recover()
        {
            if (GmPartyRun.Current.TryRecoverWipedParty()) Render();
        }
    }
}
