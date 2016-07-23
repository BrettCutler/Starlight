using UnityEngine;
using System.Collections;

public class GameBallCharacterController : MonoBehaviour
{
  [SerializeField] private GameObject m_ImpactEffect;

  [SerializeField] private bool m_DebugLog;

  [SerializeField] private float m_RacquetNeutralAngleY;
  [SerializeField] private DelayType m_DelayOnHitRacquet;
  [SerializeField] private DelayType m_DelayOnHitSolidTarget;
  [SerializeField] private DelayType m_DelayOnHitBackWall;
  [SerializeField] private DelayType m_DelayOnHitNonBackWall;

  public enum DelayType
  {
    Never,
    IfNeeded,
    Always
  }

  private bool m_WaitingForMoveContinue;
  //private int m_lastReflectionFrame = 0;

  private Rigidbody m_Rigidbody;
  public float m_Radius;
  private TrailRenderer m_TrailRenderer;
  private TrailRendererHelper m_TrailRendererHelper;
  private bool m_MovementLock = false;

  private static float k_MinYVelocityOffBottom = 5f;

  private const string k_EnvironmentTag = "environment";
  private const string k_RacquetTag = "racquet";
  private const string k_TargetTileTag = "target";
  private const string k_OutOfBoundsTag = "outOfBounds";
  private const string k_BackWallTag = "backWall";
  private const string k_BottomWallTag = "bottomWall";
  private const string k_NearRacquetTag = "nearRacquet";
  private const string k_ComboTargetTag = "comboTarget";

  private const float k_BounceAngleScalar = .1f;
  private static readonly Vector2 k_MinBouncebackAngle = new Vector2( -63f, -63f );
  private static readonly Vector2 k_MaxBouncebackAngle = new Vector2( 63f, 63f );

  public event System.Action OnBallHitRacquet;

  //private string d_FixedPosOutput = "";
  //private string d_FixedTimeOutput = "";

  public Vector3 m_Position
  {
    get { return transform.position; }
    set
    {
      if( m_Rigidbody.isKinematic )
      {
        transform.position = value;
      }
      else
      {
        m_Rigidbody.MovePosition( value );
      }
    }
  }

  public Vector3 m_Velocity
  {
    get { return m_Rigidbody.velocity; }
    set { m_Rigidbody.velocity = value; }
  }

  private void Awake()
  {
    Vector3 ballVel = new Vector3( 0, 5.4f, -10.2f );
    Vector3 racquetNormal = new Vector3( 0f, .9f, .5f );
    Vector3 reflection = ballVel - 2f * Vector3.Dot(ballVel, racquetNormal) * racquetNormal;
    Debug.Log( "last reflect result is: " +
      Vector3.Reflect( ballVel, racquetNormal ) +
      ", hand-calculated is " + reflection);
    // result should be: ( 0f, 13.4, -7.1 )

    //angle = invTan( y,x)

    NameRegistry.Instance.GameObjectRegistry.Add( RegisteredGameObjectNames.GameBall, gameObject );
    NameRegistry.Instance.ScriptsRegistry.Add( RegisteredSingularScripts.GameBallScript, this );

    m_Rigidbody = GetComponent<Rigidbody>();
    m_TrailRenderer = GetComponent<TrailRenderer>();
    m_TrailRendererHelper = GetComponent<TrailRendererHelper>();

    m_Radius = GetComponent<SphereCollider>().radius * transform.localScale.x;

    WaitForMusicManager.Instance.OnDefaultContinue += OnMoveContinue;
    WaitForMusicManager.Instance.OnStopMovement += OnStopMovement;
  }

  //private void FixedUpdate()
  //{
  //  d_FixedPosOutput += "\n" + transform.position.z.ToString( "F4" );
  //  d_FixedTimeOutput += "\n" + Time.time;
  //  //Debug.Log( "FixedUpdate: time = " + Time.time.ToString( "F4" ) +
  //  //  ", pos = " + transform.position.ToString( "F4" ) +
  //  //  ", vel = " + m_Rigidbody.velocity.ToString( "F4" ) );
  //}

