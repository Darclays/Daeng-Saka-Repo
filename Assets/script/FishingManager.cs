using UnityEngine;
using System.Collections.Generic;

public enum FishingState { Idle, Casting, Waiting, Catching }

public class FishingManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject fishingUIPanel;
    public UnityEngine.UI.Slider progressSlider;
    public UnityEngine.UI.Slider fishSlider;
    public UnityEngine.UI.Slider catchZoneSlider;

    [Header("Catch Notification Setup")]
    public GameObject catchNotificationPanel; // Slot Prefab UI Notifikasi
    public Transform mainCanvas; // Drag GameObject 'canva' ke slot ini di Inspector
    [Header("State")]
    public FishingState currentState = FishingState.Idle;

    [Header("Pool Data Ikan")]
    public FishData[] availableFish;
    private FishData currentTargetFish;

    [Header("Water Setup")]
    public WaterType currentWaterType = WaterType.FreshWater; // WaterType bawaan dari FishData

    [Header("Timer Waiting")]
    private float waitTimer;

    [Header("Minigame Logic Variables")]
    public float progress = 50f;
    private float fishPosition = 50f;
    private float catchZonePosition = 50f;
    public float catchZoneSize = 20f;

    void Start()
    {
        if (progressSlider != null) { progressSlider.minValue = 0; progressSlider.maxValue = 100; }
        if (fishSlider != null) { fishSlider.minValue = 0; fishSlider.maxValue = 100; }
        if (catchZoneSlider != null) { catchZoneSlider.minValue = 0; catchZoneSlider.maxValue = 100; }

        if (fishingUIPanel != null) fishingUIPanel.SetActive(false);
    }

    void Update()
    {
        switch (currentState)
        {
            case FishingState.Idle:
                if (Input.GetKeyDown(KeyCode.Space)) StartCasting();
                break;

            case FishingState.Waiting:
                HandleWaiting();
                break;

            case FishingState.Catching:
                HandleCatching();
                break;
        }
    }

    void StartCasting()
    {
        currentState = FishingState.Casting;

        if (availableFish == null || availableFish.Length == 0)
        {
            Debug.LogError("Pool Data Ikan masih kosong di Inspector!");
            currentState = FishingState.Idle;
            return;
        }

        // --- FILTER IKAN SESUAI HABITAT DI AREA AIR ---
        List<FishData> filteredFish = new List<FishData>();
        foreach (FishData fish in availableFish)
        {
            if (fish != null && fish.habitat == currentWaterType)
            {
                filteredFish.Add(fish);
            }
        }

        if (filteredFish.Count == 0)
        {
            Debug.LogWarning($"Tidak ada ikan yang cocok untuk habitat {currentWaterType}!");
            currentState = FishingState.Idle;
            return;
        }

        // Pilih ikan acak KHUSUS dari hasil filter habitat
        currentTargetFish = filteredFish[Random.Range(0, filteredFish.Count)];
        waitTimer = Random.Range(2f, 5f);
        currentState = FishingState.Waiting;
    }

    void HandleWaiting()
    {
        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0)
        {
            progress = 30f;
            currentState = FishingState.Catching;

            if (fishingUIPanel != null) fishingUIPanel.SetActive(true);
        }
    }

    void HandleCatching()
    {
        fishPosition = Mathf.PingPong(Mathf.PerlinNoise(Time.time * currentTargetFish.movementSpeed, 0) * 100f, 100f);

        if (Input.GetKey(KeyCode.Space))
        {
            catchZonePosition += 60f * Time.deltaTime;
        }
        else
        {
            catchZonePosition -= 60f * Time.deltaTime;
        }
        catchZonePosition = Mathf.Clamp(catchZonePosition, 0f, 100f);

        bool isInside = Mathf.Abs(fishPosition - catchZonePosition) <= (catchZoneSize / 2f);

        if (isInside)
        {
            progress += currentTargetFish.catchSpeed * Time.deltaTime;
        }
        else
        {
            progress -= currentTargetFish.decaySpeed * Time.deltaTime;
        }

        progress = Mathf.Clamp(progress, 0f, 100f);

        if (fishSlider != null) fishSlider.value = fishPosition;
        if (catchZoneSlider != null) catchZoneSlider.value = catchZonePosition;
        if (progressSlider != null) progressSlider.value = progress;

        // --- KONDISI MENANG ---
        if (progress >= 100f)
        {
            // 1. Hitung Berat Acak
            float caughtWeight = Random.Range(currentTargetFish.minWeight, currentTargetFish.maxWeight);
            caughtWeight = Mathf.Round(caughtWeight * 100f) / 100f;

            // 2. Spawn Pop-up Notifikasi UI
          if (catchNotificationPanel != null)
{
    // 1. Wajib aktifkan panelnya agar muncul di layar
    catchNotificationPanel.SetActive(true);

    // 2. Ambil komponen script
    CatchNotification notification = catchNotificationPanel.GetComponent<CatchNotification>();

    if (notification != null)
    {
        notification.Setup(
            currentTargetFish.fishIcon,
            currentTargetFish.rarity,
            currentTargetFish.fishName,
            caughtWeight
        );
    }
    else
    {
        Debug.LogError("Komponen CatchNotification tidak ditemukan pada catchNotificationPanel!");
    }
}

// Panggil EndMinigame untuk menutup Reel UI
EndMinigame();
        }

        void EndMinigame()
    {
        currentState = FishingState.Idle;
        if (fishingUIPanel != null) fishingUIPanel.SetActive(false);
    }
}
    }

