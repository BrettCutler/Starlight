using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
  public PlayerRacquet m_RacquetReference;
  public bool m_LockedToRacquetY;
  [SerializeField] private Vector3 m_CameraShakeScalar;
  [SerializeField] private bool d_DebugCameraShake;

  private Vector3 m_CameraShakeNoiseOffset;
  private Vector3 m_LastCameraShakeOffset;

  private struct CameraShake
  {
    public float duration;
    public float startTime;
    public float intensity;

    public CameraShake( float durationVal, float intensityVal )
    {
      duration = durationVal;
      startTime = Time.time;
      intensity = intensityVal;
    }
  }

  private List<CameraShake> m_CurrentCameraShake = new List<CameraShake>();
  private const float k_PerlinTimeScalar = 20f;

  void Awake()
  {
    NameRegistry.Instance.GameObjectRegistry.Add( RegisteredGameObjectNames.Camera, gameObject );
    NameRegistry.Instance.ScriptsRegistry.Add( RegisteredSingularScripts.CameraController, this );

    m_RacquetReference.OnRacquetUpdatePosition += OnRacquetUpdatePosition;

    m_CameraShakeNoiseOffset = new Vector3(
      Random.value * k_PerlinTimeScalar,
      Random.value * k_PerlinTimeScalar,
      Random.value * k_PerlinTimeScalar
      );
  }

  private void OnRacquetUpdatePosition()
  {
    transform.position = new Vector3( 
      m_RacquetReference.transform.position.x,
      m_LockedToRacquetY ? m_RacquetReference.transform.position.y : transform.position.y,
      transform.position.z );

    // sum current intensities, and make one sample
    float intensitySum = 0f;

    for( int i = 0; i < m_CurrentCameraShake.Count; ++i )
    {
      float percentComplete = (Time.time - m_CurrentCameraShake[i].startTime) / m_CurrentCameraShake[i].duration;
      float dampen =  1f - Mathf.Clamp( 4f * percentComplete - 3f, 0f, 1f );

      intensitySum += m_CurrentCameraShake[i].intensity *  dampen;
    }

    // use noise to generate a position offset
    Vector3 shakeOffset = new Vector3(
      ( Mathf.PerlinNoise( Time.time * k_PerlinTimeScalar + m_CameraShakeNoiseOffset.x, 0f )
        * 2f - 1f ) * m_CameraShakeScalar.x * intensitySum,
      ( Mathf.PerlinNoise( Time.time * k_PerlinTimeScalar + m_CameraShakeNoiseOffset.y, 0f )
        * 2f - 1f ) * m_CameraShakeScalar.y * intensitySum,
      ( Mathf.PerlinNoise( Time.time * k_PerlinTimeScalar + m_CameraShakeNoiseOffset.z, 0f )
        * 2f - 1f ) * m_CameraShakeScalar.z * intensitySum
      );


    //Debug.Log( "shake intensity = " + intensitySum +
    //  ", shakeOffset = " + shakeOffset +
    //  ", lastCameraShakeOffset = " + m_LastCameraShakeOffset );

    transform.position -= m_LastCameraShakeOffset;
    transform.position += shakeOffset;
    m_LastCameraShakeOffset = shakeOffset;

    // cull expired camera shakes
    for( int i = m_CurrentCameraShake.Count - 1; i > -1; i-- )
    {
      if( Time.time > m_CurrentCameraShake[i].startTime + m_CurrentCameraShake[i].duration )
      {
        m_CurrentCameraShake.RemoveAt( i );
      }
    }
  }

  void Update()
  {
    if( d_DebugCameraShake )
    {
      //StopAllCameraShake();
      //float intensity =  Mathf.Abs( Input.GetAxis("Vertical") );
      //AddCameraShake( 1f, intensity );

      if( Input.GetButtonDown( "DebugButton" ) )
      {
        AddCameraShake( .25f, 1f );
      }
    }
  }

  public void AddCameraShake( float duration, float  intensity )
  {
    m_CurrentCameraShake.Add( new CameraShake( duration, intensity ) );
  }

  public void StopAllCameraShake()
  {
    m_CurrentCameraShake.Clear();
  }
}