  protected void OnCollisionEnter( Collision collision )
  {
    if( m_DebugLog )
    {
      Debug.Log( "BOUNCE, cur velocity = " + GetComponent<Rigidbody>().velocity +
        ", magnitude = " + GetComponent<Rigidbody>().velocity.magnitude );

      Debug.Log( "on collisionenter other tag = " + collision.gameObject.tag +
      ", frame = " + Time.frameCount );

      Debug.Log( "collision relative velocity: " + collision.relativeVelocity +
        ", impulse velocity = " + collision.impulse );
    }

    if( m_WaitingForMoveContinue )
    {
      return;
    }


    // Bounce off bounceable objects
    if( collision.gameObject.CompareTag( k_EnvironmentTag ) ||
        collision.gameObject.CompareTag( k_BackWallTag ) ||
        collision.gameObject.CompareTag( k_BottomWallTag ) ||
        collision.gameObject.CompareTag( k_RacquetTag ) ||
        collision.gameObject.CompareTag( k_TargetTileTag ) ||
        collision.gameObject.CompareTag( k_ComboTargetTag ) )
    {
      //Vector3 newMoveDir = Vector3.Reflect( m_CurVelocity, collision. );

      ////////////////////////////////////////
      StartCoroutine( "WaitAndResetVelocity", collision );
      ////////////////////////////////////////

      //if( !( m_lastReflectionFrame == Time.frameCount ) )
      //{
      //  m_CurVelocity = newMoveDir;
      //  m_lastReflectionFrame = Time.frameCount;

      //  if( m_DebugLog )
      //  {
      //    Debug.Log( "on collisionenter other tag = " + collision.gameObject.tag +
      //    ", frame = " + Time.frameCount +
      //    ", collisionNormal = " + collision.normal +
      //    ", newMoveDir = " + newMoveDir );
      //  }
      //}
      //else
      //{
      //  Debug.LogError( "Second reflection!" +
      //    " on collisionenter other tag = " + collision.gameObject.tag +
      //    ", frame = " + Time.frameCount +
      //    ", collisionNormal = " + collision.normal +
      //    ", newMoveDir = " + newMoveDir );
      //}

      //if( collision.gameObject.CompareTag( k_EnvironmentTag ) )
      //{
      //  AkSoundEngine.PostEvent( "BallHitEnvironment", gameObject );
      //}
      //else if( collision.gameObject.CompareTag( k_RacquetTag ) )
      //{
      //  AkSoundEngine.PostEvent( "BallHitRacquet", gameObject );
      //}
      //else if( collision.gameObject.CompareTag( k_TargetTileTag ) )
      //{
      //  TargetManager.Instance.BallHitTarget( collision.gameObject );

      //  AkSoundEngine.PostEvent( "BallHitTarget", gameObject );
      //}

      //m_WaitingForMoveContinue = true;
    }
    // If this is bounds, reset ball
    else if( collision.gameObject.CompareTag( k_OutOfBoundsTag ) )
    {
      m_Position = new Vector3( 0f, 0f, -200f );
      StartCoroutine( GameManager.Instance.OnLostBall() );
    }
  }

