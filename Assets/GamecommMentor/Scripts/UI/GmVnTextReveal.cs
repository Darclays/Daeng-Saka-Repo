using System;
using System.Collections.Generic;
using System.Globalization;
using ProjectVN.Preferences;

namespace GamecommMentor.DaengSaka
{
    // All start times use EZ VN's character-rate clock, including pauses and fades.
    internal sealed class GmVnTextReveal
    {
        private const double WordFade = 4.5, WordSweep = 1.0, LetterFade = 2.8;
        private readonly List<Unit> units = new List<Unit>();
        private double clock, finish;
        private int started;
        private bool letters;
        private struct Unit
        {
            internal int end;
            internal double start;
        }

        internal string Text { get; private set; } = string.Empty;
        internal int Count => units.Count;
        internal int EndOf(int unit) => units[unit].end;
        internal bool IsWriting => clock < finish;
        internal int VisibleLength => !IsWriting ? Text.Length : started == 0 ? 0 : EndOf(started - 1);

        internal void Begin(string text, VNTextSpeedMode speed, GmVnRevealMode mode = GmVnRevealMode.PerKata)
        {
            Text = text ?? string.Empty;
            units.Clear();
            started = 0;
            clock = finish = 0d;
            if (speed == VNTextSpeedMode.Instant || string.IsNullOrWhiteSpace(Text)) return;

            letters = mode == GmVnRevealMode.PerHuruf;
            double next = 0d;
            if (letters)
            {
                int[] boundaries = StringInfo.ParseCombiningCharacters(Text);
                for (int i = 0; i < boundaries.Length; i++)
                {
                    int end = i + 1 < boundaries.Length ? boundaries[i + 1] : Text.Length;
                    units.Add(new Unit { start = next, end = end });
                    next += 1d + Pause(Text[boundaries[i]]);
                }
            }
            else
            {
                int cursor = 0;
                while (cursor < Text.Length)
                {
                    while (cursor < Text.Length && char.IsWhiteSpace(Text[cursor])) cursor++;
                    int start = cursor;
                    while (cursor < Text.Length && !char.IsWhiteSpace(Text[cursor])) cursor++;
                    int end = cursor;
                    while (cursor < Text.Length && char.IsWhiteSpace(Text[cursor])) cursor++;
                    units.Add(new Unit { start = next, end = cursor });
                    int last = end - 1;
                    while (last > start && IsClosing(Text[last])) last--;
                    next += Math.Min(12, Math.Max(3, end - start + 1)) + Pause(Text[last]);
                }
            }

            started = 1;
            finish = units[units.Count - 1].start + FadeWindow;
        }

        private double FadeWindow => letters ? LetterFade : WordFade + WordSweep;

        internal float Opacity(int unit, float acrossWord)
        {
            if (!IsWriting) return 1f;
            double age = clock - units[unit].start;
            double t = letters ? age / LetterFade : (age - acrossWord * WordSweep) / WordFade;
            if (t <= 0d) return 0f;
            if (t >= 1d) return 1f;
            return (float)(t * t * (3d - 2d * t));
        }

        internal bool Tick(float elapsed, VNTextSpeedMode speed)
        {
            if (!IsWriting) return false;
            if (speed == VNTextSpeedMode.Instant)
            {
                Complete();
                return true;
            }
            if (elapsed <= 0f) return false;
            double before = clock;
            clock = Math.Min(finish, clock + elapsed * VNPreferenceKeys.GetTextSpeedForMode(speed));
            int previous = started;
            while (started < units.Count && units[started].start <= clock) started++;
            return previous != started || before < units[started - 1].start + FadeWindow;
        }

        internal void Complete()
        {
            clock = finish;
            started = units.Count;
        }

        private static double Pause(char c) => c switch
        {
            ',' => .04d * VNPreferenceKeys.NormalTextSpeed,
            ':' or ';' => .06d * VNPreferenceKeys.NormalTextSpeed,
            '.' or '!' or '?' or '…' => .10d * VNPreferenceKeys.NormalTextSpeed,
            _ => 0d
        };

        private static bool IsClosing(char c) =>
            c == '"' || c == '\'' || c == '”' || c == '’' || c == ')' || c == ']' || c == '»';
    }
}
