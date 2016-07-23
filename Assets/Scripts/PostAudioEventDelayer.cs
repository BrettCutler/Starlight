using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PostAudioEventDelayer : Singleton<PostAudioEventDelayer>
{
  private List<QueuedSoundEvent> m_WaitingForBar;
  private List<QueuedSoundEvent> m_WaitingForBeat;
  private List<QueuedSoundEvent> m_WaitingForHalfBeat;
  private List<QueuedSoundEvent> m_WaitingForQuarterBeat;

  public override void Init()
  {
    base.Init();

    m_WaitingForBar = new List<QueuedSoundEvent>();
    m_WaitingForBeat = new List<QueuedSoundEvent>();
    m_WaitingForHalfBeat = new List<QueuedSoundEvent>();
    m_WaitingForQuarterBeat = new List<QueuedSoundEvent>();

    WaitForMusicManager.Instance.OnBarContinue += OnBarContinue;
    WaitForMusicManager.Instance.OnBeatContinue += OnBeatContinue;
    WaitForMusicManager.Instance.OnHalfBeatContinue += OnHalfBeatContinue;
    WaitForMusicManager.Instance.OnQuarterBeatContinue += OnQuarterBeatContinue;
  }

  public void PostDelayedSound( string eventName, MusicDivision waitUntil, GameObject queueObject )
  {
    QueuedSoundEvent queueEvent = new QueuedSoundEvent() { eventName = eventName, queuedObject = queueObject };

    switch( waitUntil )
    {
      case MusicDivision.OnBar:
        m_WaitingForBar.Add( queueEvent );
        break;
      case MusicDivision.OnBeat:
        m_WaitingForBeat.Add( queueEvent );
        break;
      case MusicDivision.OnHalfBeat:
        m_WaitingForHalfBeat.Add( queueEvent );
        break;
      case MusicDivision.OnQuarterBeat:
        m_WaitingForQuarterBeat.Add( queueEvent );
        break;
    }
  }

  private void OnBarContinue()
  {
    for( int i = 0; i < m_WaitingForBar.Count; ++i )
    {
      AkSoundEngine.PostEvent( m_WaitingForBar[i].eventName, m_WaitingForBar[i].queuedObject );
    }

    m_WaitingForBar.Clear();
  }
  private void OnBeatContinue()
  {
    for( int i = 0; i < m_WaitingForBeat.Count; ++i )
    {
      AkSoundEngine.PostEvent( m_WaitingForBar[i].eventName, m_WaitingForBeat[i].queuedObject );
    }

    m_WaitingForBeat.Clear();
  }
  private void OnHalfBeatContinue()
  {
    for( int i = 0; i < m_WaitingForHalfBeat.Count; ++i )
    {
      AkSoundEngine.PostEvent( m_WaitingForHalfBeat[i].eventName, m_WaitingForHalfBeat[i].queuedObject );
    }

    m_WaitingForHalfBeat.Clear();
  }
  private void OnQuarterBeatContinue()
  {
    for( int i = 0; i < m_WaitingForQuarterBeat.Count; ++i )
    {
      AkSoundEngine.PostEvent( m_WaitingForQuarterBeat[i].eventName, m_WaitingForQuarterBeat[i].queuedObject );
      Debug.Log( "Play " + m_WaitingForQuarterBeat[i].eventName + ", time||frame: " + Time.time + "||" + Time.frameCount );
    }

    m_WaitingForQuarterBeat.Clear();
  }

  private struct QueuedSoundEvent
  {
    public string eventName;
    public GameObject queuedObject;
  }
}
