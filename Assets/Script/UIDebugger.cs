using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIDebugger : MonoBehaviour
{
    private Text debugText;

    void Start()
    {
        // Create debug text
        GameObject textObj = new GameObject("DebugText");
        textObj.transform.SetParent(transform, false);

        debugText = textObj.AddComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        debugText.fontSize = 24;
        debugText.color = Color.red;

        // Position it
        RectTransform rt = debugText.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, -10);
        rt.sizeDelta = new Vector2(500, 200);

        // Set initial text
        debugText.text = "Touch screen or click to see position";
    }

    void Update()
    {
        // Show pointer info
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 pos = Input.mousePosition;
            debugText.text = "Click at: " + pos + "\n";

            // Check if click hit any UI elements
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = pos;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            if (results.Count > 0)
            {
                debugText.text += "Hit UI: ";
                foreach (var result in results)
                {
                    debugText.text += result.gameObject.name + ", ";
                }
            }
            else
            {
                debugText.text += "No UI hit!";
            }
        }
    }
}