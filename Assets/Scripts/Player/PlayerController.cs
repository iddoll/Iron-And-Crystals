    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine.EventSystems;
    using Cinemachine;
    using UnityEngine.Tilemaps;
    
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance;

        [Header("Animation Controllers")]
        public RuntimeAnimatorController BaseAnimatorController;

        [Header("Player Stats")]
        public float maxHealth = 100f;
        private float currentHealth;
        public float damageCooldown = 1f;
        private bool canTakeDamage = true;

        [Header("Mining Settings")]
        public float miningRadius = 2f;
        private bool isMining = false;

        [Header("Equipment References")]
        public Transform holdPoint;
        public Transform shieldPoint;
        public Item currentEquippedItem;
        private GameObject heldObject;
        public GameObject heldShieldObject;
        private Item pendingItem;

        // Силтові посилання
        private Rigidbody2D rb;
        private Animator animator;
        private PlayerMovement movement;
        private PlayerCombat combat;
        private PlayerHealthUI healthUI;
        public CinemachineImpulseSource impulseSource;

        private UnityEngine.Tilemaps.Tilemap groundTilemap;
        private Dictionary<Vector3Int, int> tileHealthMap = new Dictionary<Vector3Int, int>();
        public int hitsToDestroyDirt = 3; // Скільки ударів потрібно для руйнування
        public OreBlock oreDataTemplate;
        
        [Header("Mining Visuals")]
        public Tilemap cracksTilemap; // Перетягни сюди новий CracksTilemap
        public TileBase[] oreDamageTiles; // Сюди перетягни тайли тріщин (3-4 стадії)
        private Dictionary<Vector3Int, int> tileMaxHealthMap = new Dictionary<Vector3Int, int>();
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            impulseSource = GetComponent<CinemachineImpulseSource>();
            groundTilemap = GameObject.FindGameObjectWithTag("Ground")?.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        }

        void Start()
        {
            currentHealth = maxHealth;
            healthUI = FindObjectOfType<PlayerHealthUI>();
            if (healthUI != null)
            {
                healthUI.InitHearts(20);
                healthUI.UpdateHearts((int)currentHealth, (int)maxHealth);
            }
        }

        void Update()
        {
            if (ShouldBlockInput())
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                animator.SetBool("isMooving", false);
                return;
            }

            bool isBlocking = ShieldController.Instance != null && ShieldController.Instance.IsBlocking;

            // --- РУХ ---
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            bool run = Input.GetKey(KeyCode.LeftShift) && !isBlocking;
            bool crouch = Input.GetKey(KeyCode.LeftControl) && !isBlocking;

            float speedMod = isBlocking ? ShieldController.Instance.blockMoveMultiplier : 1f;
            
            movement.HandleMovement(x, run, crouch, speedMod);
            if (!isBlocking) movement.HandleClimbing(y);

            if (Input.GetKeyDown(KeyCode.Space) && !isBlocking) movement.Jump();

            // --- БОЙОВА СИСТЕМА ---
            if (!isBlocking && !isMining)
            {
                // Якщо в руках лук — викликаємо нову логіку натягування в Update
                if (currentEquippedItem != null && currentEquippedItem.itemType == ItemType.Bow)
                {
                    combat.HandleBowCharging();
                }
                // Для іншої зброї залишаємо старий клік
                else if (Input.GetMouseButtonDown(0) && !combat.IsAttacking && combat.CanAttack)
                {
                    if (currentEquippedItem != null && currentEquippedItem.itemType == ItemType.Pickaxe)
                        TryMine();
                    else
                        combat.HandleAttack(currentEquippedItem);
                }
            }
            UpdateAnimatorParams(x);
        }

        private void UpdateAnimatorParams(float x)
        {
            animator.SetBool("isMooving", x != 0);
            animator.SetBool("isGrounded", movement.IsGrounded);
            animator.SetBool("isClimbing", movement.IsClimbing && movement.Velocity.y != 0);
            animator.SetBool("isClimbIdle", movement.IsClimbing && movement.Velocity.y == 0);
            animator.SetBool("isFalling", movement.Velocity.y < -0.1f && !movement.IsGrounded && !movement.IsClimbing);
            animator.SetBool("isRunning", Input.GetKey(KeyCode.LeftShift));
            animator.SetBool("isCrouching", Input.GetKey(KeyCode.LeftControl));
            animator.SetFloat("yVelocity", movement.Velocity.y);
        }

        private void TryMine()
        {
            OreBlock targetOre = FindNearestOre();
            if (targetOre != null)
            {
                isMining = true;
                animator.SetBool("isMining", true);
                targetOre.Mine();
                return;
            }

            if (groundTilemap != null)
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3Int cellPosition = groundTilemap.WorldToCell(mouseWorldPos);

                if (Vector2.Distance(transform.position, mouseWorldPos) <= miningRadius && groundTilemap.HasTile(cellPosition))
                {
                    isMining = true;
                    animator.SetBool("isMining", true);

                    TileBase clickedTile = groundTilemap.GetTile(cellPosition);
            
                    // 1. Визначаємо, чи це руда
                    bool isOre = (clickedTile == WorldGenerator.Instance.oreTile || IsDamageStageTile(clickedTile));

                    if (!tileHealthMap.ContainsKey(cellPosition))
                    {
                        int hp = isOre ? 5 : hitsToDestroyDirt; 
                        tileHealthMap[cellPosition] = hp;
                        tileMaxHealthMap[cellPosition] = hp;
                    }

                    tileHealthMap[cellPosition] -= 1;

                    // 2. ВІЗУАЛІЗАЦІЯ
                    if (isOre)
                    {
                        // Руда замінюється на "биті" тайли
                        UpdateOreVisualStage(cellPosition);
                    }
                    else
                    {
                        // ЗЕМЛЯ: Тут ми нічого не робимо. 
                        // Вона просто залишається суцільним блоком, поки HP не стане 0.
                    }

                    // 3. ЗНИЩЕННЯ
                    if (tileHealthMap[cellPosition] <= 0)
                    {
                        // Якщо раптом на цьому місці був тайл тріщин (про всяк випадок) — чистимо
                        if (cracksTilemap != null) cracksTilemap.SetTile(cellPosition, null);
                
                        DestroyTile(cellPosition, clickedTile);
                    }

                    Invoke("EndMining", 0.3f); 
                }
            }
        }

    private void UpdateOreVisualStage(Vector3Int pos)
    {
        if (oreDamageTiles == null || oreDamageTiles.Length == 0) return;

        int currentHP = tileHealthMap[pos];
        int maxHP = tileMaxHealthMap[pos];

        // Розраховуємо прогрес (від 1.0 до 0.0)
        float healthPercentage = (float)currentHP / maxHP;
    
        // Інвертуємо, щоб отримати індекс: чим менше HP, тим більший індекс
        // Якщо HP 100% -> index -1 (або 0), якщо HP 20% -> index високий
        int stageIndex = Mathf.FloorToInt((1f - healthPercentage) * oreDamageTiles.Length);
    
        // Обмежуємо, щоб не вийти за межі (остання стадія — перед самим знищенням)
        stageIndex = Mathf.Clamp(stageIndex, 0, oreDamageTiles.Length - 1);

        // Встановлюємо тайл
        groundTilemap.SetTile(pos, oreDamageTiles[stageIndex]);
    
        // ВАЖЛИВО: Оновлюємо конкретну клітинку, щоб зміни з'явилися візуально
        groundTilemap.RefreshTile(pos);
    }

    private void UpdateCracksVisual(Vector3Int cellPos)
    {
        if (cracksTilemap == null || oreDamageTiles == null || oreDamageTiles.Length == 0) return;

        float damageProgress = 1f - ((float)tileHealthMap[cellPos] / tileMaxHealthMap[cellPos]);
        int crackIndex = Mathf.FloorToInt(damageProgress * oreDamageTiles.Length);
        crackIndex = Mathf.Clamp(crackIndex, 0, oreDamageTiles.Length - 1);

        cracksTilemap.SetTile(cellPos, oreDamageTiles[crackIndex]);
    }

    private void DestroyTile(Vector3Int pos, TileBase tile)
    {
        groundTilemap.SetTile(pos, null);
        
        tileHealthMap.Remove(pos);
        tileMaxHealthMap.Remove(pos);

        Vector3 spawnPos = groundTilemap.GetCellCenterWorld(pos);

        // Якщо зламали руду (будь-яку стадію) - дропаємо кристали
        if (tile == WorldGenerator.Instance.oreTile || IsDamageStageTile(tile))
        {
            DropOreFromTile(spawnPos);
        }
        else
        {
            Debug.Log("Земля зламана");
        }
    }

    private bool IsDamageStageTile(TileBase tile)
    {
        if (oreDamageTiles == null) return false;
        foreach (var t in oreDamageTiles)
        {
            if (tile == t) return true;
        }
        return false;
    }

        private void DropOreFromTile(Vector3 position)
        {
            if (oreDataTemplate == null) return;

            // Використовуємо логіку з твого старого скрипта
            int amount = Random.Range(2, 5); // Кількість кристалів
            for (int i = 0; i < amount; i++)
            {
                GameObject selectedOrePrefab = oreDataTemplate.orePrefabs[Random.Range(0, oreDataTemplate.orePrefabs.Length)];
        
                Vector2 dropPos = (Vector2)position + new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f));
        
                // Створюємо кристал
                GameObject spawnedOre = Instantiate(selectedOrePrefab, dropPos, Quaternion.identity);

                // Додаємо невеликий імпульс, щоб кристали "розліталися" (якщо є Rigidbody2D)
                if (spawnedOre.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
                {
                    rb.AddForce(Random.insideUnitCircle * 2f, ForceMode2D.Impulse);
                }
            }
        }
        
        public void OnActionFinished() // Викликається з Animation Events через Combat
        {
            if (pendingItem != null)
            {
                EquipItem(pendingItem);
                pendingItem = null;
            }
        }

        public void EndMining()
        {
            animator.SetBool("isMining", false);
            isMining = false;
            OnActionFinished();
        }

        // Додай цей метод для викидання предметів (використовується в InventorySlot)
    public void DropItemFromInventory(Item item, int amount)
    {
        if (item.worldPrefab == null) return;
        
        for (int i = 0; i < amount; i++)
        {
            Vector2 dropPos = (Vector2)transform.position + Random.insideUnitCircle * 0.5f;
            Instantiate(item.worldPrefab, dropPos, Quaternion.identity);
        }
    }

    // Додай цей метод для щита (використовується в PlayerEquipment)
    public void SetupShield(Item item)
    {
        UnequipShield(); // Очищуємо старий, якщо був
        if (item.equippedPrefab != null && shieldPoint != null)
        {
            heldShieldObject = Instantiate(item.equippedPrefab, shieldPoint.position, Quaternion.identity, shieldPoint);
            heldShieldObject.transform.localPosition = Vector3.zero;
            heldShieldObject.transform.localRotation = Quaternion.identity;

            if (heldShieldObject.TryGetComponent<Rigidbody2D>(out Rigidbody2D rbShield))
            {
                rbShield.simulated = false;
            }
        }
    }

    // Онови EquipItem, щоб він не конфліктував з бронею
        public void EquipItem(Item item)
        {
            // 1. Якщо ми зараз атакуємо або копаємо — запам'ятовуємо предмет на потім
            if (combat.IsAttacking || isMining)
            {
                pendingItem = item;
                return;
            }

            // 2. Спочатку завжди знімаємо те, що вже в руках
            UnequipItem();

            // 3. Якщо новий предмет порожній — виходимо (UnequipItem вже все скинув до бази)
            if (item == null) return;

            // 4. ПУНКТ 3 ТВОГО ЗАПИТУ: 
            // Якщо це броня (шолом) або щит — ми НЕ беремо їх у праву руку як зброю.
            // Вони активуються ТІЛЬКИ через спеціальні слоти екіпірування.
            if (item.itemType == ItemType.Helmet || item.itemType == ItemType.Shield)
            {
                return; 
            }

            // 5. Встановлюємо новий предмет як активний
            currentEquippedItem = item;

            // 6. Спавним візуал предмета в руках
            if (item.equippedPrefab != null)
            {
                heldObject = Instantiate(item.equippedPrefab, holdPoint.position, Quaternion.identity, holdPoint);
                heldObject.transform.localPosition = Vector3.zero;
                heldObject.transform.localRotation = Quaternion.Euler(0, 0, -90);
            
                if (heldObject.TryGetComponent<Rigidbody2D>(out Rigidbody2D rbHeld))
                    rbHeld.simulated = false;
            }

            // 7. Змінюємо анімації під цей тип зброї (меч/сокира/лук)
            animator.runtimeAnimatorController = item.overrideController != null ? item.overrideController : BaseAnimatorController;
        }

        public void UnequipItem()
        {
            if (heldObject != null) Destroy(heldObject);
            heldObject = null;
            currentEquippedItem = null;
            animator.runtimeAnimatorController = BaseAnimatorController;
        }

        public void TakeDamage(float amount, DamageType type = DamageType.Melee)
        {
            if (!canTakeDamage) return;

            if (ShieldController.Instance != null)
                amount = ShieldController.Instance.ModifyDamage(amount, type);

            currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
            
            if (healthUI != null) healthUI.UpdateHearts((int)currentHealth, (int)maxHealth);
            if (impulseSource != null) impulseSource.GenerateImpulse();

            canTakeDamage = false;
            StartCoroutine(DamageCooldownCoroutine());

            if (currentHealth <= 0) Die();
        }

        // Додай це всередину класу PlayerController
        public void DealArmedDamageToEnemy(EnemyBase enemy)
        {
            if (currentEquippedItem != null)
            {
                enemy.TakeDamage(currentEquippedItem.damage, currentEquippedItem.damageType);
            }
        }

        public void DealUnarmedDamageToEnemy(EnemyBase enemy)
        {
            // combat — це посилання на PlayerCombat, воно у тебе вже є в Awake
            enemy.TakeDamage(combat.unarmedDamage, DamageType.Melee);
        }
        
        private IEnumerator DamageCooldownCoroutine()
        {
            yield return new WaitForSeconds(damageCooldown);
            canTakeDamage = true;
        }

        private void Die()
        {
            this.enabled = false;
            animator.enabled = false;
        }

        private OreBlock FindNearestOre()
        {
            OreBlock[] ores = FindObjectsOfType<OreBlock>();
            OreBlock nearest = null;
            float closestDist = miningRadius;
            foreach (var ore in ores)
            {
                float dist = Vector2.Distance(transform.position, ore.transform.position);
                if (dist < closestDist) { closestDist = dist; nearest = ore; }
            }
            return nearest;
        }

        public void OnStartBlocking()
        {
            combat.ResetCombatStates();
            if (isMining) EndMining();
            animator.SetBool("isBlocking", true);
        }

        public void OnStopBlocking() => animator.SetBool("isBlocking", false);

        private bool ShouldBlockInput() => InventoryUIManager.Instance != null && InventoryUIManager.Instance.IsInventoryOpen();

        public void UnequipShield()
        {
            if (heldShieldObject != null) Destroy(heldShieldObject);
            ShieldController.Instance?.SetEquipped(false);
        }
        
        // Перенаправлення Animation Events до Combat скрипта
        public void AE_AttackStart() => combat.AttackStart();
        public void AE_EndAttack() => combat.EndAttack();
    }
