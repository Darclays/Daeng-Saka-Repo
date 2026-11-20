using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka
{
    // Keep the complete, plain UGUI string in layout. Only edit alpha on its native glyphs.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public sealed class GmVnLineFade : BaseMeshEffect
    {
        private readonly List<int> glyphEnds = new List<int>();
        private GmVnTextReveal reveal;
        private Text label;
        private TextGenerator probe;
        private int mappedQuads = -1;
        private Vector2 mappedSize;
        private int mappedFontSize;

        internal void Bind(Text body, GmVnTextReveal value)
        {
            label = body;
            reveal = value;
            mappedQuads = -1;
            label.supportRichText = false;
            label.text = value.Text;
            label.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || reveal == null || !reveal.IsWriting || mesh.currentVertCount == 0) return;
            int quads = mesh.currentVertCount / 4;
            Vector2 size = label.rectTransform.rect.size;
            int fontSize = label.resizeTextForBestFit ? label.cachedTextGenerator.fontSizeUsedForBestFit : label.fontSize;
            if (quads != mappedQuads || size != mappedSize || fontSize != mappedFontSize)
            {
                MapGlyphs(quads, size, fontSize);
                mappedQuads = quads;
                mappedSize = size;
                mappedFontSize = fontSize;
            }
            int first = 0;
            var vertex = default(UIVertex);
            for (int unit = 0; unit < glyphEnds.Count; unit++)
            {
                int end = glyphEnds[unit];
                int width = end - first;
                for (int q = first; q < end; q++)
                {
                    float alpha = reveal.Opacity(unit, width > 1 ? (float)(q - first) / (width - 1) : 0f);
                    if (alpha >= 1f) continue;
                    for (int v = q * 4; v < q * 4 + 4; v++)
                    {
                        mesh.PopulateUIVertex(ref vertex, v);
                        var color = vertex.color;
                        color.a = (byte)(color.a * alpha + .5f);
                        vertex.color = color;
                        mesh.SetUIVertex(vertex, v);
                    }
                }
                first = end;
            }
        }

        private void MapGlyphs(int quads, Vector2 size, int fontSize)
        {
            // Common Latin dialogue has one quad per printable character and none for whitespace.
            // Verify that against the actual mesh. For other scripts or clipped layouts, ask the same
            // generator for prefix lengths at the final font size. This runs only when layout changes.
            glyphEnds.Clear();
            int cursor = 0, count = 0;
            bool simple = true;
            for (int i = 0; i < reveal.Count; i++)
            {
                while (cursor < reveal.EndOf(i))
                {
                    char c = reveal.Text[cursor++];
                    if (c >= '!' && c <= '~') count++;
                    else if (c != ' ' && c != '\t' && c != '\r' && c != '\n') simple = false;
                }
                glyphEnds.Add(count);
            }
            if (simple && count == quads) return;

            if (probe == null) probe = new TextGenerator();
            var settings = label.GetGenerationSettings(size);
            settings.resizeTextForBestFit = false;
            if (fontSize > 0 && settings.font != null && settings.font.dynamic) settings.fontSize = fontSize;
            int previous = 0;
            for (int i = 0; i < glyphEnds.Count - 1; i++)
            {
                string prefix = reveal.Text.Substring(0, reveal.EndOf(i));
                if (probe.Populate(prefix, settings))
                    previous = Math.Max(previous, Math.Min(quads, probe.vertexCount / 4));
                glyphEnds[i] = previous;
            }
            glyphEnds[glyphEnds.Count - 1] = quads;
        }

        protected override void OnDestroy()
        {
            (probe as IDisposable)?.Dispose();
            probe = null;
            base.OnDestroy();
        }
    }
}
