using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmBattleStageView : IDisposable
    {
        private const float SpriteScale = 3.75f;
        private const float StatusBadgeWidth = 26f;
        private static readonly Color Ground = new Color(0.34f, 0.40f, 0.44f, 0.45f);
        private static readonly Color Active = new Color(0.11f, 0.52f, 0.43f);
        private static readonly Color Target = new Color(0.78f, 0.48f, 0.16f);
        private static readonly Color GuardRing = new Color(0.26f, 0.50f, 0.69f);
        private static readonly Color LabelBackground = new Color(0.96f, 0.97f, 0.95f);
        private static readonly Color PlateEdge = new Color(0.09f, 0.14f, 0.13f, 0.76f);
        private static readonly Color HpPlate = new Color(0.09f, 0.14f, 0.13f, 0.93f);
        private static readonly Color HpInk = new Color(0.98f, 0.98f, 0.93f);
        private static readonly Color ChargeRing = new Color(0.78f, 0.30f, 0.16f);
        private static readonly Color PoisonNumber = new Color(0.97f, 0.71f, 1f);
        private static readonly Color EnragedBackdrop = new Color(.86f, .80f, 1f);

        private sealed class UnitView
        {
            public RectTransform Slot;
            public RawImage Ring;
            public RawImage Sprite;
            public RawImage Effect;
            public RawImage HitSpark;
            public RawImage Poison;
            public RawImage PoisonFrame;
            public RawImage Stun;
            public RawImage StunFrame;
            public RawImage DefenseDown;
            public Button Button;
            public Image NameBackground;
            public Text Name;
            public Text Hp;
            public Text Status;
            public Text Amount;
            public GmBattleArt.Clip Idle;
            public GmBattleArt.Clip Hit;
            public GmBattleArt.Clip Death;
            public float Scale;
        }

        private readonly GmBattleArt art = new GmBattleArt();
        private readonly GmBattleArt.Clip hitSpark;
        private readonly GmBattleArt.Clip poisonStatus;
        private readonly GmBattleArt.Clip stunStatus;
        private readonly GmBattleArt.Clip defenseDownStatus;
        private readonly GmBattleArt.Clip statusFrame;
        private readonly RectTransform arena;
        private readonly RectTransform stage;
        private readonly float hitZoom;
        private readonly RawImage background;
        private readonly Color locationTint;
        private readonly RawImage partyImpact;
        private readonly UnitView[] views;
        private readonly Texture2D ring;
        private readonly bool hasContactShadows;
        internal Texture2D LocationBackdrop { get; }

        public GmBattleStageView(RectTransform arena, GmBattleSession session, Font font,
            Action<int> select, Action<GameObject, int> statusFocus, float hitZoom,
            string originSceneName, float arenaAspect)
        {
            this.arena = arena;
            this.hitZoom = Mathf.Clamp(hitZoom, 0f, 0.08f);
            hitSpark = art.Find("shieldImpact");
            poisonStatus = art.Find("statusPoison");
            stunStatus = art.Find("statusStun");
            defenseDownStatus = art.Find("statusDefenseDown");
            statusFrame = art.Find("statusFrame");
            arena.gameObject.AddComponent<RectMask2D>();
            background = Texture(GmBattleView.Rect("Arena Backdrop", arena, 0f, 0f, 1f, 1f));
            LocationBackdrop = art.FindLocationBackdrop(originSceneName);
            locationTint = LocationBackdrop == null ? Color.white : LocationTint(originSceneName);
            background.texture = LocationBackdrop != null ? LocationBackdrop : art.Find("arena")?.Texture;
            if (LocationBackdrop != null)
            {
                background.uvRect = BackdropUv(LocationBackdrop, arenaAspect);
                Texture2D shade = Resources.Load<Texture2D>("DaengSaka/BattleStage/HudEdgeFade");
                if (shade != null)
                {
                    AddBackdropShade(arena, shade, .87f, 1f, false);
                    AddBackdropShade(arena, shade, 0f, .14f, true);
                }
            }
            background.color = background.texture != null ? locationTint : new Color(0.69f, 0.75f, 0.78f);
            Texture2D contactShadow = LocationBackdrop != null ? art.FindContactShadow() : null;
            hasContactShadows = contactShadow != null;
            stage = GmBattleView.Rect("Battle Framing", arena, 0f, 0f, 1f, 1f);
            RectTransform partyOrigin = GmBattleView.Rect("Party Impact", stage, 0.27f, 0.31f, 0.27f, 0.31f);
            partyImpact = Frame("Effect", partyOrigin);
            partyImpact.enabled = false;
            ring = art.Find("ring")?.Texture;
            views = new UnitView[session.Units.Count];
            for (int i = 0; i < views.Length; i++)
            {
                int index = i;
                GmBattleUnit unit = session.Units[i];
                Vector2 position = Position(session, i);
                RectTransform slot = GmBattleView.Rect(unit.Name, stage, position.x, position.y, position.x, position.y);
                if (hasContactShadows)
                {
                    RawImage shadow = Texture(At("Contact Shadow", slot, 104f, 28f, 0f, -3f));
                    shadow.texture = contactShadow;
                    shadow.color = new Color(0.04f, 0.06f, 0.06f, 0.90f);
                }
                RawImage ground = Texture(At("Ground", slot, 70f, 18f, 0f, -2f));
                ground.texture = ring;
                ground.enabled = ring != null;
                RawImage sprite = Frame("Sprite", slot);
                RawImage effect = Frame("Impact", slot);
                effect.enabled = false;
                RawImage spark = Frame("Hit Spark", slot);
                spark.enabled = false;
                RawImage poisonFrame = Frame("Poison Frame", slot);
                poisonFrame.enabled = false;
                RawImage poison = Frame("Poison VFX", slot);
                poison.enabled = false;
                RawImage stunFrame = Frame("Stun Frame", slot);
                stunFrame.enabled = false;
                RawImage stun = Frame("Stun VFX", slot);
                stun.enabled = false;
                RawImage defenseDown = Frame("Defense Down Icon", slot);
                defenseDown.enabled = false;

                bool boss = session.Boss?.Index == i;
                RectTransform hit = boss ? At("Select", slot, 180f, 240f, 0f, 94f) : At("Select", slot, 120f, 150f, 0f, 50f);
                Image hitImage = GmBattleView.Fill(hit, Color.clear);
                hitImage.canvasRenderer.cullTransparentMesh = false;
                Button button = GmBattleView.Clickable(hitImage, () => select(index));
                statusFocus(hit.gameObject, index);

                RectTransform name = At("Name Plate", slot, boss ? 160f : 118f, 24f, 0f, -25f);
                Image nameBackground = GmBattleView.Fill(name, LabelBackground);
                OutlinePlate(nameBackground);
                button.targetGraphic = nameBackground;
                if (!unit.IsParty)
                {
                    nameBackground.raycastTarget = true;
                    var events = name.gameObject.AddComponent<EventTrigger>();
                    var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                    entry.callback.AddListener(_ => { if (button.interactable) select(index); });
                    events.triggers.Add(entry);
                    statusFocus(name.gameObject, index);
                }
                Text nameText = GmBattleView.Label(font, name, "Name", "", 15, 0.02f, 0f, 0.98f, 1f);
                nameText.fontStyle = FontStyle.Bold;
                Text hp = null;
                Text status = null;
                if (!unit.IsParty)
                {
                    RectTransform health = At("Enemy HP", slot, 116f, 20f, 0f, -48f);
                    GmBattleView.Fill(health, HpPlate);
                    hp = GmBattleView.Label(font, health, "Value", "", 15, 0.03f, 0f, 0.97f, 1f);
                    hp.color = HpInk;
                    hp.fontStyle = FontStyle.Bold;
                    RectTransform pill = At("Enemy Status", slot, boss ? 192f : 154f, 18f, 0f, -69f);
                    Image statusBackground = GmBattleView.Fill(pill, LabelBackground);
                    statusBackground.raycastTarget = true;
                    OutlinePlate(statusBackground);
                    statusFocus(pill.gameObject, index);
                    status = GmBattleView.Label(font, pill, "Effects", "", 13, 0.02f, 0f, 0.98f, 1f);
                    pill.gameObject.SetActive(false);
                }

                Text amount = GmBattleView.Label(font, At("Amount", slot, 120f, 42f, 0f, 104f), "Value", "", 30,
                    0f, 0f, 1f, 1f);
                amount.fontStyle = FontStyle.Bold;
                Outline amountOutline = amount.gameObject.AddComponent<Outline>();
                amountOutline.effectColor = new Color(0.07f, 0.09f, 0.09f, 0.96f);
                amountOutline.effectDistance = new Vector2(1.6f, -1.6f);
                amount.enabled = false;
                views[i] = new UnitView
                {
                    Slot = slot, Ring = ground, Sprite = sprite, Effect = effect, HitSpark = spark,
                    Button = button, NameBackground = nameBackground, Name = nameText, Hp = hp, Amount = amount,
                    Status = status, Poison = poison, PoisonFrame = poisonFrame, Stun = stun,
                    StunFrame = stunFrame, DefenseDown = defenseDown
                };
            }
        }

        public void Render(GmBattleSession session, int actor, int round, bool focus,
            bool choosingPartyMember, GmBattleMove selection, int bossVisualPhase = 0)
        {
            int visualPhase = bossVisualPhase > 0 ? bossVisualPhase : session.Boss?.Phase ?? 0;
            art.SetBlur(background, focus && background.texture != null, isBackdrop: true);
            if (background.texture != null)
            {
                Color tint = BackdropTint(visualPhase);
                background.color = focus ? new Color(tint.r * .91f, tint.g * .92f, tint.b * .93f) : tint;
            }
            for (int i = 0; i < views.Length; i++)
                if (session.Units[i].IsPresent) views[i].Slot.SetAsLastSibling();
            if (actor >= 0 && session.Units[actor].IsPresent) views[actor].Slot.SetAsLastSibling();
            for (int i = 0; i < views.Length; i++)
            {
                UnitView view = views[i];
                GmBattleUnit unit = session.Units[i];
                bool isBoss = session.Boss?.Index == i;
                bool isPoppo = unit.EnemyKind == GmBattleEnemyKind.Poppo;
                view.Slot.gameObject.SetActive(unit.IsPresent);
                if (!unit.IsPresent) continue;
                bool acting = i == actor;
                bool marked = selection != null && session.CanTarget(selection, i);
                bool canChoose = choosingPartyMember && session.CanChoosePartyMember(i);
                bool selectable = canChoose || (marked && !selection.NeedsConfirm);
                bool guarding = unit.IsAlive && unit.GuardRound == round;
                bool charging = isBoss && unit.IsAlive && session.Boss.IsCharging;
                view.Button.interactable = selectable;
                Color ringColor = guarding ? GuardRing : Ground;
                Color nameColor = guarding ? GmBattleView.GuardColor : LabelBackground;
                if (acting || canChoose) ringColor = Active;
                if (acting) nameColor = new Color(0.80f, 0.91f, 0.86f);
                if (marked)
                {
                    ringColor = Target;
                    nameColor = new Color(0.98f, 0.87f, 0.70f);
                }
                view.Ring.color = ringColor;
                view.Ring.enabled = ring != null && (!hasContactShadows || acting || canChoose || marked || guarding || charging);
                view.NameBackground.color = nameColor;
                view.Name.text = guarding ? GmBattleView.ShortName(unit) + " · Guard"
                    : GmBattleView.UnitLabel(unit, round, acting);
                if (charging)
                {
                    view.Name.text += " · Charge";
                    if (!marked) view.Ring.color = ChargeRing;
                }
                if (view.Status != null)
                {
                    view.Hp.text = $"{unit.Hp}/{unit.MaxHp} HP";
                    view.Status.text = GmBattleView.StatusSummary(unit, round);
                    view.Status.color = GmBattleView.StatusColor(unit);
                    view.Status.transform.parent.gameObject.SetActive(view.Status.text.Length > 0);
                }

                string idleKey = GmBattleArt.IdleKey(unit.Character) ?? "enemyIdle";
                if (isBoss) idleKey = $"pakandeIdle{visualPhase}";
                view.Idle = isBoss ? art.Find(idleKey) : art.EnemyMotion(unit.EnemyKind, "Idle") ?? art.Find(idleKey);
                view.Idle ??= art.Find("enemyIdle");
                view.Hit = isBoss ? art.Find($"pakandeHit{visualPhase}") : art.EnemyMotion(unit.EnemyKind, "Hit");
                view.Death = art.EnemyMotion(unit.EnemyKind, "Death");
                view.Scale = isBoss ? SpriteScale * 0.94f : unit.IsParty ? SpriteScale : SpriteScale * (isPoppo ? 0.70f : 0.80f);
                view.Amount.enabled = false;
                if (!unit.IsAlive && view.Death != null)
                    GmBattleArt.Draw(view.Sprite, view.Death, view.Death.count - 1, view.Scale);
                else GmBattleArt.Draw(view.Sprite, view.Idle, 0, view.Scale);
                art.SetBlur(view.Sprite, focus && !acting && view.Idle != null);
                view.Sprite.rectTransform.localScale = Vector3.one * (focus && acting ? 1.24f : 1f);
                view.Sprite.rectTransform.anchoredPosition *= focus && acting ? 1.24f : 1f;
                float opacity = unit.IsAlive ? 1f : 0.29f;
                if (unit.IsParty && unit.LastActedRound == round && !acting) opacity *= 0.60f;
                if (focus && !acting) opacity *= art.HasBlur ? 0.85f : 0.63f;
                view.Sprite.color = new Color(1f, 1f, 1f, opacity);
            }
            AnimateStatuses(session, round);
        }

        public void AnimateStatuses(GmBattleSession session, int round)
        {
            float time = (float)Time.unscaledTimeAsDouble;
            for (int i = 0; i < views.Length; i++)
            {
                UnitView view = views[i];
                GmBattleUnit unit = session.Units[i];
                bool hasPoison = unit.IsAlive && unit.Poison.Count > 0 && poisonStatus != null;
                bool hasStun = unit.IsAlive && unit.IsStunned && stunStatus != null;
                bool hasDefenseDown = unit.IsAlive && unit.DefenseDownPercent(round) > 0 && defenseDownStatus != null;
                float top = view.Idle != null ? view.Idle.anchorY * view.Scale * view.Sprite.rectTransform.localScale.y : 95f;
                float available = (1f - view.Slot.anchorMin.y) * stage.rect.height;
                int count = (hasPoison ? 1 : 0) + (hasStun ? 1 : 0) + (hasDefenseDown ? 1 : 0);
                bool flank = view.Amount.enabled && count < 3;
                bool crowded = view.Amount.enabled && count == 3;
                float height = Mathf.Max(0f, Mathf.Min(top + (crowded ? -14f : 12f), available - (crowded ? 50f : 29f)));
                float x = flank ? -38f : -16f * (count - 1);
                float spacing = flank ? 76f : 32f;
                DrawFramedStatus(view.Poison, view.PoisonFrame, hasPoison ? poisonStatus : null, time, x, height);
                if (hasPoison) x += spacing;
                DrawFramedStatus(view.Stun, view.StunFrame, hasStun ? stunStatus : null, time, x, height);
                if (hasStun) x += spacing;
                DrawStatus(view.DefenseDown, hasDefenseDown ? defenseDownStatus : null, time, x, height, StatusBadgeWidth);
            }
        }

        private void DrawFramedStatus(RawImage image, RawImage frame, GmBattleArt.Clip clip, float time, float x, float y)
        {
            DrawStatus(frame, clip != null ? statusFrame : null, time, x, y, StatusBadgeWidth);
            if (clip == null || statusFrame == null)
            {
                DrawStatus(image, clip, time, x, y, 30f);
                return;
            }
            const float contentWidth = 20f;
            float scale = contentWidth / clip.width;
            float centerX = (clip.anchorX - clip.width * .5f) * scale;
            float centerY = (StatusBadgeWidth - clip.height * scale) * .5f - (clip.anchorY - clip.height) * scale;
            DrawStatus(image, clip, time, x + centerX, y + centerY, contentWidth);
        }

        private static void DrawStatus(RawImage image, GmBattleArt.Clip clip, float time, float x, float y, float width)
        {
            if (clip == null)
            {
                image.enabled = false;
                return;
            }
            int frame = Mathf.FloorToInt(time * 10f) % clip.count;
            GmBattleArt.Draw(image, clip, frame, width / clip.width);
            image.rectTransform.anchoredPosition += new Vector2(x, y);
        }

        public void DrawPoisonTick(int index, int damage, float progress)
        {
            UnitView view = views[index];
            DrawAmountLabel(view, $"−{damage} HP", PoisonNumber, progress, pop: true);
        }

        public int FrameCount(GmBattleMove move, GmBattleUnit actor, GmBattleImpactCue cue)
        {
            GmBattleArt.Clip animation = ActionArt(move, actor);
            int count = Mathf.Max(8, animation?.count ?? 0);
            if (cue == null) return count;
            int effectFrames = art.Impact(move)?.count ?? 0;
            return Mathf.Max(count, cue.Contact + effectFrames - cue.EffectLead);
        }

        private GmBattleArt.Clip ActionArt(GmBattleMove move, GmBattleUnit unit) =>
            move == GmBattleMove.BasicAttack && !unit.IsParty
                ? art.EnemyMotion(unit.EnemyKind, "Attack") : art.Animation(move, unit.Character);

        public int BossTransitionFrames(int phase, bool wasAirborne) => art.BossTransition(phase, wasAirborne)?.count ?? 0;

        public void DrawBossTransition(int index, int phase, bool wasAirborne, int frame)
        {
            UnitView boss = views[index];
            GmBattleArt.Clip clip = art.BossTransition(phase, wasAirborne);
            GmBattleArt.Draw(boss.Sprite, clip, frame, boss.Scale);
            if (phase == 3 && LocationBackdrop != null && clip != null)
                background.color = Color.Lerp(locationTint, EnragedBackdrop, frame / (float)Mathf.Max(1, clip.count - 1));
        }

        private Color BackdropTint(int phase) => phase == 3 && LocationBackdrop != null ? EnragedBackdrop : locationTint;

        private static Color LocationTint(string scene) => scene switch
        {
            "IbuKota" => new Color(1f, .97f, .89f),
            "DesaSorelia" => new Color(1f, .98f, .90f),
            "GoaLembanyung" or "GoaLembayung" => new Color(.91f, .95f, 1f),
            _ => Color.white
        };

        public void DrawAction(GmBattleSession session, int actorIndex, GmBattleMove move, int[] targets,
            GmBattleHit[] hits, GmBattleActionFrame pose)
        {
            UnitView actor = views[actorIndex];
            GmBattleUnit unit = session.Units[actorIndex];
            bool poppoBasic = move == GmBattleMove.BasicAttack && unit.EnemyKind == GmBattleEnemyKind.Poppo;
            GmBattleArt.Clip animation = ActionArt(move, unit);
            GmBattleArt.Draw(actor.Sprite, animation ?? actor.Idle, pose.Index, actor.Scale);
            if (move.IsDamaging) FrameImpact(pose);

            bool approach = move == GmBattleMove.ShieldSwing || move == GmBattleMove.DominionStrike ||
                move == GmBattleMove.GBite || move == GmBattleMove.CaneStrike ||
                move == GmBattleMove.Uppercut || move == GmBattleMove.TailSweep ||
                (move == GmBattleMove.BasicAttack && (unit.Character == GmBattleCharacter.Pacca ||
                    GmBattleArt.EnemyKey(unit.EnemyKind) != null));
            if (approach && targets.Length == 1)
            {
                UnitView target = views[targets[0]];
                Vector2 delta = Vector2.Scale(target.Slot.anchorMin - actor.Slot.anchorMin, arena.rect.size);
                float travel = pose.Position <= pose.Contact ? pose.Position / pose.Contact
                    : (pose.Count - 1 - pose.Position) / Mathf.Max(1, pose.Count - 1 - pose.Contact);
                travel = Mathf.Clamp01(travel);
                float stop = Mathf.Min(100f, delta.magnitude * 0.45f);
                if (pose.Cue != null) stop = Mathf.Min(pose.Cue.ApproachGap, delta.magnitude * 0.36f);
                actor.Slot.anchoredPosition = (delta - delta.normalized * stop) * travel;
            }

            GmBattleArt.Clip effect = poppoBasic ? art.Find("poppoImpact") : art.Impact(move);
            int effectLead = pose.Cue?.EffectLead ?? 0;
            int effectFrame = pose.Index - pose.Contact + effectLead;
            if (hits == null)
            {
                foreach (int index in targets)
                {
                    UnitView target = views[index];
                    if (!unit.IsParty && (move.IsDamaging || move.Effect == GmBattleEffect.Poison)) target.Ring.color = Target;
                    if (effectFrame >= 0) DrawEffect(target, move, effect, effectFrame);
                }
                return;
            }

            int elapsed = pose.Index - pose.Contact;
            bool sharedImpact = move == session.Boss?.FireRain;
            if (sharedImpact)
                GmBattleArt.Draw(partyImpact, effect, effectFrame, SpriteScale * move.ImpactScale);
            foreach (GmBattleHit hit in hits)
            {
                int index = hit.Index;
                UnitView target = views[index];
                if (index != actorIndex && move.IsDamaging)
                {
                    bool alive = session.Units[index].IsAlive;
                    GmBattleArt.Clip reaction = alive ? target.Hit : target.Death ?? target.Hit;
                    if (reaction != null) GmBattleArt.Draw(target.Sprite, reaction, elapsed, target.Scale);
                    float impulse = pose.Cue != null ? 1f - Mathf.SmoothStep(0f, 1f, pose.HitAge / 0.15f)
                        : Mathf.Clamp01(1f - elapsed / 2f);
                    float direction = target.Slot.anchorMin.x >= actor.Slot.anchorMin.x ? 1f : -1f;
                    float distance = pose.Cue?.Recoil ?? 14f;
                    if (session.Units[index].IsBoss) distance *= 0.57f;
                    target.Slot.anchoredPosition = new Vector2(direction * distance * impulse, 0f);
                    float flash = pose.Cue != null ? Mathf.Clamp01(1f - pose.HitAge / 0.09f) : impulse;
                    Color tint = Color.Lerp(Color.white, new Color(1f, 0.57f, 0.45f), flash);
                    tint.a = alive || target.Death != null ? 1f : 0.45f;
                    target.Sprite.color = tint;
                    if (pose.Cue != null && (move == GmBattleMove.BasicAttack || effect == null))
                        DrawHitSpark(target, pose.HitAge);
                }
                target.Ring.color = move.IsDamaging ? Target : Active;
                if (!sharedImpact) DrawEffect(target, move, effect, effectFrame);
                float numberProgress = pose.Cue != null ? Mathf.Clamp01(pose.HitAge / 0.30f)
                    : elapsed / (float)Mathf.Max(1, pose.Count - pose.Contact);
                DrawAmount(target, move, hit, numberProgress, pose.Cue != null);
            }
        }

        private static void DrawEffect(UnitView target, GmBattleMove move, GmBattleArt.Clip effect, int frame)
        {
            GmBattleArt.Draw(target.Effect, effect, frame, SpriteScale * move.ImpactScale);
            target.Effect.rectTransform.SetAsLastSibling();
        }

        private void DrawHitSpark(UnitView target, float age)
        {
            if (hitSpark == null || age >= 0.12f)
            {
                target.HitSpark.enabled = false;
                return;
            }
            int frame = 1 + Mathf.FloorToInt(age / 0.027f);
            GmBattleArt.Draw(target.HitSpark, hitSpark, frame, 1.6f);
            float height = target.Idle != null ? target.Idle.anchorY * target.Scale : 95f;
            target.HitSpark.rectTransform.anchoredPosition += Vector2.up * (0.53f * height - 35f);
            target.HitSpark.rectTransform.SetAsLastSibling();
        }

        private static void DrawAmount(UnitView view, GmBattleMove move, GmBattleHit hit, float progress, bool pop)
        {
            int amount = hit.Amount;
            bool stunOnly = amount == 0 && hit.Stunned;
            if (move.Effect == GmBattleEffect.Poison)
            {
                DrawAmountLabel(view, "RACUN", PoisonNumber, progress, pop);
                return;
            }
            if (move.Effect == GmBattleEffect.StunDamage && amount == 0 && !hit.Stunned)
            {
                DrawAmountLabel(view, "TAHAN STUN", new Color(0.82f, 0.91f, 1f), progress, pop);
                return;
            }
            if (hit.CleansedCount == 0 && (amount <= 0 && !stunOnly || move.Effect == GmBattleEffect.Buff)) return;
            bool cleanseOnly = hit.CleansedCount > 0 && (amount == 0 || move.Effect == GmBattleEffect.Buff);
            string label = cleanseOnly ? "BERSIH" : stunOnly ? "STUN" : move.IsDamaging ? $"−{amount}"
                : move.Effect == GmBattleEffect.RestoreMp ? $"+{amount} MP" : $"+{amount} HP";
            Color color = stunOnly ? new Color(1f, 0.79f, 0.49f) : move.IsDamaging ? new Color(1f, 0.90f, 0.82f)
                : move.Effect == GmBattleEffect.RestoreMp ? new Color(0.66f, 0.86f, 1f) : new Color(0.65f, 0.99f, 0.76f);
            DrawAmountLabel(view, label, color, progress, pop);
        }

        private static void DrawAmountLabel(UnitView view, string label, Color color, float progress, bool pop)
        {
            Text text = view.Amount;
            if (!text.enabled || !pop) text.text = label;
            text.enabled = true;
            color.a = pop ? Mathf.Clamp01((1f - progress) * 2.5f) : Mathf.Clamp01(1.4f - progress);
            text.color = color;
            RectTransform rect = (RectTransform)text.rectTransform.parent;
            float height = view.Idle != null ? view.Idle.anchorY * view.Scale : 95f;
            RectTransform stage = (RectTransform)view.Slot.parent;
            float available = (1f - view.Slot.anchorMin.y) * stage.rect.height;
            float labelHeight = Mathf.Min(height + 15f + 24f * progress, available - 24f);
            rect.anchoredPosition = new Vector2(0f, labelHeight);
            rect.localScale = Vector3.one * (pop ? 1f + 0.16f * (1f - Mathf.Clamp01(progress * 3f)) : 1f);
            rect.SetAsLastSibling();
        }

        private void FrameImpact(GmBattleActionFrame pose)
        {
            float progress = pose.Cue != null
                ? pose.HitAge < 0f ? 0f : Mathf.Clamp01(1f - pose.HitAge / 0.18f)
                : pose.Index <= pose.Contact ? pose.Index / (float)pose.Contact
                    : (pose.Count - 1 - pose.Index) / (float)(pose.Count - 1 - pose.Contact);
            float zoom = 1f + hitZoom * Mathf.SmoothStep(0f, 1f, progress);
            stage.localScale = Vector3.one * zoom;
            Vector2 offset = Vector2.zero;
            if (pose.Enhanced && pose.HitAge >= 0f && pose.HitAge < 0.10f && pose.Cue.Shake > 0f)
            {
                float t = pose.HitAge / 0.10f;
                float amplitude = pose.Cue.Shake * (1f - t);
                offset = new Vector2(Mathf.Cos(t * 4f * Mathf.PI) * amplitude,
                    Mathf.Sin(t * 3f * Mathf.PI) * amplitude * 0.28f);
            }
            stage.anchoredPosition = background.rectTransform.anchoredPosition = offset;
            // Keep the backdrop covering the arena even when Hit Zoom is set to zero.
            float coverageX = 1f + 2f * Mathf.Abs(offset.x) / Mathf.Max(1f, arena.rect.width);
            float coverageY = 1f + 2f * Mathf.Abs(offset.y) / Mathf.Max(1f, arena.rect.height);
            background.rectTransform.localScale = Vector3.one * Mathf.Max(zoom, Mathf.Max(coverageX, coverageY));
        }

        public void ClearAction()
        {
            stage.localScale = background.rectTransform.localScale = Vector3.one;
            stage.anchoredPosition = background.rectTransform.anchoredPosition = Vector2.zero;
            partyImpact.enabled = false;
            foreach (UnitView view in views)
            {
                view.Slot.anchoredPosition = Vector2.zero;
                view.Effect.enabled = false;
                view.HitSpark.enabled = false;
                view.Amount.enabled = false;
            }
        }

        public void Dispose() => art.Dispose();

        internal Button ButtonForUnit(int index) => views[index].Button;

        internal static Rect BackdropUv(Texture2D texture, float arenaAspect)
        {
            float width = Mathf.Min(1f, arenaAspect * texture.height / texture.width);
            float height = Mathf.Min(1f, texture.width / (texture.height * arenaAspect));
            return new Rect((1f - width) * 0.5f, (1f - height) * 0.5f, width, height);
        }

        private static void OutlinePlate(Image image)
        {
            Outline edge = image.gameObject.AddComponent<Outline>();
            edge.effectColor = PlateEdge;
            edge.effectDistance = new Vector2(1f, -1f);
        }

        private static void AddBackdropShade(RectTransform arena, Texture2D shade, float bottom, float top, bool flip)
        {
            RawImage band = Texture(GmBattleView.Rect("Tepi cahaya arena", arena, 0f, bottom, 1f, top));
            band.texture = shade;
            if (flip) band.uvRect = new Rect(0f, 1f, 1f, -1f);
            band.color = new Color(.07f, .11f, .09f, .73f);
        }

        private static Vector2 Position(GmBattleSession session, int index)
        {
            if (index < session.PartyCount)
            {
                if (session.PartyCount == 3)
                {
                    return index switch
                    {
                        0 => new Vector2(0.27f, 0.28f),
                        1 => new Vector2(0.40f, 0.47f),
                        _ => new Vector2(0.14f, 0.45f)
                    };
                }
                float t = session.PartyCount > 1 ? index / (float)(session.PartyCount - 1) : 0.66f;
                return new Vector2(Mathf.Lerp(0.12f, 0.41f, t), index % 2 == 0 ? 0.45f : 0.28f);
            }
            int enemy = index - session.PartyCount;
            if (session.Boss != null)
            {
                return enemy switch
                {
                    0 => new Vector2(0.79f, 0.29f),
                    1 => new Vector2(0.60f, 0.31f),
                    _ => new Vector2(0.90f, 0.54f)
                };
            }
            int count = session.Units.Count - session.PartyCount;
            if (count <= 2)
                return enemy == 0 ? new Vector2(0.70f, 0.46f) : new Vector2(0.83f, 0.28f);
            int columns = (count + 1) / 2;
            float position = (enemy / 2) / (float)(columns - 1);
            return new Vector2(Mathf.Lerp(0.65f, 0.90f, position), enemy % 2 == 0 ? 0.50f : 0.22f);
        }

        private static RectTransform At(string name, Transform parent, float width, float height, float x, float y)
        {
            RectTransform rect = GmBattleView.Rect(name, parent, 0.5f, 0.5f, 0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }

        private static RawImage Frame(string name, Transform parent)
        {
            RectTransform rect = At(name, parent, 0f, 0f, 0f, 0f);
            rect.pivot = Vector2.zero;
            return Texture(rect);
        }

        private static RawImage Texture(RectTransform rect)
        {
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            return image;
        }
    }
}
