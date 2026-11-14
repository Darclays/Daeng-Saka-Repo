using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GamecommMentor.DaengSaka
{
    internal enum GmBattleMenu { Command, Attack, Item, Target }

    internal sealed class GmBattleView : IDisposable
    {
        private static readonly Color Ink = new Color(0.13f, 0.18f, 0.22f);
        private static readonly Color Muted = new Color(0.35f, 0.41f, 0.45f);
        private static readonly Color Shell = new Color(0.87f, 0.90f, 0.92f);
        private static readonly Color Panel = new Color(0.96f, 0.97f, 0.98f);
        private static readonly Color Border = new Color(0.73f, 0.78f, 0.81f);
        private static readonly Color Focus = new Color(0.78f, 0.90f, 0.85f);
        private static readonly Color Target = new Color(0.96f, 0.87f, 0.73f);
        private static readonly Color Disabled = new Color(0.89f, 0.91f, 0.93f);
        private static readonly Color HpColor = new Color(0.21f, 0.47f, 0.42f);
        private static readonly Color MpColor = new Color(0.33f, 0.51f, 0.64f);

        private sealed class PartyHud
        {
            public Button Button;
            public Image Background;
            public Text Name;
            public Text Hp;
            public Text Mp;
            public RectTransform HpFill;
            public RectTransform MpFill;
        }

        private readonly RectTransform screen;
        private readonly Font font;
        private readonly GmBattleStageView stage;
        private readonly PartyHud[] party;
        private readonly Image[] queueChips = new Image[4];
        private readonly Text[] queueLabels = new Text[4];
        private readonly Text turnNumber;
        private readonly Text turnLabel;
        private readonly Text actionLog;
        private readonly Text actionTitle;
        private readonly Text hint;
        private readonly Button attackButton;
        private readonly Button itemButton;
        private readonly Button restButton;
        private readonly Button backButton;
        private readonly Button firstMove;
        private readonly Button secondMove;
        private readonly Button confirmButton;
        private readonly Button leaveButton;
        private readonly GameObject resultOverlay;
        private readonly Text resultTitle;
        private readonly Button resultButton;

        public GmBattleView(Transform parent, GmBattleSession session, Action<int> select, Action attack,
            Action item, Action rest, Action<int> pickMove, Action back, Action confirm, Action leave)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform frame = Rect("Battle Screen", parent, 0f, 0f, 1f, 1f);
            screen = frame;
            Fill(frame, new Color(0.12f, 0.16f, 0.19f));
            RectTransform content = Rect("Battle 16x9", frame, 0f, 0f, 1f, 1f);
            AspectRatioFitter fitter = content.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;
            Fill(content, Shell);

            RectTransform header = Rect("Header", content, 0f, 0.865f, 1f, 1f);
            turnNumber = Label(font, header, "Turn Number", "", 16, 0.02f, 0.55f, 0.46f, 0.88f, TextAnchor.MiddleLeft);
            turnNumber.color = Muted;
            turnLabel = Label(font, header, "Actor", "", 32, 0.02f, 0.12f, 0.50f, 0.57f, TextAnchor.MiddleLeft);
            turnLabel.fontStyle = FontStyle.Bold;
            Label(font, header, "Enemy Order", "Urutan musuh", 15, 0.55f, 0.59f, 0.87f, 0.91f, TextAnchor.MiddleLeft).color = Muted;
            for (int i = 0; i < queueLabels.Length; i++)
            {
                float x = 0.55f + i * 0.077f;
                RectTransform chip = Rect("Enemy Slot", header, x, 0.17f, x + 0.073f, 0.58f);
                queueChips[i] = Fill(chip, Panel);
                queueLabels[i] = Label(font, chip, "Unit", "—", 16, 0.02f, 0f, 0.98f, 1f);
            }
            leaveButton = MakeButton(header, "Mundur", 0.894f, 0.22f, 0.98f, 0.66f, leave);

            RectTransform arena = Rect("Arena", content, 0f, 0.305f, 1f, 0.865f);
            stage = new GmBattleStageView(arena, session, font, select);
            actionLog = Label(font, content, "Action Log", "", 17, 0.02f, 0.245f, 0.98f, 0.302f, TextAnchor.MiddleLeft);
            actionLog.color = Muted;
            party = BuildPartyHud(content, session, select);

            RectTransform actions = Rect("Actions", content, 0.55f, 0.027f, 0.98f, 0.24f);
            Fill(actions, Border);
            RectTransform panel = Inset("Panel", actions, 1.4f);
            Fill(panel, Panel);
            actionTitle = Label(font, panel, "Action Title", "Aksi", 19, 0.026f, 0.66f, 0.72f, 0.98f, TextAnchor.MiddleLeft);
            actionTitle.fontStyle = FontStyle.Bold;
            attackButton = MakeButton(panel, "Attack", 0.026f, 0.23f, 0.322f, 0.61f, attack);
            itemButton = MakeButton(panel, "Item", 0.352f, 0.23f, 0.648f, 0.61f, item);
            restButton = MakeButton(panel, "Rest", 0.678f, 0.23f, 0.974f, 0.61f, rest);
            backButton = MakeButton(panel, "Kembali", 0.759f, 0.72f, 0.974f, 0.96f, back, 16);
            firstMove = MakeButton(panel, "", 0.026f, 0.37f, 0.974f, 0.665f, () => pickMove(0), 18, TextAnchor.MiddleLeft);
            secondMove = MakeButton(panel, "", 0.026f, 0.05f, 0.974f, 0.345f, () => pickMove(1), 18, TextAnchor.MiddleLeft);
            hint = Label(font, panel, "Target Prompt", "", 17, 0.026f, 0.39f, 0.974f, 0.68f, TextAnchor.MiddleLeft);
            hint.color = Muted;
            confirmButton = MakeButton(panel, "Pakai", 0.026f, 0.065f, 0.974f, 0.36f, confirm);
            confirmButton.targetGraphic.color = Focus;

            RectTransform overlay = Rect("Battle Result", content, 0f, 0f, 1f, 1f);
            Fill(overlay, new Color(0.09f, 0.13f, 0.16f, 0.62f)).raycastTarget = true;
            resultOverlay = overlay.gameObject;
            RectTransform result = Rect("Result Panel", overlay, 0.29f, 0.33f, 0.71f, 0.66f);
            Fill(result, Panel);
            resultTitle = Label(font, result, "Result", "", 42, 0.08f, 0.50f, 0.92f, 0.88f);
            resultTitle.fontStyle = FontStyle.Bold;
            resultButton = MakeButton(result, "Kembali ke Eksplorasi", 0.13f, 0.15f, 0.87f, 0.43f, leave);
            resultButton.targetGraphic.color = Focus;
            resultOverlay.SetActive(false);
        }

        public void Render(GmBattleSession session, GmBattleMenu menu, GmBattleMove selection, string lastAction,
            bool resolving, int actingIndex, int displayRound)
        {
            bool choosingMember = !resolving && session.IsChoosingPartyMember;
            bool finished = !resolving && !session.IsActive;
            bool partyTurn = !resolving && session.IsPartyTurn && !session.Actor.IsStunned;
            bool choosing = partyTurn && menu == GmBattleMenu.Target && selection != null;
            bool area = choosing && selection.Target == GmBattleTarget.AllEnemies;
            bool submenu = partyTurn && menu != GmBattleMenu.Command;
            bool focus = partyTurn && !choosing;
            GmBattleMove targetMove = choosing ? selection : null;
            Button preferred = stage.Render(session, actingIndex, displayRound, focus, choosingMember, targetMove);

            GmBattleUnit actor = actingIndex >= 0 ? session.Units[actingIndex] : null;
            string phaseLabel = actor != null && !actor.IsParty ? "GILIRAN MUSUH" : "GILIRAN PEMAIN";
            turnNumber.text = finished ? $"RONDE {displayRound}" : $"RONDE {displayRound} · {phaseLabel}";
            turnLabel.text = choosingMember ? "Pilih karakter" : actor?.Name ?? "Battle selesai";
            actionLog.text = lastAction ?? "";
            RenderEnemyOrder(session, displayRound, actingIndex);
            Button firstParty = RenderParty(session, choosingMember, actingIndex, displayRound, targetMove);
            if (preferred == null) preferred = firstParty;

            bool showCommands = partyTurn && !submenu;
            SetButton(attackButton, showCommands, true);
            SetButton(itemButton, showCommands, true);
            SetButton(restButton, showCommands, true);
            SetButton(backButton, partyTurn, true);
            if (partyTurn) ButtonLabel(backButton).text = submenu ? "Kembali" : "Ganti karakter";
            bool moves = submenu && !choosing;
            if (moves)
            {
                GmBattleMove first = menu == GmBattleMenu.Attack ? GmBattleMove.BasicAttack : GmBattleMove.HpItem;
                GmBattleMove second = menu == GmBattleMenu.Attack ? session.Actor.Skill : GmBattleMove.MpItem;
                RenderMove(firstMove, first, session);
                RenderMove(secondMove, second, session);
            }
            else
            {
                SetButton(firstMove, false, false);
                SetButton(secondMove, false, false);
            }
            SetButton(confirmButton, area, true);
            if (area) ButtonLabel(confirmButton).text = "Pakai " + selection.Name;
            hint.gameObject.SetActive(choosing || choosingMember);
            if (choosing)
            {
                actionTitle.text = selection.MpCost > 0 ? $"{selection.Name} · {selection.MpCost} MP" : selection.Name;
                hint.text = selection.Target switch
                {
                    GmBattleTarget.Ally => "Pilih satu teman.",
                    GmBattleTarget.AllEnemies => "Semua musuh hidup terpilih.",
                    _ => "Pilih satu musuh."
                };
                if (area) preferred = confirmButton;
            }
            else if (choosingMember)
            {
                actionTitle.text = $"Sisa aksi party · {session.PartyActionsRemaining}";
                hint.text = "Pilih lewat sprite atau kartu HP/MP.";
            }
            else if (resolving) actionTitle.text = "Aksi berlangsung";
            else if (moves)
            {
                actionTitle.text = menu == GmBattleMenu.Attack ? "Attack" : "Item";
                preferred = firstMove.interactable ? firstMove : secondMove.interactable ? secondMove : backButton;
            }
            else if (partyTurn || finished)
            {
                actionTitle.text = "Aksi";
                if (partyTurn) preferred = attackButton;
            }
            else if (actor != null && actor.IsStunned) actionTitle.text = "Terkena Stun";
            else actionTitle.text = "Musuh bertindak";

            resultOverlay.SetActive(finished);
            leaveButton.interactable = session.IsActive;
            if (finished)
            {
                resultTitle.text = session.Phase == GmBattlePhase.Victory ? "Menang" : "Kalah";
                preferred = resultButton;
            }
            FocusAvailableControl(preferred);
        }

        public int FrameCount(GmBattleMove move, GmBattleCharacter character) => stage.FrameCount(move, character);

        public void DrawAction(GmBattleSession session, int actor, GmBattleMove move, int[] targets,
            int frame, int count, int impact) => stage.DrawAction(session, actor, move, targets, frame, count, impact);

        public void ClearAction() => stage.ClearAction();

        public void Dispose()
        {
            if (screen != null)
            {
                screen.gameObject.SetActive(false);
                Object.Destroy(screen.gameObject);
            }
            stage.Dispose();
        }

        private PartyHud[] BuildPartyHud(RectTransform content, GmBattleSession session, Action<int> select)
        {
            var cards = new PartyHud[session.PartyCount];
            float width = 0.517f / cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                float x = 0.02f + i * width;
                RectTransform card = Rect("Party HUD", content, x, 0.027f, x + width - 0.006f, 0.24f);
                Fill(card, Border);
                RectTransform panel = Inset("Panel", card, 1.4f);
                Image background = Fill(panel, Panel);
                Button button = Clickable(background, () => select(index));
                Text name = Label(font, panel, "Name", "", 20, 0.075f, 0.68f, 0.925f, 0.96f, TextAnchor.MiddleLeft);
                name.fontStyle = FontStyle.Bold;
                Text hp = Label(font, panel, "HP", "", 16, 0.075f, 0.43f, 0.925f, 0.66f, TextAnchor.MiddleLeft);
                RectTransform hpFill = Meter(panel, "HP Bar", HpColor, 0.40f);
                Text mp = Label(font, panel, "MP", "", 16, 0.075f, 0.15f, 0.925f, 0.38f, TextAnchor.MiddleLeft);
                RectTransform mpFill = Meter(panel, "MP Bar", MpColor, 0.12f);
                cards[i] = new PartyHud { Button = button, Background = background, Name = name,
                    Hp = hp, Mp = mp, HpFill = hpFill, MpFill = mpFill };
            }
            return cards;
        }

        private Button RenderParty(GmBattleSession session, bool choosingMember, int actor, int round, GmBattleMove move)
        {
            Button first = null;
            for (int i = 0; i < party.Length; i++)
            {
                GmBattleUnit unit = session.Units[i];
                PartyHud card = party[i];
                bool marked = move != null && session.CanTarget(move, i);
                bool selectable = choosingMember && session.CanChoosePartyMember(i) ||
                    marked && move.Target != GmBattleTarget.AllEnemies;
                card.Button.interactable = selectable;
                if (selectable && first == null) first = card.Button;
                bool spent = unit.LastActedRound == round;
                card.Background.color = marked ? Target : i == actor ? Focus : unit.IsAlive && !spent ? Panel : Disabled;
                card.Name.text = UnitLabel(unit, round, i == actor);
                card.Hp.text = $"HP  {unit.Hp}/{unit.MaxHp}";
                card.Mp.text = $"MP  {unit.Mp}/{unit.MaxMp}";
                card.HpFill.anchorMax = new Vector2(unit.Hp / (float)unit.MaxHp, 1f);
                card.MpFill.anchorMax = new Vector2(unit.MaxMp > 0 ? unit.Mp / (float)unit.MaxMp : 0f, 1f);
            }
            return first;
        }

        private void RenderEnemyOrder(GmBattleSession session, int round, int actor)
        {
            int slot = 0;
            foreach (int index in session.EnemyOrder)
            {
                GmBattleUnit unit = session.Units[index];
                bool acting = index == actor;
                if (!unit.IsAlive || (!acting && unit.LastActedRound == round)) continue;
                queueChips[slot].gameObject.SetActive(true);
                queueChips[slot].color = acting ? Target : Panel;
                queueLabels[slot].text = UnitLabel(unit, round, acting);
                if (++slot == queueLabels.Length) break;
            }
            for (; slot < queueLabels.Length; slot++) queueChips[slot].gameObject.SetActive(false);
        }

        private static void RenderMove(Button button, GmBattleMove move, GmBattleSession session)
        {
            if (move == null)
            {
                SetButton(button, false, false);
                return;
            }
            string reason = session.UnavailableReason(move);
            string detail = move.Target switch
            {
                GmBattleTarget.Ally => "1 teman",
                GmBattleTarget.AllEnemies => "AoE",
                _ => "1 musuh"
            };
            if (move.IsItem) detail = $"×{session.ItemCount(move)}";
            else if (move.MpCost > 0) detail = $"{move.MpCost} MP · {detail}";
            if (reason != null) detail += " · " + reason;
            ButtonLabel(button).text = move.Name + "  ·  " + detail;
            SetButton(button, true, reason == null);
        }

        private void FocusAvailableControl(Button preferred)
        {
            EventSystem events = EventSystem.current;
            if (events == null) return;
            GameObject current = events.currentSelectedGameObject;
            if (resultOverlay.activeSelf && current != resultButton.gameObject)
            {
                events.SetSelectedGameObject(resultButton.gameObject);
                return;
            }
            Button selected = current != null ? current.GetComponent<Button>() : null;
            if (selected != null && selected.IsActive() && selected.IsInteractable() && selected.transform.IsChildOf(screen)) return;
            events.SetSelectedGameObject(preferred != null ? preferred.gameObject : null);
        }

        public static string ShortName(GmBattleUnit unit) => unit.Character switch
        {
            GmBattleCharacter.Saka => "Saka",
            GmBattleCharacter.Pacca => "Pacca",
            GmBattleCharacter.Daung => "Daung",
            _ => unit.Name.Replace("Musuh Uji ", "Musuh ")
        };

        public static string UnitLabel(GmBattleUnit unit, int round, bool acting)
        {
            string name = ShortName(unit);
            if (!unit.IsAlive) return name + " · KO";
            if (unit.IsParty && !acting && unit.LastActedRound == round) return name + " · Selesai";
            return unit.IsStunned ? name + " · Stun" : name;
        }

        private Button MakeButton(RectTransform parent, string text, float left, float bottom, float right, float top,
            Action action, int size = 19, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            RectTransform rect = Rect(string.IsNullOrEmpty(text) ? "Move" : text, parent, left, bottom, right, top);
            Fill(rect, Border);
            RectTransform face = Inset("Face", rect, 1.4f);
            Button button = Clickable(Fill(face, Shell), action);
            Label(font, face, "Label", text, size, 0.045f, 0.03f, 0.955f, 0.97f, alignment);
            return button;
        }

        private static Text ButtonLabel(Button button) => button.GetComponentInChildren<Text>(true);

        private static void SetButton(Button button, bool visible, bool interactable)
        {
            button.transform.parent.gameObject.SetActive(visible);
            button.interactable = visible && interactable;
        }

        public static Button Clickable(Image background, Action action)
        {
            background.raycastTarget = true;
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.92f, 0.98f, 0.95f);
            colors.selectedColor = new Color(0.83f, 0.93f, 0.88f);
            colors.pressedColor = new Color(0.73f, 0.82f, 0.77f);
            colors.disabledColor = new Color(0.94f, 0.95f, 0.96f);
            button.colors = colors;
            button.onClick.AddListener(() => action());
            return button;
        }

        private static RectTransform Meter(RectTransform parent, string name, Color color, float y)
        {
            RectTransform track = Rect(name, parent, 0.075f, y, 0.925f, y + 0.035f);
            Fill(track, Border);
            RectTransform fill = Rect("Fill", track, 0f, 0f, 1f, 1f);
            Fill(fill, color);
            return fill;
        }

        public static Text Label(Font font, RectTransform parent, string name, string value, int size,
            float left, float bottom, float right, float top, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            Text text = Rect(name, parent, left, bottom, right, top).gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.alignment = alignment;
            text.color = Ink;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = size;
            return text;
        }

        public static RectTransform Rect(string name, Transform parent, float left, float bottom, float right, float top)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform Inset(string name, RectTransform parent, float padding)
        {
            RectTransform rect = Rect(name, parent, 0f, 0f, 1f, 1f);
            rect.offsetMin = Vector2.one * padding;
            rect.offsetMax = Vector2.one * -padding;
            return rect;
        }

        public static Image Fill(RectTransform rect, Color color)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
