using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class UITextController : MonoBehaviour
{
  private Text m_TextComponent;
  public Text m_TextShadow;

  void Awake()
  {
    m_TextComponent = GetComponent<Text>();
  }

  public void SetText( string newText )
  {
    m_TextComponent.text = newText;
    m_TextShadow.text = newText;
  }

  public void ChangeColor( Color newColor )
  {
    m_TextComponent.color = newColor;
  }
}
