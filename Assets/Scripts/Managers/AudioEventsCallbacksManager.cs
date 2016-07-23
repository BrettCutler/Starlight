using UnityEngine;
using System.Collections;

public class AudioEventsCallbacksManager : Singleton<AudioEventsCallbacksManager>
{
  public int m_BeatCount = 0;

  private bool m_PlayingMusic;
  private float m_FirstBeatTime;

  public bool IsMusicPlaying()
  {
    return m_PlayingMusic;
  }

  public void StartMusicTrack()
  {
    uint callbackFlags = (uint)AkCallbackType.AK_MusicSyncBeat | (uint)AkCallbackType.AK_MusicSyncBar;

    m_BeatCount = 0;

    AkCallbackManager.AkMusicSyncCallbackInfo callbackInfo = new AkCallbackManager.AkMusicSyncCallbackInfo();

    AkSoundEngine.PostEvent( "PlayGalileo", gameObject, callbackFlags,
      ProcessMusicCallback, callbackInfo );

    m_PlayingMusic = true;
  }

  public void StopMusicTrack()
  {
    AkSoundEngine.PostEvent( "StopAllMusic", gameObject );

    m_PlayingMusic = false;
  }

  private void ProcessMusicCallback( object in_cookie, AkCallbackType callbackType, object callbackInfo )
  {
    float beatDuration = ((AkCallbackManager.AkMusicSyncCallbackInfo)callbackInfo).fBeatDuration;
    float barDuration = ( (AkCallbackManager.AkMusicSyncCallbackInfo)callbackInfo ).fBarDuration;

    switch( callbackType )
    {
      case AkCallbackType.AK_MusicSyncBar:
        WaitForMusicManager.Instance.MusicEvent( MusicDivision.OnBar );

        break;
      case AkCallbackType.AK_MusicSyncBeat:
        //WaitForMusicManager.Instance.MusicEvent( MusicDivision.OnBeat );

        if( m_BeatCount == 0 )
        {
          string oddBeatTimes = "start music at: " + Time.time.ToString("F4") + ", odd beats at: ";
          for( int i = 0; i < 124; ++i )
          {
            oddBeatTimes += "\n" + ( Time.time + ( .96775 * i ) ).ToString( "F4" );
          }
          Debug.Log( oddBeatTimes );

          m_FirstBeatTime = Time.time;

          m_BeatCount = 1;

          WaitForMusicManager.Instance.MusicEvent( MusicDivision.OnBeat );
        }

        //m_BeatCount++;

        //if( m_BeatCount % 2 != 0 )
        //{
        //  WaitForMusicManager.Instance.MusicEvent( MusicDivision.OnOddBeat );
        //}

        WaitForMusicManager.Instance.m_BeatDuration = beatDuration;
        WaitForMusicManager.Instance.m_BarDuration = barDuration;
        break;
      default:
        break;
    }
  }

  private void Update()
  {
    if( m_BeatCount > 0 &&
        Time.time > m_FirstBeatTime + 
                    WaitForMusicManager.Instance.m_BeatDuration * m_BeatCount )
    {
      m_BeatCount++;

      WaitForMusicManager.Instance.MusicEvent( MusicDivision.OnBeat );
      
      if( m_BeatCount % 2 != 0 )
      {
        WaitForMusicManager.Instance.MusicEvent( MusicDivision.OnOddBeat );
      }
    }
  }

  /// <summary>
  ///  Remember, beats count from Zero
  /// </summary>
  public float GetBeatTime( int beatCount )
  {
    return m_FirstBeatTime + ( WaitForMusicManager.Instance.m_BeatDuration * ( beatCount - 1 ) );
  }
}
