using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CatchUIHandler : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject catchResultPanel;

    [Header("UI Elements")]
    public TextMeshProUGUI fishNameText;
    public TextMeshProUGUI fishRarityText;
    public TextMeshProUGUI fishDetailsText;
    public Button okButton;

    private void Start()
    {
        // Pastikan panel mati di awal & pasang listener tombol OK
        if (catchResultPanel != null) catchResultPanel.SetActive(false);
        if (okButton != null) okButton.onClick.AddListener(ClosePanel);
    }

    // Panggil fungsi ini pas minigame pancingan BERHASIL
    public void ShowFishCaught(FishData fish, float caughtWeight)
    {
        if (fish == null) return;

        // Set Data Ikan ke UI// Sesuaikan nama variabel sprite di FishData kamu
        if (fishNameText != null) fishNameText.text = fish.fishName;
        
        if (fishRarityText != null)
        {
            fishRarityText.text = "Rarity: " + GetRarityName(fish.catchChance);
        }

        if (fishDetailsText != null)
        {
            int calculatedPrice = Mathf.RoundToInt(fish.basePrice * (caughtWeight / fish.minWeight));
            fishDetailsText.text = $"Weight: {caughtWeight:F1} kg\nPrice: {calculatedPrice} Gold";
        }

        // Tampilkan Panel
        catchResultPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        catchResultPanel.SetActive(false);
    }

    private string GetRarityName(float chance)
    {
        if (chance <= 2) return "Special";
        if (chance <= 8) return "Legendary";
        if (chance <= 15) return "Epic";
        if (chance <= 30) return "Rare";
        return "Common";
    }
}
