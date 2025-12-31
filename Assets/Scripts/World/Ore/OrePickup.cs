using UnityEngine;

public class OrePickup : MonoBehaviour
{
    public Item oreTemplate;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (oreTemplate != null)
            {
                Item pickedItem = Instantiate(oreTemplate);
                InventorySystem.Instance.AddItem(pickedItem);
            }
            Destroy(gameObject);
        }
    }
}