using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CatchNotification : MonoBehaviour
{
    [Header("UI References")]
    public Image fishIconImage;
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI fishInfoText;
    public Button okButton;

    public void Setup(Sprite icon, FishRarity rarity, string fishName, float weight)
    {
        if (fishIconImage != null) fishIconImage.sprite = icon;
        if (rarityText != null) rarityText.text = rarity.ToString();
        if (fishInfoText != null) fishInfoText.text = $"{fishName}({weight} kg)";

        if (okButton != null)
        {
            okButton.onClick.RemoveAllListeners();
            okButton.onClick.AddListener(ClosePopup);
        }

        gameObject.SetActive(true);
    }

    public void ClosePopup()
    {
        gameObject.SetActive(false);
    }
}