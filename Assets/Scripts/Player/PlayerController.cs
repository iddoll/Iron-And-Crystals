    using UnityEngine;
    using System.Collections;
    using UnityEngine.EventSystems;
    using Cinemachine;

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

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            impulseSource = GetComponent<CinemachineImpulseSource>();
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