  private void OnTriggerEnter( Collider otherCollider )
  {
    // If this is marker for ball about to hit racquet, calculate impact point, notify racquet to begin swing
    if( otherCollider.gameObject.CompareTag( k_NearRacquetTag ) && m_Rigidbody.velocity.z < 0 )
    {
      float distanceToRacquet = DistanceToRacquet();
      float timeUntilReachRacquet = Mathf.Abs( distanceToRacquet / m_Rigidbody.velocity.z );
      Debug.Log( "estimate until reach racquet|timeOf: " + timeUntilReachRacquet +
        "|" + ( Time.time + timeUntilReachRacquet ) +
        ", pos|vel|time = " + transform.position.z.ToString("F4") + "|" + m_Rigidbody.velocity.z.ToString("F4") + "|" + Time.time +
        ", timeUntilBeat = " + WaitForMusicManager.Instance.m_TimeUntilNextBeat + "|" + ( Time.time + WaitForMusicManager.Instance.m_TimeUntilNextBeat ) +
        ", disparity = " + Mathf.Abs( timeUntilReachRacquet - WaitForMusicManager.Instance.m_TimeUntilNextBeat ) );

      Vector3 impactPointDiscrete = GetTrajectoryPointAtTimeDiscrete( transform.position, m_Rigidbody.velocity, timeUntilReachRacquet );
      Debug.Log( "impactPointDiscrete = " + impactPointDiscrete.ToString( "F4" ) );

      (NameRegistry.Instance.ScriptsRegistry[RegisteredSingularScripts.PlayerRacquetScript] as PlayerRacquet).InitiateSwing( timeUntilReachRacquet, impactPointDiscrete );
    }
    else if( otherCollider.gameObject.CompareTag( k_TargetTileTag ) )
    { 
      TargetManager.Instance.BallHitTarget( otherCollider.gameObject );

      PlayOrDelaySound( "BallHitTarget" );

      ( NameRegistry.Instance.ScriptsRegistry[RegisteredSingularScripts.CameraController] as CameraController ).
        AddCameraShake(
        Mathf.Min( WaitForMusicManager.Instance.m_TimeUntilNextOnDefault, .0625f ), .0625f );

    }
  }

  private Vector3 GetTrajectoryPointAtTimeDiscrete( Vector3 startPos, Vector3 startVelocity, float time )
  {
    float timeStep = Time.fixedDeltaTime;
    Vector3 stepVelocity = timeStep * startVelocity;
    Vector3 stepGravity = timeStep * timeStep * Physics.gravity;
    float iterationSteps = time / timeStep;

    Vector3 pointAtTime = startPos + ( iterationSteps * stepVelocity ) + 
      ( 0.5f * ( iterationSteps * iterationSteps + iterationSteps ) * stepGravity );

    return pointAtTime;
  }

