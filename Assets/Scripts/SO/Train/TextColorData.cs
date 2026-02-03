using UnityEngine;

[CreateAssetMenu(fileName = "TextColorData", menuName = "Scriptable Objects/TextColorData")]
public class TextColorData : ScriptableObject
{
    [Header("색상 설정")]
    public Gradient gradient;
    public Color _minColor = Color.red;
    public Color _maxColor = Color.green;
}
