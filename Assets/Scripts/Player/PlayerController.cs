using UnityEngine;
using System.Collections;

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

    public float attackRadius = 1.5f;
    public float attackDamage = 25f;
    public LayerMask enemyLayer;
    private bool isAttacking = false; // Використовуємо цю змінну для контролю стану
    private bool canAttack = true;
    public float attackCooldown = 2f;
    
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
            healthUI.InitHearts(20); // 20 секцій
            healthUI.UpdateHearts((int)currentHealth, (int)maxHealth);
        }
    }

    private void Update()
    {
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

        // Атака
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
            }
        }
    }

    // Корутіна для списа
    private IEnumerator ResetSpearAttackAnimation()
    {
        animator.SetBool("isAttacking", true);
        // Очікуємо 0.1 секунди, щоб анімація точно почалась.
        yield return new WaitForSeconds(0.1f); 
    
        // Наносимо шкоду ворогам під час анімації.
        Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(transform.position, attackRadius, enemyLayer);
        foreach (Collider2D enemyCol in enemiesHit)
        {
            EnemyBase enemy = enemyCol.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(currentEquippedItem.damage);
                Debug.Log($"🏹 Вдарили списом {enemy.enemyName} на {currentEquippedItem.damage} урону!");
            }
        }

        // Очікуємо завершення анімації (або її тривалості).
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        // Використовуємо `stateInfo.length` для точної тривалості анімації.
        yield return new WaitForSeconds(stateInfo.length);

        isAttacking = false;
        // Повертаємось в Idle для списа.
        animator.Play("Player_Lance_Idle_Anim");
        animator.SetBool("isAttacking", false);
    }

    private IEnumerator ResetMiningAnimation()
    {
        animator.SetBool("isMining", true);
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
    }

    private IEnumerator ResetAttackAnimation()
    {
        animator.SetBool("isAttacking", true);
        float timer = 0f;
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Attacking_Anim") && timer < 1.0f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Attacking_Anim"))
        {
            Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(transform.position, attackRadius, enemyLayer);
            foreach (Collider2D enemyCol in enemiesHit)
            {
                EnemyBase enemy = enemyCol.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(currentEquippedItem.damage);
                    Debug.Log($"🗡️ Вдарили {enemy.enemyName} на {currentEquippedItem.damage} урону!");
                }
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(stateInfo.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            Debug.LogWarning("Attack animation state was not entered correctly.");
        }

        animator.SetBool("isAttacking", false);
        isAttacking = false;
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

    public void EquipItem(Item item)
    {
        Debug.Log($"[PlayerController] EquipItem викликано для {item?.name ?? "null Item"}");

        if (heldObject != null)
        {
            UnequipItem();
        }

        if (item != null && item.equippedPrefab != null )
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

            // ✅ Оновлюємо стан аніматора
            animator.SetBool("hasLance", item.itemType == ItemType.Lance);

            Debug.Log("Спроба екіпірувати: " + item.name);
        }
        else
        {
            currentEquippedItem = null;
            SetCurrentTool("None");
            animator.SetBool("hasLance", false); // При знятті зброї
            Debug.Log("Скинуто поточний інструмент.");
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
        InventorySystem.Instance.RemoveItem(itemToDrop);
        Debug.Log($"Викинуто {itemToDrop.itemName} з інвентаря та у світ.");
    }

    public bool IsHolding(Item item)
    {
        if (heldObject == null || item == null) return false;

        if (currentTool == item.name) return true;

        return false;
    }
    
    public void TakeDamage(float amount)
    {
        if (!canTakeDamage) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"Гравець отримав {amount} урону. Поточне HP: {currentHealth}");

        if (healthUI != null)
            healthUI.UpdateHearts((int)currentHealth, (int)maxHealth);

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

        // Відключаємо можливість рухатись, стрибати та атакувати.
        // Просто відключаємо сам компонент PlayerController.
        this.enabled = false;

        // Вимикаємо аніматор (опціонально), щоб персонаж не продовжував рухатись.
        if (animator != null)
        {
            animator.enabled = false;
        }
        
        // Деактивуємо об'єкт гравця через кілька секунд (опціонально)
        // або показуємо екран "Game Over".
        // Наприклад, можна викликати корутину, яка почекає 3 секунди, а потім перезапустить сцену.
        // StartCoroutine(RestartLevelAfterDelay(3f));
    }
    
}