  private IEnumerator WaitAndResetVelocity( Collision collision)
  {
    Vector3 oldVelocity = m_Rigidbody.velocity;

    GameObject.Instantiate( m_ImpactEffect, collision.contacts[0].point, 
        Quaternion.LookRotation( collision.contacts[0].normal ) );

    //// CASES:
    //// * Hit racquet: delay until beat
    //// * Hit back, side wall, target: bounce immediately, delay effects until quarter beat 
    if( collision.gameObject.CompareTag( k_RacquetTag ) )
    {
      PlayerRacquet racquetScript =
        NameRegistry.Instance.ScriptsRegistry[RegisteredSingularScripts.PlayerRacquetScript] as PlayerRacquet;

      if( OnBallHitRacquet != null )
      {
        OnBallHitRacquet();
      }

      ( NameRegistry.Instance.ScriptsRegistry[RegisteredSingularScripts.CameraController] as CameraController ).
        AddCameraShake(
        Mathf.Min( WaitForMusicManager.Instance.m_TimeUntilNextOnDefault, .125f ), .25f );

      Debug.Log( "REAL impact point = " + collision.contacts[0].point.ToString("F4") +
        ", ballPos = " + transform.position.ToString("F4") + 
        ", curBallVelocity = " + m_Rigidbody.velocity +
        ", relativeVelocity = " + collision.relativeVelocity +
        ", impulse = " + collision.impulse +
        ", collision normal = " + collision.contacts[0].normal +
        ", racquet pos = " + collision.transform.position.ToString("F4") +
        ", racquet rotation = " + collision.transform.rotation.eulerAngles.ToString("F4") +
        ", racquet body total rotation = "  + collision.transform.GetChild(0).rotation.eulerAngles.ToString("F4") +
        ",  racquet body rotation-local = " + collision.transform.GetChild(0).localRotation.eulerAngles.ToString("F4") +
        ", impact time|frame = " + Time.time.ToString("F4") + "|" + Time.frameCount);

      // Do we need to delay the ball?
      if( m_DelayOnHitRacquet == DelayType.Always ||
          ( m_DelayOnHitRacquet == DelayType.IfNeeded && !IsCloseToBeatDivision( ) ) )
      {
        Debug.Log( "we're delaying the ball from the racquet," );
        
        WaitForMusicManager.Instance.StopMovement();

        while( m_WaitingForMoveContinue
          && ( Time.frameCount - WaitForMusicManager.Instance.m_FrameLastOnDefault ) > 2 )
        {
          Debug.LogWarning( "racquet delay!" );
          yield return null;
        }
      }

      AkSoundEngine.PostEvent( "BallHitRacquet", gameObject );

      float distanceToBackWall = DistanceToBackWall();
      float timeUntilReachBackWall = GetReasonableTimeUntilNextBeatDivision(distanceToBackWall);

      float newZVelocity = distanceToBackWall /timeUntilReachBackWall;// * Mathf.Sign( oldVelocity.z );
      Vector3 newVelocity = new Vector3( oldVelocity.x, oldVelocity.y, newZVelocity );

      // Calculate bounce vector based on current bounce and racquet input angle
      float bounceAngleX = Mathf.Atan2( oldVelocity.y, oldVelocity.z ) * Mathf.Rad2Deg;
      bounceAngleX = bounceAngleX < 180f ? bounceAngleX : bounceAngleX - 360f;
      bounceAngleX = Mathf.Clamp( bounceAngleX, k_MinBouncebackAngle.x, k_MaxBouncebackAngle.x );

      float inputAngleX = racquetScript.GetBodyRotationEuler( ).x;
      inputAngleX = inputAngleX < 180f ? inputAngleX : inputAngleX - 360f;
      inputAngleX = -inputAngleX + m_RacquetNeutralAngleY; // flip around 0 to match output coordinate system
      inputAngleX = Mathf.Clamp( inputAngleX, k_MinBouncebackAngle.x, k_MaxBouncebackAngle.x );

      float bounceAngleY = Mathf.Atan2( oldVelocity.x, oldVelocity.z ) * Mathf.Rad2Deg;
      bounceAngleY = bounceAngleY < 180f ? bounceAngleY : bounceAngleY - 360f;

      float inputAngleY = racquetScript.GetBodyRotationEuler().y;
      inputAngleY = inputAngleY < 180f ? inputAngleY : inputAngleY - 360f;      

      float endAngleX = ( inputAngleX * ( 1f - k_BounceAngleScalar ) ) +
                        ( bounceAngleX * k_BounceAngleScalar );
      
      endAngleX = Mathf.Clamp( endAngleX, k_MinBouncebackAngle.x, k_MaxBouncebackAngle.x );
      newVelocity.y = newVelocity.z * Mathf.Tan( Mathf.Deg2Rad * endAngleX );

      Debug.Log( "newVelocity = " + newVelocity.ToString( "F4" ) +
        ", inputAngleX = " + inputAngleX +
        ", bounceAngleX = " + bounceAngleX +
        ", endAngleX = " + endAngleX +
        ", oldVelocity = " + oldVelocity.ToString( "F4" ) );
      
      m_Rigidbody.velocity = newVelocity;

      Debug.Log( "GO (racquet) on time||frame " + Time.time + "||" + Time.frameCount +
        ", newVelocity = " + m_Rigidbody.velocity.ToString( "F4" ) +
        ", oldVelocity = " + oldVelocity.ToString( "F4" ) +
        ", distanceToBackWall = " + distanceToBackWall + 
        ", timeUntilReachBackWall = " + timeUntilReachBackWall +
        ", timeUntilNextOddBeat = " + WaitForMusicManager.Instance.m_TimeUntilNextOddBeat +
        ", oddBeatDuration = " + WaitForMusicManager.Instance.m_OddBeatDuration );

      //Debug.Log( "d_FixedPos: " + d_FixedPosOutput );
      //Debug.Log( "d_FixedTime: " + d_FixedTimeOutput );
      //d_FixedPosOutput = "";
      //d_FixedTimeOutput = "";
    }
    else if( collision.gameObject.CompareTag( k_BackWallTag ) )
    {
      ( NameRegistry.Instance.ScriptsRegistry[RegisteredSingularScripts.CameraController] as CameraController ).
        AddCameraShake(
        Mathf.Min( WaitForMusicManager.Instance.m_TimeUntilNextOnDefault, .1f ), .0625f );

      // Do we need to delay the ball?
      if( m_DelayOnHitBackWall == DelayType.Always ||
          ( m_DelayOnHitBackWall == DelayType.IfNeeded && !IsCloseToBeatDivision() ) )
      {
        Debug.Log( "we're delaying the ball from the back wall," );

        WaitForMusicManager.Instance.StopMovement();

        while( m_WaitingForMoveContinue
          && ( Time.frameCount - WaitForMusicManager.Instance.m_FrameLastOnDefault ) > 2 )
        {
          Debug.LogWarning( "back wall delay!" );
          yield return null;
        }
      }
      
      float distanceToRacquet = DistanceToRacquet();

      float timeUntilReachRacquet = GetReasonableTimeUntilNextBeatDivision(distanceToRacquet);

      float newZVelocity = distanceToRacquet / timeUntilReachRacquet * Mathf.Sign( oldVelocity.z );

      m_Rigidbody.velocity = new Vector3( oldVelocity.x, oldVelocity.y, newZVelocity );

      Debug.Log( "GO (backwall) on time||frame " + Time.time + "||" + Time.frameCount +
        ", newVelocity = " + m_Rigidbody.velocity.ToString( "F4" ) +
        ", oldVelocity = " + oldVelocity.ToString( "F4" ) +
        ", zPos = " + transform.position.z.ToString("F4") +
        ", distanceToRacquet = " + distanceToRacquet +
        ", next odd beat time: " + 
        (WaitForMusicManager.Instance.m_TimeUntilNextOddBeat + Time.time ) +
        ", next-next odd beat time: " + 
        (WaitForMusicManager.Instance.m_TimeUntilNextOddBeat +
          Time.time + WaitForMusicManager.Instance.m_OddBeatDuration ) );
      
      PlayOrDelaySound( "BallHitEnvironment" );
    }
    else
    {
      ( NameRegistry.Instance.ScriptsRegistry[RegisteredSingularScripts.CameraController] as CameraController ).
        AddCameraShake(
        Mathf.Min( WaitForMusicManager.Instance.m_TimeUntilNextOnDefault, .125f ), .125f );

      // Do we need to delay the ball?
      bool hitEnvironment = ( collision.gameObject.CompareTag( k_EnvironmentTag ) ||
                                    collision.gameObject.CompareTag( k_BottomWallTag ) );
      bool delayFromEnvironment = hitEnvironment && 
        ( m_DelayOnHitNonBackWall == DelayType.Always ||
          ( m_DelayOnHitNonBackWall == DelayType.IfNeeded && !IsCloseToBeatDivision( ) ) );

      bool hitTarget = collision.gameObject.CompareTag( k_TargetTileTag );
      bool delayFromTarget = hitTarget &&
        ( m_DelayOnHitSolidTarget == DelayType.Always ||
          ( m_DelayOnHitSolidTarget == DelayType.IfNeeded && !IsCloseToBeatDivision( ) ) );

      if( delayFromEnvironment || delayFromTarget )
      {
        Debug.Log( "we're delaying the ball from the back wall," );

        WaitForMusicManager.Instance.StopMovement();

        while( m_WaitingForMoveContinue
          && ( Time.frameCount - WaitForMusicManager.Instance.m_FrameLastOnDefault ) > 2 )
        {
          Debug.LogWarning( "back wall delay!" );
          yield return null;
        }
      }

      float zMoveSign = Mathf.Sign( oldVelocity.z );
      float distanceToImpact;
      if( zMoveSign < 0 ) // towards racquet
      {
        distanceToImpact = DistanceToRacquet();
      }
      else // towards back wall
      {
        distanceToImpact = DistanceToBackWall();
      }
      float timeUntilBeat = GetReasonableTimeUntilNextBeatDivision( distanceToImpact );

      Vector3 newVelocity = oldVelocity;
      //// MAINTAINING CONSTANT Z SPEED
      newVelocity.z = distanceToImpact / timeUntilBeat * zMoveSign;

      if( collision.gameObject.CompareTag( k_BottomWallTag ) )
      {
        newVelocity.y = Mathf.Max( oldVelocity.y, k_MinYVelocityOffBottom );
      }

      //float newZVelocity = GameManager.Instance.CalculateZVelocity() * Mathf.Sign( oldVelocity.z );

      //Debug.Log( "new velZ calculation: calcZ[ " + GameManager.Instance.CalculateZVelocity() +
      //  " * sign( " + Mathf.Sign( oldVelocity.z ) + " )" );

      m_Rigidbody.velocity = newVelocity;

      Debug.Log( "GO on frame " + Time.frameCount + ", newVelocity = " + m_Rigidbody.velocity +
        ", oldVelocity = " + oldVelocity );

      if( collision.gameObject.CompareTag( k_EnvironmentTag ) ||
          collision.gameObject.CompareTag( k_BottomWallTag ) )
      {
        PlayOrDelaySound( "BallHitEnvironment" );
      }
      else if( collision.gameObject.CompareTag( k_TargetTileTag ) )
      {
        TargetManager.Instance.BallHitTarget( collision.gameObject );

        PlayOrDelaySound( "BallHitTarget" );
      }
      else if( collision.gameObject.CompareTag( k_ComboTargetTag ) )
      {
        ComboTarget ctScript = collision.gameObject.GetComponent<ComboTarget>();
        ctScript.OnHitByBall();

        PlayOrDelaySound( "BallHitTarget" );
      }  

      yield break;
    }
  }

