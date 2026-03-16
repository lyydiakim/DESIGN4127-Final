using UnityEngine;

public class Wanderer : MonoBehaviour
{
    public enum WanderType
    {
        XYWander,
        FreeWander
    }

    [Header("Wander Settings")]
    [Tooltip("The type of wandering behavior to use")]
    public WanderType wanderType = WanderType.XYWander;

    [Tooltip("How fast the object moves")]
    public float moveSpeed = 2.0f;

    [Header("XY Wander Settings")]
    [Tooltip("Minimum time before changing direction (seconds)")]
    public float minDirectionChangeTime = 1.0f;

    [Tooltip("Maximum time before changing direction (seconds)")]
    public float maxDirectionChangeTime = 3.0f;

    [Header("Free Wander Settings")]
    [Tooltip("How sharply the object can turn (degrees per second)")]
    public float turnSpeed = 90.0f;

    [Tooltip("How frequently the object changes its turning direction (seconds)")]
    public float turnChangeFrequency = 1.0f;

    [Header("Boundaries (Optional)")]
    [Tooltip("If true, object will stay within defined boundaries")]
    public bool useBoundaries = false;

    [Tooltip("Minimum X position")]
    public float minX = -10f;

    [Tooltip("Maximum X position")]
    public float maxX = 10f;

    [Tooltip("Minimum Y position")]
    public float minY = -10f;

    [Tooltip("Maximum Y position")]
    public float maxY = 10f;

    [Tooltip("Leave over Time")]
    public bool leaveOverTime = false;
    public float timeToLeave = 5.0f;
    private float leaveCount = 0;

    // Private variables
    private Vector2 currentDirection;
    private float directionChangeTimer;
    private float turnChangeTimer;
    private float currentTurnRate;

    private bool facingRight = true;
    public GameObject objSprite;
    public bool flipSpriteOnMove = false;

    void Start()
    {
        // Initialize with a random direction
        ChooseNewDirection();

        // Set initial timers
        directionChangeTimer = Random.Range(minDirectionChangeTime, maxDirectionChangeTime);
        turnChangeTimer = turnChangeFrequency;

        // Set initial turn rate for Free Wander
        currentTurnRate = Random.Range(-turnSpeed, turnSpeed);
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState == GameState.Playing) //only update if playing
        { 

            if (wanderType == WanderType.XYWander)
            {
                HandleXYWander();
            } else // WanderType.FreeWander
            {
                HandleFreeWander();
            }

            // Move the object
            transform.position += (Vector3)(currentDirection * moveSpeed * Time.deltaTime);

            // Check boundaries if enabled
            if (useBoundaries)
            {
                EnforceBoundaries();
            }

            if (leaveOverTime)
            {
                leaveOverTimeCalc();
            }

            if (flipSpriteOnMove) FlipCharacter(currentDirection.x);
        }
    }

    public void FlipCharacter(float horizontalInput)
    {
        // Check if we need to flip
        if (horizontalInput > 0 && !facingRight)
        {
            Flip();
        } else if (horizontalInput < 0 && facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = objSprite.transform.localScale;
        scale.x *= -1; // Flip the x axis
        objSprite.transform.localScale = scale;
    }

    void leaveOverTimeCalc()
    {
            leaveCount += Time.deltaTime;
            if (leaveCount > timeToLeave)
            {
                Destroy(this.gameObject);
            }
    }

    void HandleXYWander()
    {
        // Count down the timer
        directionChangeTimer -= Time.deltaTime;

        // Change direction when timer runs out
        if (directionChangeTimer <= 0)
        {
            ChooseNewDirection();
            directionChangeTimer = Random.Range(minDirectionChangeTime, maxDirectionChangeTime);
        }
    }

    void HandleFreeWander()
    {
        // Count down the turn change timer
        turnChangeTimer -= Time.deltaTime;

        // Change turn rate when timer runs out
        if (turnChangeTimer <= 0)
        {
            currentTurnRate = Random.Range(-turnSpeed, turnSpeed);
            turnChangeTimer = turnChangeFrequency;
        }

        // Apply turn
        float turnAmount = currentTurnRate * Time.deltaTime;
        currentDirection = RotateVector2(currentDirection, turnAmount);
        currentDirection.Normalize(); // Ensure consistent speed
    }

    void ChooseNewDirection()
    {
        if (wanderType == WanderType.XYWander)
        {
            // In XY mode, move either horizontally or vertically
            if (Random.value > 0.5f)
            {
                // Move horizontally (left or right)
                currentDirection = Random.value > 0.5f ? Vector2.right : Vector2.left;
            } else
            {
                // Move vertically (up or down)
                currentDirection = Random.value > 0.5f ? Vector2.up : Vector2.down;
            }
        } else // WanderType.FreeWander
        {
            // For free wander, initialize with a completely random direction
            float randomAngle = Random.Range(0f, 360f);
            currentDirection = new Vector2(
                Mathf.Cos(randomAngle * Mathf.Deg2Rad),
                Mathf.Sin(randomAngle * Mathf.Deg2Rad)
            );
        }
    }

    // Helper function to rotate a Vector2 by an angle in degrees
    Vector2 RotateVector2(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    void EnforceBoundaries()
    {
        Vector3 position = transform.position;
        bool needsNewDirection = false;

        // Check and adjust X position
        if (position.x < minX)
        {
            position.x = minX;
            needsNewDirection = true;
        } else if (position.x > maxX)
        {
            position.x = maxX;
            needsNewDirection = true;
        }

        // Check and adjust Y position
        if (position.y < minY)
        {
            position.y = minY;
            needsNewDirection = true;
        } else if (position.y > maxY)
        {
            position.y = maxY;
            needsNewDirection = true;
        }

        // Update position and potentially choose a new direction
        transform.position = position;

        if (needsNewDirection)
        {
            if (wanderType == WanderType.XYWander)
            {
                ChooseNewDirection();
            } else // WanderType.FreeWander
            {
                // Reflect the direction based on which boundary was hit
                if (position.x == minX || position.x == maxX)
                {
                    currentDirection.x = -currentDirection.x;
                }

                if (position.y == minY || position.y == maxY)
                {
                    currentDirection.y = -currentDirection.y;
                }
            }
        }
    }

    // Optional: Visualization of the direction in the editor
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, (Vector3)currentDirection * 0.5f);
        }

        if (useBoundaries)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(
                new Vector3((minX + maxX) / 2, (minY + maxY) / 2, 0),
                new Vector3(maxX - minX, maxY - minY, 0.1f)
            );
        }
    }
}