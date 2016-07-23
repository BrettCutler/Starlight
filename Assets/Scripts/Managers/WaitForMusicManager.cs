using UnityEngine;
using System.Collections;

public class WaitForMusicManager : Singleton<WaitForMusicManager>
{
  public event System.Action OnDefaultContinue;
  public event System.Action OnManualContinue;
  public event System.Action OnBarContinue;
  public event System.Action OnOddBeatContinue;
  public event System.Action OnBeatContinue;
  public event System.Action OnHalfBeatContinue;
  public event System.Action OnQuarterBeatContinue;
  public event System.Action OnEighthBeatContinue;

  public event System.Action OnStopMovement;

  public int m_FrameLastOnDefault;
  public int m_FrameLastOnManual;
  public int m_FrameLastOnBar;
  public int m_FrameOnLastOnOddBeat;
  public int m_FrameLastOnBeat;
  public int m_FrameLastOnHalfBeat;
  public int m_FrameLastOnQuarterBeat;
  public int m_FrameLastOnEighthBeat;

  public float m_TimeLastOnDefault;
  public float m_TimeLastOnManual;
  public float m_TimeLastOnBar;
  public float m_TimeLastOnOddBeat;
  public float m_TimeLastOnBeat;
  public float m_TimeLastOnHalfBeat;
  public float m_TimeLastOnQuarterBeat;
  public float m_TimeLastOnEighthBeat;

  public float m_TimeNextDefault;
  public float m_TimeNextBar;
  public float m_TimeNextOddBeat;
  public float m_TimeNextBeat;
  public float m_TimeNextHalfBeat
  {
    get { return m_TimeLastOnBeat + m_BeatDuration * 0.5f; }
  }
  public float m_TimeNextxQuarterBeat
  {
    get { return m_TimeLastOnHalfBeat + m_BeatDuration * 0.25f; }
  }
  public float m_TimeNextEighthBeat
  {
    get { return m_TimeLastOnQuarterBeat + m_BeatDuration * 0.125f; }
  }

  public float m_BarDuration;
  public float m_BeatDuration;
  public float m_OddBeatDuration
  {
    get { return m_BeatDuration * 2f; }
  }

  public float BeatsPerBar
  {
    get { return Mathf.RoundToInt( m_BarDuration / m_BeatDuration ); }
  }


  public float m_DefaultDuration
  {
    get
    {
      if( ( m_DefaultWait & MusicDivision.OnEighthBeat ) != 0 )
      {
        return m_BeatDuration * 0.125f;
      }
      else if( ( m_DefaultWait & MusicDivision.OnQuarterBeat ) != 0 )
      {
        return m_BeatDuration * 0.25f;
      }
      else if( ( m_DefaultWait & MusicDivision.OnHalfBeat ) != 0 )
      {
        return m_BeatDuration * 0.5f;
      }
      else if( ( m_DefaultWait & MusicDivision.OnBeat ) != 0 )
      {
        return m_BeatDuration;
      }
      else if( ( m_DefaultWait & MusicDivision.OnOddBeat ) != 0 )
      {
        return m_BeatDuration * 2f;
      }
      else // OnBar
        return m_BeatDuration * 4f;
    }
  }

  public float m_BeatsPerSecond
  {
    get { return 1f / m_BeatDuration; }
  }

  public float m_BPM
  {
    get { return Mathf.RoundToInt( 60f / m_BeatDuration ); }
  }

  public float m_TimeUntilNextBeat
  {
    get { return m_BeatDuration - ( Time.time - m_TimeLastOnBeat ); }
  }

  public float m_TimeUntilNextOddBeat
  {
    get { return m_OddBeatDuration - ( Time.time - m_TimeLastOnOddBeat ); }
  }

  public float m_TimeUntilNextOnDefault
  {
    get { return m_DefaultDuration - ( Time.time - m_TimeLastOnDefault ); }
  }

  public int m_FramesUntilNextOddBeat
  {
    get
    {
      int returnVal = Mathf.RoundToInt( m_TimeUntilNextOddBeat / Time.smoothDeltaTime );

      ////////////////////////////
      //if( returnVal > 2 )
      //{
      //  Debug.Log( "framesUntilNextOddBeat = " + returnVal +
      //    ", timeUntilNextOddBeat = " + m_TimeUntilNextOddBeat +
      //    ", smoothDeltaTime = " + Time.smoothDeltaTime.ToString( "F4" ) +
      //    ", oddDuration = " + m_OddBeatDuration +
      //    ", Time.time = " + Time.time.ToString( "F4" ) +
      //    ", lastTimeOnOddBeat = " + m_TimeLastOnOddBeat );
      //}
      ////////////////////////////
      return returnVal;

    }
  }

