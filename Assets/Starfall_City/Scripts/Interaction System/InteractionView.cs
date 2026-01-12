using TMPro;
using UnityEngine;

namespace Interaction
{
    public class InteractionView : MonoBehaviour
    {
        [SerializeField] private GameObject _promptPrefab;
        [SerializeField] private Vector3 _promptOffset = new(0, 1.5f, 0);

        private GameObject _currentPrompt;

        public void ShowPrompt(string text)
        {
            if (_currentPrompt == null && _promptPrefab != null)
            {
                _currentPrompt = Instantiate(_promptPrefab, WorldCanvasManager.Instance.worldCanvas.transform);
                if (_currentPrompt.TryGetComponent<TMP_Text>(out var textComponent))
                    textComponent.text = text;

                var rt = _currentPrompt.GetComponent<RectTransform>();
                rt.anchoredPosition = Vector2.zero;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            }

            if (_currentPrompt != null)
            {
                UpdatePromptPosition();
                _currentPrompt.SetActive(true);
            }
        }

        public void HidePrompt()
        {
            if (_currentPrompt != null)
            {
                _currentPrompt.SetActive(false);
            }
        }

        private void UpdatePromptPosition()
        {
            if (_currentPrompt == null) return;

            var mainCam = Camera.main;
            var uiCam = WorldCanvasManager.Instance.worldCanvas.worldCamera;

            if (mainCam == null || uiCam == null) return;

            Vector3 worldPos = transform.position + _promptOffset;
            Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0)
            {
                _currentPrompt.SetActive(false);
                return;
            }

            var canvasRect = WorldCanvasManager.Instance.worldCanvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCam, out Vector2 localPoint);

            _currentPrompt.GetComponent<RectTransform>().anchoredPosition = localPoint;

            bool isOnScreen = screenPos.x >= 0 && screenPos.x <= Screen.width &&
                              screenPos.y >= 0 && screenPos.y <= Screen.height;

            _currentPrompt.SetActive(isOnScreen);
        }

        private void OnDestroy()
        {
            if (_currentPrompt != null)
                Destroy(_currentPrompt);
        }
    }
}
