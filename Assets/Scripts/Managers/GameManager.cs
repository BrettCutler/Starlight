using UnityEngine;
using System.Collections;

// Manages starting game, resetting after ball loss
public class GameManager : Singleton<GameManager>
{
  public GameObject m_GameBall;
  public GameObject m_Racquet;
  public UITextController m_CountdownText;
  public UITextController m_TitleText;
  public UITextController m_ScoreText;
  public UITextController m_ComboText;

  public int m_BeatsPerBallTraversal
  {
    get { return m_BeatsPerBallTraversalIntern; }
    set
    {
      if( value < 1 || value > 2 )
      {
        Debug.LogError( "Attempt to assign m_BeatsPerBallTraversal to unsupported value." +
          "Must be either 1 or 2." );
      }

      m_BeatsPerBallTraversalIntern = value;
    }
  }
  [SerializeField] private int m_BeatsPerBallTraversalIntern;

  public float m_RoomDepth = 10f;
  public float m_OutOfBoundsZ;
  public float m_TileSpawnZ;
  public Vector2 m_BoundsTopLeft;
  public Vector2 m_BoundsBottomRight;
  public Vector3 m_BallStartPos;
  public Vector3 m_RacquetStartPos;

  public bool m_TestSpawnTargets;
  private bool m_WaitingForBeat;
  //private bool m_WaitingForOddBeat;

  public int m_ComboScoreToCharge;
  public bool m_ComboIsPlayerActivated;

  public int m_Score
  {
    get { return m_ScoreIntern; }
    set
    {
      m_ScoreIntern = value;
      m_ScoreText.SetText( m_ScoreIntern.ToString() + " // SCORE" );
    }
  }
  private int m_ScoreIntern;

  public int m_ComboPower
  {
    get { return m_ComboIntern; }
    set
    {
      m_ComboIntern = Mathf.Min( value, m_ComboScoreToCharge );

      if( m_ComboIntern >= m_ComboScoreToCharge )
      {
        if( m_ComboIsPlayerActivated )
        {
          m_ComboText.SetText( "<color=red>[SQUARE] to !</color>" );
        }
        else
        {
          m_ComboText.SetText( "<color=red>" + m_ComboIntern.ToString() + " //  COMBO!</color>" );
        }
      }
      else
      {
        m_ComboText.SetText( m_ComboIntern.ToString() + " // COMBO" );
      }
    }
  }
  private int m_ComboIntern;

  private static float k_WaitAfterLostBall = 1.25f;

  public override void Init()
  {
    base.Init();

    WaitForMusicManager.Instance.OnBeatContinue += OnBeatContinue;
    //WaitForMusicManager.Instance.OnOddBeatContinue += OnOddBeatContinue;
  }

  //private void OnOddBeatContinue()
  //{
  //  m_WaitingForOddBeat = false;
  //}

  private void OnBeatContinue()
  {
    m_WaitingForBeat = false;
  }

  private void FixedUpdate()
  {
    //Debug.Log( "start = " + Input.GetButton( "Start" ) );
    //Debug.Log( "BPM down = " + Input.GetButtonDown( "BPM Down" ) );
    //Debug.Log( "BPM Up = " + Input.GetButtonDown( "BPM Up" ) );

    if( Input.GetButtonDown( "Start") )
    {
      StartGame();
    }
  }

  private void StartGame()
  {
    (NameRegistry.Instance.ScriptsRegistry[RegisteredSingularScripts.PlayerRacquetScript]
      as PlayerRacquet).OnGameStart( );

    m_TitleText.SetText( "" );

    m_Score = 0;

    AudioEventsCallbacksManager.Instance.StopMusicTrack( );
    AudioEventsCallbacksManager.Instance.StartMusicTrack( );

    StopCoroutine( "GameBeginCountdown" );
    StartCoroutine( "GameBeginCountdown", 4 );

    StopCoroutine( "TestSpawnTargets" );
    if( m_TestSpawnTargets )
    {
      StartCoroutine( "TestSpawnTargets" );
    }
  }

