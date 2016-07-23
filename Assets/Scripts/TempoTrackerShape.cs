using UnityEngine;
using System.Collections;

/// <summary>
/// Attach this to a visible object and
/// call Tick() on a regular event to see that event visualized.
/// This assumes the transform is equally-scaled along x, y, z.
/// </summary>
public class TempoTrackerShape : MonoBehaviour
{
  public float m_TickScalar;
  public float m_TimeToReset;
  public MusicDivision m_TickDivision;

  private Vector3 m_InitialScale;
  private float m_LastTickTime;

  void Awake()
  {
    m_InitialScale = transform.localScale;

    switch( m_TickDivision )
    {
      case MusicDivision.OnBar:
        WaitForMusicManager.Instance.OnBarContinue += Tick;
        break;
      case MusicDivision.OnBeat:
        WaitForMusicManager.Instance.OnBeatContinue += Tick;
        break;
      default:
        Debug.LogError( "No case defined for [" + m_TickDivision + "]" );
        break;
    }
  }

  void Update()
  {
    // shrink, if we're above start size
    if( transform.localScale.sqrMagnitude > m_InitialScale.sqrMagnitude )
    {
      float percent = 1 - ( (Time.time - m_LastTickTime) / m_TimeToReset );
      float xScale = Mathf.Lerp( m_InitialScale.x, m_InitialScale.x * m_TickScalar, percent );

      transform.localScale = new Vector3( xScale, xScale, xScale );

      // cap at minimum
      if( transform.localScale.sqrMagnitude < m_InitialScale.sqrMagnitude )
      {
        transform.localScale = m_InitialScale;
      }
    }
  }

  public void Tick()
  {
    transform.localScale = m_InitialScale * m_TickScalar;
    m_LastTickTime = Time.time;
  }
}
