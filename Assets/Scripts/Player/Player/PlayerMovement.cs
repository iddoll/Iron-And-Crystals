using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 2f;
    public float jumpForce = 10f;
    public float climbSpeed = 3f;
    public float runMultiplier = 1.8f;
    public float crouchMultiplier = 0.5f;

    [Header("Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private float defaultGravity;
    private bool isGrounded;
    private bool isClimbing;
    private bool isNearLadder;
    private float ladderCenterX;

    // Стан для аніматора
    public Vector2 Velocity => rb.linearVelocity;
    public bool IsGrounded => isGrounded;
    public bool IsClimbing => isClimbing;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravity = rb.gravityScale;
    }

    public void HandleMovement(float input, bool run, bool crouch, float speedModifier = 1f)
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        float currentSpeed = moveSpeed * speedModifier;
        if (run && !crouch) currentSpeed *= runMultiplier;
        if (crouch) currentSpeed *= crouchMultiplier;

        rb.linearVelocity = new Vector2(input * currentSpeed, rb.linearVelocity.y);

        // Поворот
        if (input > 0) transform.localScale = Vector3.one;
        else if (input < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    public void Jump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    public void HandleClimbing(float verticalInput)
    {
        if (isNearLadder && Mathf.Abs(verticalInput) > 0)
        {
            isClimbing = true;
            rb.gravityScale = 0;
        }

        if (isClimbing)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, verticalInput * climbSpeed);
        }
    }

    public void StopClimbing()
    {
        isClimbing = false;
        rb.gravityScale = defaultGravity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            isNearLadder = true;
            ladderCenterX = other.bounds.center.x;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            isNearLadder = false;
            StopClimbing();
        }
    }
}