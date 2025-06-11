using UnityEngine;

public class FacePlayer : MonoBehaviour
{
    public Transform player;              // Assign the player or camera here
    public Animator animator;            // Assign the Animator component

    void Update()
    {
        if (player != null)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f; // Keeps rotation horizontal
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform == player && animator != null)
        {
            animator.SetTrigger("In");
        }
    }
}