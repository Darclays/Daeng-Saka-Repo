using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GamecommMentor.DaengSaka
{
    internal enum GmBattleMenu { Command, Skill, Item, Food, Rest, Target }

    internal sealed class GmBattleView : IDisposable
    {
        private enum ControlPanel { None, Pause, Withdraw, Info, Options }

        private const float ScreenAspect = 16f / 9f;
        private const float ArenaBottom = 0.305f;
        private const float ArenaTop = 0.865f;
        private static readonly Color Ink = new Color(.13f, .16f, .12f);
        private static readonly Color Muted = new Color(.25f, .32f, .26f);
        private static readonly Color Shell = new Color(.97f, .94f, .87f);
        private static readonly Color Panel = new Color(.94f, .90f, .81f);
        private static readonly Color Border = new Color(.64f, .58f, .48f);
        private static readonly Color Bright = new Color(.99f, .96f, .87f);
        private static readonly Color BrightMuted = new Color(.83f, .89f, .81f);
        private static readonly Color Focus = new Color(.88f, .92f, .79f);
        private static readonly Color Target = new Color(.97f, .87f, .67f);
        private static readonly Color Disabled = new Color(.78f, .79f, .72f);
        private static readonly Color HpColor = new Color(0.21f, 0.47f, 0.42f);
        private static readonly Color MpColor = new Color(0.33f, 0.51f, 0.64f);
        private static readonly Color Threat = new Color(1f, .79f, .49f);
        internal static readonly Color GuardColor = new Color(0.82f, 0.90f, 0.96f);

        private sealed class PartyHud
        {
            public Button Button;
            public Image Background;
            public Text Name;
            public Text Status;
            public Text Hp;
            public Text Mp;
            public RectTransform HpFill;
            public RectTransform MpFill;
        }

        private readonly RectTransform screen;
        private readonly CanvasGroup battleControls;
        private readonly Font font;
        private GmBattlePhaseDialogue bossDialogue;
        private readonly GmBattleStageView stage;
        private readonly AudioSource impactAudio;
        private readonly AudioClip swingSound;
        private readonly AudioClip basicSound;
        private readonly AudioClip shieldSound;
        private readonly AudioClip sharkSound;
        private readonly AudioClip itemUseSound;
        private readonly PartyHud[] party;
        private readonly Image[] queueChips = new Image[4];
        private readonly Text[] queueLabels = new Text[4];
        private readonly Text turnNumber;
        private readonly Text turnLabel;
        private readonly Text actionLog;
        private readonly Text statusDetail;
        private readonly Text controlHint;
        private readonly Text bossStatus;
        private readonly Text enemyWarning;
        private readonly Text actionTitle;
        private readonly Text hint;
        private readonly Button attackButton;
        private readonly Button skillButton;
        private readonly Button itemButton;
        private readonly Button foodButton;
        private readonly Button restButton;
        private readonly Button backButton;
        private readonly Button[] options = new Button[Math.Max(3,
            Math.Max(GmBattleMove.Potions.Count + 1, GmBattleMove.Food.Count))];
        private readonly Button confirmButton;
        private readonly Button impactModeButton;
        private readonly Button leaveButton;
        private readonly GameObject resultOverlay;
        private readonly Text resultTitle;
        private readonly Button resultButton;
        private readonly Button recoverButton;
        private readonly Button[] commands;
        private readonly GmControlDialog controlDialog;
        private GmOptionsView optionsView;
        private readonly Action retreat;
        private readonly List<Button> navigation = new List<Button>();
        private readonly Dictionary<GameObject, int> statusTargets = new Dictionary<GameObject, int>();
        private ControlPanel controlPanel;
        private bool controlsAvailable;
        private Button nextFocus;
        private Button focusBeforeDialog;
        private int pointedUnit = -1;
        private int inspectedUnit = -1;
        private int displayedActor = -1;
        private int statusRound;

        public bool HasControlDialog => controlPanel != ControlPanel.None;

        public GmBattleView(Transform parent, GmBattleSession session, string originSceneName, float hitZoom, float impactSfxVolume,
            Action<int> select, Action attack, Action skill, Action item, Action food, Action rest, Action<int> pickMove,
            Action back, Action confirm, Action leave, Action recoverAndLeave, Action toggleImpactMode)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            retreat = leave;
            RectTransform frame = Rect("Battle Screen", parent, 0f, 0f, 1f, 1f);
            screen = frame;
            Fill(frame, new Color(.063f, .105f, .084f));
            impactAudio = frame.gameObject.AddComponent<AudioSource>();
            impactAudio.playOnAwake = false;
            impactAudio.spatialBlend = 0f;
            impactAudio.volume = Mathf.Clamp01(impactSfxVolume);
            swingSound = Resources.Load<AudioClip>("DaengSaka/BattleImpact/swing");
            basicSound = Resources.Load<AudioClip>("DaengSaka/BattleImpact/basicHit");
            shieldSound = Resources.Load<AudioClip>("DaengSaka/BattleImpact/shieldHit");
            sharkSound = Resources.Load<AudioClip>("DaengSaka/BattleImpact/sharkHit");
            itemUseSound = Resources.Load<AudioClip>(GmGameplayAudio.ItemUse);
            RectTransform content = Rect("Battle 16x9", frame, 0f, 0f, 1f, 1f);
            AspectRatioFitter fitter = content.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = ScreenAspect;
            battleControls = content.gameObject.AddComponent<CanvasGroup>();
            RawImage ambient = Rect("Pantulan warna arena", content, 0f, 0f, 1f, 1f).gameObject.AddComponent<RawImage>();
            ambient.raycastTarget = false;
            ambient.color = new Color(.30f, .37f, .31f, .88f);

            RectTransform headerFrame = Rect("Header", content, .02f, ArenaTop + .006f, .98f, .985f);
            Fill(headerFrame, new Color(.53f, .61f, .52f, .75f));
            RectTransform header = Inset("Panel giliran", headerFrame, 1.4f);
            Fill(header, new Color(.09f, .19f, .15f, .88f));
            turnNumber = Label(font, header, "Turn Number", "", 16, 0.02f, 0.55f, 0.46f, 0.88f, TextAnchor.MiddleLeft);
            turnNumber.color = BrightMuted;
            turnLabel = Label(font, header, "Actor", "", 32, 0.02f, 0.12f, 0.50f, 0.57f, TextAnchor.MiddleLeft);
            turnLabel.fontStyle = FontStyle.Bold;
            turnLabel.color = Bright;
            for (int i = 0; i < queueLabels.Length; i++)
            {
                float x = 0.55f + i * 0.077f;
                RectTransform chip = Rect("Enemy Slot", header, x, 0.17f, x + 0.073f, 0.58f);
                queueChips[i] = Fill(chip, Panel);
                queueLabels[i] = Label(font, chip, "Unit", "—", 16, 0.02f, 0f, 0.98f, 1f);
            }
            bossStatus = Label(font, header, "Boss Status", "", 17, 0.55f, 0.59f, 0.87f, 0.79f, TextAnchor.MiddleLeft);
            bossStatus.fontStyle = FontStyle.Bold;
            enemyWarning = Label(font, header, "Enemy Warning", "", 15, 0.55f, 0.80f, 0.884f, 0.99f, TextAnchor.MiddleLeft);
            enemyWarning.color = Threat;
            enemyWarning.fontStyle = FontStyle.Bold;
            impactModeButton = MakeButton(header, "Efek B", 0.894f, 0.70f, 0.98f, 0.96f, toggleImpactMode, 14);
            leaveButton = MakeButton(header, "Jeda", 0.894f, 0.22f, 0.98f, 0.66f, TogglePause);

            RectTransform arena = Rect("Arena", content, 0f, ArenaBottom, 1f, ArenaTop);
            stage = new GmBattleStageView(arena, session, font, select, BindStatusFocus, hitZoom,
                originSceneName, ScreenAspect / (ArenaTop - ArenaBottom));
            ambient.texture = stage.LocationBackdrop;
            ambient.enabled = ambient.texture != null;
            if (stage.LocationBackdrop != null) ambient.uvRect = GmBattleStageView.BackdropUv(stage.LocationBackdrop, ScreenAspect);
            actionLog = Label(font, content, "Action Log", "", 17, 0.02f, 0.272f, 0.98f, 0.302f, TextAnchor.MiddleLeft);
            actionLog.color = Bright;
            statusDetail = Label(font, content, "Status Detail", "", 15, 0.02f, 0.242f, 0.98f, 0.270f, TextAnchor.MiddleLeft);
            statusDetail.color = BrightMuted;
            controlHint = Label(font, content, "Kontrol battle", "", 17, .02f, .006f, .98f, .049f, TextAnchor.MiddleLeft);
            controlHint.color = Bright;
            party = BuildPartyHud(content, session, select);

            RectTransform actions = Rect("Actions", content, 0.55f, 0.055f, 0.98f, 0.237f);
            Fill(actions, Border);
            RectTransform panel = Inset("Panel", actions, 1.4f);
            Fill(panel, Panel);
            actionTitle = Label(font, panel, "Action Title", "Aksi", 19, 0.026f, 0.66f, 0.72f, 0.98f, TextAnchor.MiddleLeft);
            actionTitle.fontStyle = FontStyle.Bold;
            attackButton = MakeButton(panel, "Attack", 0.026f, 0.23f, 0.250f, 0.61f, attack);
            skillButton = MakeButton(panel, "Skill", 0.267f, 0.23f, 0.491f, 0.61f, skill);
            itemButton = MakeButton(panel, "Item", 0.509f, 0.23f, 0.733f, 0.61f, item);
            restButton = MakeButton(panel, "Defensif", 0.750f, 0.23f, 0.974f, 0.61f, rest);
            Text defenseLabel = ButtonLabel(restButton);
            defenseLabel.rectTransform.anchorMin = new Vector2(.045f, .42f);
            defenseLabel.rectTransform.anchorMax = new Vector2(.955f, .98f);
            Label(font, (RectTransform)restButton.transform, "Ringkasan Defensif", "Guard · Isi MP", 14,
                .045f, .03f, .955f, .45f);
            commands = new[] { attackButton, skillButton, itemButton, restButton };
            backButton = MakeButton(panel, "Kembali", 0.759f, 0.72f, 0.974f, 0.96f, back, 16);
            foodButton = MakeOption(panel, GmBattleMove.Potions.Count, "Food ›", food);
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                options[i] = MakeOption(panel, i, "", () => pickMove(index));
            }
            hint = Label(font, panel, "Target Prompt", "", 17, 0.026f, 0.39f, 0.974f, 0.68f, TextAnchor.MiddleLeft);
            hint.color = Muted;
            confirmButton = MakeButton(panel, "Pakai", 0.026f, 0.065f, 0.974f, 0.36f, confirm);
            confirmButton.targetGraphic.color = Focus;

            RectTransform overlay = Rect("Battle Result", content, 0f, 0f, 1f, 1f);
            Fill(overlay, new Color(0.09f, 0.13f, 0.16f, 0.62f)).raycastTarget = true;
            resultOverlay = overlay.gameObject;
            RectTransform result = Rect("Result Panel", overlay, 0.29f, 0.33f, 0.71f, 0.66f);
            Fill(result, Panel);
            resultTitle = Label(font, result, "Result", "", 42, 0.08f, 0.66f, 0.92f, 0.96f);
            resultTitle.fontStyle = FontStyle.Bold;
            recoverButton = MakeButton(result, $"Pulihkan {GmPartyRun.WipeRecoveryPercent}% HP & kembali",
                0.10f, 0.36f, 0.90f, 0.61f, recoverAndLeave);
            resultButton = MakeButton(result, "Kembali ke Eksplorasi", 0.10f, 0.07f, 0.90f, 0.32f, leave);
            resultButton.targetGraphic.color = Focus;
            resultOverlay.SetActive(false);
            controlDialog = new GmControlDialog(screen, font, BackFromOverlay, ChooseOverlayOption, OpenOptions);
        }

        public void Render(GmBattleSession session, GmBattleMenu menu, GmBattleMove selection, string lastAction,
            bool resolving, int actingIndex, int displayRound, bool enhancedImpact, int bossVisualPhase = 0)
        {
            displayedActor = actingIndex;
            statusRound = displayRound;
            bool choosingMember = !resolving && session.IsChoosingPartyMember;
            bool betweenWaves = session.Phase == GmBattlePhase.BetweenWaves;
            bool finished = !resolving && (session.Phase == GmBattlePhase.Victory || session.Phase == GmBattlePhase.Defeat);
            bool partyTurn = !resolving && session.IsPartyTurn && !session.Actor.IsStunned;
            SetButton(impactModeButton, !finished, choosingMember || partyTurn);
            ButtonLabel(impactModeButton).text = enhancedImpact ? "Efek B" : "Efek A";
            bool choosing = partyTurn && menu == GmBattleMenu.Target && selection != null;
            bool confirm = choosing && selection.NeedsConfirm;
            bool submenu = partyTurn && menu != GmBattleMenu.Command;
            bool focus = partyTurn && !choosing;
            GmBattleMove targetMove = choosing ? selection : null;
            stage.Render(session, actingIndex, displayRound, focus, choosingMember, targetMove, bossVisualPhase);

            GmBattleUnit actor = actingIndex >= 0 ? session.Units[actingIndex] : null;
            string phaseLabel = actor != null && !actor.IsParty ? "GILIRAN MUSUH" : "GILIRAN PEMAIN";
            turnNumber.text = displayRound == 0 ? "PEMBUKA · GILIRAN MUSUH"
                : finished ? $"RONDE {displayRound}" : $"RONDE {displayRound} · {phaseLabel}";
            turnLabel.text = betweenWaves ? "Musuh berikutnya" : choosingMember ? "Pilih karakter" : actor?.Name ?? "Battle selesai";
            actionLog.text = lastAction ?? "";
            RenderEnemyOrder(session, displayRound, actingIndex);
            enemyWarning.text = session.Phase == GmBattlePhase.Party ? DescribeEnemyThreat(session) : "";
            if (session.Boss == null)
            {
                bossStatus.text = session.WaveCount > 1 ? $"GELOMBANG {session.WaveNumber}/{session.WaveCount}" : "Urutan musuh";
                bossStatus.color = BrightMuted;
            }
            else
            {
                GmBattleMove chargedMove = session.Boss.ChargedMove;
                if (session.Boss.IsWaitingForPhaseChange) bossStatus.text = "PAKANDE BERSIAP BERGANTI FASE";
                else if (chargedMove != null) bossStatus.text = $"FASE {session.Boss.Phase} · {chargedMove.Name.ToUpperInvariant()} BERIKUTNYA";
                else if (session.Boss.BasicAttackReduction > 0)
                    bossStatus.text = $"FASE 2 · TERBANG · ATTACK BIASA −{session.Boss.BasicAttackReduction}%";
                else bossStatus.text = $"FASE {session.Boss.Phase} · URUTAN MUSUH";
                bossStatus.color = chargedMove != null ? Threat : Bright;
            }
            RenderParty(session, choosingMember, actingIndex, displayRound, targetMove);

            bool showCommands = partyTurn && !submenu;
            SetButton(attackButton, showCommands, showCommands && session.UnavailableReason(GmBattleMove.BasicAttack) == null);
            SetButton(skillButton, showCommands, showCommands && session.Actor.Skills.Count > 0);
            SetButton(itemButton, showCommands, true);
            SetButton(restButton, showCommands, true);
            SetButton(backButton, partyTurn, true);
            if (partyTurn) ButtonLabel(backButton).text = submenu ? "Kembali" : "Ganti karakter";
            bool moves = submenu && !choosing;
            SetButton(foodButton, moves && menu == GmBattleMenu.Item, true);
            if (moves) RenderOptions(session, menu);
            else foreach (Button option in options) SetButton(option, false, false);
            SetButton(confirmButton, confirm, true);
            if (confirm) ButtonLabel(confirmButton).text = "Pakai " + selection.Name;
            hint.gameObject.SetActive(choosing || choosingMember);
            if (choosing)
            {
                actionTitle.text = selection.MpCost > 0 ? $"{selection.Name} · {selection.MpCost} MP" : selection.Name;
                if (selection.Effect == GmBattleEffect.Buff)
                {
                    string recipient = selection.Target == GmBattleTarget.Self ? "Diri" : "Party";
                    hint.text = $"{recipient} · {BuffDetail(selection)}";
                }
                else
                {
                    hint.text = selection.IsItem && selection.TargetsAll
                        ? "HP/MP penuh dan hapus debuff untuk anggota yang masih punya jatah item."
                        : selection.IsItem ? "Pilih teman · 1 item per karakter tiap ronde." : selection.Target switch
                    {
                        GmBattleTarget.Ally => "Pilih satu teman.",
                        GmBattleTarget.AllEnemies => "Semua musuh hidup terpilih.",
                        GmBattleTarget.AllAllies => "Semua anggota party hidup terpilih.",
                        _ => "Pilih satu musuh."
                    };
                    if (selection.IsDamaging && selection.Buff != GmBattleBuff.None)
                        hint.text += selection.AppliesDebuffOnHit ? $" {BuffDetail(selection)} setelah hit."
                            : $" Diri · {BuffDetail(selection)}";
                }
                if (!selection.IsItem && selection.Cleanse != GmBattleCleanse.None) hint.text += $" · {CleanseDetail(selection.Cleanse)}";
            }
            else if (choosingMember)
            {
                actionTitle.text = $"Sisa aksi party · {session.PartyActionsRemaining}";
                hint.text = "Pilih lewat sprite atau kartu HP/MP.";
            }
            else if (resolving) actionTitle.text = "Aksi berlangsung";
            else if (moves) actionTitle.text = menu switch
            {
                GmBattleMenu.Item => "Item · 1 per karakter tiap ronde",
                GmBattleMenu.Food => "Food · Berbagi jatah dengan ramuan",
                GmBattleMenu.Rest => "Defensif · Pilih satu aksi",
                _ => menu.ToString()
            };
            else if (partyTurn || finished) actionTitle.text = "Aksi";
            else if (actor != null && actor.IsStunned) actionTitle.text = "Terkena Stun";
            else actionTitle.text = "Musuh bertindak";

            resultOverlay.SetActive(finished);
            controlsAvailable = session.IsActive && (choosingMember || partyTurn);
            leaveButton.interactable = controlsAvailable;
            if (finished)
            {
                bool defeated = session.Phase == GmBattlePhase.Defeat;
                SetButton(recoverButton, defeated, defeated);
                resultTitle.text = defeated ? "Kalah" : "Menang";
            }
            BuildNavigation(session, choosingMember, choosing, confirm, selection, moves, showCommands, finished);
            RefreshControlHint();
            RefreshStatusDetail(session, force: true);
        }

        private void BuildNavigation(GmBattleSession session, bool choosingMember, bool choosing,
            bool confirm, GmBattleMove move, bool moves, bool showCommands, bool finished)
        {
            navigation.Clear();
            if (finished)
            {
                AddNavigation(recoverButton);
                AddNavigation(resultButton);
                nextFocus = resultButton;
            }
            else if (choosingMember || (choosing && move.Target == GmBattleTarget.Ally))
            {
                foreach (PartyHud card in party) AddNavigation(card.Button);
            }
            else if (confirm) AddNavigation(confirmButton);
            else if (choosing)
            {
                for (int i = session.PartyCount; i < session.Units.Count; i++)
                    AddNavigation(stage.ButtonForUnit(i));
            }
            else if (moves)
            {
                foreach (Button option in options) AddNavigation(option);
                AddNavigation(foodButton);
            }
            else if (showCommands)
            {
                foreach (Button command in commands) AddNavigation(command);
            }
            if (navigation.Count == 0 && (moves || choosing)) AddNavigation(backButton);
            for (int i = 0; i < navigation.Count; i++)
                GmMenuButton.Link(navigation[i], i > 0 ? navigation[i - 1] : null,
                    i + 1 < navigation.Count ? navigation[i + 1] : null);

            if (!HasControlDialog) FocusBattle(nextFocus);
            nextFocus = null;
        }

        private void AddNavigation(Button button)
        {
            if (GmMenuButton.Available(button)) navigation.Add(button);
        }

        private static Button SelectedButton()
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            return selected != null ? selected.GetComponent<Button>() : null;
        }

        private void FocusBattle(Button preferred = null)
        {
            if (preferred != null && navigation.Contains(preferred)) GmMenuButton.Focus(preferred);
            else if (!navigation.Contains(SelectedButton()))
                GmMenuButton.Focus(navigation.Count > 0 ? navigation[0] : null);
        }

        public void RestoreFocusAfterBack(GmBattleMenu previous, GmBattleMenu current, int actor, int option)
        {
            if (previous == GmBattleMenu.Command)
                nextFocus = actor >= 0 && actor < party.Length ? party[actor].Button : null;
            else if (previous == GmBattleMenu.Food)
                nextFocus = foodButton;
            else if (current is GmBattleMenu.Skill or GmBattleMenu.Item or GmBattleMenu.Food)
                nextFocus = option >= 0 && option < options.Length ? options[option] : null;
            else
                nextFocus = previous switch
                {
                    GmBattleMenu.Skill => skillButton,
                    GmBattleMenu.Item => itemButton,
                    GmBattleMenu.Rest => restButton,
                    _ => attackButton
                };
        }

        public void TogglePause()
        {
            if (!controlsAvailable) return;
            if (controlPanel == ControlPanel.Pause) { BackFromOverlay(); return; }
            if (HasControlDialog) return;
            OpenControls(ControlPanel.Pause);
        }

        public void ToggleUnitInfo(GmBattleSession session)
        {
            if (!controlsAvailable) return;
            if (controlPanel == ControlPanel.Info) { BackFromOverlay(); return; }
            if (HasControlDialog) return;
            int index = InspectedUnit();
            if (index < 0 || !session.Units[index].IsAlive)
                for (int i = 0; i < session.Units.Count; i++)
                    if (session.Units[i].IsAlive) { index = i; break; }
            if (index < 0) return;
            GmBattleUnit unit = session.Units[index];
            string status = DescribeStatuses(session, index, statusRound);
            OpenControls(ControlPanel.Info);
            controlDialog.Show(unit.Name, $"HP  {unit.Hp}/{unit.MaxHp}    MP  {unit.Mp}/{unit.MaxMp}\n" +
                (status.Length > 0 ? status : "Tanpa efek status aktif."), "Kembali");
        }

        private void OpenControls(ControlPanel panel)
        {
            focusBeforeDialog = SelectedButton();
            battleControls.interactable = false;
            battleControls.blocksRaycasts = false;
            controlPanel = panel;
            if (panel == ControlPanel.Pause) ShowPause();
        }

        private void ShowPause(bool focusWithdraw = false)
        {
            controlPanel = ControlPanel.Pause;
            controlDialog.Show("Jeda", "Lanjutkan dari pilihan terakhir.", "Lanjutkan", "Mundur dari battle",
                focusWithdraw, third: "Opsi");
        }

        private void OpenOptions()
        {
            if (controlPanel != ControlPanel.Pause) return;
            controlPanel = ControlPanel.Options;
            controlDialog.Hide();
            optionsView ??= new GmOptionsView(screen, BackFromOverlay);
            optionsView.Show("Jeda battle");
        }

        public bool UpdateOptionsInput()
        {
            if (controlPanel != ControlPanel.Options) return false;
            optionsView.UpdateInput();
            return true;
        }

        public void BackFromOverlay()
        {
            if (!HasControlDialog) return;
            GmGameplayAudio.Ui(GmUiSound.Back);
            if (controlPanel == ControlPanel.Options)
            {
                optionsView.Hide();
                ShowPause();
                controlDialog.FocusExtra();
                return;
            }
            if (controlPanel == ControlPanel.Withdraw)
            {
                ShowPause(focusWithdraw: true);
                return;
            }
            controlPanel = ControlPanel.None;
            controlDialog.Hide();
            battleControls.interactable = true;
            battleControls.blocksRaycasts = true;
            FocusBattle(focusBeforeDialog);
            focusBeforeDialog = null;
        }

        private void ChooseOverlayOption()
        {
            if (controlPanel == ControlPanel.Pause)
            {
                controlPanel = ControlPanel.Withdraw;
                controlDialog.Show("Mundur dari battle?", "Party akan kembali ke eksplorasi.", "Batal", "Ya, mundur");
            }
            else if (controlPanel == ControlPanel.Withdraw)
            {
                controlPanel = ControlPanel.Pause;
                BackFromOverlay();
                retreat();
            }
        }

        private void RefreshControlHint()
        {
            if (navigation.Count == 0) { controlHint.text = ""; return; }
            bool pad = GmGamepadInput.UsingGamepad;
            controlHint.text = pad
                ? controlsAvailable ? "D-pad / Stick kiri  Geser     A  Pilih     B  Kembali     X  Info     Menu  Jeda"
                    : "D-pad / Stick kiri  Geser     A  Pilih"
                : controlsAvailable ? "Panah  Geser     Enter  Pilih     Esc  Kembali     X  Info     P  Jeda"
                    : "Panah  Geser     Enter  Pilih";
        }

        private static bool HammerSweepReady(GmBattleSession session)
        {
            foreach (int index in session.EnemyOrder)
            {
                GmBattleUnit unit = session.Units[index];
                if (unit.IsAlive && !unit.IsStunned && unit.EnemySkill == GmBattleMove.HammerSweep &&
                    unit.CooldownRemaining(GmBattleMove.HammerSweep) == 0)
                    return true;
            }
            return false;
        }

        private static string DescribeEnemyThreat(GmBattleSession session)
        {
            GmBossBattle boss = session.Boss;
            if (boss != null && session.Units[boss.Index].IsAlive)
            {
                if (boss.IsCharging)
                {
                    if (session.Units[boss.Index].IsStunned) return "Pakande terkena Stun · Greater Shadow tertahan";
                    int marked = boss.ChargeStunTarget;
                    if (marked >= 0 && session.Units[marked].IsStunned)
                        return "Saka terkena Stun · Cleanse Daung atau Guard party";
                    return "AoE segera · Guard atau coba putus dengan Shield Swing";
                }
                if (boss.Phase == 2)
                {
                    if (session.Units[boss.Index].IsStunned) return "Pakande terkena Stun · Melewatkan giliran";
                    GmBattleMove move = boss.AirborneMove;
                    string scope = move.TargetsAll ? "area" : "1 target";
                    return $"Siap · {move.Name} ({scope}) · Racun {move.PoisonDamage} HP × {move.PoisonTurns}";
                }
            }
            return HammerSweepReady(session)
                ? $"Siap · Hammer Sweep · {GmBattleMove.HammerSweep.Amount} DMG area · {BuffValue(GmBattleMove.HammerSweep)}"
                : "";
        }

        public void UpdateStatusPresentation(GmBattleSession session)
        {
            stage.AnimateStatuses(session, statusRound);
            if (controlPanel == ControlPanel.Options) return;
            if (HasControlDialog) controlDialog.RestoreFocus();
            else if (GmGamepadInput.UsingGamepad && navigation.Count > 0) FocusBattle();
            RefreshControlHint();
            RefreshStatusDetail(session);
        }

        private void BindStatusFocus(GameObject target, int index)
        {
            statusTargets[target] = index;
            EventTrigger trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => pointedUnit = index);
            trigger.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => { if (pointedUnit == index) pointedUnit = -1; });
            trigger.triggers.Add(exit);
        }

        private int InspectedUnit()
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (GmGamepadInput.UsingGamepad && selected != null && statusTargets.TryGetValue(selected, out int padUnit))
                return padUnit;
            if (pointedUnit >= 0 && !GmGamepadInput.UsingGamepad) return pointedUnit;
            return selected != null && statusTargets.TryGetValue(selected, out int focused) ? focused : displayedActor;
        }

        private void RefreshStatusDetail(GmBattleSession session, bool force = false)
        {
            int index = InspectedUnit();
            if (!force && index == inspectedUnit) return;
            inspectedUnit = index;
            statusDetail.text = index >= 0 && session.Units[index].IsAlive
                ? DescribeStatuses(session, index, statusRound) : "";
        }

        public int FrameCount(GmBattleMove move, GmBattleUnit actor, GmBattleImpactCue cue) =>
            stage.FrameCount(move, actor, cue);

        public int BossTransitionFrames(int phase, bool wasAirborne) => stage.BossTransitionFrames(phase, wasAirborne);

        public void DrawBossTransition(int index, int phase, bool wasAirborne, int frame) =>
            stage.DrawBossTransition(index, phase, wasAirborne, frame);

        public void DrawAction(GmBattleSession session, int actor, GmBattleMove move, int[] targets,
            GmBattleHit[] hits, GmBattleActionFrame pose) => stage.DrawAction(session, actor, move, targets, hits, pose);

        public void ClearAction() => stage.ClearAction();

        public void DrawPoisonTick(int index, int damage, float progress) => stage.DrawPoisonTick(index, damage, progress);

        internal void PlayMoveSound(AudioClip clip, string fallback = null)
        {
            if (clip != null) PlayBattleSound(clip);
            else if (fallback != null) PlayImpactSound(fallback);
        }

        internal void PlayBattleCue(string cue) => PlayBattleSound(GmGameplayAudio.Load(GmBattleAudio.Folder + cue));

        public void PlayImpactSound(string cue)
        {
            AudioClip clip = cue switch
            {
                "swing" => swingSound,
                "shieldHit" => shieldSound,
                "sharkHit" => sharkSound,
                "itemUse" => itemUseSound,
                _ => basicSound
            };
            PlayBattleSound(clip);
        }

        public float PlayBattleSound(AudioClip clip)
        {
            if (clip == null || impactAudio.volume <= 0f) return 0f;
            float gain = GmGameOptions.EffectsGain;
            if (gain > 0f) impactAudio.PlayOneShot(clip, gain);
            return clip.length;
        }

        public IEnumerator PlayBossDialogue(GmPakandePhasePresentation.Line[] lines)
        {
            if (lines == null || lines.Length == 0 || font == null) yield break;

            bossDialogue ??= new GmBattlePhaseDialogue(screen, font);
            bool restoreRetreat = leaveButton.interactable;
            leaveButton.interactable = false;
            GmMenuButton.Focus(null);
            yield return bossDialogue.Play(lines);
            leaveButton.interactable = restoreRetreat;
        }

        public void Dispose()
        {
            optionsView?.Hide();
            if (impactAudio != null) impactAudio.Stop();
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
                RectTransform card = Rect("Party HUD", content, x, 0.055f, x + width - 0.006f, 0.237f);
                Fill(card, Border);
                RectTransform panel = Inset("Panel", card, 1.4f);
                Image background = Fill(panel, Panel);
                Button button = Clickable(background, () => select(index));
                BindStatusFocus(panel.gameObject, index);
                Text name = Label(font, panel, "Name", "", 20, 0.075f, 0.80f, 0.925f, 0.97f, TextAnchor.MiddleLeft);
                name.fontStyle = FontStyle.Bold;
                Text status = Label(font, panel, "Status", "", 14, 0.075f, 0.68f, 0.925f, 0.80f, TextAnchor.MiddleLeft);
                status.color = Muted;
                Text hp = Label(font, panel, "HP", "", 16, 0.075f, 0.43f, 0.925f, 0.66f, TextAnchor.MiddleLeft);
                RectTransform hpFill = Meter(panel, "HP Bar", HpColor, 0.40f);
                Text mp = Label(font, panel, "MP", "", 16, 0.075f, 0.15f, 0.925f, 0.38f, TextAnchor.MiddleLeft);
                RectTransform mpFill = Meter(panel, "MP Bar", MpColor, 0.12f);
                cards[i] = new PartyHud { Button = button, Background = background, Name = name,
                    Status = status, Hp = hp, Mp = mp, HpFill = hpFill, MpFill = mpFill };
            }
            return cards;
        }

        private void RenderParty(GmBattleSession session, bool choosingMember, int actor, int round, GmBattleMove move)
        {
            for (int i = 0; i < party.Length; i++)
            {
                GmBattleUnit unit = session.Units[i];
                PartyHud card = party[i];
                bool marked = move != null && session.CanTarget(move, i);
                bool selectable = choosingMember && session.CanChoosePartyMember(i) ||
                    marked && !move.NeedsConfirm;
                card.Button.interactable = selectable;
                bool spent = unit.LastActedRound == round;
                bool guarding = unit.IsAlive && unit.GuardRound == round;
                Color color = unit.IsAlive && !spent ? Panel : Disabled;
                if (guarding) color = GuardColor;
                if (i == actor) color = Focus;
                if (marked) color = Target;
                card.Background.color = color;
                card.Name.text = UnitLabel(unit, round, i == actor);
                if (guarding) card.Name.text += " · Guard";
                card.Status.text = unit.IsAlive && unit.PendingPoisonDamage(round) >= unit.Hp
                    ? "Racun · KO saat dipilih" : StatusSummary(unit, round);
                card.Status.color = StatusColor(unit);
                if (move?.IsItem == true && unit.IsAlive && unit.LastItemRound == round)
                {
                    card.Status.text = "Jatah item terpakai";
                    card.Status.color = Muted;
                }
                card.Hp.text = $"HP  {unit.Hp}/{unit.MaxHp}";
                card.Mp.text = $"MP  {unit.Mp}/{unit.MaxMp}";
                card.HpFill.anchorMax = new Vector2(unit.Hp / (float)unit.MaxHp, 1f);
                card.MpFill.anchorMax = new Vector2(unit.MaxMp > 0 ? unit.Mp / (float)unit.MaxMp : 0f, 1f);
            }
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
                queueLabels[slot].text = session.Boss?.Index == index ? "Pakande" : UnitLabel(unit, round, acting);
                if (++slot == queueLabels.Length) break;
            }
            for (; slot < queueLabels.Length; slot++) queueChips[slot].gameObject.SetActive(false);
        }

        private void RenderOptions(GmBattleSession session, GmBattleMenu menu)
        {
            switch (menu)
            {
                case GmBattleMenu.Skill:
                case GmBattleMenu.Item:
                case GmBattleMenu.Food:
                    var moves = menu switch
                    {
                        GmBattleMenu.Item => GmBattleMove.Potions,
                        GmBattleMenu.Food => GmBattleMove.Food,
                        _ => session.Actor.Skills
                    };
                    for (int i = 0; i < options.Length; i++)
                        RenderMove(options[i], i < moves.Count ? moves[i] : null, session);
                    break;
                case GmBattleMenu.Rest:
                    int amount = session.RechargeAmount;
                    ButtonLabel(options[0]).text = amount > 0 ? $"Isi MP  ·  +{amount} MP" : "Isi MP  ·  MP penuh";
                    ButtonLabel(options[1]).text = $"Guard  ·  −{GmBattleSession.GuardReductionPercent}% damage selama fase musuh";
                    SetButton(options[0], true, true);
                    SetButton(options[1], true, true);
                    for (int i = 2; i < options.Length; i++) SetButton(options[i], false, false);
                    break;
            }
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
                GmBattleTarget.AllEnemies => "Semua musuh",
                GmBattleTarget.AllAllies => "Party",
                GmBattleTarget.Self => "Diri",
                _ => "1 musuh"
            };
            if (move.IsItem)
            {
                detail = reason == null || move.TargetsAll
                    ? $"×{session.ItemCount(move)} · {move.Item.RecoveryLabel}" : move.Item.RecoveryLabel;
            }
            else
            {
                string effect = move.Effect switch
                {
                    GmBattleEffect.StunDamage => $"{session.Actor.PowerFor(move)} DMG + Stun",
                    GmBattleEffect.Heal => $"+{move.Amount} HP",
                    GmBattleEffect.HealPercent => $"+{move.Amount}% maks. HP",
                    GmBattleEffect.Buff => BuffDetail(move),
                    _ => $"{session.Actor.PowerFor(move)} DMG"
                };
                if (move.IsDamaging && move.Buff != GmBattleBuff.None)
                    effect += move.AppliesDebuffOnHit ? $" · {BuffValue(move)} / {move.BuffRounds}R"
                        : $" · Diri {BuffValue(move)}";
                if (move.Cleanse != GmBattleCleanse.None) effect += $" · {CleanseDetail(move.Cleanse)}";
                detail = $"{move.MpCost} MP · {detail} · {effect}";
            }
            if (reason != null) detail += " · " + reason;
            ButtonLabel(button).text = move.Name + " · " + detail;
            SetButton(button, true, reason == null);
        }

        internal static string RestorationLabel(GmBattleHit hit)
        {
            string hp = hit.Amount > 0 ? $"+{hit.Amount} HP" : "";
            if (hit.MpRestored <= 0) return hp;
            return hp.Length > 0 ? $"{hp} · +{hit.MpRestored} MP" : $"+{hit.MpRestored} MP";
        }

        public static string BuffDetail(GmBattleMove move) => move.Buff switch
        {
            GmBattleBuff.Weaken => $"{BuffValue(move)} pada ronde pemain berikutnya",
            _ => $"{BuffValue(move)} / {move.BuffRounds} ronde"
        };

        public static string BuffValue(GmBattleMove move) => move.Buff switch
        {
            GmBattleBuff.Attack => $"DMG {Signed(move.BuffPercent)}%",
            GmBattleBuff.Weaken => $"DMG {Signed(-move.BuffPercent)}%",
            GmBattleBuff.Defense when move.IsNegativeBuff => $"DEF {Signed(move.BuffPercent)}%",
            _ => $"Tahan {Signed(move.BuffPercent)}%"
        };

        private static string CleanseDetail(GmBattleCleanse cleanse) => cleanse == GmBattleCleanse.DefenseDown
            ? "Hapus DEF Down" : "Hapus semua efek negatif";

        public static string StatusSummary(GmBattleUnit unit, int round)
        {
            if (!unit.IsAlive) return "";
            var badges = new List<string>(6);
            if (unit.IsStunned) badges.Add("Stun");
            if (unit.Poison.Count > 0) badges.Add($"Racun ×{unit.Poison.Count}");
            int attack = unit.BuffPercent(GmBattleBuff.Attack, round);
            int weaken = unit.BuffPercent(GmBattleBuff.Weaken, round);
            int defense = unit.BuffPercent(GmBattleBuff.Defense, round);
            int defenseDown = unit.DefenseDownPercent(round);
            if (defenseDown > 0) badges.Add($"DEF −{defenseDown}%");
            if (attack != 0) badges.Add($"DMG {Signed(attack)}%");
            if (weaken != 0) badges.Add($"DMG {Signed(-weaken)}%");
            if (defense > 0) badges.Add($"Tahan {defense}%");
            return string.Join(" · ", badges);
        }

        public static Color StatusColor(GmBattleUnit unit) => unit.Poison.Count > 0 ? new Color(0.38f, 0.18f, 0.49f)
            : unit.IsStunned ? new Color(0.50f, 0.28f, 0.08f) : Muted;

        private static string DescribeStatuses(GmBattleSession session, int index, int round)
        {
            GmBattleUnit unit = session.Units[index];
            var details = new List<string>();
            int healthFloor = session.Boss?.Index == index ? session.Boss.HealthFloor : 0;
            long poisonDamage = Math.Min(unit.PendingPoisonDamage(round), Math.Max(0, unit.Hp - healthFloor));
            if (poisonDamage > 0)
            {
                string tick = $"Tick racun berikutnya −{poisonDamage} HP";
                if (poisonDamage >= unit.Hp) tick += " · KO pada awal giliran";
                details.Add(tick);
            }
            if (unit.IsStunned) details.Add("Stun sampai giliran berikutnya");
            foreach (GmBattlePoisonStack stack in unit.Poison)
                details.Add($"Racun {stack.Damage} HP × {stack.Turns} giliran" +
                    (stack.Source == null ? "" : $" ({stack.Source.Name})"));
            foreach (var buff in unit.ActiveBuffs(round))
                details.Add($"{buff.Move.Name} {BuffValue(buff.Move)} ({buff.Rounds} ronde)");
            int defenseDown = unit.DefenseDownPercent(round);
            int defense = unit.BuffPercent(GmBattleBuff.Defense, round);
            if (defenseDown > 0 && defense != -defenseDown)
                details.Add($"DEF efektif {Signed(defense)}%");
            return details.Count > 0 ? $"{ShortName(unit)}   |   {string.Join("   ·   ", details)}" : "";
        }

        private static string Signed(int value) => value < 0 ? $"−{-(long)value}" : $"+{value}";

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

        private Button MakeOption(RectTransform parent, int row, string text, Action action)
        {
            float rowStep = .63f / options.Length;
            float top = .665f - rowStep * row;
            return MakeButton(parent, text, .026f, top - rowStep + .02f, .974f, top, action, 17, TextAnchor.MiddleLeft);
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
            Button button = background.gameObject.AddComponent<GmMenuButton>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.92f, 0.98f, 0.95f);
            colors.selectedColor = new Color(0.83f, 0.93f, 0.88f);
            colors.pressedColor = new Color(0.73f, 0.82f, 0.77f);
            colors.disabledColor = new Color(0.94f, 0.95f, 0.96f);
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                bool interactive = button.IsActive() && button.IsInteractable();
                action();
                if (interactive) GmGameplayAudio.Ui(GmUiSound.Confirm);
            });
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
