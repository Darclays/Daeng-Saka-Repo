using UnityEngine;

public class WaterZone : MonoBehaviour
{
    [Header("Jenis Air")]
    public WaterType waterType;

    // Saat Player menyentuh/berdiri di area air ini
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FishingManager manager = FindFirstObjectByType<FishingManager>();
            if (manager != null)
            {
                manager.currentWaterType = waterType;
                Debug.Log("Player masuk ke area air: " + waterType);
            }
        }
    }
}
