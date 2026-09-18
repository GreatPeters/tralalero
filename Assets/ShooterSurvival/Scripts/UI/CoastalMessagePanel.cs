using TMPro;
using UnityEngine;

public sealed class CoastalMessagePanel : MonoBehaviour
{
    public TMP_Text heading, message;
    public void Show(string title,string text)
    {
        heading.text=title;message.text=text;gameObject.SetActive(true);
    }
    public void Close()=>gameObject.SetActive(false);
    private void Update(){if(Input.GetKeyDown(KeyCode.Escape))Close();}
}
