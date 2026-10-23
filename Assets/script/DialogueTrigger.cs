using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Pake input system baru

// ========================================================
// STURKTUR DATA DIALOG (Jangan Dihapus, Ini Biar Gak Error)
// ========================================================
[System.Serializable]
public class DialogueCharacter
{
    public string name;
    public Sprite icon;
}

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;
    [TextArea(3, 10)]
    public string line;
}

[System.Serializable]
public class Dialogue
{
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
}

// ========================================================
// LOGIKA UTAMA TRIGGER (Nempel di Katak Ijo)
// ========================================================
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Content")]
    public Dialogue dialogue;

    private bool isPlayerNearby = false;

    void Update()
    {
        // Cek input tombol E pas player deket pake Input System Baru
        if (isPlayerNearby && Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (!DialogueManager.Instance.isDialogueActive)
                {
                    TriggerDialogue();
                }
            }
        }
    }

    public void TriggerDialogue()
    {
        DialogueManager.Instance.StartDialogue(dialogue);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            Debug.Log("Player mendekat. Tekan E untuk bicara.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
        }
    }
}