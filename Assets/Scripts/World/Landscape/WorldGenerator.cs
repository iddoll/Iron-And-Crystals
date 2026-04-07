using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;

    [Header("Tiles")]
    public TileBase grassTopTile;
    public TileBase dirtTile;
    public TileBase oreTile;

    [Header("Generation Settings")]
    public int width = 100;
    public int maxHeight = 30;
    public int minHeight = 10;
    public float terrainScale = 20f; // Плавність пагорбів
    
    [Header("Ore Settings")]
    [Range(0, 100)] public float oreChance = 5f; // Шанс появи руди (у %)
    public int oreStartDepth = 5; // На якій глибині починає з'являтися руда

    [Header("Seed")]
    public float seed;

    public static WorldGenerator Instance;
    void Awake() { Instance = this; }
    
    void Start()
    {
        if (seed == 0) seed = Random.Range(-100000f, 100000f);
        GenerateWorld();
        PlacePlayerOnSurface();
    }

    public void GenerateWorld()
    {
        groundTilemap.ClearAllTiles();

        for (int x = 0; x < width; x++)
        {
            // 1. Визначаємо висоту поверхні для цього X
            float perlinValue = Mathf.PerlinNoise((x + seed) / terrainScale, seed * 0.5f);
            int columnHeight = Mathf.RoundToInt(perlinValue * (maxHeight - minHeight) + minHeight);

            for (int y = 0; y < columnHeight; y++)
            {
                TileBase tileToPlace;

                // 2. Логіка вибору тайла залежно від висоти (Y)
                if (y == columnHeight - 1)
                {
                    // Найвищий блок — завжди з травою зверху
                    tileToPlace = grassTopTile;
                }
                else
                {
                    // Внутрішні блоки
                    int depth = columnHeight - 1 - y;

                    // Перевірка на генерацію руди (тільки на певній глибині)
                    if (depth >= oreStartDepth && Random.Range(0f, 100f) < oreChance)
                    {
                        tileToPlace = oreTile;
                    }
                    else
                    {
                        tileToPlace = dirtTile;
                    }
                }

                groundTilemap.SetTile(new Vector3Int(x, y, 0), tileToPlace);
            }
        }
    }
    
    public void PlacePlayerOnSurface()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            int startX = width / 2; // Спавнимо в центрі карти
        
            // Шукаємо найвищий блок у цьому стовпці
            for (int y = maxHeight; y >= 0; y--)
            {
                if (groundTilemap.HasTile(new Vector3Int(startX, y, 0)))
                {
                    // Ставимо гравця на 2 блоки вище знайденого тайла
                    // Множимо на CellSize, якщо він у тебе не 1x1
                    float cellSize = groundTilemap.layoutGrid.cellSize.x;
                    player.transform.position = new Vector3(startX * cellSize + (cellSize/2), (y + 2) * cellSize, 0);
                    break;
                }
            }
        }
    }
}