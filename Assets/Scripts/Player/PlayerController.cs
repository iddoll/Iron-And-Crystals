using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    [Header("Animation Controllers")]
    public RuntimeAnimatorController BaseAnimatorController;
    [Header("Player Stats")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float moveSpeed = 2f;
    public float jumpForce = 10f;
    private float defaultGravityScale;
    
    [Header("Movement Modifiers")]
    public float runSpeedMultiplier = 1.8f;
    public float crouchSpeedMultiplier = 0.5f;
    private bool isRunning = false;
    private bool isCrouching = false;
    
    private PlayerHealthUI healthUI;

    [Header("Unarmed Attacks")]
    [SerializeField] private AnimationClip unarmedAttackLeft;
    [SerializeField] private AnimationClip unarmedAttackRight;
    public float unarmedDamage = 10f;

    [Header("Combat Settings")]
    public float damageCooldown = 1f;
    private bool canTakeDamage = true;

    private Rigidbody2D rb;
    private Animator animator;

    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;

    public Transform holdPoint;
    private GameObject heldObject;
    public Item currentEquippedItem;

    private string currentTool = "None";
    public float miningRadius = 2f;
    private bool isMining = false;

    private bool isAttacking = false;
    private bool canAttack = true;
    private bool isShooting = false;

    [Header("Attack Zones")]
    public AttackZone swordAttackZone;
    public AttackZone axeAttackZone;
    public AttackZone lanceAttackZone;
    public AttackZone PunchAttackZone;

    [Header("Bow Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float bowCooldown = 0.5f;
    private float lastShotTime;
    private Item pendingItem;
    
    [Header("Shield Settings")]
    public Transform shieldPoint; 
    public GameObject heldShieldObject;
    
    [Header("Climbing Settings")]
    public float climbSpeed = 3f;
    private bool isClimbing = false;
    private bool isNearLadder = false;
    private Collider2D currentLadderCollider;
    private float ladderCenterX;
    

    public Cinemachine.CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        defaultGravityScale = rb.gravityScale;
        currentHealth = maxHealth;

        healthUI = FindObjectOfType<PlayerHealthUI>();
        if (healthUI != null)
        {
            healthUI.InitHearts(20);
            healthUI.UpdateHearts((int)currentHealth, (int)maxHealth);
        }
        impulseSource = GetComponent<Cinemachine.CinemachineImpulseSource>();
    }

// У PlayerController.cs

    private void Update()
    {
        // 🛡️ Перевірка статусу блокування на початку Update 🛡️
        bool isBlocking = ShieldController.Instance != null && ShieldController.Instance.IsBlocking;
        
        if (ShouldBlockInput())
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetBool("isMooving", false);
            return;
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("yVelocity", rb.linearVelocity.y);

        float moveInput = Input.GetAxis("Horizontal");

// Перевіряємо присідання
        if (Input.GetKey(KeyCode.LeftControl))
        {
            isCrouching = true;
            isRunning = false; // не можна бігти, коли присідаєш
        }
        else
        {
            isCrouching = false;
            isRunning = Input.GetKey(KeyCode.LeftShift);
        }

        // 🛡️ Обмеження руху при блокуванні 🛡️
        if (isBlocking)
        {
            isRunning = false;
            isCrouching = false;
        }

// Встановлюємо швидкість
        float currentSpeed = moveSpeed;
        if (isRunning) currentSpeed *= runSpeedMultiplier;
        if (isCrouching) currentSpeed *= crouchSpeedMultiplier;
        
        // 🛡️ Застосовуємо множник швидкості блоку 🛡️
        if (isBlocking) currentSpeed *= ShieldController.Instance.blockMoveMultiplier;


        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);

// Анімація руху
        animator.SetBool("isMooving", moveInput != 0);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isCrouching", isCrouching);

// Поворот персонажа
        if (moveInput > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0) transform.localScale = new Vector3(-1, 1, 1);
        
        
        // отримуємо "сирий" вертикальний ввід (W/S або стрілки)
        float verticalRaw = Input.GetAxisRaw("Vertical");

// якщо поруч з драбиною і натиснуто вгору/вниз — починаємо лазити
        // 🛡️ Блок лазіння при блокуванні 🛡️
        if (isNearLadder && Mathf.Abs(verticalRaw) > 0f && !isBlocking)
        {
            if (!isClimbing)
            {
                // опціонально: при старті підйому «прилипаємо» по X до центру драбини
                if (currentLadderCollider != null)
                {
                    Vector3 p = transform.position;
                    p.x = ladderCenterX;
                    transform.position = p;
                }
                // скидаємо швидкість, щоб не «проштовхувало»
                rb.linearVelocity = Vector2.zero;
            }
            isClimbing = true;
        }
        else if (!isNearLadder || isBlocking) // Припиняємо лазіння, якщо почали блокувати
        {
            isClimbing = false;
        }

        // виконання лазіння
        if (isClimbing)
        {
            rb.gravityScale = 0f;
            float vertical = Input.GetAxisRaw("Vertical");

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, vertical * climbSpeed);

            if (vertical != 0)
            {
                animator.SetBool("isClimbing", true);   // рух по драбині
                animator.SetBool("isClimbIdle", false);
            }
            else
            {
                animator.SetBool("isClimbing", false);
                animator.SetBool("isClimbIdle", true);  // зависли на драбині
                rb.linearVelocity = new Vector2(0, 0);  // щоб не ковзав вниз
            }
        }
        else
        {
            // повертаємо початкову гравітацію
            rb.gravityScale = defaultGravityScale;
            animator.SetBool("isClimbing", false);
            animator.SetBool("isClimbIdle", false);
        }


        // 🛡️ Блок стрибка при блокуванні 🛡️
        if (!isBlocking && Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            animator.SetTrigger("Jump");
        }


        if (isClimbing || animator.GetBool("isClimbIdle"))
        {
            animator.SetBool("isFalling", false);
        }
        else
        {
            animator.SetBool("isFalling", rb.linearVelocity.y < -0.1f && !isGrounded);
        }

        // 🛡️ Блок атаки при блокуванні 🛡️
        if (!isBlocking && Input.GetMouseButtonDown(0) && !isMining && !isAttacking && canAttack)
        {
            if (currentEquippedItem != null)
            {
                if (currentEquippedItem.itemType == ItemType.Lance)
                {
                    isAttacking = true;
                    canAttack = false;
                    animator.Play("Player_Lance_Attack_Anim");
                    StartCoroutine(ResetSpearAttackAnimation());
                    StartCoroutine(AttackCooldownCoroutine(currentEquippedItem.attackCooldown));
                }
                else if (currentEquippedItem.itemType == ItemType.Sword || currentEquippedItem.itemType == ItemType.Axe)
                {
                    isAttacking = true;
                    canAttack = false;
                    animator.SetBool("isAttacking", true);
                    StartCoroutine(AttackCooldownCoroutine(currentEquippedItem.attackCooldown));
                }
                else if (currentEquippedItem.itemType == ItemType.Pickaxe)
                {
                    OreBlock targetOre = FindNearestOre();
                    if (targetOre != null)
                    {
                        isMining = true;
                        animator.SetBool("isMining", true);
                        targetOre.Mine();
                    }
                }
                else if (currentEquippedItem.itemType == ItemType.Bow)
                {
                    if (InventorySystem.Instance.HasItemOfType(ItemType.Arrow) && Time.time >= lastShotTime + bowCooldown)
                    {
                        isShooting = true;
                        animator.SetBool("isShooting", true);
                        lastShotTime = Time.time;
                    }
                    else
                    {
                        isShooting = false;
                        animator.SetBool("isShooting", false);
                    }
                }
                else
                {
                    // 🥊 Якщо предмет є, але він не зброя → б’ємо кулаком
                    DoUnarmedAttack();
                }
            }
            else
            {
                // 🥊 Unarmed attack
                DoUnarmedAttack();
            }
        }
    }

    private void DoUnarmedAttack()
    {
        isAttacking = true;
        canAttack = false;
        Debug.Log("🥊 Атака кулаками");

        if (Random.value > 0.5f)
        {
            animator.SetBool("PunchLeft", true);
            animator.SetBool("PunchRight", false);
        }
        else
        {
            animator.SetBool("PunchRight", true);
            animator.SetBool("PunchLeft", false);
        }

        AttackStart(); 
        StartCoroutine(AttackCooldownCoroutine(0.5f));
    }
    
    private bool ShouldBlockInput()
    {
        if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.IsInventoryOpen())
            return true;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return true;

        return false;
    }

    private IEnumerator ResetSpearAttackAnimation()
    {
        // Не потрібно викликати lanceAttackZone.Activate() тут, оскільки це робить AttackStart()
        animator.SetBool("isAttacking", true);
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(stateInfo.length);

        isAttacking = false;
        lanceAttackZone.Deactivate();
        animator.Play("Player_Lance_Idle_Anim");
        animator.SetBool("isAttacking", false);
    }

    public void EndMining()
    {
        animator.SetBool("isMining", false);
        isMining = false;

        if (pendingItem != null)
        {
            EquipItem(pendingItem);
            pendingItem = null;
        }
    }

    public void EndUnarmedAttack()
    {
        isAttacking = false;
        canAttack = true;

        animator.SetBool("PunchLeft", false);
        animator.SetBool("PunchRight", false);
        
        AttackZone currentAttackZone = GetCurrentAttackZone();
        if (currentAttackZone != null)
        {
            currentAttackZone.Deactivate();
            Debug.Log("EndUnarmedAttack → зона атаки деактивована");
        }
    }
    
    public void DealUnarmedDamageToEnemy(EnemyBase enemy)
    {
        if (enemy != null)
        {
            enemy.TakeDamage(unarmedDamage);
        }
    }

    private AttackZone GetCurrentAttackZone()
    {
        if (currentEquippedItem == null) return PunchAttackZone;
        if (currentEquippedItem.itemType == ItemType.Sword) return swordAttackZone;
        if (currentEquippedItem.itemType == ItemType.Axe) return axeAttackZone;
        if (currentEquippedItem.itemType == ItemType.Lance) return lanceAttackZone;
        return null;
    }

    public void DealArmedDamageToEnemy(EnemyBase enemy)
    {
        if (currentEquippedItem != null && enemy != null)
        {
            enemy.TakeDamage(currentEquippedItem.damage);
        }
    }

    public void AttackStart()
    {
        AttackZone currentAttackZone = GetCurrentAttackZone();
        if (currentAttackZone != null)
        {
            currentAttackZone.Activate();
            Debug.Log("AttackStart → зона атаки активована");
        }
    }

    public void EndAttack()
    {
        AttackZone currentAttackZone = GetCurrentAttackZone();
        if (currentAttackZone != null)
        {
            currentAttackZone.Deactivate();
        }

        animator.SetBool("isAttacking", false);
        isAttacking = false;

        if (pendingItem != null)
        {
            EquipItem(pendingItem);
            pendingItem = null;
        }
    }

    private IEnumerator AttackCooldownCoroutine(float cooldownTime)
    {
        yield return new WaitForSeconds(cooldownTime);
        canAttack = true;
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    public bool HasPickaxe() => currentTool == "Pickaxe";
    public bool HasSword() => currentTool == "Sword";
    public bool HasAxe() => currentTool == "Axe";
    public string GetCurrentTool() => currentTool;
    public void SetCurrentTool(string toolName)
    {
        Debug.Log("🔧 Встановлено інструмент: " + toolName);
        currentTool = toolName;
    }

    private OreBlock FindNearestOre()
    {
        GameObject[] ores = GameObject.FindGameObjectsWithTag("OreBlock");
        OreBlock nearest = null;
        float closestDist = miningRadius;

        foreach (GameObject ore in ores)
        {
            float dist = Vector2.Distance(transform.position, ore.transform.position);
            if (dist <= closestDist)
            {
                closestDist = dist;
                nearest = ore.GetComponent<OreBlock>();
            }
        }
        return nearest;
    }

    public void ShootArrow()
    {
        if (arrowPrefab == null || firePoint == null)
        {
            Debug.LogWarning("Arrow prefab or fire point not assigned!");
            return;
        }

        if (InventorySystem.Instance.RemoveItemByType(ItemType.Arrow, 1))
        {
            GameObject arrowObj = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
            Arrow arrow = arrowObj.GetComponent<Arrow>();
            bool facingRight = transform.localScale.x > 0;
            arrow.Shoot(facingRight);
        }
        else
        {
            Debug.Log("Немає стріл для пострілу!");
        }
        StopShooting();
    }

    public void StopShooting()
    {
        isShooting = false;
        animator.SetBool("isShooting", false);
    }

    public void EquipItem(Item item)
    {
        if (isAttacking || isMining)
        {
            pendingItem = item;
            return;
        }

        // 🛑 ВИДАЛЕНО ЛОГІКУ ЩИТА З EQUIPMENT ITEM. 
        // Тепер EquipItem обробляє ТІЛЬКИ зброю/інструменти (те, що береться в руки).
        // ЛОГІКА ЩИТА/БРОНІ ТЕПЕР ТІЛЬКИ ЧЕРЕЗ PlayerEquipment.cs
    
        // ⚔️ ЛОГІКА ЗБРОЇ/ІНСТРУМЕНТІВ ⚔️
    
        // Спочатку знімаємо попередній предмет з рук
        UnequipItem(); 

        // Перевірка: Якщо новий предмет є щитом або шоломом, ми нічого не беремо в руки і виходимо.
        if (item != null && (item.itemType == ItemType.Shield || item.itemType == ItemType.Helmet))
        {
            currentEquippedItem = null;
            SetCurrentTool("None");
            animator.runtimeAnimatorController = BaseAnimatorController;
            return; 
        }

        if (item != null && item.equippedPrefab != null)
        {
            currentEquippedItem = item;
            heldObject = Instantiate(item.equippedPrefab, holdPoint.position, Quaternion.identity, holdPoint);
            heldObject.transform.localPosition = Vector3.zero;
            heldObject.transform.localRotation = Quaternion.Euler(0, 0, -90);
            SetCurrentTool(item.name);
            Rigidbody2D rbHeld = heldObject.GetComponent<Rigidbody2D>();
            if (rbHeld != null)
            {
                rbHeld.simulated = false;
                rbHeld.isKinematic = true;
            }

            animator.runtimeAnimatorController = item.overrideController != null
                ? item.overrideController
                : BaseAnimatorController;
        }
        else
        {
            currentEquippedItem = null;
            SetCurrentTool("None");
            animator.runtimeAnimatorController = BaseAnimatorController;
        }
    }

    public void UnequipItem()
    {
        if (heldObject != null)
        {
            Destroy(heldObject);
            heldObject = null;
        }

        SetCurrentTool("None");
        currentEquippedItem = null;

        if (animator != null && BaseAnimatorController != null)
        {
            animator.runtimeAnimatorController = BaseAnimatorController;
        }
        Debug.Log("Предмет успішно де-екіпіровано.");
    }

    
    public void UnequipShield()
    {
        if (heldShieldObject != null)
        {
            Destroy(heldShieldObject);
            heldShieldObject = null;
        }

        ShieldController.Instance?.SetEquipped(false);
    }
    
    public void DropItemFromInventory(Item itemToDrop)
    {
        DropItemFromInventory(itemToDrop, 1);
    }

    public void DropItemFromInventory(Item itemToDrop, int amount)
    {
        if (itemToDrop == null || itemToDrop.worldPrefab == null)
        {
            Debug.LogWarning($"Неможливо викинути {itemToDrop?.name ?? "null"}: предмет null або немає worldPrefab.");
            return;
        }

        if (amount <= 0) return;

        Vector3 basePosition = transform.position + (Vector3)((transform.localScale.x > 0) ? Vector2.right : Vector2.left) * 0.5f;

        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPos = basePosition + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0f, 0.2f), 0f);

            GameObject droppedWorldObject = Instantiate(itemToDrop.worldPrefab, spawnPos, Quaternion.Euler(0, 0, Random.Range(-25f, 25f)));
            droppedWorldObject.transform.parent = null;

            Rigidbody2D rbDropped = droppedWorldObject.GetComponent<Rigidbody2D>();
            if (rbDropped != null)
            {
                rbDropped.simulated = true;
                rbDropped.isKinematic = false;
                float direction = transform.localScale.x > 0 ? 1f : -1f;
                rbDropped.AddForce(new Vector2(direction, 0.5f) * Random.Range(2f, 4f), ForceMode2D.Impulse);
                rbDropped.AddTorque(Random.Range(-5f, 5f), ForceMode2D.Impulse);
            }
        }

        bool ok = InventorySystem.Instance.RemoveItems(itemToDrop, amount);
        if (!ok)
        {
            Debug.LogWarning($"DropItemFromInventory: не вдалося видалити {amount}x {itemToDrop.itemName} з інвентаря.");
        }
        else
        {
            Debug.Log($"Викинуто {amount}x {itemToDrop.itemName} з інвентаря у світ.");
        }
    }

    public bool IsHolding(Item item)
    {
        if (heldObject == null || item == null) return false;
        return currentTool == item.name;
    }

    // Викликається, коли щит починає блокувати (ПКМ натиснуто)
    public void OnStartBlocking()
    {
        // Прериваємо поточні дії
        if (isAttacking)
        {
            // миттєво скидаємо атаку
            isAttacking = false;
            animator.SetBool("isAttacking", false);
            // Деактивувати зони атаки
            AttackZone az = GetCurrentAttackZone();
            if (az != null) az.Deactivate();
        }

        if (isMining)
        {
            isMining = false;
            animator.SetBool("isMining", false);
        }

        // Забороняємо нові атаки поки блокуємо
        canAttack = false;

        // Вмикаємо блок-анімацію
        if (animator != null)
            animator.SetBool("isBlocking", true);

        Debug.Log("Player: почав блокувати");
    }

