using UnityEngine;
using UnityEngine.UI;

namespace GamecommMentor.DaengSaka
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GmEncounterBattleRoot))]
    public sealed class GmEncounterTestPanel : MonoBehaviour
    {
        [SerializeField] private Text encounterLabel;
        [SerializeField] private Button retreatButton;
        [SerializeField] private Button victoryButton;
        private GmEncounterBattleRoot battle;

        public void SetView(Text label, Button retreat, Button victory)
        {
            encounterLabel = label;
            retreatButton = retreat;
            victoryButton = victory;
        }

        private void Awake() => battle = GetComponent<GmEncounterBattleRoot>();

        private void OnEnable()
        {
            GmEnemyEncounter enemy = battle.Encounter;
            if (encounterLabel != null && enemy != null) encounterLabel.text = enemy.DisplayName;
            if (retreatButton != null) retreatButton.onClick.AddListener(Retreat);
            if (victoryButton != null) victoryButton.onClick.AddListener(Victory);
        }

        private void OnDisable()
        {
            if (retreatButton != null) retreatButton.onClick.RemoveListener(Retreat);
            if (victoryButton != null) victoryButton.onClick.RemoveListener(Victory);
        }

        private void Retreat() => battle.Resolve(GmEncounterOutcome.Retreat);
        private void Victory() => battle.Resolve(GmEncounterOutcome.Victory);
    }
}