  /// <summary>
  /// *REQUIRES* consistent framerate
  /// </summary>
  public int m_FramesUntilNexOnDefault
  {
    get
    {
      int returnVal = Mathf.RoundToInt( m_TimeUntilNextOnDefault / Time.smoothDeltaTime );

      ////////////////////////////
      //if( returnVal > 2 )
      //{
      //  Debug.Log( "framesUntilNextOnDefault = " + returnVal +
      //    ", timeUntilNextOnDefault = " + m_TimeUntilNextOnDefault +
      //    ", smoothDeltaTime = " + Time.smoothDeltaTime.ToString("F4") +
      //    ", defaultDuration = " + m_DefaultDuration +
      //    ", Time.time = " + Time.time.ToString("F4") +
      //    ", lastTimeOnDefault = " + m_TimeLastOnDefault );
      //}
      ////////////////////////////
      return returnVal;
    }
  }

  /// <summary>
  /// How long before or after a beat is it acceptable to consider a game event as occuring 'on beat'?
  /// </summary>
  public const float k_MaxAcceptableDeviationFromBeat = .06f;

  [EnumFlagAttribute] public MusicDivision m_DefaultWait;
  
  private void Awake()
  {
    // Initialize timers so no events occur before the game starts
    m_TimeLastOnDefault = float.MaxValue;
    m_TimeLastOnManual = float.MaxValue;
    m_TimeLastOnBar = float.MaxValue;
    m_TimeLastOnOddBeat = float.MaxValue;
    m_TimeLastOnBeat = float.MaxValue;
    m_TimeLastOnHalfBeat = float.MaxValue;
    m_TimeLastOnQuarterBeat = float.MaxValue;
    m_TimeLastOnEighthBeat = float.MaxValue;

    OnDefaultContinue += LocalOnDefaultContinue;
    OnManualContinue += LocalOnManualContinue;
    OnBarContinue += LocalOnBarContinue;
    OnOddBeatContinue += LocalOnOddBeatContinue;
    OnBeatContinue += LocalOnBeatContinue;
    OnHalfBeatContinue += LocalOnHalfBeatContinue;
    OnQuarterBeatContinue += LocalOnQuarterBeatContinue;
  }

  private void LocalOnDefaultContinue()
  {
    m_FrameLastOnDefault = Time.frameCount;
    m_TimeLastOnDefault = Time.time;
    //Debug.LogWarning( "onDefault, time||frame: " + Time.time.ToString( "F4" ) + "||" + Time.frameCount );
  }
  private void LocalOnManualContinue()
  {
    m_FrameLastOnManual = Time.frameCount;
    m_TimeLastOnManual = Time.time;
  }
  private void LocalOnBarContinue()
  {
    m_FrameLastOnBar = Time.frameCount;
    m_TimeLastOnBar = Time.time;
  }
  private void LocalOnOddBeatContinue()
  {
    m_FrameOnLastOnOddBeat = Time.frameCount;
    //m_TimeLastOnOddBeat = Time.time;
    m_TimeLastOnOddBeat = AudioEventsCallbacksManager.Instance.GetBeatTime(
      AudioEventsCallbacksManager.Instance.m_BeatCount );
    Debug.Log( "Odd beat: canonTime||frame||realTime=" + m_TimeLastOnOddBeat + "||" + Time.frameCount + "||" + Time.time );
  }
  private void LocalOnBeatContinue()
  {
    m_FrameLastOnBeat = Time.frameCount;
    //m_TimeLastOnBeat = Time.time;
    m_TimeLastOnBeat = AudioEventsCallbacksManager.Instance.GetBeatTime( 
      AudioEventsCallbacksManager.Instance.m_BeatCount );
    //Debug.Log( "Beat: canonTime||frame||realTime" + m_TimeLastOnBeat + "||" + Time.frameCount + "||" + Time.time );
  }
  private void LocalOnHalfBeatContinue()
  {
    m_FrameLastOnHalfBeat = Time.frameCount;
    m_TimeLastOnHalfBeat = Time.time;
  }
  private void LocalOnQuarterBeatContinue()
  {
    m_FrameLastOnQuarterBeat = Time.frameCount;
    m_TimeLastOnQuarterBeat = Time.time;
  }
  private void LocalOnEighthBeatContinue()
  {
    m_FrameLastOnEighthBeat = Time.frameCount;
    m_TimeLastOnEighthBeat = Time.time;
  }

