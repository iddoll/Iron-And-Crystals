using UnityEngine;

public class OreBlock : MonoBehaviour
{
    public Sprite[] damageStages;
    // ТУТ БУДУТЬ ПРЕФАБИ ОБ'ЄКТІВ, ЩО ВИПАДАЮТЬ У СВІТІ (з PickableItem та ItemHolder)
    public GameObject[] orePrefabs; // Сюди перетягуєте Purple_fifth.prefab, Red_fifth.prefab тощо
    public int[] oreAmounts; // Кількість інстанцій кожного префабу
    public int clicksPerDamage = 3;

    private int currentStage = 0;
    private int clickCount = 0;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Mine()
    {
        Debug.Log("⚒ Mine() викликано");
        clickCount++;
        if (clickCount >= clicksPerDamage)
        {
            TakeDamage();
            clickCount = 0;
        }
    }

    private void TakeDamage()
    {
        Debug.Log("💥 TakeDamage() — стадія " + currentStage);
        if (currentStage < damageStages.Length)
        {
            spriteRenderer.sprite = damageStages[currentStage];
            // Тут просто викликаємо DropOre для поточної стадії
            DropOre(oreAmounts.Length > currentStage ? oreAmounts[currentStage] : 1);
            currentStage++;
        }
        else
        {
            // Фінальний дроп і знищення блоку руди
            DropOre(3); // або більша фінальна кількість
            Destroy(gameObject);
        }
    }

    private void DropOre(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // Обираємо випадковий префаб з масиву
            GameObject selectedOrePrefab = orePrefabs[Random.Range(0, orePrefabs.Length)];

            Vector2 dropPos = (Vector2)transform.position + new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.1f, 0.1f));
            
            // Інстанціюємо префаб, який уже має PickableItem та ItemHolder
            GameObject spawnedOre = Instantiate(selectedOrePrefab, dropPos, Quaternion.identity);

            ItemHolder holder = spawnedOre.GetComponent<ItemHolder>();
            if (holder == null)
            {
                Debug.LogError($"Spawned ore {spawnedOre.name} is missing an ItemHolder component!");
            }
        }
    }
}