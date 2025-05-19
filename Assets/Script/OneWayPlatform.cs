using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class OneWayPlatform : MonoBehaviour
{
    private void Start()
    {
        // Get or add required components
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        PlatformEffector2D platformEffector = GetComponent<PlatformEffector2D>();

        // Add PlatformEffector2D if it doesn't exist
        if (platformEffector == null)
        {
            platformEffector = gameObject.AddComponent<PlatformEffector2D>();
        }

        // Configure BoxCollider2D
        boxCollider.usedByEffector = true;
        boxCollider.isTrigger = false;

        // Configure PlatformEffector2D correctly
        platformEffector.useOneWay = true;
        platformEffector.useOneWayGrouping = true;

        // These are the correct values for a platform you can pass through from sides/below
        platformEffector.rotationalOffset = 0f;  // 0 means the surface normal points upward
        platformEffector.surfaceArc = 80f;       // Only block from a small arc above (try 60-90)

        platformEffector.useSideFriction = false;
        platformEffector.sideArc = 1;           // Minimum side arc

        // Make sure this object is in the correct layer
        if (LayerMask.NameToLayer("Platform") != -1)
        {
            gameObject.layer = LayerMask.NameToLayer("Platform");
        }
        else
        {
            Debug.LogWarning("Platform layer not found. Please create a layer named 'Platform'");
        }
    }
}