  private void Update()
  {
    if( AudioEventsCallbacksManager.Instance.IsMusicPlaying() )
    {
      // If no wait, keep going
      if( ( m_DefaultWait == 0 ) )
      {
        if( OnDefaultContinue != null )
        {
          TryOnDefaultContinue();
        }
      }

      if( Input.GetButton( "ManualContinue" ) )
      {
        MusicEvent( MusicDivision.OnManual );
      }


      if( Time.time > m_TimeNextHalfBeat )
      {
        MusicEvent( MusicDivision.OnHalfBeat );
      }
      if( Time.time > m_TimeNextxQuarterBeat )
      {
        MusicEvent( MusicDivision.OnQuarterBeat );
      }
      if( Time.time > m_TimeNextEighthBeat )
      {
        MusicEvent( MusicDivision.OnEighthBeat );
      }
    }
  }

  public void StopMovement()
  {
    if( OnStopMovement != null )
    {
      OnStopMovement();
    }
  }

  public void MusicEvent( MusicDivision eventDefinition )
  {
    switch( eventDefinition )
    {
      case MusicDivision.OnManual:
        if( OnManualContinue != null )
        {
          OnManualContinue();
        }
        if( ( m_DefaultWait & MusicDivision.OnManual ) != 0 )
        {
          if( OnDefaultContinue != null )
          {
            TryOnDefaultContinue();
          }
        }
        break;
      case MusicDivision.OnBar:
        if( OnBarContinue != null )
        {
          OnBarContinue();
        }
        if( ( m_DefaultWait & MusicDivision.OnBar ) != 0 )
        {
          if( OnDefaultContinue != null )
          {
            TryOnDefaultContinue();
          }
        }
        break;
      case MusicDivision.OnOddBeat:
        //Debug.Log( "ODD BEAT frame " + Time.frameCount );
        if( OnOddBeatContinue != null )
        {
          OnOddBeatContinue();
        }
        if( ( m_DefaultWait & MusicDivision.OnOddBeat ) != 0 )
        {
          if( OnDefaultContinue != null )
          {
            TryOnDefaultContinue();
          }
        }
        break;
      case MusicDivision.OnBeat:
        if( OnBeatContinue != null )
        {
          OnBeatContinue();
        }
        if( ( m_DefaultWait & MusicDivision.OnBeat ) != 0 )
        {
          if( OnDefaultContinue != null )
          {
            TryOnDefaultContinue();
          }
        }
        break;
      case MusicDivision.OnHalfBeat:
        if( OnHalfBeatContinue != null )
        {
          OnHalfBeatContinue();
        }
        if( ( m_DefaultWait & MusicDivision.OnHalfBeat ) != 0 )
        {
          if( OnDefaultContinue != null )
          {
            TryOnDefaultContinue();
          }
        }
        break;
      case MusicDivision.OnQuarterBeat:
        if( OnQuarterBeatContinue != null )
        {
          OnQuarterBeatContinue();
        }
        if( ( m_DefaultWait & MusicDivision.OnQuarterBeat ) != 0 )
        {
          if( OnDefaultContinue != null )
          {
            TryOnDefaultContinue();
          }
        }
        break;
      case MusicDivision.OnEighthBeat:
        if( OnEighthBeatContinue != null )
        {
          OnEighthBeatContinue();
        }
        if( ( m_DefaultWait & MusicDivision.OnEighthBeat ) != 0 )
        {
          if( OnDefaultContinue != null )
          {
            TryOnDefaultContinue();
          }
        }
        break;
      default:
        break;
    }
  }

  /// <summary>
  /// Call OnDefaultContinue, but only once per frame
  /// </summary>
  private void TryOnDefaultContinue()
  {
    if( Time.frameCount != m_FrameLastOnDefault )
    {
      OnDefaultContinue();
    }
  }
}

[System.Flags] public enum MusicDivision
{
  OnManual = 1 << 0,
  OnBar = 1 << 1,
  OnBeat = 1 << 2,
  OnOddBeat = 1 << 3,
  OnHalfBeat = 1 << 4,
  OnQuarterBeat = 1 << 5,
  OnEighthBeat = 1 << 6
}
