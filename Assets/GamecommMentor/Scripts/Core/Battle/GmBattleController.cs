using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmEncounterBattleRoot))]
    public sealed class GmBattleController : MonoBehaviour
    {
        private static readonly GmBattleStats[] PoppoEnemies =
        {
            new GmBattleStats("Poppo A", 60, 0, 0, 20, 8, enemyKind: GmBattleEnemyKind.Poppo),
            new GmBattleStats("Poppo B", 54, 0, 0, 16, 7, enemyKind: GmBattleEnemyKind.Poppo)
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
        private GmPakandePhasePresentation pakandePresentation;
        private GmBattleMenu menu;
        private GmBattleMove selection;
        private int selectedOption = -1;
        private bool resolving;
        private bool leaving;
        private bool enhancedImpact;
        private int shownBossPhase;
        private string lastAction;

        public void SetCanvas(Canvas canvas) => battleCanvas = canvas;

        private bool CanAct => session != null && !leaving && !resolving && view?.HasControlDialog != true &&
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
                GmBattleStats[] roster = source.Roster != null && source.Roster.HasEnemies
                    ? source.Roster.Enemies : source.UsePoppoBattle ? PoppoEnemies : enemies;
                session = new GmBattleSession(party, roster, restMp, source.BossBattleProfile,
                    reinforcements: encounter.Reinforcement?.Roster?.Enemies);
                if (session.Boss != null)
                    pakandePresentation = Resources.Load<GmPakandePhasePresentation>(GmPakandePhasePresentation.ResourcePath);
                enhancedImpact = useEnhancedImpact;
                GmGamepadInput.ConfigureUi(EventSystem.current);
                view = new GmBattleView(battleCanvas.transform, session, encounter.OriginSceneName,
                    hitZoom, impactSfxVolume, SelectUnit,
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
            if (session == null || leaving || view == null) return;
            view.UpdateStatusPresentation(session);
            if (view.UpdateOptionsInput()) return;
            if (resolving || (!CanAct && !session.IsChoosingPartyMember && !view.HasControlDialog)) return;
            if (GmGamepadInput.PressedMenu()) view.TogglePause();
            else if (GmGamepadInput.PressedBack())
            {
                if (view.HasControlDialog) view.BackFromOverlay();
                else Back();
            }
            else if (GmGamepadInput.PressedInfo()) view.ToggleUnitInfo(session);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            view?.Dispose();
            view = null;
            session = null;
            pakandePresentation = null;
            menu = GmBattleMenu.Command;
            selection = null;
            selectedOption = -1;
            resolving = false;
            leaving = false;
            shownBossPhase = 0;
            lastAction = null;
        }

        private void SelectUnit(int index)
        {
            if (session == null || leaving || resolving || view?.HasControlDialog == true) return;
            if (session.IsChoosingPartyMember)
            {
                int round = session.RoundNumber;
                if (!session.TryChoosePartyMember(index, out int poisonDamage)) return;
                if (poisonDamage > 0)
                {
                    resolving = true;
                    StartCoroutine(PlaySelectedTurnPoison(index, round, poisonDamage));
                }
                else if (session.Actor.IsStunned) StartCoroutine(PlayAutomaticTurns());
                else Refresh();
                return;
            }
            if (!CanAct || menu != GmBattleMenu.Target || selection == null || selection.NeedsConfirm) return;
            StartMove(selection, index);
        }

        private void ToggleImpactMode()
        {
            if (session == null || leaving || resolving || view?.HasControlDialog == true ||
                session.Phase != GmBattlePhase.Party) return;
            enhancedImpact = !enhancedImpact;
            Refresh();
        }

        private void OpenMenu(GmBattleMenu next)
        {
            if (!CanAct || menu != GmBattleMenu.Command) return;
            selectedOption = -1;
            if (next == GmBattleMenu.Target)
            {
                if (session.UnavailableReason(GmBattleMove.BasicAttack) != null) return;
                selection = GmBattleMove.BasicAttack;
            }
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
                case GmBattleMenu.Item:
                    var moves = menu == GmBattleMenu.Item ? GmBattleMove.Items : session.Actor.Skills;
                    if (index >= moves.Count) return;
                    move = moves[index];
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
            selectedOption = index;
            menu = GmBattleMenu.Target;
            Refresh();
        }

        private void Back()
        {
            if (!CanAct) return;
            GmBattleMenu previous = menu;
            int actor = session.ActorIndex;
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
            view.RestoreFocusAfterBack(previous, menu, actor, selectedOption);
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

        private IEnumerator PlaySelectedTurnPoison(int index, int round, int damage)
        {
            yield return PlayPoisonTick(index, round, damage);
            yield return PlayAutomaticTurns();
        }

        private IEnumerator PlayPoisonTick(int index, int round, int damage)
        {
            resolving = true;
            GmBattleUnit unit = session.Units[index];
            lastAction = $"{GmBattleView.ShortName(unit)} · Racun −{damage} HP";
            if (!unit.IsAlive) lastAction += " · KO";
            Refresh(index, round);
            const float duration = 0.42f;
            double start = Time.unscaledTimeAsDouble;
            float age = 0f;
            while (age < duration && session != null && !leaving)
            {
                view.DrawPoisonTick(index, damage, age / duration);
                yield return null;
                age = (float)(Time.unscaledTimeAsDouble - start);
            }
            view?.ClearAction();
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
            if (!actor.IsParty && move.IsDamaging && move.TargetsAll)
            {
                lastAction += " · Seluruh party";
                if (move.AppliesDebuffOnHit) lastAction += $" · {GmBattleView.BuffDetail(move)}";
            }
            if (move.Effect == GmBattleEffect.Charge && targets.Length > 1)
                lastAction += $" · Membidik {GmBattleView.ShortName(session.Units[targets[1]])}";
            else if (!actor.IsParty && move.Effect == GmBattleEffect.StunDamage && targets.Length == 1)
                lastAction += $" · Membidik {GmBattleView.ShortName(session.Units[targets[0]])}";
            if (move.IsDamaging && move.InflictsPoison) lastAction += $" · {PoisonDetail(move)}";
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
                    if (move.IsItem) view.PlayImpactSound("itemUse");
                    else if (cue != null) view.PlayImpactSound(cue.Sound);
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
                GmBattleUnit actor = session.Actor;
                int actorIndex = session.ActorIndex;
                int round = session.RoundNumber;
                resolving = false;
                int poisonDamage = session.PrepareEnemyTurn();
                if (poisonDamage > 0)
                {
                    yield return PlayPoisonTick(actorIndex, round, poisonDamage);
                    if (session == null || leaving) yield break;
                    if (!actor.IsAlive) continue;
                }
                int phase = session.Boss?.Phase ?? 0;
                bool firstTurnOfPhase = phase > shownBossPhase;
                if (firstTurnOfPhase)
                {
                    shownBossPhase = phase;
                    yield return PlayBossPhase(phase);
                }

                Refresh();
                yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, enemyActionDelay));
                if (session == null || leaving) yield break;
                bool charged = session.Boss?.Index == actorIndex && session.Boss.IsCharging;
                if (session.TrySkipStunnedTurn())
                {
                    resolving = true;
                    lastAction = charged ? "Pakande terkena Stun · Greater Shadow batal"
                        : $"{GmBattleView.ShortName(actor)} melewatkan giliran · Stun";
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

                if (firstTurnOfPhase && phase == 1 && move.Effect == GmBattleEffect.Summon &&
                    session != null && !leaving && pakandePresentation != null)
                    yield return view.PlayBossDialogue(pakandePresentation.AfterSummon);
            }
            if (session == null || leaving) yield break;
            if (session.Phase == GmBattlePhase.BetweenWaves)
            {
                resolving = true;
                lastAction = "Gelombang pertama selesai. Musuh berikutnya mendekat.";
                Refresh();
                yield return new WaitForSecondsRealtime(0.7f);
                if (session == null || leaving || !session.TryStartNextWave()) yield break;
                lastAction = $"{encounter.Reinforcement.DisplayName} memasuki battle";
            }
            resolving = false;
            Refresh();
        }

        private IEnumerator PlayBossPhase(int phase)
        {
            resolving = true;
            int previousVisual = phase == 3 ? session.Boss.WasAirborne ? 2 : 1 : 0;
            if (phase == 3 && pakandePresentation?.EnragedOpening?.Length > 0)
            {
                lastAction = "Nenek Pakande bersiap berubah";
                Refresh(bossVisualPhase: previousVisual);
                yield return view.PlayBossDialogue(pakandePresentation.EnragedOpening);
            }

            if (phase > 1)
            {
                lastAction = phase == 3 ? "Nenek Pakande mengaum"
                    : session.Boss.BasicAttackReduction > 0 ? "Pakande terbang. Gunakan skill yang memberi damage."
                    : $"Nenek Pakande memasuki Fase {phase}";
                Refresh(bossVisualPhase: previousVisual);
                float hold = phase == 3 && pakandePresentation != null
                    ? view.PlayBattleSound(pakandePresentation.Roar) : 0f;
                yield return PlayBossTransition(phase, hold);
            }

            Refresh();
            if (pakandePresentation == null) yield break;
            yield return view.PlayBossDialogue(phase switch
            {
                1 => pakandePresentation.Opening,
                2 => pakandePresentation.Airborne,
                _ => pakandePresentation.EnragedReaction
            });
        }

        private IEnumerator PlayBossTransition(int phase, float minimumDuration)
        {
            int count = view.BossTransitionFrames(phase, session.Boss.WasAirborne);
            float step = Mathf.Clamp(animationFrameTime, 0.035f, 0.15f);
            var wait = new WaitForSecondsRealtime(step);
            for (int frame = 0; frame < count; frame++)
            {
                view.DrawBossTransition(session.Boss.Index, phase, session.Boss.WasAirborne, frame);
                yield return wait;
            }

            float remaining = minimumDuration - count * step;
            if (remaining <= 0f) yield break;
            Refresh();
            yield return new WaitForSecondsRealtime(remaining);
        }

        private string FormatAction(GmBattleUnit actor, GmBattleMove move, GmBattleHit[] hits, int round)
        {
            string title = $"{GmBattleView.ShortName(actor)} · {move.Name} · ";
            if (move.Effect == GmBattleEffect.Buff)
            {
                int cleared = Array.FindAll(hits, hit => hit.CleansedCount > 0).Length;
                return title + GmBattleView.BuffDetail(move) +
                    (cleared > 0 ? $" · DEF Down hilang dari {cleared} anggota" : "");
            }
            if (move.Effect == GmBattleEffect.Poison)
            {
                string[] affected = Array.ConvertAll(hits, hit => GmBattleView.ShortName(session.Units[hit.Index]));
                return title + $"{string.Join(" / ", affected)} · {PoisonDetail(move)}";
            }
            if (move.Effect == GmBattleEffect.Summon) return "Nenek Pakande memanggil Poppo";
            if (move.Effect == GmBattleEffect.Charge)
            {
                string warning = $"{session.Boss.ChargedMove.Name} siap · AoE pada giliran musuh berikutnya";
                int marked = Array.FindIndex(hits, hit => hit.Stunned);
                if (marked >= 0) warning += $" · {GmBattleView.ShortName(session.Units[hits[marked].Index])} terkena Stun";
                return warning;
            }

            string[] effects = new string[hits.Length];
            for (int i = 0; i < hits.Length; i++)
            {
                GmBattleHit hit = hits[i];
                GmBattleUnit target = session.Units[hit.Index];
                if (hit.Amount == 0 && hit.Stunned)
                {
                    effects[i] = $"{GmBattleView.ShortName(target)} · Stun";
                    continue;
                }
                string amount = move.Effect switch
                {
                    GmBattleEffect.StunDamage when hit.Amount == 0 => "",
                    GmBattleEffect.Heal or GmBattleEffect.HealPercent when hit.Amount == 0 && hit.CleansedCount > 0 => "",
                    GmBattleEffect.Heal => $"+{hit.Amount} HP",
                    GmBattleEffect.HealPercent => $"+{hit.Amount} HP",
                    GmBattleEffect.RestoreMp => $"+{hit.Amount} MP",
                    _ => $"−{hit.Amount} HP"
                };
                string status = !target.IsAlive ? " · KO" : hit.Stunned ? " · Stun" : "";
                if (move.Effect == GmBattleEffect.StunDamage && target.IsAlive && !hit.Stunned)
                    status = " · Tahan Stun";
                if (!actor.IsParty && target.GuardRound == round && move.IsDamaging)
                    status += " · Guard";
                if (hit.CleansedCount > 0) status += " · Bersih";
                effects[i] = $"{GmBattleView.ShortName(target)}{(amount.Length > 0 ? " " + amount : "")}{status}";
            }
            string summary = title + string.Join(" / ", effects);
            if (Array.Exists(hits, hit => hit.DebuffApplied))
                summary += $" · {GmBattleView.BuffDetail(move)}";
            else if (move.IsDamaging && move.Buff != GmBattleBuff.None && !move.AppliesDebuffOnHit)
                summary += $" · Diri {GmBattleView.BuffDetail(move)}";
            if (Array.Exists(hits, hit => hit.Poisoned)) summary += $" · {PoisonDetail(move)}";
            return summary;
        }

        private static string PoisonDetail(GmBattleMove move) => $"Racun {move.PoisonDamage} HP × {move.PoisonTurns} giliran";

        private void Refresh(int actorIndex = -1, int round = -1, int bossVisualPhase = 0)
        {
            view.Render(session, menu, selection, lastAction, resolving,
                actorIndex >= 0 ? actorIndex : session.ActorIndex,
                round >= 0 ? round : session.RoundNumber, enhancedImpact, bossVisualPhase);
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
