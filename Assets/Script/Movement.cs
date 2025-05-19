using UnityEngine;
using System.Collections.Generic;

public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;
    // Ray-based ground check parameters
    public bool useRayGroundCheck = true;
    public int groundRayCount = 3;
    public float groundRayLength = 0.2f;
    public float rayVerticalOffset = 0.1f; // Offset to prevent self-detection

    [Header("Keyboard Controls")]
    public KeyCode moveLeftKey = KeyCode.J;
    public KeyCode moveRightKey = KeyCode.L;
    public KeyCode jumpKey = KeyCode.W;

    [Header("EMG Controls")]
    public EMGSignalTester emgSignalTester;
    public bool useEMGControls = true;
    public float emgCooldown = 0.3f; // Cooldown to prevent rapid movement changes
    public enum EMGControlMode { Toggle, Direct }
    public EMGControlMode controlMode = EMGControlMode.Direct; // Default to direct control
    public bool jumpWithBothChannels = true; // Enable jumping with both channels
    public float jumpCooldown = 0.5f; // Cooldown for EMG jumping

    [Header("EMG Channel Configuration")]
    public int leftChannelIndex = 0;   // For Player 1: 0, For Player 2: 2
    public int rightChannelIndex = 1;  // For Player 1: 1, For Player 2: 3

    [Header("Player Stacking")]
    public bool enablePlayerStacking = true;  // Allow players to ride on each other
    public float riderCheckHeight = 0.1f;     // Height above the player to check for riders
    public float contactThreshold = 0.1f;     // How close riders need to be to be considered "on top"

    private Rigidbody2D rb;
    public bool isGrounded = false;
    private float leftCooldownTimer = 0f;
    private float rightCooldownTimer = 0f;
    private float jumpCooldownTimer = 0f;
    private int moveDirection = 0; // -1 = left, 0 = stop, 1 = right

    // For direct mode: tracks if channels are currently active
    private bool leftChannelActive = false;
    private bool rightChannelActive = false;
    private Collider2D playerCollider;

    // Player stacking fields
    private Movement playerBelow = null;    // Reference to player we're standing on
    private List<Movement> playersAbove = new List<Movement>();  // References to players standing on us
    private Vector2 lastPosition;           // Track our last position to calculate movement

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        lastPosition = transform.position;

        // Find EMG signal tester if not assigned
        if (emgSignalTester == null && useEMGControls)
        {
            emgSignalTester = FindObjectOfType<EMGSignalTester>();
            if (emgSignalTester == null)
            {
                Debug.LogWarning("EMG Signal Tester not found! Falling back to keyboard controls.");
                useEMGControls = false;
            }
        }
    }

    private void Update()
    {
        // Update cooldown timers
        if (leftCooldownTimer > 0) leftCooldownTimer -= Time.deltaTime;
        if (rightCooldownTimer > 0) rightCooldownTimer -= Time.deltaTime;
        if (jumpCooldownTimer > 0) jumpCooldownTimer -= Time.deltaTime;

        // Get movement input
        float moveX = GetMovementInput();

        // Apply movement
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        // Check for EMG jump (both channels active)
        CheckEMGJump();

        // Standard keyboard jump
        if (Input.GetKey(jumpKey) && isGrounded)
        {
            Jump();
        }

        // Ground check - now using ray-based method if enabled
        isGrounded = useRayGroundCheck ? CheckGroundedRay() : Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Update player stacking
        if (enablePlayerStacking)
        {
            UpdatePlayerStacking();
        }
    }

    // NEW METHOD: Handle player stacking logic
    private void UpdatePlayerStacking()
    {
        // Calculate how much we've moved horizontally since last frame
        Vector2 currentPosition = transform.position;
        Vector2 movement = currentPosition - lastPosition;

        // Move any players that are riding on top of us
        if (playersAbove.Count > 0)
        {
            foreach (Movement rider in playersAbove.ToArray()) // Use ToArray to avoid collection modification issues
            {
                if (rider != null)
                {
                    // Apply our horizontal movement to the rider
                    rider.transform.position = new Vector3(
                        rider.transform.position.x + movement.x,
                        rider.transform.position.y,
                        rider.transform.position.z
                    );
                }
                else
                {
                    // Remove null references
                    playersAbove.Remove(rider);
                }
            }
        }

        // Update our last position for the next frame
        lastPosition = currentPosition;

        // Check for players above us
        CheckForPlayersAbove();
    }

    // NEW METHOD: Check for players standing on us
    private void CheckForPlayersAbove()
    {
        // Calculate the top of our collider
        Bounds bounds = playerCollider.bounds;
        float topY = bounds.max.y;
        float centerX = bounds.center.x;
        float width = bounds.size.x;

        // Cast a box upward to detect players
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            new Vector2(centerX, topY + riderCheckHeight / 2),
            new Vector2(width * 0.8f, riderCheckHeight),
            0f
        );

        // Clear players that are no longer above us
        for (int i = playersAbove.Count - 1; i >= 0; i--)
        {
            bool stillAbove = false;
            foreach (Collider2D col in colliders)
            {
                if (col.gameObject != gameObject && col.gameObject.GetComponent<Movement>() == playersAbove[i])
                {
                    stillAbove = true;
                    break;
                }
            }

            if (!stillAbove)
            {
                // This player is no longer above us
                Movement player = playersAbove[i];
                if (player != null)
                {
                    player.playerBelow = null;
                }
                playersAbove.RemoveAt(i);
            }
        }

        // Add new players that are now above us
        foreach (Collider2D col in colliders)
        {
            // Skip our own collider
            if (col.gameObject == gameObject)
                continue;

            // Check if this is a player
            Movement otherPlayer = col.gameObject.GetComponent<Movement>();
            if (otherPlayer != null)
            {
                // Check if player is close enough to be considered on top
                float otherBottom = col.bounds.min.y;
                if (Mathf.Abs(otherBottom - topY) < contactThreshold)
                {
                    // Check if this player isn't already in our list
                    if (!playersAbove.Contains(otherPlayer))
                    {
                        playersAbove.Add(otherPlayer);
                        otherPlayer.playerBelow = this;
                    }
                }
            }
        }
    }

    // Modified ray-based ground checking method with vertical offset
    private bool CheckGroundedRay()
    {
        // Calculate width for ray placement based on collider
        float width = 0.5f; // Default width if no suitable collider found

        if (playerCollider != null)
        {
            if (playerCollider is BoxCollider2D)
            {
                width = ((BoxCollider2D)playerCollider).size.x * transform.localScale.x;
            }
            else if (playerCollider is CapsuleCollider2D)
            {
                width = ((CapsuleCollider2D)playerCollider).size.x * transform.localScale.x;
            }
            else if (playerCollider is CircleCollider2D)
            {
                width = ((CircleCollider2D)playerCollider).radius * 2f * transform.localScale.x;
            }
        }

        // Ensure we have a sensible width
        width = Mathf.Max(width, 0.5f);

        // Calculate start position (leftmost ray)
        float startX = -width / 2f;
        float raySpacing = width / (groundRayCount - 1);

        // Apply vertical offset to ray starting position
        Vector2 rayStartPos = new Vector2(
            transform.position.x,
            transform.position.y - rayVerticalOffset  // Apply the offset here
        );

        // Cast multiple rays along the width of the character
        for (int i = 0; i < groundRayCount; i++)
        {
            // Calculate ray origin position
            Vector2 rayOrigin = new Vector2(
                rayStartPos.x + startX + (raySpacing * i),
                rayStartPos.y
            );

            // Cast ray downward
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundRayLength, groundLayer);

            // Draw the ray in Scene view for debugging
            Debug.DrawRay(rayOrigin, Vector2.down * groundRayLength, hit.collider != null ? Color.green : Color.red);

            // If any ray hits a platform, check if it's not our own collider
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                return true;
            }
        }

        // If we're not on a platform, we might still be on another player
        return playerBelow != null;
    }

    // Check if both EMG channels are active for jumping
    private void CheckEMGJump()
    {
        if (useEMGControls && emgSignalTester != null && emgSignalTester.IsCapturing && jumpWithBothChannels)
        {
            // Get current state of both channels
            bool isLeftChannelActive = CheckChannel(leftChannelIndex);
            bool isRightChannelActive = CheckChannel(rightChannelIndex);

            // Jump if both channels are active and we're not in cooldown
            if (isLeftChannelActive && isRightChannelActive && jumpCooldownTimer <= 0 && isGrounded)
            {
                Jump();
                jumpCooldownTimer = jumpCooldown;
                Debug.Log("Both EMG channels active (" + leftChannelIndex + " and " + rightChannelIndex + ") - JUMP!");
            }
        }
    }

    private float GetMovementInput()
    {
        float moveX = 0f;

        // Check for EMG control if enabled
        if (useEMGControls && emgSignalTester != null && emgSignalTester.IsCapturing)
        {
            if (controlMode == EMGControlMode.Toggle)
            {
                // Toggle mode (original behavior)
                CheckEMGToggleMode();
                moveX = moveDirection;
            }
            else // Direct mode
            {
                // Check for left/right channel activations
                leftChannelActive = CheckChannel(leftChannelIndex);
                rightChannelActive = CheckChannel(rightChannelIndex);

                // In direct mode, movement only happens while channels are active
                // If both channels are active AND we're using jump with both channels,
                // don't move left or right (just prepare for jump)
                if (leftChannelActive && rightChannelActive && jumpWithBothChannels)
                {
                    moveX = 0f;
                }
                else if (leftChannelActive && !rightChannelActive)
                {
                    moveX = -1f;
                }
                else if (rightChannelActive && !leftChannelActive)
                {
                    moveX = 1f;
                }
                else
                {
                    moveX = 0f;
                }
            }
        }

        // Keyboard control as fallback or alternative
        if (Input.GetKey(moveLeftKey))
        {
            moveX = -1f; // Move left
            moveDirection = -1;
        }
        else if (Input.GetKey(moveRightKey))
        {
            moveX = 1f;  // Move right
            moveDirection = 1;
        }
        else if (!useEMGControls || controlMode == EMGControlMode.Direct)
        {
            // Only reset direction when using keyboard only or in direct mode
            if (!leftChannelActive && !rightChannelActive)
                moveDirection = 0;
        }

        return moveX;
    }

    // Helper to check if a channel is activated
    private bool CheckChannel(int channelIndex)
    {
        if (emgSignalTester.IsChannelDataAvailable(channelIndex))
        {
            // Get channel data from EMG signal tester
            var channelConfig = EMGChannelManager.Instance.GetChannelConfig(channelIndex);
            float signal = emgSignalTester.GetChannelBufferedSignal(channelIndex);
            float threshold = channelConfig != null ? channelConfig.threshold : 50f;

            // Channel is active if signal exceeds threshold
            return signal >= threshold;
        }
        return false;
    }

    // Original toggle behavior in a separate method
    private void CheckEMGToggleMode()
    {
        // Check left channel for left movement
        if (emgSignalTester.IsChannelDataAvailable(leftChannelIndex) &&
            emgSignalTester.IsChannelActivated(leftChannelIndex) &&
            leftCooldownTimer <= 0)
        {
            // Toggle movement direction
            if (moveDirection == -1) // If already moving left, stop
                moveDirection = 0;
            else // Otherwise, move left
                moveDirection = -1;

            leftCooldownTimer = emgCooldown;
            Debug.Log("Channel " + leftChannelIndex + " activated! Movement: " + (moveDirection == -1 ? "LEFT" : "STOP"));
        }

        // Check right channel for right movement
        if (emgSignalTester.IsChannelDataAvailable(rightChannelIndex) &&
            emgSignalTester.IsChannelActivated(rightChannelIndex) &&
            rightCooldownTimer <= 0)
        {
            // Toggle movement direction
            if (moveDirection == 1) // If already moving right, stop
                moveDirection = 0;
            else // Otherwise, move right
                moveDirection = 1;

            rightCooldownTimer = emgCooldown;
            Debug.Log("Channel " + rightChannelIndex + " activated! Movement: " + (moveDirection == 1 ? "RIGHT" : "STOP"));
        }
    }

    public void Jump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // When jumping, clear the player below reference
            playerBelow = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null && !useRayGroundCheck)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Show ray visualizations in editor if using ray ground check
        if (useRayGroundCheck && Application.isEditor && !Application.isPlaying)
        {
            // Get width for visualization (similar to runtime logic)
            float width = 0.5f;
            var tempCollider = GetComponent<Collider2D>();

            if (tempCollider != null)
            {
                if (tempCollider is BoxCollider2D)
                    width = ((BoxCollider2D)tempCollider).size.x * transform.localScale.x;
                else if (tempCollider is CapsuleCollider2D)
                    width = ((CapsuleCollider2D)tempCollider).size.x * transform.localScale.x;
                else if (tempCollider is CircleCollider2D)
                    width = ((CircleCollider2D)tempCollider).radius * 2f * transform.localScale.x;
            }

            width = Mathf.Max(width, 0.5f);
            float startX = -width / 2f;
            float raySpacing = width / (groundRayCount - 1);

            Gizmos.color = Color.yellow;
            // Draw the rays with the vertical offset
            Vector3 offsetPosition = new Vector3(transform.position.x, transform.position.y - rayVerticalOffset, transform.position.z);

            for (int i = 0; i < groundRayCount; i++)
            {
                Vector3 rayOrigin = new Vector3(
                    offsetPosition.x + startX + (raySpacing * i),
                    offsetPosition.y,
                    offsetPosition.z
                );

                Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * groundRayLength);
            }
        }

        // Draw player stacking detection area
        if (enablePlayerStacking && playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            float topY = bounds.max.y;
            float centerX = bounds.center.x;
            float width = bounds.size.x;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(
                new Vector3(centerX, topY + riderCheckHeight / 2, 0),
                new Vector3(width * 0.8f, riderCheckHeight, 0.1f)
            );
        }
    }
}