using UnityEngine;

public class ResettableObject : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;

    // Component references
    private Rigidbody2D rb;
    private Collider2D col;

    // Flag to know if initial state was saved
    private bool hasInitialState = false;

    void Awake()
    {
        // Save initial state on awake
        SaveInitialState();
    }

    public void SaveInitialState()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;

        // Cache component references
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        hasInitialState = true;
    }

    public void ResetToInitialState()
    {
        if (!hasInitialState)
        {
            Debug.LogWarning("Trying to reset object " + gameObject.name + " without saved initial state!");
            return;
        }

        transform.position = initialPosition;
        transform.rotation = initialRotation;
        transform.localScale = initialScale;

        // Reset Rigidbody if present
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Reset other components as needed
        // Add more reset logic here depending on your game's needs
    }
}