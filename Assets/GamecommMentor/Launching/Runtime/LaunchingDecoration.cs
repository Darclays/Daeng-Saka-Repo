using UnityEngine;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka.Launching
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LaunchingDecoration : MaskableGraphic
    {
        internal enum Shape { Backdrop, Frame, Card, Plaque, Diamond, Badge, Halo, Shadow }
        [SerializeField] private Shape shape;
        [SerializeField] private Color trim;
        private float energy, time, pulse;

        internal void Configure(Shape value, Color fill, Color edge)
        {
            shape = value;
            raycastTarget = false;
            Tint(fill, edge);
        }

        internal void Tint(Color fill, Color edge)
        {
            bool changed = trim != edge;
            trim = edge;
            color = fill;
            if (changed) SetVerticesDirty();
        }

        internal void Animate(float intensity, float age, float burst)
        {
            if (energy == intensity && time == age && pulse == burst) return;
            energy = intensity;
            time = age;
            pulse = burst;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0) return;
            switch (shape)
            {
                case Shape.Backdrop:
                    Panel(mesh, rect, 0f, color, Color.clear);
                    Ellipse(mesh, rect.center, new Vector2(rect.width * .62f, rect.height * .65f), trim, radial: true);
                    break;
                case Shape.Frame:
                    Panel(mesh, rect, 0f, Color.clear, trim);
                    Panel(mesh, Inset(rect, 9f), 0f, Color.clear, Alpha(trim, .45f));
                    break;
                case Shape.Card:
                    Panel(mesh, rect, 21f, color, trim);
                    Panel(mesh, Inset(rect, 8f), 16f, Color.clear, Alpha(trim, .46f));
                    break;
                case Shape.Plaque:
                    Panel(mesh, rect, 5f, color, trim);
                    break;
                case Shape.Diamond:
                    Diamond(mesh, rect.center, rect.width * .5f, rect.height * .5f, color);
                    break;
                case Shape.Badge:
                    Ellipse(mesh, rect.center, rect.size * .5f, trim);
                    Ellipse(mesh, rect.center, rect.size * .5f - Vector2.one * 1.2f, color);
                    break;
                case Shape.Halo:
                    Halo(mesh, rect);
                    break;
                case Shape.Shadow:
                    Ellipse(mesh, rect.center, rect.size * .5f, trim);
                    Ellipse(mesh, rect.center, rect.size * .5f - Vector2.one, color);
                    break;
            }
        }

        private void Halo(VertexHelper mesh, Rect rect)
        {
            Vector2 center = rect.center + new Vector2(0f, 6f);
            Ellipse(mesh, center, new Vector2(174f, 154f), Alpha(color, energy * .44f + pulse * .18f), radial: true);
            Ring(mesh, center, 130f, Alpha(trim, .48f), 1.1f);
            Ring(mesh, center, 112f, Alpha(trim, .29f), 1.1f);
            if (pulse > 0f)
                Ring(mesh, center, 122f + 39f * (1f - pulse), Alpha(color, .72f * pulse), 2.1f);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Diamond(mesh, center + direction * 130f, 2.8f, 3.5f, Alpha(trim, .75f));
                if (energy <= 0f) continue;
                float drift = Mathf.Sin(time * 1.05f + i * 2.1f);
                float orbit = angle + .10f * drift;
                var sparkle = new Vector2(Mathf.Cos(orbit), Mathf.Sin(orbit)) * (151f + drift * 10 + (i % 3) * 5);
                Diamond(mesh, center + sparkle, 2.4f + i % 2, 4.6f + i % 3,
                    Alpha(color, energy * (.64f + drift * .23f)));
            }
        }

        private static Rect Inset(Rect rect, float size) =>
            new Rect(rect.x + size, rect.y + size, rect.width - size * 2f, rect.height - size * 2f);

        private static Color Alpha(Color ink, float alpha) => new Color(ink.r, ink.g, ink.b, ink.a * alpha);
        private static Vector2 Corner(Rect r, float bevel, int index)
        {
            switch (index & 7)
            {
                case 0: return new Vector2(r.xMin + bevel, r.yMin);
                case 1: return new Vector2(r.xMax - bevel, r.yMin);
                case 2: return new Vector2(r.xMax, r.yMin + bevel);
                case 3: return new Vector2(r.xMax, r.yMax - bevel);
                case 4: return new Vector2(r.xMax - bevel, r.yMax);
                case 5: return new Vector2(r.xMin + bevel, r.yMax);
                case 6: return new Vector2(r.xMin, r.yMax - bevel);
                default: return new Vector2(r.xMin, r.yMin + bevel);
            }
        }

        private static void Panel(VertexHelper mesh, Rect rect, float bevel, Color fill, Color edge)
        {
            bevel = Mathf.Min(bevel, Mathf.Min(rect.width, rect.height) * .5f);
            Rect inner = Inset(rect, 1.65f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 a = Corner(rect, bevel, i), b = Corner(rect, bevel, i + 1);
                if (fill.a > 0f) Triangle(mesh, rect.center, a, b, fill, fill);
                if (edge.a <= 0f) continue;
                float innerBevel = Mathf.Max(0f, bevel - .8f);
                Quad(mesh, a, b, Corner(inner, innerBevel, i + 1), Corner(inner, innerBevel, i), edge);
            }
        }

        private static void Ellipse(VertexHelper mesh, Vector2 center, Vector2 radius, Color fill, bool radial = false)
        {
            if (fill.a <= 0f) return;
            const int segments = 48;
            // Two falloff bands keep the lighting tight around the sprite without a texture.
            for (int i = 0; i < segments; i++)
            {
                Vector2 a = Vector2.Scale(At(i, segments), radius), b = Vector2.Scale(At(i + 1, segments), radius);
                if (!radial) { Triangle(mesh, center, center + a, center + b, fill, fill); continue; }
                Color middle = Alpha(fill, .52f), outside = Alpha(fill, 0f);
                Vector2 aa = center + a * .48f, bb = center + b * .48f;
                Triangle(mesh, center, aa, bb, fill, middle);
                Quad(mesh, aa, center + a, center + b, bb, middle, outside, outside, middle);
            }
        }

        private static Vector2 At(int index, int segments)
        {
            float angle = Mathf.PI * 2f * index / segments;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        private static void Ring(VertexHelper mesh, Vector2 center, float radius, Color ink, float width)
        {
            const int segments = 56;
            for (int i = 0; i < segments; i++)
            {
                Vector2 a = At(i, segments), b = At(i + 1, segments);
                Quad(mesh, center + a * radius, center + b * radius,
                    center + b * (radius - width), center + a * (radius - width), ink);
            }
        }

        private static void Diamond(VertexHelper mesh, Vector2 center, float x, float y, Color ink) =>
            Quad(mesh, center + Vector2.down * y, center + Vector2.right * x, center + Vector2.up * y, center + Vector2.left * x, ink);

        private static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color one, Color two)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(a, one, Vector2.zero);
            mesh.AddVert(b, two, Vector2.zero);
            mesh.AddVert(c, two, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
        }

        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ink) =>
            Quad(mesh, a, b, c, d, ink, ink, ink, ink);

        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d,
            Color ca, Color cb, Color cc, Color cd)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(a, ca, Vector2.zero);
            mesh.AddVert(b, cb, Vector2.zero);
            mesh.AddVert(c, cc, Vector2.zero);
            mesh.AddVert(d, cd, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
