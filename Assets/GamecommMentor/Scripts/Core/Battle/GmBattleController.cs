using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmEncounterBattleRoot))]
    public sealed class GmBattleController : MonoBehaviour
    {
        private static readonly GmBattleStats[] PoppoEnemies =
        {
            new GmBattleStats("Poppo A", 48, 0, 0, 20, 8, enemyKind: GmBattleEnemyKind.Poppo),
            new GmBattleStats("Poppo B", 42, 0, 0, 16, 7, enemyKind: GmBattleEnemyKind.Poppo)
        };

        [SerializeField] private Canvas battleCanvas;
        [SerializeField] private GmBattleStats[] party = GmBattleStats.CreateDefaultParty();
        [SerializeField] private GmBattleStats[] enemies =
        {
            new GmBattleStats("Musuh Uji A", 60, 0, 0, 22, 15),
            new GmBattleStats("Musuh Uji B", 48, 0, 0, 16, 12)
        };
        [SerializeField, Min(1)] private int restMp = 6;
        [SerializeField, Min(0f)] private float enemyActionDelay = 0.55f;
        [SerializeField, Range(0.035f, 0.15f)] private float animationFrameTime = 0.075f;
        [SerializeField, Range(0f, 0.08f)] private float hitZoom = 0.035f;
        [SerializeField] private bool useEnhancedImpact = true;
        [SerializeField, Range(0f, 1f)] private float impactSfxVolume = 0.8f;

        private GmEncounterBattleRoot encounter;
        private GmBattleSession session;
        private GmBattleView view;
        private GmBattleMenu menu;
        private GmBattleMove selection;
        private bool resolving;
        private bool leaving;
        private bool enhancedImpact;
        private int shownBossPhase;
        private string lastAction;

        public void SetCanvas(Canvas canvas) => battleCanvas = canvas;

        private bool CanAct => session != null && !leaving && !resolving &&
            session.IsPartyTurn && !session.Actor.IsStunned;

        private void OnEnable() => StartCoroutine(BeginBattle());

        private IEnumerator BeginBattle()
        {
            // Let the encounter finish activating its scene and EventSystem first.
            yield return null;
            encounter = GetComponent<GmEncounterBattleRoot>();
            if (encounter.Encounter == null)
            {
                Debug.LogWarning("[Daeng Saka] Mulai battle dengan menyentuh musuh di scene eksplorasi.", this);
                yield break;
            }

            try
            {
                if (battleCanvas == null || !battleCanvas.isActiveAndEnabled)
                    throw new InvalidOperationException("Battle Core needs an active battle canvas.");
                GmEnemyEncounter source = encounter.Encounter;
                session = new GmBattleSession(party, source.UsePoppoBattle ? PoppoEnemies : enemies, restMp, source.BossBattleProfile);
                enhancedImpact = useEnhancedImpact;
                view = new GmBattleView(battleCanvas.transform, session, hitZoom, impactSfxVolume, SelectUnit,
                    () => OpenMenu(GmBattleMenu.Target), () => OpenMenu(GmBattleMenu.Skill),
                    () => OpenMenu(GmBattleMenu.Item), () => OpenMenu(GmBattleMenu.Rest),
                    PickMove, Back, ConfirmMove, ExitBattle, RecoverAndExitBattle, ToggleImpactMode);
                Refresh();
                if (session.IsOpeningEnemyPhase) StartCoroutine(PlayAutomaticTurns());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                encounter.Resolve(GmEncounterOutcome.Retreat);
            }
        }

        private void Update()
        {
            if (!CanAct) return;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Back();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) Back();
#endif
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            view?.Dispose();
            view = null;
            session = null;
            menu = GmBattleMenu.Command;
            selection = null;
            resolving = false;
            leaving = false;
            shownBossPhase = 0;
            lastAction = null;
        }

        private void SelectUnit(int index)
        {
            if (session == null || leaving || resolving) return;
            if (session.IsChoosingPartyMember)
            {
                if (!session.TryChoosePartyMember(index)) return;
                if (session.Actor.IsStunned) StartCoroutine(PlayAutomaticTurns());
                else Refresh();
                return;
            }
            if (!CanAct || menu != GmBattleMenu.Target || selection == null || selection.NeedsConfirm) return;
            StartMove(selection, index);
        }

        private void ToggleImpactMode()
        {
            if (session == null || leaving || resolving || session.Phase != GmBattlePhase.Party) return;
            enhancedImpact = !enhancedImpact;
            Refresh();
        }

        private void OpenMenu(GmBattleMenu next)
        {
            if (!CanAct || menu != GmBattleMenu.Command) return;
            if (next == GmBattleMenu.Target) selection = GmBattleMove.BasicAttack;
            menu = next;
            Refresh();
        }

        private void PickMove(int index)
        {
            if (!CanAct || index < 0) return;
            GmBattleMove move;
            switch (menu)
            {
                case GmBattleMenu.Skill:
                    if (index >= session.Actor.Skills.Count) return;
                    move = session.Actor.Skills[index];
                    break;
                case GmBattleMenu.Item:
                    if (index > 1) return;
                    move = index == 0 ? GmBattleMove.HpItem : GmBattleMove.MpItem;
                    break;
                case GmBattleMenu.Rest:
                    if (index > 1) return;
                    UseRestOption(guard: index == 1);
                    return;
                default:
                    return;
            }
            if (session.UnavailableReason(move) != null) return;
            selection = move;
            menu = GmBattleMenu.Target;
            Refresh();
        }

        private void Back()
        {
            if (!CanAct) return;
            switch (menu)
            {
                case GmBattleMenu.Command:
                    if (!session.TryDeselectPartyMember()) return;
                    break;
                case GmBattleMenu.Target:
                    if (selection == GmBattleMove.BasicAttack) menu = GmBattleMenu.Command;
                    else menu = selection.IsItem ? GmBattleMenu.Item : GmBattleMenu.Skill;
                    break;
                default:
                    menu = GmBattleMenu.Command;
                    break;
            }
            selection = null;
            Refresh();
        }

        private void ConfirmMove()
        {
            if (CanAct && menu == GmBattleMenu.Target && selection != null && selection.NeedsConfirm)
                StartMove(selection, -1);
        }

        private void StartMove(GmBattleMove move, int targetIndex)
        {
            if (session.UnavailableReason(move) != null) return;
            int[] targets = session.TargetsFor(move, targetIndex);
            if (targets.Length == 0) return;
            resolving = true;
            StartCoroutine(PlayPartyMove(move, targetIndex, targets));
        }

        private IEnumerator PlayPartyMove(GmBattleMove move, int targetIndex, int[] targets)
        {
            yield return PlayMove(move, targetIndex, targets);
            yield return PlayAutomaticTurns();
        }

        private IEnumerator PlayMove(GmBattleMove move, int targetIndex, int[] targets)
        {
            int actorIndex = session.ActorIndex;
            int round = session.RoundNumber;
            GmBattleUnit actor = session.Actor;
            resolving = true;
            menu = GmBattleMenu.Command;
            selection = null;
            lastAction = $"{GmBattleView.ShortName(actor)} · {move.Name}";
            Refresh();

            GmBattleImpactCue cue = GmBattleImpactCue.For(move, actor.Character);
            int count = view.FrameCount(move, actor, cue);
            int impact = cue != null ? cue.Contact : Mathf.Max(1, count / 2);
            float step = Mathf.Clamp(animationFrameTime, 0.035f, 0.15f);
            if (cue != null)
            {
                if (move == GmBattleMove.BasicAttack) step *= 0.70f;
                count = Mathf.Max(count, impact + Mathf.CeilToInt(0.30f / step));
            }
            bool enhanced = cue != null && enhancedImpact;
            var wait = new WaitForSecondsRealtime(step);
            GmBattleHit[] hits = null;
            double hitAt = 0;
            double frameStarted = Time.unscaledTimeAsDouble;
            for (int frame = 0; frame < count; frame++)
            {
                if (cue != null && frame == cue.Windup) view.PlayImpactSound("swing");
                if (frame == impact)
                {
                    if (!session.TryUse(move, targetIndex, out hits))
                    {
                        Debug.LogError("[Daeng Saka] Battle action could not resolve its target.", this);
                        ExitBattle();
                        yield break;
                    }
                    lastAction = FormatAction(actor, move, hits, round);
                    Refresh(actorIndex, round);
                    if (cue != null) view.PlayImpactSound(cue.Sound);
                    hitAt = Time.unscaledTimeAsDouble;
                }

                if (cue == null)
                {
                    var pose = new GmBattleActionFrame(frame, count, impact, frame, -1f);
                    view.DrawAction(session, actorIndex, move, targets, hits, pose);
                    yield return wait;
                    continue;
                }

                // Pause the contact pose locally while audio, hit reactions, and HUD continue.
                float hold = enhanced && frame == impact ? cue.HitStop : 0f;
                double now = Time.unscaledTimeAsDouble;
                if (frame == impact || now - frameStarted >= step) frameStarted = now;
                double frameEnds = frameStarted + step + hold;
                do
                {
                    float progress = Mathf.Clamp01((float)(now - frameStarted - hold) / step);
                    float hitAge = hits != null ? (float)(now - hitAt) : -1f;
                    var pose = new GmBattleActionFrame(frame, count, impact, frame + progress, hitAge, cue, enhanced);
                    view.DrawAction(session, actorIndex, move, targets, hits, pose);
                    yield return null;
                    now = Time.unscaledTimeAsDouble;
                } while (now < frameEnds);
                frameStarted = frameEnds;
            }

            view.ClearAction();
        }

        private void UseRestOption(bool guard)
        {
            string actor = GmBattleView.ShortName(session.Actor);
            if (guard)
            {
                if (!session.TryGuard()) return;
                lastAction = $"{actor} · Guard · −{GmBattleSession.GuardReductionPercent}% damage selama fase musuh";
            }
            else
            {
                if (!session.TryRest(out int recovered)) return;
                lastAction = recovered > 0 ? $"{actor} · Recharge MP · +{recovered} MP" : $"{actor} · Recharge MP · MP penuh";
            }
            menu = GmBattleMenu.Command;
            StartCoroutine(PlayAutomaticTurns());
        }

        private IEnumerator PlayAutomaticTurns()
        {
            while (session != null && !leaving && session.Actor != null &&
                (session.Phase == GmBattlePhase.Enemy || session.Actor.IsStunned))
            {
                resolving = false;
                session.PrepareEnemyTurn();
                if (session.Boss != null && shownBossPhase != session.Boss.Phase)
                {
                    shownBossPhase = session.Boss.Phase;
                    if (shownBossPhase > 1)
                    {
                        lastAction = $"Nenek Pakande memasuki Fase {shownBossPhase}";
                        Refresh();
                        yield return PlayBossTransition(shownBossPhase);
                    }
                }
                Refresh();
                yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, enemyActionDelay));
                GmBattleUnit actor = session.Actor;
                int actorIndex = session.ActorIndex;
                int round = session.RoundNumber;
                if (session.TrySkipStunnedTurn())
                {
                    resolving = true;
                    lastAction = $"{GmBattleView.ShortName(actor)} melewatkan giliran · Stun";
                    Refresh(actorIndex, round);
                    yield return new WaitForSecondsRealtime(0.35f);
                    continue;
                }

                GmBattleMove move = session.ChooseEnemyMove(out GmBattleMove support, out int target);
                if (support != null)
                {
                    yield return PlayMove(support, -1, session.TargetsFor(support, -1));
                    if (session == null || leaving) yield break;
                }
                int[] targets = session.TargetsFor(move, target);
                if (targets.Length == 0)
                {
                    Debug.LogError("[Daeng Saka] Musuh tidak menemukan target battle.", this);
                    ExitBattle();
                    yield break;
                }
                yield return PlayMove(move, target, targets);
            }
            if (session == null || leaving) yield break;
            resolving = false;
            Refresh();
        }

        private IEnumerator PlayBossTransition(int phase)
        {
            int count = view.BossTransitionFrames(phase, session.Boss.WasAirborne);
            var wait = new WaitForSecondsRealtime(Mathf.Clamp(animationFrameTime, 0.035f, 0.15f));
            for (int frame = 0; frame < count; frame++)
            {
                view.DrawBossTransition(session.Boss.Index, phase, session.Boss.WasAirborne, frame);
                yield return wait;
            }
        }

        private string FormatAction(GmBattleUnit actor, GmBattleMove move, GmBattleHit[] hits, int round)
        {
            string title = $"{GmBattleView.ShortName(actor)} · {move.Name} · ";
            if (move.Effect == GmBattleEffect.Buff) return title + GmBattleView.BuffDetail(move);
            if (move.Effect == GmBattleEffect.Summon) return "Nenek Pakande memanggil Poppo";
            if (move.Effect == GmBattleEffect.Charge)
                return $"{session.Boss.ChargedMove.Name} siap menghantam party pada fase musuh berikutnya";

            string[] effects = new string[hits.Length];
            for (int i = 0; i < hits.Length; i++)
            {
                GmBattleHit hit = hits[i];
                GmBattleUnit target = session.Units[hit.Index];
                string amount = move.Effect switch
                {
                    GmBattleEffect.Heal => $"+{hit.Amount} HP",
                    GmBattleEffect.HealPercent => $"+{hit.Amount} HP",
                    GmBattleEffect.RestoreMp => $"+{hit.Amount} MP",
                    _ => $"−{hit.Amount} HP"
                };
                string status = !target.IsAlive ? " · KO" : hit.Stunned ? " · Stun" : "";
                if (move.Effect == GmBattleEffect.StunDamage && target.IsBoss && target.IsAlive && !hit.Stunned)
                    status = " · Tahan Stun";
                if (!actor.IsParty && target.GuardRound == round && move.IsDamaging)
                    status += " · Guard";
                effects[i] = $"{GmBattleView.ShortName(target)} {amount}{status}";
            }
            string summary = title + string.Join(" / ", effects);
            if (move.IsDamaging && move.Buff != GmBattleBuff.None)
                summary += $" · Diri {GmBattleView.BuffDetail(move)}";
            return summary;
        }

        private void Refresh(int actorIndex = -1, int round = -1)
        {
            view.Render(session, menu, selection, lastAction, resolving,
                actorIndex >= 0 ? actorIndex : session.ActorIndex,
                round >= 0 ? round : session.RoundNumber, enhancedImpact);
        }

        private void RecoverAndExitBattle()
        {
            if (session == null || session.Phase != GmBattlePhase.Defeat || leaving) return;
            // Resolve can disable the battle root synchronously. Recover only after it accepts the return.
            GmPartyRun run = GmPartyRun.Current;
            if (TryExitBattle()) run.TryRecoverWipedParty();
        }

        private void ExitBattle() => TryExitBattle();

        private bool TryExitBattle()
        {
            if (session == null || leaving) return false;
            leaving = true;
            GmEncounterOutcome outcome = session.Phase == GmBattlePhase.Victory
                ? GmEncounterOutcome.Victory : GmEncounterOutcome.Retreat;
            if (encounter.Resolve(outcome)) return true;

            leaving = false;
            Debug.LogWarning("[Daeng Saka] Encounter belum bisa kembali ke eksplorasi.", this);
            return false;
        }
    }
}
