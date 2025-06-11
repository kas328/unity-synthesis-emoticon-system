using UnityEngine;

[CreateAssetMenu(fileName = "EmoticonContainer", menuName = "Custom/Emoticon Container")]
public class EmoticonContainer : ScriptableObject
{
    public EmoticonData[] emoticons;
    public int pageLength;
}