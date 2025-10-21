using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CharacteristicBar
{
    public CharacteristicType Type;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI valueText;
    public Image positiveFillImage;
    public Image negativeFillImage;
    public Image backgroundImage;
}
