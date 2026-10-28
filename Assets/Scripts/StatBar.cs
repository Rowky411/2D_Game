using UnityEngine;
using UnityEngine.UI;

public class StatBar : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private Text label;
    [SerializeField] private Color fullColor = Color.white;
    [SerializeField] private Color emptyColor = Color.white;

    public void SetValue(int current, int max)
    {
        float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        fill.fillAmount = ratio;
        fill.color = Color.Lerp(emptyColor, fullColor, ratio);
        if (label != null) label.text = $"{current}/{max}";
    }
}