  public void OnMoveContinue()
  {
    if( !m_MovementLock )
    {
      m_Rigidbody.isKinematic = false;

      m_WaitingForMoveContinue = false;
    }
  }

  public void OnStopMovement()
  {
    m_Rigidbody.isKinematic = true;

    m_WaitingForMoveContinue = true;
  }

  public void LockMovement( bool state )
  {
    m_MovementLock = state;
  }

  public void SetTrailEnabled( bool setting )
  {
    m_TrailRenderer.enabled = setting;
    m_TrailRendererHelper.enabled = setting;
  }

  private float DistanceToRacquet()
  {
    return transform.position.z;// + m_Radius;
  }

  private float DistanceToBackWall()
  {
    return GameManager.Instance.m_RoomDepth - transform.position.z - m_Radius;
  }

  private bool IsClostToOnDefault( )
  {
    float timeSinceLastDefault = (Time.time - WaitForMusicManager.Instance.m_TimeLastOnDefault );
    float timeUntilNextDefault = WaitForMusicManager.Instance.m_TimeUntilNextOnDefault;

    if( !( timeSinceLastDefault <= .06f || timeUntilNextDefault <= .06f ) )
    {
      Debug.Log( "timeSinceLastOddBeat = " + timeSinceLastDefault +
        ", timeUntilNextOddBeat = " + timeUntilNextDefault );
    }

    return timeSinceLastDefault <= .06f || timeUntilNextDefault <= .06f;
  }

