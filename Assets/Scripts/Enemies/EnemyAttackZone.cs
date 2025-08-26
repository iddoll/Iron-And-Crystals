using UnityEngine;

public class EnemyAttackZone : MonoBehaviour
{
    [HideInInspector] public bool playerInZone;
    [HideInInspector] public Transform player;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            player = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            if (player == other.transform) player = null;
        }
    }
}