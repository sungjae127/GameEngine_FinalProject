using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private GameObject attackHitbox;

    [Header("Player Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private float invincibilityDuration = 1f;
    [SerializeField] private float blinkSpeed = 10f;

    private Vector2 moveInput;
    private float invincibilityTimer = 0f;
    private bool isSprinting;
    private float lastAttackTime;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Keyboard keyboard;
    private Mouse mouse;

    // Track facing direction
    private bool facingRight = true;

    // Track enemies in contact
    private HashSet<Collider2D> enemiesInContact = new HashSet<Collider2D>();

    private void Awake()
    {
        // Get required components
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Get input devices
        keyboard = Keyboard.current;
        mouse = Mouse.current;

        // Initialize health
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (keyboard == null || mouse == null)
        {
            Debug.LogWarning("Keyboard or Mouse not detected!");
            return;
        }

        // Update invincibility timer and blink effect
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;

            // Blink effect during invincibility
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
        else
        {
            // Reset to full visibility when invincibility ends
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }

        // Check for damage from enemies in contact (경직 상태가 아닌 적만)
        foreach (Collider2D enemyCollider in enemiesInContact)
        {
            if (enemyCollider != null)
            {
                Enemy enemy = enemyCollider.GetComponent<Enemy>();
                if (enemy != null && !enemy.IsStaggeredOrDead())
                {
                    TakeDamage(10);
                    break; // 한 번만 대미지 받음
                }
            }
        }

        HandleInput();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleInput()
    {
        // Read WASD movement input
        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.wKey.isPressed) vertical += 1f;
        if (keyboard.sKey.isPressed) vertical -= 1f;
        if (keyboard.aKey.isPressed) horizontal -= 1f;
        if (keyboard.dKey.isPressed) horizontal += 1f;

        moveInput = new Vector2(horizontal, vertical).normalized;

        // Check sprint input (Left Shift)
        isSprinting = keyboard.leftShiftKey.isPressed;

        // Check attack input (Left Mouse Button)
        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryAttack();
        }
    }

    private void HandleMovement()
    {
        // Calculate current speed
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // Calculate velocity
        Vector2 velocity = moveInput * currentSpeed;

        // Apply movement using Rigidbody2D
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
        else
        {
            // Fallback: use Transform if no Rigidbody2D
            transform.Translate(velocity * Time.fixedDeltaTime, Space.World);
        }

        // Update animator based on movement
        UpdateAnimator();

        // Handle sprite flipping based on horizontal movement
        HandleSpriteFlip();
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        // Calculate movement speed for animator
        float speed = moveInput.magnitude;

        // Update MoveSpeed parameter (this matches your animator parameter)
        animator.SetFloat("MoveSpeed", speed);

        // Optional: Update IsSprinting if you add this parameter later
        // animator.SetBool("IsSprinting", isSprinting);
    }

    private void HandleSpriteFlip()
    {
        // Only flip based on horizontal movement
        if (moveInput.x > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput.x < 0 && facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;

        // Flip using SpriteRenderer instead of Transform.localScale
        // This prevents the "squishing" effect you were seeing
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }

        // Flip the attack hitbox position
        if (attackHitbox != null)
        {
            Vector3 hitboxPos = attackHitbox.transform.localPosition;
            hitboxPos.x = -hitboxPos.x; // Flip X position
            attackHitbox.transform.localPosition = hitboxPos;
        }
    }

    private void TryAttack()
    {
        // Check cooldown
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        PerformAttack();
    }

    private void PerformAttack()
    {
        Debug.Log("Attack performed!");
        lastAttackTime = Time.time;

        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    // Animation Event functions - called from PlayerAttack animation
    public void ActivateHitbox()
    {
        if (attackHitbox != null)
            attackHitbox.SetActive(true);
    }

    public void DeactivateHitbox()
    {
        if (attackHitbox != null)
            attackHitbox.SetActive(false);
    }

    // Track when enemies enter/exit collision with player's hurtbox only
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only add enemies that touch the player's main body collider
        if (other.CompareTag("Enemy"))
        {
            enemiesInContact.Add(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInContact.Remove(other);
        }
    }

    public void TakeDamage(int damage)
    {
        // Check if player is invincible
        if (invincibilityTimer > 0)
        {
            Debug.Log("Player is invincible!");
            return;
        }

        currentHealth -= damage;
        Debug.Log($"Player took {damage} damage! Current health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Start invincibility period with blink effect
            invincibilityTimer = invincibilityDuration;
        }
    }

    private void Die()
    {
        Debug.Log("Player died!");
        currentHealth = 0;

        // Stop all movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Reset movement input
        moveInput = Vector2.zero;

        // Trigger death animation
        if (animator != null)
        {
            animator.SetTrigger("Die");
            animator.SetFloat("MoveSpeed", 0);
        }

        // Disable player controls
        enabled = false;

        // Optional: Restart level after delay
        // Invoke(nameof(RestartLevel), 2f);
    }

    // Optional: Add this if you want to restart the level
    // private void RestartLevel()
    // {
    //     UnityEngine.SceneManagement.SceneManager.LoadScene(
    //         UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
    //     );
    // }

    // Optional: Visualize movement in editor
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && moveInput.magnitude > 0.1f)
        {
            Gizmos.color = Color.blue;
            Vector2 direction = moveInput;
            Gizmos.DrawRay(transform.position, direction * 2f);
        }
    }
}
