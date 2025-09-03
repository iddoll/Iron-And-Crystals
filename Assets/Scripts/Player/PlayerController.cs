using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems; // 🔧 Додано для використання EventSystem

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;
    
    [Header("Player Stats")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float moveSpeed = 2f;
    public float jumpForce = 10f;
   
    private PlayerHealthUI healthUI;
    
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
    private Item currentEquippedItem;

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
    
    [Header("Bow Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float bowCooldown = 0.5f;

   
    private float lastShotTime;
    
    private Item pendingItem; // зберігає предмет, який чекає на еквіп
    
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
        currentHealth = maxHealth;
        
        healthUI = FindObjectOfType<PlayerHealthUI>();
        if (healthUI != null)
        {
            healthUI.InitHearts(20);
            healthUI.UpdateHearts((int)currentHealth, (int)maxHealth);
        }
        impulseSource = GetComponent<Cinemachine.CinemachineImpulseSource>();
    }

    private void Update()
    {
        if (ShouldBlockInput())
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetBool("isMooving", false);
            return;
        }

        // 🔒 Якщо курсор над UI → теж блокуємо
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        float moveInput = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        animator.SetBool("isMooving", moveInput != 0);
        
        if (moveInput > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0)
            transform.localScale = new Vector3(-1, 1, 1);
        
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (Input.GetMouseButtonDown(0) && !isMining && !isAttacking && canAttack)
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
                    StartCoroutine(ResetAttackAnimation());
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
                        StartCoroutine(ResetMiningAnimation());
                    }
                }
                else if (currentEquippedItem.itemType == ItemType.Bow)
                {
                    if (Time.time >= lastShotTime + bowCooldown)
                    {
                        isShooting = true;
                        animator.SetBool("isShooting", true);
                        lastShotTime = Time.time;
                    }
                }
            }
        }
    }

    private bool ShouldBlockInput()
    {
        if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.IsInventoryOpen())
            return true;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return true;

        return false;
    }

    
    // Корутина для списа
    private IEnumerator ResetSpearAttackAnimation()
    {
        animator.SetBool("isAttacking", true);
        lanceAttackZone.Activate();

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(stateInfo.length);

        isAttacking = false;
        lanceAttackZone.Deactivate();
        animator.Play("Player_Lance_Idle_Anim");
        animator.SetBool("isAttacking", false);
    }

    private IEnumerator ResetMiningAnimation()
    {
        float timer = 0f;
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Ore_Mining_Anim") && timer < 1.0f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Ore_Mining_Anim"))
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(stateInfo.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            Debug.LogWarning("Mining animation state was not entered correctly.");
        }

        animator.SetBool("isMining", false);
        isMining = false;

        if (pendingItem != null)
        {
            DoEquip(pendingItem);
            pendingItem = null;
        }
    }


    // Корутина для меча/сокири
    private IEnumerator ResetAttackAnimation()
    {
        animator.SetBool("isAttacking", true);

        AttackZone currentAttackZone = GetCurrentAttackZone();
        if (currentAttackZone != null)
        {
            currentAttackZone.Activate();

            // Чекаємо поки реально увійде в анімацію атаки
            float timer = 0f;
            while (!animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack") && timer < 1f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // Чекаємо закінчення самої анімації
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(stateInfo.length);

            currentAttackZone.Deactivate();
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            Debug.LogWarning("Attack zone not found.");
        }

        animator.SetBool("isAttacking", false);
        isAttacking = false;
    }
    
    private AttackZone GetCurrentAttackZone()
    {
        if (currentEquippedItem == null) return null;
        if (currentEquippedItem.itemType == ItemType.Sword) return swordAttackZone;
        if (currentEquippedItem.itemType == ItemType.Axe) return axeAttackZone;
        return null;
    }
    
    // Публічний метод для нанесення шкоди, який викликається AttackZone
    public void DealDamageToEnemy(EnemyBase enemy)
    {
        if (currentEquippedItem != null && enemy != null)
        {
            enemy.TakeDamage(currentEquippedItem.damage);
        }
    }
// Викликається з Animation Event
    public void AttackStart()
    {
        AttackZone currentAttackZone = GetCurrentAttackZone();
        if (currentAttackZone != null)
        {
            currentAttackZone.Activate();
            Debug.Log("AttackStart → зона атаки активована");
        }
    }

    // Викликається з Animation Event
    public void AttackEnd()
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
            DoEquip(pendingItem);
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
        if (arrowPrefab == null || firePoint == null) return;

        GameObject arrowObj = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
        Arrow arrow = arrowObj.GetComponent<Arrow>();

        bool facingRight = transform.localScale.x > 0;
        arrow.Shoot(facingRight);
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
            // Якщо йде анімація → запам’ятовуємо
            pendingItem = item;
            return;
        }

        DoEquip(item);
        
        if (heldObject != null)
        {
            UnequipItem();
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

            // 🔧 Підміняємо анімації
            if (item.overrideController != null)
            {
                animator.runtimeAnimatorController = item.overrideController;
            }
        }
        else
        {
            currentEquippedItem = null;
            SetCurrentTool("None");
            animator.runtimeAnimatorController = null; // можна поставити базовий
        }
    }

    private void DoEquip(Item item)
    {
        if (heldObject != null)
        {
            UnequipItem();
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

            if (item.overrideController != null)
            {
                animator.runtimeAnimatorController = item.overrideController;
            }
        }
        else
        {
            currentEquippedItem = null;
            SetCurrentTool("None");
            animator.runtimeAnimatorController = null;
        }
    }

    public void UnequipItem()
    {
        if (heldObject != null)
        {
            Destroy(heldObject);
            heldObject = null;
            SetCurrentTool("None");
            currentEquippedItem = null;
            Debug.Log("Предмет успішно де-екіпіровано.");
        }
    }

    public void DropItemFromInventory(Item itemToDrop)
    {
        if (itemToDrop == null || itemToDrop.worldPrefab == null)
        {
            Debug.LogWarning($"Неможливо викинути {itemToDrop?.name ?? "null"}: предмет null або немає worldPrefab.");
            return;
        }

        // --- якщо цей предмет у активному слоті ---
        if (InventorySystem.Instance.GetActiveSlot() != null &&
            InventorySystem.Instance.GetActiveSlot().GetItem() == itemToDrop)
        {
            UnequipItem(); // видаляє з holdPoint + скидає tool
            Debug.Log($"[PlayerController] {itemToDrop.itemName} був у активному слоті → де-екіпіровано при дропі.");
        }


        // --- спавнимо предмет у світі ---
        Vector3 dropPosition = transform.position + (Vector3)(transform.localScale.x > 0 ? Vector2.right : Vector2.left) * 0.5f;

        GameObject droppedWorldObject = Instantiate(itemToDrop.worldPrefab, dropPosition, Quaternion.identity);
        droppedWorldObject.transform.parent = null;

        float randomZRotation = Random.Range(-25f, 25f);
        droppedWorldObject.transform.rotation = Quaternion.Euler(0, 0, randomZRotation);

        Rigidbody2D rbDropped = droppedWorldObject.GetComponent<Rigidbody2D>();
        if (rbDropped != null)
        {
            rbDropped.simulated = true;
            rbDropped.isKinematic = false;
            float direction = transform.localScale.x > 0 ? 1f : -1f;
            rbDropped.AddForce(new Vector2(direction, 0.5f) * 3f, ForceMode2D.Impulse);
            rbDropped.AddTorque(Random.Range(-5f, 5f), ForceMode2D.Impulse);
        }

        // --- видаляємо з інвентаря ---
        InventorySystem.Instance.RemoveItem(itemToDrop);

        Debug.Log($"Викинуто {itemToDrop.itemName} з інвентаря у світ.");
    }


    public bool IsHolding(Item item)
    {
        if (heldObject == null || item == null) return false;
        return currentTool == item.name;
    }
    
    public void TakeDamage(float amount)
    {
        if (!canTakeDamage) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"Гравець отримав {amount} урону. Поточне HP: {currentHealth}");

        if (healthUI != null)
            healthUI.UpdateHearts((int)currentHealth, (int)maxHealth);

        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(); // Генеруємо імпульс, який трясе камеру
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
}