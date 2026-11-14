using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmBattleStageView : IDisposable
    {
        private const float SpriteScale = 3.75f;
        private static readonly Color Ground = new Color(0.34f, 0.40f, 0.44f, 0.45f);
        private static readonly Color Active = new Color(0.11f, 0.52f, 0.43f);
        private static readonly Color Target = new Color(0.78f, 0.48f, 0.16f);
        private static readonly Color GuardRing = new Color(0.26f, 0.50f, 0.69f);
        private static readonly Color LabelBackground = new Color(0.96f, 0.97f, 0.98f);

        private sealed class UnitView
        {
            public RectTransform Slot;
            public RawImage Ring;
            public RawImage Sprite;
            public RawImage Effect;
            public RawImage HitSpark;
            public Button Button;
            public Image NameBackground;
            public Text Name;
            public Text Hp;
            public Text Amount;
            public GmBattleArt.Clip Idle;
            public GmBattleArt.Clip Hit;
            public GmBattleArt.Clip Death;
            public float Scale;
        }

        private readonly GmBattleArt art = new GmBattleArt();
        private readonly GmBattleArt.Clip hitSpark;
        private readonly RectTransform arena;
        private readonly RectTransform stage;
        private readonly float hitZoom;
        private readonly RawImage background;
        private readonly RawImage partyImpact;
        private readonly UnitView[] views;
        private readonly Texture2D ring;

        public GmBattleStageView(RectTransform arena, GmBattleSession session, Font font, Action<int> select, float hitZoom)
        {
            this.arena = arena;
            this.hitZoom = Mathf.Clamp(hitZoom, 0f, 0.08f);
            hitSpark = art.Find("shieldImpact");
            arena.gameObject.AddComponent<RectMask2D>();
            background = Texture(GmBattleView.Rect("Arena Backdrop", arena, 0f, 0f, 1f, 1f));
            background.texture = art.Find("arena")?.Texture;
            background.color = background.texture != null ? Color.white : new Color(0.69f, 0.75f, 0.78f);
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
                RawImage ground = Texture(At("Ground", slot, 70f, 18f, 0f, -2f));
                ground.texture = ring;
                ground.enabled = ring != null;
                RawImage sprite = Frame("Sprite", slot);
                RawImage effect = Frame("Impact", slot);
                effect.enabled = false;
                RawImage spark = Frame("Hit Spark", slot);
                spark.enabled = false;

                bool boss = session.Boss?.Index == i;
                RectTransform hit = boss ? At("Select", slot, 180f, 240f, 0f, 94f) : At("Select", slot, 120f, 150f, 0f, 50f);
                Image hitImage = GmBattleView.Fill(hit, Color.clear);
                hitImage.canvasRenderer.cullTransparentMesh = false;
                Button button = GmBattleView.Clickable(hitImage, () => select(index));

                RectTransform name = At("Name Plate", slot, boss ? 160f : 118f, 24f, 0f, -25f);
                Image nameBackground = GmBattleView.Fill(name, LabelBackground);
                button.targetGraphic = nameBackground;
                if (!unit.IsParty)
                {
                    nameBackground.raycastTarget = true;
                    var events = name.gameObject.AddComponent<EventTrigger>();
                    var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                    entry.callback.AddListener(_ => { if (button.interactable) select(index); });
                    events.triggers.Add(entry);
                }
                Text nameText = GmBattleView.Label(font, name, "Name", "", 15, 0.02f, 0f, 0.98f, 1f);
                Text hp = unit.IsParty ? null : GmBattleView.Label(font,
                    At("Enemy HP", slot, 128f, 22f, 0f, -49f), "Value", "", 15, 0f, 0f, 1f, 1f);

                Text amount = GmBattleView.Label(font, At("Amount", slot, 120f, 42f, 0f, 104f), "Value", "", 30,
                    0f, 0f, 1f, 1f);
                amount.fontStyle = FontStyle.Bold;
                Shadow shadow = amount.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.08f, 0.11f, 0.14f, 0.9f);
                shadow.effectDistance = new Vector2(1.2f, -1.3f);
                amount.enabled = false;
                views[i] = new UnitView
                {
                    Slot = slot, Ring = ground, Sprite = sprite, Effect = effect, HitSpark = spark,
                    Button = button, NameBackground = nameBackground, Name = nameText, Hp = hp, Amount = amount
                };
            }
        }

        public Button Render(GmBattleSession session, int actor, int round, bool focus,
            bool choosingPartyMember, GmBattleMove selection)
        {
            art.SetBlur(background, focus && background.texture != null, isBackdrop: true);
            if (background.texture != null) background.color = focus ? new Color(0.91f, 0.92f, 0.93f) : Color.white;
            for (int i = 0; i < views.Length; i++)
                if (session.Units[i].IsPresent) views[i].Slot.SetAsLastSibling();
            if (actor >= 0 && session.Units[actor].IsPresent) views[actor].Slot.SetAsLastSibling();
            Button first = null;
            for (int i = 0; i < views.Length; i++)
            {
                UnitView view = views[i];
                GmBattleUnit unit = session.Units[i];
                bool isBoss = session.Boss?.Index == i;
                bool isMinion = session.Boss != null && !unit.IsParty && !isBoss;
                view.Slot.gameObject.SetActive(unit.IsPresent);
                if (!unit.IsPresent) continue;
                bool acting = i == actor;
                bool marked = selection != null && session.CanTarget(selection, i);
                bool canChoose = choosingPartyMember && session.CanChoosePartyMember(i);
                bool selectable = canChoose || (marked && !selection.NeedsConfirm);
                bool guarding = unit.IsAlive && unit.GuardRound == round;
                view.Button.interactable = selectable;
                if (selectable && first == null) first = view.Button;
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
                view.NameBackground.color = nameColor;
                view.Name.text = guarding ? GmBattleView.ShortName(unit) + " · Guard"
                    : GmBattleView.UnitLabel(unit, round, acting);
                if (view.Hp != null)
                {
                    int bonus = unit.IsAlive ? unit.BuffPercent(GmBattleBuff.Attack, round) : 0;
                    view.Hp.text = $"{unit.Hp}/{unit.MaxHp} HP";
                    if (bonus > 0) view.Hp.text += $" · +{bonus}% DMG";
                }

                string idleKey = GmBattleArt.IdleKey(unit.Character) ?? "enemyIdle";
                if (isBoss) idleKey = $"pakandeIdle{session.Boss.Phase}";
                if (isMinion) idleKey = "poppoIdle";
                view.Idle = art.Find(idleKey) ?? art.Find("enemyIdle");
                view.Hit = isBoss ? art.Find($"pakandeHit{session.Boss.Phase}") : isMinion ? art.Find("poppoHit") : null;
                view.Death = isMinion ? art.Find("poppoDeath") : null;
                view.Scale = isBoss ? SpriteScale * 0.94f : unit.IsParty ? SpriteScale : SpriteScale * (isMinion ? 0.70f : 0.80f);
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
            return first;
        }

        public int FrameCount(GmBattleMove move, GmBattleUnit actor, bool isMinion, GmBattleImpactCue cue)
        {
            GmBattleArt.Clip animation = isMinion ? art.Find("poppoAttack") : art.Animation(move, actor.Character);
            int count = Mathf.Max(8, animation?.count ?? 0);
            if (cue == null) return count;
            int effectFrames = art.Impact(move)?.count ?? 0;
            return Mathf.Max(count, cue.Contact + effectFrames - cue.EffectLead);
        }

        public int BossTransitionFrames(int phase, bool wasAirborne) => art.BossTransition(phase, wasAirborne)?.count ?? 0;

        public void DrawBossTransition(int index, int phase, bool wasAirborne, int frame)
        {
            UnitView boss = views[index];
            GmBattleArt.Draw(boss.Sprite, art.BossTransition(phase, wasAirborne), frame, boss.Scale);
        }

        public void DrawAction(GmBattleSession session, int actorIndex, GmBattleMove move, int[] targets,
            GmBattleHit[] hits, GmBattleActionFrame pose)
        {
            UnitView actor = views[actorIndex];
            GmBattleUnit unit = session.Units[actorIndex];
            bool minion = session.Boss != null && !unit.IsParty && session.Boss.Index != actorIndex;
            GmBattleArt.Clip animation = minion ? art.Find("poppoAttack") : art.Animation(move, unit.Character);
            GmBattleArt.Draw(actor.Sprite, animation ?? actor.Idle, pose.Index, actor.Scale);
            if (move.IsDamaging) FrameImpact(pose);

            bool approach = move == GmBattleMove.ShieldSwing || move == GmBattleMove.DominionStrike ||
                (move == GmBattleMove.BasicAttack && (minion || unit.Character == GmBattleCharacter.Pacca));
            if (approach && targets.Length == 1)
            {
                UnitView target = views[targets[0]];
                Vector2 delta = Vector2.Scale(target.Slot.anchorMin - actor.Slot.anchorMin, arena.rect.size);
                float travel = pose.Position <= pose.Contact ? pose.Position / pose.Contact
                    : (pose.Count - 1 - pose.Position) / Mathf.Max(1, pose.Count - 1 - pose.Contact);
                travel = Mathf.Clamp01(travel);
                float stop = Mathf.Min(100f, delta.magnitude * 0.45f);
                if (pose.Cue != null) stop = Mathf.Min(62f, delta.magnitude * 0.36f);
                actor.Slot.anchoredPosition = (delta - delta.normalized * stop) * travel;
            }

            GmBattleArt.Clip effect = minion ? art.Find("poppoImpact") : art.Impact(move);
            int effectLead = pose.Cue?.EffectLead ?? 0;
            int effectFrame = pose.Index - pose.Contact + effectLead;
            if (hits == null)
            {
                if (effectFrame >= 0)
                    foreach (int index in targets) DrawEffect(views[index], move, effect, effectFrame);
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
                    if (pose.Cue != null && move == GmBattleMove.BasicAttack) DrawHitSpark(target, pose.HitAge);
                }
                target.Ring.color = move.IsDamaging ? Target : Active;
                if (!sharedImpact) DrawEffect(target, move, effect, effectFrame);
                float numberProgress = pose.Cue != null ? Mathf.Clamp01(pose.HitAge / 0.30f)
                    : elapsed / (float)Mathf.Max(1, pose.Count - pose.Contact);
                DrawAmount(target, move, hit.Amount, numberProgress, pose.Cue != null);
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

        private static void DrawAmount(UnitView view, GmBattleMove move, int amount, float progress, bool pop)
        {
            if (amount <= 0 || move.Effect == GmBattleEffect.Buff) return;
            Text text = view.Amount;
            text.enabled = true;
            if (!pop || progress == 0f)
                text.text = move.IsDamaging ? $"−{amount}" : move.Effect == GmBattleEffect.RestoreMp ? $"+{amount} MP" : $"+{amount} HP";
            Color color = move.IsDamaging ? new Color(1f, 0.90f, 0.82f)
                : move.Effect == GmBattleEffect.RestoreMp ? new Color(0.66f, 0.86f, 1f) : new Color(0.65f, 0.99f, 0.76f);
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
