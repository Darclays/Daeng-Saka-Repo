using UnityEngine;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;
using static GamecommMentor.DaengSaka.GmQuestJournalView;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmPartyBagView
    {
        private static readonly GmItemDefinition Dish = GmItemDefinition.GrilledWader;
        private static bool showEquipment;
        private readonly Font font;
        private readonly Button stockTab, equipmentTab;
        private readonly RectTransform stockPage;
        private readonly GmPartyEquipmentView equipmentView;
        private readonly RectTransform items;
        private readonly RectTransform members;
        private readonly Button recover;
        private readonly Button cook;
        private readonly Text cookLabel;
        private readonly Text ingredientStock;
        private readonly Text cookingStatus;
        private readonly Text wallet;
        private int lastInventoryRevision = -1;
        private int lastCoins = -1;
        public RectTransform Panel { get; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetTab() => showEquipment = false;

        internal GmPartyBagView(RectTransform window, Font font)
        {
            this.font = font;
            Panel = Rect("Tas Party", window, .026f, .045f, .974f, .833f);
            stockTab = AddButton(font, Panel, "Isi tas", 0f, .905f, .205f, 1f, () => SelectTab(false));
            equipmentTab = AddButton(font, Panel, "Perlengkapan", .22f, .905f, .49f, 1f, () => SelectTab(true));
            Caption(stockTab).supportRichText = Caption(equipmentTab).supportRichText = true;
            wallet = AddText(font, Panel, "Kas Party", "", 22, .54f, .905f, 1f, 1f,
                TextAnchor.MiddleRight, bold: true);
            RectTransform content = Rect("Halaman Tas", Panel, 0f, 0f, 1f, .885f);
            stockPage = Rect("Bekal dan Status", content, 0f, 0f, 1f, 1f);
            equipmentView = new GmPartyEquipmentView(content, font);
            RectTransform stock = Rect("Isi Tas", stockPage, 0f, 0f, .46f, 1f);
            Fill(stock, Inset);
            AddText(font, stock, "Title", "Isi tas", 26, .055f, .835f, .95f, .98f, bold: true);
            items = CreateScrollContent(Rect("Item List", stock, .035f, .35f, .965f, .81f), out _);

            RectTransform recipe = Rect("Resep", stock, .035f, .03f, .965f, .32f);
            Fill(recipe, Paper);
            AddText(font, recipe, "Nama masakan", $"Resep {Dish.Name}", 21, .045f, .63f, .56f, .96f, bold: true);
            AddText(font, recipe, "Efek", $"+{Dish.HpRecovery} HP saat battle", 16,
                .56f, .63f, .955f, .96f, TextAnchor.MiddleRight);
            ingredientStock = AddText(font, recipe, "Bahan", "", 16, .045f, .34f, .64f, .63f);
            cookingStatus = AddText(font, recipe, "Hasil masak", "", 15, .045f, .065f, .64f, .34f);
            cook = AddButton(font, recipe, "Masak 1", .67f, .10f, .955f, .53f, Cook, primary: true);
            cookLabel = cook.GetComponentInChildren<Text>(true);

            RectTransform party = Rect("Status Party", stockPage, .48f, 0f, 1f, 1f);
            Fill(party, Inset);
            AddText(font, party, "Title", "Status party", 26, .055f, .835f, .95f, .98f, bold: true);
            members = CreateScrollContent(Rect("Party List", party, .035f, .18f, .965f, .81f), out _);
            recover = AddButton(font, party, $"Pulihkan party ke {GmPartyRun.WipeRecoveryPercent}% HP",
                .045f, .03f, .955f, .15f, Recover);
        }

        private void SelectTab(bool equipment)
        {
            if (!Panel.gameObject.activeInHierarchy) return;
            if (showEquipment != equipment) MarkViewed();
            showEquipment = equipment;
            Render();
        }

        internal void Open(GmBagSection? section)
        {
            if (section.HasValue) showEquipment = section.Value == GmBagSection.Equipment;
            Render();
            if (section.HasValue && GmGamepadInput.UsingGamepad)
                GmMenuButton.Focus(showEquipment ? equipmentTab : stockTab);
        }

        internal void MarkViewed() =>
            GmPartyRun.Current.AcknowledgeBagSection(showEquipment ? GmBagSection.Equipment : GmBagSection.Stock);

        internal void Render()
        {
            RenderWallet();
            RenderBadges();
            SetSelected(stockTab, !showEquipment);
            SetSelected(equipmentTab, showEquipment);
            stockPage.gameObject.SetActive(!showEquipment);
            equipmentView.Panel.gameObject.SetActive(showEquipment);
            if (showEquipment)
            {
                equipmentView.Render();
                return;
            }
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
            RenderWallet();
            RenderBadges();
            if (showEquipment) equipmentView.RefreshIfChanged();
            else if (lastInventoryRevision != GmPartyRun.Current.Inventory.Revision) RenderItems();
        }

        private void RenderBadges()
        {
            var run = GmPartyRun.Current;
            Caption(stockTab).text = WithAlert("Isi tas", run.HasUnreadItems(GmBagSection.Stock));
            Caption(equipmentTab).text = WithAlert("Perlengkapan", run.HasUnreadItems(GmBagSection.Equipment));
        }

        private void RenderWallet()
        {
            int coins = GmPartyRun.Current.Wallet.Coins;
            if (coins == lastCoins) return;
            lastCoins = coins;
            wallet.text = $"Kas party   {coins:N0} koin";
        }

        private void RenderItems(bool justCooked = false)
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

            int rawFish = bag.Count(Dish.Ingredient);
            ingredientStock.text = $"Perlu 1 {Dish.Ingredient.Name} · Punya {rawFish}";
            cook.interactable = bag.CanCook(Dish);
            cook.targetGraphic.color = cook.interactable ? Leaf : Inset;
            cookLabel.color = cook.interactable ? Paper : Ink;
            if (justCooked) cookingStatus.text = "+1 porsi masuk Tas.";
            else if (bag.Count(Dish) == int.MaxValue) cookingStatus.text = "Tumpukan makanan penuh.";
            else if (rawFish == 0) cookingStatus.text = "Tangkap Wader di Sorelia.";
            else cookingStatus.text = "";
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
            if (stockPage.gameObject.activeInHierarchy && GmPartyRun.Current.TryRecoverWipedParty()) Render();
        }

        private void Cook()
        {
            if (!stockPage.gameObject.activeInHierarchy) return;
            bool cooked = GmPartyRun.Current.Inventory.TryCook(Dish);
            RenderItems(cooked);
        }
    }
}
