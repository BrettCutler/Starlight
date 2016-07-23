/////////////////////////////////////////////////////////
// Script grabbed from Unity support forums:
// http://forum.unity3d.com/threads/trailrenderer-reset.38927/#post-2046623
/////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;

/// <summary>
/// Fixes trail renderer drawing in-between when object is instantaneously moved.
/// To use, disable/reenable both this component and the trail renderer.
/// </summary>
public class TrailRendererHelper : MonoBehaviour
{
  protected TrailRenderer mTrail;
  protected float mTime = 0;

  void Awake()
  {
    mTrail = gameObject.GetComponent<TrailRenderer>();
    if( null == mTrail )
    {
      Debug.LogError( "[TrailRendererHelper.Awake] invalid TrailRenderer." );
      return;
    }

    mTime = mTrail.time;
  }

  void OnEnable()
  {
    if( null == mTrail )
    {
      return;
    }

    StartCoroutine( ResetTrails() );
  }

  IEnumerator ResetTrails()
  {
    mTrail.time = 0;

    yield return new WaitForEndOfFrame();

    mTrail.time = mTime;
  }
}