  private bool IsCloseToBeatDivision( )
  {
    float timeSinceLastDivision = Time.time -
      ( GameManager.Instance.m_BeatsPerBallTraversal == 1 ? 
          WaitForMusicManager.Instance.m_TimeLastOnBeat : 
          WaitForMusicManager.Instance.m_TimeLastOnOddBeat );

    float timeUntilNextDivision = GameManager.Instance.m_BeatsPerBallTraversal == 1 ?
      WaitForMusicManager.Instance.m_TimeUntilNextBeat :
      WaitForMusicManager.Instance.m_TimeUntilNextOddBeat;

    bool timeSinceAcceptable = timeSinceLastDivision <= WaitForMusicManager.k_MaxAcceptableDeviationFromBeat;
    bool timeUntilAcceptable = timeUntilNextDivision <= WaitForMusicManager.k_MaxAcceptableDeviationFromBeat;

    if( !( timeSinceAcceptable || timeUntilAcceptable ) )
    {
      Debug.Log( "!CloseToLastBeatDivision: timeSinceLastDivision = " + timeSinceLastDivision.ToString( "F4" ) +
        ", timeUntilNextDivision = " + timeUntilNextDivision );
    }

    return timeSinceAcceptable || timeUntilAcceptable;
  }

  /// <summary>
  /// Return time until next ball-sync beat division.
  /// If we are slightly before the next one, return time
  /// until next-next beat division.
  /// </summary>
  private float GetReasonableTimeUntilNextBeatDivision( float distance )
  {
    float timeUntil = GameManager.Instance.m_BeatsPerBallTraversal == 1 ?
      WaitForMusicManager.Instance.m_TimeUntilNextBeat :
      WaitForMusicManager.Instance.m_TimeUntilNextOddBeat;

    // If we have a very small time until next beat, it means we're early. Therefore, roll
    // over the next beat
    // Use the distance property to normalize this.
    // Ex: we're heading to the back wall, 2.5 units away, we bounce off
    // the ground, and the we're 75% of the way to the next odd beat --
    // this should return the remaining 25% of oddbeatduration, not flip over
    // and return 100% + 25%
    if( timeUntil < WaitForMusicManager.Instance.m_OddBeatDuration * 
        ( 0.5f * distance / ( GameManager.Instance.m_RoomDepth - m_Radius ) ) )
    {
      timeUntil += WaitForMusicManager.Instance.m_OddBeatDuration;
    }

    return timeUntil;
  }

