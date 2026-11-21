using System.Collections;
using ProjectVN.Preferences;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GamecommMentor.DaengSaka
{
    internal sealed class GmBattlePhaseDialogue
    {
        private readonly GmVnDialogueStage stage;
        private readonly GmVnTextReveal reveal = new GmVnTextReveal();
        private bool advanceClicked, speedClicked;

        internal GmBattlePhaseDialogue(RectTransform canvas, Font font)
        {
            stage = new GmVnDialogueStage(canvas, font, () => advanceClicked = true,
                () => speedClicked = true, null);
            stage.SetWorldFraming(false, true);
        }

        internal IEnumerator Play(GmPakandePhasePresentation.Line[] lines)
        {
            VNTextSpeedMode speed = GmVnReadingSpeed.Load();
            stage.SetSpeed(speed);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.text)) continue;
                advanceClicked = speedClicked = false;
                reveal.Begin(line.text, speed);
                stage.Show(line.portrait, line.speaker, reveal);
                stage.SetAction(reveal.IsWriting, true);

                bool wasFocused = Application.isFocused;
                while (true)
                {
                    yield return null;

                    float dt = Time.unscaledDeltaTime;
                    stage.Tick(dt);
                    bool focused = Application.isFocused;
                    bool advance = advanceClicked;
                    bool changeSpeed = speedClicked;
                    advanceClicked = speedClicked = false;
                    if (focused && wasFocused)
                    {
#if ENABLE_INPUT_SYSTEM
                        var keyboard = Keyboard.current;
                        advance |= keyboard != null &&
                            (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
                        changeSpeed |= keyboard != null && keyboard.tKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                        advance |= Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
                        changeSpeed |= Input.GetKeyDown(KeyCode.T);
#endif
                        if (changeSpeed)
                        {
                            speed = GmVnReadingSpeed.Cycle(speed);
                            stage.SetSpeed(speed);
                        }
                        else if (advance)
                        {
                            if (!reveal.IsWriting) break;
                            reveal.Complete();
                            stage.RefreshText();
                            stage.SetAction(false, true);
                        }
                    }

                    wasFocused = focused;
                    if (!focused || !reveal.Tick(dt, speed)) continue;
                    stage.RefreshText();
                    if (!reveal.IsWriting) stage.SetAction(false, true);
                }
            }

            stage.Hide();
            // Let the advance input expire before the battle UI becomes actionable again.
            yield return null;
        }
    }
}