  private IEnumerator GameBeginCountdown( int countFrom )
  {
    Debug.Log( "GameBeginCountdown(), start" );
    
    m_WaitingForBeat = true;

    GameBallCharacterController ball = 
      NameRegistry.Instance.ScriptsRegistry[RegisteredSingularScripts.GameBallScript] as GameBallCharacterController;
    ball.LockMovement(true);
    ball.OnStopMovement();
    ball.SetTrailEnabled( false );

    m_GameBall.GetComponent<Rigidbody>().velocity = Vector3.zero;

    ball.m_Position = m_BallStartPos + new Vector3( 0f, 0f, m_RoomDepth );

    Collider gameBallCollider = m_GameBall.GetComponent<Collider>();
    gameBallCollider.enabled = false;

    for( int i = 0; i < countFrom; i++ )
    {
      while( m_WaitingForBeat )
      {
        yield return null;
      }
      
      switch( i )
      {
        case 0:
          m_CountdownText.SetText( "1" );
          break;
        case 1:
          m_CountdownText.SetText( "1\n      2" );
          break;
        case 2:
          m_CountdownText.SetText( "1\n      2\n            3" );
          break;
        case 3:
          m_CountdownText.SetText( "1\n      2\n            3\n                  4" );
          //m_UIText.SetText( "\n      FOUR" );
          break;
      }
      //m_UIText.SetText( (i + 1 ).ToString() );


      float ballZPos = m_RoomDepth - ( (m_RoomDepth / ( countFrom - 1 ) ) * i );
      ball.m_Position = m_BallStartPos + new Vector3( 0f, 0f, ballZPos );

      m_WaitingForBeat = true;
    }

    while( m_WaitingForBeat )
    {
      yield return null;
    }

    m_CountdownText.SetText( "" );

    ball.m_Position = m_BallStartPos;
    ball.m_Velocity = new Vector3( 0f, 0f, CalculateZVelocity() );
    ball.LockMovement(false);
    ball.OnMoveContinue();
    ball.SetTrailEnabled( true );

    Debug.Log( "Begin ball movement, ball velocity = " + ball.m_Velocity +
      ", frame is " + Time.frameCount + ", ball should arrive at time " +
      (WaitForMusicManager.Instance.m_TimeUntilNextOddBeat + Time.time) );

    // now, we wait a moment to enable collision on the ball, so it can clear the racquet
    yield return new WaitForSeconds( WaitForMusicManager.Instance.m_BeatDuration / 2 );

    gameBallCollider.enabled = true;

    //yield return new WaitForSeconds( WaitForMusicManager.Instance.m_BeatDuration * 1.5f );

    //m_UIText.SetText( "" );
  }

  private IEnumerator TestSpawnTargets()
  {
    while( true )
    {
      int numberToSpawn = Random.Range( 1, 5 );
      for( int i = 0; i < numberToSpawn; ++i )
      {
        Vector3 spawnScale = TargetManager.Instance.GetRandomTargetScale();

        Vector3 spawnPos = new Vector3(
            Random.Range( m_BoundsTopLeft.x + spawnScale.x, m_BoundsBottomRight.x - spawnScale.x ),
            Random.Range( m_BoundsBottomRight.y + spawnScale.y, m_BoundsTopLeft.y - spawnScale.y ),
            m_TileSpawnZ
            );

        TargetManager.Instance.SpawnTarget( spawnPos, spawnScale );
      }

      yield return new WaitForSeconds( Random.Range( 3f, 5f ) );
    }
  }

  // Set tick rate/bpm from input (this also triggers a game reset)

  // Calculate ball speed from tick rate

  // Begin ball movement

  // Reset game
  public IEnumerator OnLostBall()
  {
    yield return new WaitForSeconds( k_WaitAfterLostBall );

    StopCoroutine( "GameBeginCountdown" );
    StartCoroutine( "GameBeginCountdown", 4 );
  }

  public float CalculateZVelocity()
  {
    // Because velocity is units/second,
    // and we want one room traversal per beat,
    // set velocity to (room depth)/beat
    float velocity = WaitForMusicManager.Instance.m_BeatsPerSecond *
      ( m_RoomDepth );
    return velocity / m_BeatsPerBallTraversal;
  }

  public void TileScored()
  {
    m_Score++;
  }

  void OnBallHitRacquet()
  {
    m_ComboPower++;
  }
}