using UnityEngine;
using UnityEngine.Rendering;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    public sealed class GmSoreliaLightRecovery : MonoBehaviour
    {
        [SerializeField] private Volume invasion;
        [SerializeField] private GmMainQuestProgress quest;
        private const float FadeSeconds = 3.8f;

        public void Configure(Volume invasionVolume, GmMainQuestProgress progress)
        {
            invasion = invasionVolume;
            quest = progress;
        }

        private bool Rescued => quest != null && quest.HasCompleted(GmMainQuestStep.SelamatkanKeluargaNelayan);

        private void OnEnable()
        {
            if (invasion != null) invasion.weight = Rescued ? 0f : 1f;
        }

        private void Update()
        {
            if (invasion == null) return;
            float target = Rescued ? 0f : 1f;
            if (invasion.weight != target)
                invasion.weight = Mathf.MoveTowards(invasion.weight, target, Time.unscaledDeltaTime / FadeSeconds);
        }
    }
}
