using UnityEngine;

public class NPCSystem : MonoBehaviour
{
    bool playerInRange = false;
    public GameObject canva;
    public GameObject playerUI;
    public GameObject NPCUI
    ;

    // Update is called once per frame
    void Update()
    {
        if (playerInRange && !PlayerMovement.dialogueActive)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                playerUI.SetActive(false);
                PlayerMovement.dialogueActive = true;
                NPCUI.SetActive(true);
                
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape) && playerInRange)
        {
            LeaveOnEsc();
        }

    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Buyer"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        playerInRange = false;
    }

    public void LeaveOnEsc()
    {
        
        playerUI.SetActive(true);
        PlayerMovement.dialogueActive = false;
        NPCUI.SetActive(false);
        
    }
}
