using UnityEngine;

public enum WaterType { FreshWater, SaltWater } // Air Tawar / Air Laut
public enum FishRarity { Common, Uncommon, Rare, Epic, Legendary, Secret } // Enum Rarity Ikan

[CreateAssetMenu(fileName = "NewFishData", menuName = "Fishing/Fish Data")]
public class FishData : ScriptableObject
{
    [Header("Basic Info")]
    public string fishName = "Buntal Duri";
    public Sprite fishIcon; // Icon/Gambar ikan untuk Notifikasi UI
    [TextArea(2, 5)] public string description; // Untuk deskripsi detail ikan
    public WaterType habitat = WaterType.FreshWater;
    
    [Header("Special Attributes")]
    public bool isPoisonous = false; // Khusus Puffer Fish yang beracun jika dimakan
    public bool isCraftingMaterial = true;

    [Header("Rarity & Catch Rate")]
    public FishRarity rarity = FishRarity.Common; // Dropdown pilihan Rarity
    [Range(1, 100)] public int catchChance = 50;

    [Header("Minigame Settings")]
    public float movementSpeed = 2f; 
    public float catchSpeed = 25f;
    public float decaySpeed = 15f;

    [Header("Economy")]
    public float minWeight = 1.0f;
    public float maxWeight = 5.0f;
    public int basePrice = 100;
}