  private void PlayOrDelaySound( string soundEventName )
  {
    bool delaySound = !IsClostToOnDefault( );
    bool delayByBeatDivision = !IsCloseToBeatDivision( );

    if( delaySound )
    {
      Debug.LogError( "delayed sound!" );

      Debug.Log( "ODD BEAT: framesSINCE = " + ( Time.frameCount - WaitForMusicManager.Instance.m_FrameOnLastOnOddBeat ) +
        ", framesUNTIL = " + WaitForMusicManager.Instance.m_FramesUntilNextOddBeat +
        ", delayByBeatDivision= " + delayByBeatDivision );

      Debug.Log( "DEFAULT: framesSINCE = " + ( Time.frameCount - WaitForMusicManager.Instance.m_FrameLastOnDefault ) +
        ", framesUNTIL = " + WaitForMusicManager.Instance.m_FramesUntilNexOnDefault +
        ", delay = " + delaySound );

      PostAudioEventDelayer.Instance.PostDelayedSound(
        "BallHitEnvironment", MusicDivision.OnQuarterBeat, gameObject );
    }
    else
    {
      AkSoundEngine.PostEvent( "BallHitEnvironment", gameObject );
      //Debug.Log( "play HitEnvironment time||frame: " + Time.time + "||" + Time.frameCount );
    }
  }
}
