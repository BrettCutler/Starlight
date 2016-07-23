using UnityEngine;
using System.Collections;

////////////////////////////////////////////////
public class SoundEventScript : MonoBehaviour 
{
  //////////////////////////////////////
  public float _lifetime = 1.0f;
  public float _radius;

  //////////////////////////////////////
  private GameObject _creator;

  private const float _kAnimationSizeConstant = 33.3f;

  //////////////////////////////////////
  public void Awake()
  {
    if( _lifetime > 0f )
    {
      Invoke( "Kill", _lifetime );
    }
  }
  public void OnDestroy()
  {
    CancelInvoke();
  }

  /////////////////////////////////////
  void OnDrawGizmos()
  {
    GizmoDraw();
  }
  void OnDrawGizmosSelected()
  {
    GizmoDraw();
  }
  void GizmoDraw()
  {
    Gizmos.color = Color.green;
    Gizmos.DrawWireSphere(transform.position, _radius);
  }
  
  //////////////////////////////////////
  private void Kill()
  {
    if(gameObject.activeInHierarchy)
      Destroy(gameObject);
  }

  //////////////////////////////////////
  public void SetRadius(float newRadius)
  {
    _radius = newRadius;
    float newScale = ( newRadius / _kAnimationSizeConstant ) * transform.localScale.x; // Because animation is about [animation size constant] big
    transform.localScale = new Vector3(newScale, newScale, newScale);
  }
}