// Викликається, коли щит перестає блокувати (ПКМ відпущено)
    public void OnStopBlocking()
    {
        // Дозволяємо атаки знову (якщо cooldown дозволяє)
        canAttack = true;

        if (animator != null)
            animator.SetBool("isBlocking", false);

        Debug.Log("Player: припинив блокувати");
    }

    public enum DamageType
    {
        Melee,
        Projectile,
        Explosion
    }

// Зворотна сумісність: старі виклики продовжать працювати як Melee
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, DamageType.Melee);
    }

// Нова версія з типом урону
    public void TakeDamage(float amount, DamageType type)
    {
        if (!canTakeDamage) return;

        // Нехай ShieldController модифікує урон (пасив/активний)
        if (ShieldController.Instance != null)
        {
            amount = ShieldController.Instance.ModifyDamage(amount, type);
        }

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"Гравець отримав {amount} урону. Поточне HP: {currentHealth}");
        if (healthUI != null)
            healthUI.UpdateHearts((int)currentHealth, (int)maxHealth);
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }
        canTakeDamage = false;
        StartCoroutine(DamageCooldownCoroutine());
        if (currentHealth <= 0)
        {
            Die();
        }
    }


    private IEnumerator DamageCooldownCoroutine()
    {
        yield return new WaitForSeconds(damageCooldown);
        canTakeDamage = true;
    }

    private void Die()
    {
        Debug.Log("Гравець помер.");
        this.enabled = false;
        if (animator != null)
        {
            animator.enabled = false;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            isNearLadder = true;
            currentLadderCollider = other;
            ladderCenterX = other.bounds.center.x;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            isNearLadder = false;
            isClimbing = false;
            currentLadderCollider = null;
        }
    }

}