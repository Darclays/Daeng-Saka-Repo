using System;
using UnityEngine;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmBattleStageView : IDisposable
    {
        private const float SpriteScale = 3.75f;
        private static readonly Color Ground = new Color(0.34f, 0.40f, 0.44f, 0.45f);
        private static readonly Color Active = new Color(0.11f, 0.52f, 0.43f);
        private static readonly Color Target = new Color(0.78f, 0.48f, 0.16f);
        private static readonly Color LabelBackground = new Color(0.96f, 0.97f, 0.98f);

        private sealed class UnitView
        {
            public RectTransform Slot;
            public RawImage Ring;
            public RawImage Sprite;
            public RawImage Effect;
            public GmBattleArt.Clip Idle;
            public Button Button;
            public Image NameBackground;
            public Text Name;
            public Text Hp;
        }

        private readonly GmBattleArt art = new GmBattleArt();
        private readonly RectTransform arena;
        private readonly RawImage background;
        private readonly UnitView[] views;
        private readonly Texture2D ring;

        public GmBattleStageView(RectTransform arena, GmBattleSession session, Font font, Action<int> select)
        {
            this.arena = arena;
            background = Texture(GmBattleView.Rect("Arena Backdrop", arena, 0f, 0f, 1f, 1f));
            background.texture = art.Find("arena")?.Texture;
            background.color = background.texture != null ? Color.white : new Color(0.69f, 0.75f, 0.78f);
            ring = art.Find("ring")?.Texture;
            views = new UnitView[session.Units.Count];
            for (int i = 0; i < views.Length; i++)
            {
                int index = i;
                GmBattleUnit unit = session.Units[i];
                Vector2 position = Position(session, i);
                RectTransform slot = GmBattleView.Rect(unit.Name, arena, position.x, position.y, position.x, position.y);
                RawImage ground = Texture(At("Ground", slot, 70f, 18f, 0f, -2f));
                ground.texture = ring;
                ground.enabled = ring != null;
                RawImage sprite = Frame("Sprite", slot);
                RawImage effect = Frame("Impact", slot);
                effect.enabled = false;

                RectTransform hit = At("Select", slot, 108f, 164f, 0f, 47f);
                Image hitImage = GmBattleView.Fill(hit, Color.clear);
                hitImage.canvasRenderer.cullTransparentMesh = false;
                Button button = GmBattleView.Clickable(hitImage, () => select(index));

                RectTransform name = At("Name Plate", slot, 106f, 24f, 0f, -25f);
                Image nameBackground = GmBattleView.Fill(name, LabelBackground);
                button.targetGraphic = nameBackground;
                Text nameText = GmBattleView.Label(font, name, "Name", "", 15, 0.02f, 0f, 0.98f, 1f);
                Text hp = unit.IsParty ? null : GmBattleView.Label(font,
                    At("Enemy HP", slot, 128f, 22f, 0f, -49f), "Value", "", 15, 0f, 0f, 1f, 1f);

                string idleKey = GmBattleArt.IdleKey(unit.Character) ?? "enemyIdle";
                var idle = art.Find(idleKey);
                views[i] = new UnitView
                {
                    Slot = slot, Ring = ground, Sprite = sprite, Effect = effect, Idle = idle,
                    Button = button, NameBackground = nameBackground, Name = nameText, Hp = hp
                };
            }
        }

        public Button Render(GmBattleSession session, int actor, int round, bool focus,
            bool choosingPartyMember, GmBattleMove selection)
        {
            art.SetBlur(background, focus && background.texture != null, isBackdrop: true);
            if (background.texture != null) background.color = focus ? new Color(0.91f, 0.92f, 0.93f) : Color.white;
            foreach (UnitView view in views) view.Slot.SetAsLastSibling();
            if (actor >= 0) views[actor].Slot.SetAsLastSibling();
            Button first = null;
            for (int i = 0; i < views.Length; i++)
            {
                UnitView view = views[i];
                GmBattleUnit unit = session.Units[i];
                bool acting = i == actor;
                bool marked = selection != null && session.CanTarget(selection, i);
                bool canChoose = choosingPartyMember && session.CanChoosePartyMember(i);
                bool selectable = canChoose || (marked && selection.Target != GmBattleTarget.AllEnemies);
                view.Button.interactable = selectable;
                if (selectable && first == null) first = view.Button;
                view.Ring.color = marked ? Target : acting || canChoose ? Active : Ground;
                view.NameBackground.color = marked ? new Color(0.98f, 0.87f, 0.70f)
                    : acting ? new Color(0.80f, 0.91f, 0.86f) : LabelBackground;
                view.Name.text = GmBattleView.UnitLabel(unit, round, acting);
                if (view.Hp != null) view.Hp.text = $"{unit.Hp}/{unit.MaxHp} HP";

                GmBattleArt.Draw(view.Sprite, view.Idle, 0, unit.IsParty ? SpriteScale : SpriteScale * 0.8f);
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

        public int FrameCount(GmBattleMove move, GmBattleCharacter character)
        {
            GmBattleArt.Clip animation = art.Animation(move, character);
            return animation != null ? animation.count : 6;
        }

        public void DrawAction(GmBattleSession session, int actorIndex, GmBattleMove move, int[] targets,
            int frame, int frameCount, int impactFrame)
        {
            UnitView actor = views[actorIndex];
            GmBattleUnit unit = session.Units[actorIndex];
            GmBattleArt.Clip animation = art.Animation(move, unit.Character);
            GmBattleArt.Draw(actor.Sprite, animation ?? actor.Idle, frame, unit.IsParty ? SpriteScale : SpriteScale * 0.8f);

            bool approach = move == GmBattleMove.ShieldSwing ||
                (move == GmBattleMove.BasicAttack && unit.Character == GmBattleCharacter.Pacca);
            if (approach && targets.Length == 1)
            {
                UnitView target = views[targets[0]];
                Vector2 delta = Vector2.Scale(target.Slot.anchorMin - actor.Slot.anchorMin, arena.rect.size);
                float travel = frame <= impactFrame ? frame / (float)impactFrame
                    : (frameCount - 1 - frame) / (float)(frameCount - 1 - impactFrame);
                float stop = Mathf.Min(100f, delta.magnitude * 0.45f);
                actor.Slot.anchoredPosition = (delta - delta.normalized * stop) * travel;
            }
            if (frame < impactFrame) return;

            GmBattleArt.Clip effect = art.Impact(move);
            foreach (int index in targets)
            {
                UnitView target = views[index];
                if (index != actorIndex)
                {
                    float opacity = session.Units[index].IsAlive ? 1f : 0.29f;
                    target.Sprite.color = new Color(1f, 0.87f, 0.73f, opacity);
                }
                target.Ring.color = Target;
                GmBattleArt.Draw(target.Effect, effect, frame - impactFrame, SpriteScale * 0.94f);
                target.Effect.rectTransform.SetAsLastSibling();
            }
        }

        public void ClearAction()
        {
            foreach (UnitView view in views)
            {
                view.Slot.anchoredPosition = Vector2.zero;
                view.Effect.enabled = false;
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
