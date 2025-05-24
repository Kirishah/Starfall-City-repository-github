using UnityEngine;
using DanceInputActions;

public class DanceArrow : MonoBehaviour
{
    public string direction; // "Up", "Down", "Left", "Right"
    public float moveSpeed = 500f;
    private RectTransform rectTransform;
    private Vector2 startPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
    }

    public void ResetArrow()
    {
        rectTransform.anchoredPosition = startPosition;
        gameObject.SetActive(true);
    }

    void Update()
    {
        // Move arrow leftward (adjust axis based on your UI setup)
        rectTransform.anchoredPosition += Vector2.left * moveSpeed * Time.deltaTime;

        // Выключить если за экраном и вернуть в пул
        if (rectTransform.anchoredPosition.x < -1000)
        {
            gameObject.SetActive(false);
            DanceInput.Instance?.ReturnArrowToPool(this);
        }
    }
}
