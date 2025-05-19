using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    // Singleton instance
    public static FadeManager Instance { get; private set; }

    // The black overlay image
    private Image fadeOverlay;

    // Is a transition currently happening?
    private bool isTransitioning = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeOverlay();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateFadeOverlay()
    {
        // Create a canvas as child of this object
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform);

        // Set up the canvas
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Make sure it's on top of everything
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create the fade overlay image
        GameObject imageObj = new GameObject("FadeOverlay");
        imageObj.transform.SetParent(canvas.transform, false);

        // Set it to cover the entire screen
        RectTransform rectTransform = imageObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localPosition = Vector3.zero;

        // Add the image component and set it to black with 0 alpha
        fadeOverlay = imageObj.AddComponent<Image>();
        fadeOverlay.color = new Color(0, 0, 0, 0);

        Debug.Log("[FadeManager] Created fade overlay");
    }

    // Public method to fade out, do something, then fade in
    public void FadeOutAndIn(float fadeOutTime, float blackScreenTime, float fadeInTime, System.Action actionDuringBlackScreen)
    {
        if (isTransitioning)
        {
            // If already transitioning, stop previous coroutine and start a new one
            StopAllCoroutines();
        }

        StartCoroutine(FadeOutAndInCoroutine(fadeOutTime, blackScreenTime, fadeInTime, actionDuringBlackScreen));
    }

    private IEnumerator FadeOutAndInCoroutine(float fadeOutTime, float blackScreenTime, float fadeInTime, System.Action actionDuringBlackScreen)
    {
        isTransitioning = true;

        // Fade out to black
        yield return FadeOut(fadeOutTime);

        // Execute the action while screen is black
        if (actionDuringBlackScreen != null)
        {
            actionDuringBlackScreen.Invoke();
        }

        // Wait for specified time while black
        yield return new WaitForSeconds(blackScreenTime);

        // Fade in
        yield return FadeIn(fadeInTime);

        isTransitioning = false;
    }

    // Fades the screen to black
    private IEnumerator FadeOut(float duration)
    {
        float timer = 0f;
        Color startColor = fadeOverlay.color;
        Color endColor = new Color(0, 0, 0, 1); // Black, fully opaque

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            fadeOverlay.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        // Ensure it's fully black
        fadeOverlay.color = endColor;
    }

    // Fades from black to clear
    private IEnumerator FadeIn(float duration)
    {
        float timer = 0f;
        Color startColor = fadeOverlay.color;
        Color endColor = new Color(0, 0, 0, 0); // Transparent

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            fadeOverlay.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        // Ensure it's fully transparent
        fadeOverlay.color = endColor;
    }

    // For testing
    [ContextMenu("Test Fade")]
    public void TestFade()
    {
        FadeOutAndIn(0.5f, 0.2f, 1.0f, () => Debug.Log("Black screen action"));
    }
}