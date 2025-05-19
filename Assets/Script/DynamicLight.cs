using UnityEngine;

public class DynamicLight : MonoBehaviour
{
    [Header("Light Settings")]
    public float radius = 5f;
    public Color lightColor = Color.white;
    public float intensity = 1f;

    [Header("Shadow Settings")]
    public LayerMask shadowCasters;
    public int shadowResolution = 24;

    // References
    private Transform _transform;
    private Camera _camera;
    private Texture2D _lightTexture;
    private Mesh _backgroundMesh;
    private Material _lightMaterial;

    // Shadow data
    private Vector2[] _rayDirections;
    private float[] _rayDistances;

    // Sorting layer info
    public string backgroundSortingLayerName = "Background";
    private int _backgroundSortingLayerID;

    void Start()
    {
        _transform = transform;
        _camera = Camera.main;

        // Get sorting layer ID for the background
        _backgroundSortingLayerID = SortingLayer.NameToID(backgroundSortingLayerName);

        // Create light material
        _lightMaterial = new Material(Shader.Find("Sprites/Default"));
        _lightMaterial.color = lightColor;

        // Create light texture with soft gradient
        _lightTexture = CreateLightTexture();

        // Initialize shadow data
        InitializeShadowData();

        // Create background mesh for light and shadow rendering
        CreateBackgroundMesh();
    }

    void Update()
    {
        // Calculate shadow ray distances
        UpdateShadowData();

        // Update the mesh with new shadow information
        UpdateBackgroundMesh();
    }

    void OnRenderObject()
    {
        // Only render during the background rendering phase
        if (Camera.current != _camera || _backgroundMesh == null || _lightMaterial == null)
            return;

        // Set up material
        _lightMaterial.mainTexture = _lightTexture;
        _lightMaterial.color = lightColor * intensity;
        _lightMaterial.SetPass(0);

        // Draw the light mesh ONLY to the background
        Graphics.DrawMeshNow(_backgroundMesh, _transform.position, Quaternion.identity);
    }

    private void InitializeShadowData()
    {
        _rayDirections = new Vector2[shadowResolution];
        _rayDistances = new float[shadowResolution];

        // Pre-calculate ray directions
        for (int i = 0; i < shadowResolution; i++)
        {
            float angle = i * (360f / shadowResolution);
            _rayDirections[i] = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );
            _rayDistances[i] = radius;
        }
    }

    private void UpdateShadowData()
    {
        // Cast rays to find shadow borders
        for (int i = 0; i < shadowResolution; i++)
        {
            // Cast ray
            RaycastHit2D hit = Physics2D.Raycast(_transform.position, _rayDirections[i], radius, shadowCasters);

            // Update distance based on hit
            if (hit.collider != null)
            {
                _rayDistances[i] = hit.distance;
            }
            else
            {
                _rayDistances[i] = radius;
            }
        }
    }

    private void CreateBackgroundMesh()
    {
        _backgroundMesh = new Mesh();

        // Set up vertex array
        Vector3[] vertices = new Vector3[shadowResolution + 1];
        vertices[0] = Vector3.zero; // Center of the light

        // Set up triangle array
        int[] triangles = new int[(shadowResolution) * 3];

        // Set up UV array
        Vector2[] uvs = new Vector2[shadowResolution + 1];
        uvs[0] = new Vector2(0.5f, 0.5f); // Center UV

        // Set initial vertex positions based on ray distances
        for (int i = 0; i < shadowResolution; i++)
        {
            // Set vertex position
            float angle = i * (360f / shadowResolution);
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * _rayDistances[i],
                Mathf.Sin(angle * Mathf.Deg2Rad) * _rayDistances[i],
                0
            );

            // Set UV coordinates (radial)
            uvs[i + 1] = new Vector2(
                (vertices[i + 1].x / radius) * 0.5f + 0.5f,
                (vertices[i + 1].y / radius) * 0.5f + 0.5f
            );

            // Set triangle indices (fan from center)
            int nextIndex = (i + 1) % shadowResolution + 1;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = nextIndex;
        }

        // Assign to mesh
        _backgroundMesh.vertices = vertices;
        _backgroundMesh.triangles = triangles;
        _backgroundMesh.uv = uvs;
        _backgroundMesh.RecalculateNormals();
    }

    private void UpdateBackgroundMesh()
    {
        if (_backgroundMesh == null)
            return;

        Vector3[] vertices = _backgroundMesh.vertices;

        // Update vertex positions based on current ray distances
        for (int i = 0; i < shadowResolution; i++)
        {
            vertices[i + 1] = new Vector3(
                _rayDirections[i].x * _rayDistances[i],
                _rayDirections[i].y * _rayDistances[i],
                0
            );
        }

        // Apply changes
        _backgroundMesh.vertices = vertices;
        _backgroundMesh.RecalculateBounds();
    }

    private Texture2D CreateLightTexture()
    {
        int textureSize = 256;
        Texture2D texture = new Texture2D(textureSize, textureSize);

        Vector2 center = new Vector2(textureSize / 2, textureSize / 2);
        float maxDistance = textureSize / 2;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDistance = Mathf.Clamp01(distance / maxDistance);

                // Create circular gradient with smooth falloff
                float alpha = 1 - normalizedDistance;
                alpha = Mathf.Pow(alpha, 2); // Softer edges

                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }

        texture.Apply();
        return texture;
    }

    // Update light properties at runtime
    public void SetColor(Color color)
    {
        lightColor = color;
    }

    public void SetRadius(float newRadius)
    {
        radius = newRadius;
    }

    public void SetIntensity(float newIntensity)
    {
        intensity = newIntensity;
    }

    // Visualize the light radius in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}