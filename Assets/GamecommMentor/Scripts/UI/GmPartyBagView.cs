using UnityEngine;
using UnityEngine.UI;
using static GamecommMentor.DaengSaka.GmBattleView;
using static GamecommMentor.DaengSaka.GmQuestJournalView;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmPartyBagView
    {
        private static bool showEquipment;
        private readonly Font font;
        private readonly Button stockTab, equipmentTab;
        private readonly RectTransform stockPage;
        private readonly GmPartyEquipmentView equipmentView;
        private readonly RectTransform items;
        private readonly RectTransform members;
        private readonly Button recover;
        private readonly Recipe[] recipes = new Recipe[GmItemDefinition.FishRecipes.Count];
        private readonly Text cookingStatus;
        private readonly Text wallet;
        private int lastInventoryRevision = -1;
        private int lastCoins = -1;
        public RectTransform Panel { get; }

        private sealed class Recipe
        {
            internal GmItemDefinition Dish;
            internal Text Stock;
            internal Button Cook;
        }

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
            items = CreateScrollContent(Rect("Item List", stock, .035f, .464f, .965f, .81f), out _);

            RectTransform recipePanel = Rect("Resep", stock, .035f, .027f, .965f, .438f);
            Fill(recipePanel, Paper);
            AddText(font, recipePanel, "Judul", "Resep ikan", 19, .04f, .823f, .43f, .993f, bold: true);
            cookingStatus = AddText(font, recipePanel, "Hasil masak", "", 15, .43f, .823f, .957f, .993f,
                TextAnchor.MiddleRight);
            cookingStatus.color = Leaf;
            for (int i = 0; i < recipes.Length; i++)
            {
                GmItemDefinition dish = GmItemDefinition.FishRecipes[i];
                float top = .81f - i * .26f;
                RectTransform row = Rect(dish.Name, recipePanel, .04f, top - .238f, .957f, top);
                Fill(row, Inset);
                AddText(font, row, "Nama masakan", dish.Name, 17, .025f, .49f, .48f, 1f, bold: true);
                Text stockCount = AddText(font, row, "Bahan", "", 14, .49f, .49f, .73f, 1f, TextAnchor.MiddleRight);
                AddText(font, row, "Efek", dish.RecoveryLabel, 14, .025f, 0f, .73f, .51f);
                Button cook = AddButton(font, row, "Masak", .746f, .069f, .98f, .925f, () => Cook(dish), primary: true);
                recipes[i] = new Recipe { Dish = dish, Stock = stockCount, Cook = cook };
            }

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

        private void RenderItems(GmItemDefinition cookedDish = null)
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

            foreach (Recipe recipe in recipes)
            {
                GmItemDefinition dish = recipe.Dish;
                int rawFish = bag.Count(dish.Ingredient);
                recipe.Stock.text = bag.Count(dish) == int.MaxValue ? "Penuh" : $"×{rawFish}";
                recipe.Cook.interactable = bag.CanCook(dish);
                recipe.Cook.targetGraphic.color = recipe.Cook.interactable ? Leaf : Paper;
                Caption(recipe.Cook).color = recipe.Cook.interactable ? Paper : Ink;
            }
            cookingStatus.text = cookedDish != null ? $"+1 {cookedDish.Name} masuk Tas" : "1 ikan per porsi";
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

        private void Cook(GmItemDefinition dish)
        {
            if (!stockPage.gameObject.activeInHierarchy) return;
            bool cooked = GmPartyRun.Current.Inventory.TryCook(dish);
            if (cooked) GmGameplayAudio.Ui(GmUiSound.Cook);
            RenderItems(cooked ? dish : null);
        }
    }
}
