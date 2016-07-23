using UnityEngine;
using System.Collections;
using System;

public class PlayerRacquet : SimpleStateMachine
{
  [SerializeField] private float m_MaxVelocity;
  [SerializeField] private float m_Acceleration;
  [SerializeField] private float m_Friction;
  [SerializeField] private AnimationCurve m_FrictionAtInputMagnitude;
  [SerializeField] private bool m_AutoReturnToCenter;
  /// <summary>
  /// Max angle of rotation in x and y.
  /// </summary>
  [SerializeField] private float m_XYRotationConstraints;
  public Vector2 m_BoundsTopLeft;
  public Vector2 m_BoundsBottomRight;
  public Vector2 m_AnimBoundsTopLeft;
  public Vector2 m_AnimBoundsBottomRight;
  public event System.Action OnRacquetUpdatePosition;
  [SerializeField] private AnimationCurve m_BounceBackCurve;
  [SerializeField] private AnimationClip m_RacquetSwingAnimClip;
  [SerializeField] private Transform m_Body;

  private AnimationState m_RacquetSwingState;
  private AnimationState m_AdjustHeightState;
  [SerializeField] private AnimationCurve m_BodyXAngleDuringSwing;

  [SerializeField] private bool d_AutoMatchXOnSwing;

  [SerializeField] private bool m_PlayerActivatedSwing;

  [SerializeField] private bool m_PlayerHasYControl;

  private Vector3 m_CurVelocity;
  private bool m_WaitingForMoveContinue;
  private Rigidbody m_Rigidbody;
  private Animation m_AnimationComponent;

  /// <summary>
  /// Flag: is player allowed to press button to initiate swing?
  /// Set false only after player initiates, or game resets
  /// </summary>
  private bool m_BallHasTriggeredSwing;


  private float m_NextSwingDuration;
  private float m_BallArrivalTime;
  private float m_FinishSwingingTime;
  private Vector3 m_NextSwingImpactPoint;
  private float k_MaxSwingTime = 1f;

  private const string k_AdjustHeightClipName = "yHeightClip";

  enum RacquetStates
  {
    WaitingForGameStart,
    Normal,
    WaitingToBeginSwing,
    Swinging,
    SwingingAfterHitBall
  }

  private void Awake()
  {
    NameRegistry.Instance.GameObjectRegistry.Add( RegisteredGameObjectNames.Racquet, gameObject );
    NameRegistry.Instance.ScriptsRegistry.Add( RegisteredSingularScripts.PlayerRacquetScript, this );

    m_Rigidbody = GetComponent<Rigidbody>();
    m_AnimationComponent = GetComponent<Animation>();

    WaitForMusicManager.Instance.OnDefaultContinue += OnMoveContinue;
    WaitForMusicManager.Instance.OnStopMovement += OnStopMovement;

    currentState = RacquetStates.WaitingForGameStart;
  }

  private void Start()
  {
    (NameRegistry.Instance.ScriptsRegistry[RegisteredSingularScripts.GameBallScript] 
      as GameBallCharacterController).OnBallHitRacquet += OnBallHitRacquet;
  }

  protected override void LateGlobalSuperUpdate()
  {
    // This triggers camera pos update, which is recommended to be
    // done on LateUpdate
    if( OnRacquetUpdatePosition != null )
    {
      OnRacquetUpdatePosition();
    }
  }

  public void OnGameStart()
  {
    currentState = RacquetStates.WaitingForGameStart;
  }

  void WaitingForGameStart_EnterState()
  {
    transform.position = GameManager.Instance.m_RacquetStartPos;
    m_BallHasTriggeredSwing = false;

    // at the moment, we have no 'holding' behavior while waiting for game start, so head
    // straight into normal movement
    currentState = RacquetStates.Normal;
  }

  void Normal_Update()
  {
    UpdatePositionFromVelocity();
  }
  
  void WaitingToBeginSwing_EnterState()
  {
    m_NextSwingDuration = m_BallArrivalTime - Time.time;

    // Delay until we're k_MaxSwingTime away from ball's arrival
    if( m_NextSwingDuration > k_MaxSwingTime )
    {
      m_NextSwingDuration = k_MaxSwingTime;
    }
  }

  void WaitingToBeginSwing_Update()
  {
    UpdatePositionFromVelocity();

    if( m_BallArrivalTime - Time.time <= m_NextSwingDuration )
    {
      currentState = RacquetStates.Swinging;
    }
  }

  void Swinging_EnterState()
  {
    // Add an animation to reach ball's height
    AnimationCurve adjustHeightToBallCurve = new AnimationCurve();
    adjustHeightToBallCurve.AddKey( 0f, 0f );
    adjustHeightToBallCurve.AddKey( 0.5f,
      Mathf.Clamp( m_NextSwingImpactPoint.y, m_AnimBoundsBottomRight.y, m_AnimBoundsTopLeft.y ) );
    adjustHeightToBallCurve.AddKey( 1f, 0f );

    AnimationCurve maintainZDepthCurve = AnimationCurve.Linear(
      0f, GameManager.Instance.m_RacquetStartPos.z, 1f, GameManager.Instance.m_RacquetStartPos.z );

    AnimationClip adjustHeightClip = new AnimationClip();
    adjustHeightClip.legacy = true;
    adjustHeightClip.name = k_AdjustHeightClipName;
    adjustHeightClip.SetCurve( "Body", typeof( Transform ), "localPosition.y", adjustHeightToBallCurve );
    adjustHeightClip.SetCurve( "Body", typeof( Transform ), "localPosition.z", maintainZDepthCurve );
    m_AnimationComponent.AddClip( adjustHeightClip, adjustHeightClip.name );

    // Adjust playback speed to match time to swing
    // Duration is *2 for followthrough
    float animDuration = m_NextSwingDuration * 2f;
    m_RacquetSwingState = m_AnimationComponent[m_RacquetSwingAnimClip.name];
    m_RacquetSwingState.speed = m_RacquetSwingState.length / animDuration;

    m_FinishSwingingTime = Time.time + animDuration;

    m_AdjustHeightState = m_AnimationComponent[adjustHeightClip.name];
    m_AdjustHeightState.speed = m_RacquetSwingState.length / animDuration;

    // Make sure they have different layers so they don't cancel each other out
    m_RacquetSwingState.layer = 1;
    m_AdjustHeightState.layer = 2;

    // Set blend states. Leave primary at blend, so they aren't purely adding onto previous frame
    m_RacquetSwingState.blendMode = AnimationBlendMode.Blend;
    m_AdjustHeightState.blendMode = AnimationBlendMode.Blend;

    //m_RacquetSwingState.weight = 1f;
    //m_AdjustHeightState.weight = 1f;

    //m_RacquetSwingState.enabled = true;
    //m_AdjustHeightState.enabled = true;

    //m_RacquetSwingState.speed = 0f;
    //m_AdjustHeightState.speed = 0f;

    m_AnimationComponent.Play( m_RacquetSwingAnimClip.name, PlayMode.StopSameLayer );
    m_AnimationComponent.Play( adjustHeightClip.name, PlayMode.StopSameLayer );
  }

  void Swinging_Update()
  {
    UpdatePositionFromVelocity();

    if( m_WaitingForMoveContinue )
    {
      m_BallArrivalTime += Time.deltaTime;
      m_NextSwingDuration += Time.deltaTime;
      m_FinishSwingingTime += Time.deltaTime;

      m_RacquetSwingState.speed = 0f;
      m_AdjustHeightState.speed = 0f;
    }
    else
    {
      float animDuration = m_NextSwingDuration * 2f;
      m_RacquetSwingState.speed = m_RacquetSwingState.length / animDuration;
      m_AdjustHeightState.speed = m_RacquetSwingState.length / animDuration;

      //Debug.Log( "begin swing update, body rot = " + m_Body.eulerAngles +
      //  ", body local rot = " + m_Body.localEulerAngles +
      //  ", root rot = " + transform.eulerAngles );

    ///// clamping rotation based on anticipated ball angle
      //float swingPercent = 1f - ( (m_BallArrivalTime - Time.time) / m_NextSwingDuration );
      ////m_RacquetSwingState.normalizedTime = swingPercent * .5f;
      ////m_AdjustHeightState.normalizedTime = swingPercent * .5f;

      ////m_AnimationComponent.Sample();

      //Vector3 ballVelocity = (NameRegistry.Instance.ScriptsRegistry[RegisteredSingularScripts.GameBallScript]
      //as GameBallCharacterController).m_Velocity;

      //// flip zVelocity to positive, this better orients our output
      //float angleOfBallYRad = Mathf.Atan2( ballVelocity.y, -ballVelocity.z );
      //float angleOfBallY = angleOfBallYRad * Mathf.Rad2Deg;
      //float minXEulerRotation = (-90f + angleOfBallY ) * .5f;
      //float maxXEulerRotation = minXEulerRotation + 90f;

      //float angleOfBallXRad = Mathf.Atan2( ballVelocity.x, -ballVelocity.z );
      //float angleOfBallX = angleOfBallXRad * Mathf.Rad2Deg;
      //float minYEulerRotation = (-90f + angleOfBallX ) * .5f;
      //float maxYEulerRotation = minYEulerRotation + 90f;

      //Vector3 oldEulerAngles = m_Body.eulerAngles;
      //Vector3 bodyRotation = m_Body.eulerAngles;
      //Vector3 rootRotation = transform.eulerAngles;

      //float bodyRotationX = m_BodyXAngleDuringSwing.Evaluate( swingPercent );

      //bodyRotationX = bodyRotation.x < 180f ? bodyRotation.x : bodyRotation.x - 360f;
      //float bodyRotationY = bodyRotation.y < 180f ? bodyRotation.y : bodyRotation.y - 360f;

      //bodyRotation.x = Mathf.Clamp( bodyRotationX, minXEulerRotation, maxXEulerRotation );
      //bodyRotation.y = Mathf.Clamp( bodyRotationY, minYEulerRotation, maxYEulerRotation );

      //m_Body.eulerAngles = bodyRotation;

      //Debug.Log( "t= " + Time.time.ToString( "F4" ) +
      //  ", Clamp euler angles: old = " + oldEulerAngles.ToString( "F4" ) +
      //  ", new = " + m_Body.eulerAngles.ToString( "F4" ) +
      //  ", root rot = " + transform.localEulerAngles +
      //  ", minXEuler = " + minXEulerRotation +
      //  ", maxXEuler = " + maxXEulerRotation +
      //  ", minYEuler = " + minYEulerRotation +
      //  ", maxYEuler = " + maxYEulerRotation +
      //  ", ball Velocity = " + ballVelocity );
    }
  }

  void SwingingAfterHitBall_EnterState()
  {
    // disable collider until animation finishes so we don't get multiple ball impacts
    // if we're waiting on a beat to continue
    m_Body.GetComponent<Collider>().enabled = false;
  }

  void SwingingAfterHitBall_Update()
  {
    if( Time.time >= m_FinishSwingingTime )
    {
      currentState = RacquetStates.Normal;
    }
    else if( m_WaitingForMoveContinue )
    {
      m_BallArrivalTime += Time.deltaTime;
      m_NextSwingDuration += Time.deltaTime;
      m_FinishSwingingTime += Time.deltaTime;

      m_RacquetSwingState.speed = 0f;
      m_AdjustHeightState.speed = 0f;
    }
    else
    {      
      float animDuration = m_NextSwingDuration * 2f;
      m_RacquetSwingState.speed = m_RacquetSwingState.length / animDuration;
      m_AdjustHeightState.speed = m_RacquetSwingState.length / animDuration;

      //float swingPercent = 1f - ( m_FinishSwingingTime - Time.time ) / ( m_NextSwingDuration * 2f );

      //Vector3 eulerAngles = m_Body.eulerAngles;
      //eulerAngles.x = m_BodyXAngleDuringSwing.Evaluate( swingPercent );
      //m_Body.eulerAngles = eulerAngles;

      //m_RacquetSwingState.normalizedTime = swingPercent;
      //m_AdjustHeightState.normalizedTime = swingPercent;

      //m_AnimationComponent.Sample();
    }

    UpdatePositionFromVelocity();
  }

  void SwingingAfterHitBall_ExitState()
  {
    // Animation cleanup
    m_AnimationComponent.Stop();
    m_AnimationComponent.RemoveClip( k_AdjustHeightClipName );

    transform.position = new Vector3( 
      transform.position.x,
      GameManager.Instance.m_RacquetStartPos.y,
      GameManager.Instance.m_RacquetStartPos.z );  

    m_Body.transform.rotation = Quaternion.identity;
    m_Body.transform.localPosition = Vector3.zero;

    m_Body.GetComponent<Collider>().enabled = true;
  }

  public void OnBallHitRacquet()
  {
    if( (RacquetStates)currentState == RacquetStates.Swinging )
    {
      currentState = RacquetStates.SwingingAfterHitBall;

      m_BallHasTriggeredSwing = false;
    }
  }

  private void OnMoveContinue()
  {
    m_WaitingForMoveContinue = false;
  }

  private void OnStopMovement()
  {
    m_WaitingForMoveContinue = true;
  }

  public void PlayerRequestSwing( )
  {
    if( m_PlayerActivatedSwing && 
        (RacquetStates)currentState == RacquetStates.Normal &&
        m_BallHasTriggeredSwing )
    {
      m_BallHasTriggeredSwing = false;

      currentState = RacquetStates.WaitingToBeginSwing;
    }
  }

  public void Move( Vector2 moveDir, Vector2 aimDir )
  {
    if( !m_PlayerHasYControl )
    {
      // take input from both aim and move on the y,
      // and use the greater of the two
      float absYAim = Mathf.Abs( aimDir.y );
      float absYMove = Mathf.Abs( moveDir.y );

      aimDir.y = absYAim > absYMove ? aimDir.y : moveDir.y;

      //zero out y movement
      moveDir.y = 0f;
    }

    AdjustMovement( moveDir );
    AdjustAngle( aimDir );
  }
  
  private void AdjustMovement( Vector2 moveDir )
  {
    bool returning = false;
    Vector3 centerPos = GameManager.Instance.m_RacquetStartPos;
    // Auto-return to center
    if( m_AutoReturnToCenter && moveDir.magnitude < 0.2f )
    {
      moveDir = ( centerPos - transform.position );
      returning = true;
    }

    // Clamp position
    if( transform.position.x <= m_BoundsTopLeft.x )
    {
      transform.position = new Vector3( m_BoundsTopLeft.x, transform.position.y, transform.position.z );
      moveDir.x = Mathf.Max( 0f, moveDir.x );
    }
    if( transform.position.y >= m_BoundsTopLeft.y )
    {
      transform.position = new Vector3( transform.position.x, m_BoundsTopLeft.y, transform.position.z );
      moveDir.y = Mathf.Min( 0f, moveDir.y );
    }
    if( transform.position.x >= m_BoundsBottomRight.x )
    {
      transform.position = new Vector3( m_BoundsBottomRight.x, transform.position.y, transform.position.z );
      moveDir.x = Mathf.Min( 0f, moveDir.x );
    }
    if( transform.position.y <= m_BoundsBottomRight.y )
    {
      transform.position = new Vector3( transform.position.x, m_BoundsBottomRight.y, transform.position.z );
      moveDir.y = Math.Max( 0f, moveDir.y );
    }

    Vector2 accelerationForce = moveDir * m_Acceleration * Time.deltaTime;
    Vector3 outputVelocity = new Vector3( accelerationForce.x, accelerationForce.y, 0f ) ;// + m_Rigidbody.velocity;

    // Cap at max velocity
    float outputVelocityMagnitude = Mathf.Min( outputVelocity.magnitude, m_MaxVelocity );

    // Apply friction; scale friction based on input magnitude (so it doesn't affect us at full throttle as much)
    float frictionScalar = m_FrictionAtInputMagnitude.Evaluate( moveDir.magnitude );
    // Clamp friction so we don't get pushed opposite our input
    float friction = 1f - Mathf.Clamp( (m_Friction * frictionScalar * Time.deltaTime ), 0f, .95f );

    outputVelocityMagnitude *= friction;

    outputVelocity = outputVelocity.normalized * outputVelocityMagnitude;
    
    // Clamp velocity so we don't go through next frame
    Vector2 nextFramePosition = transform.position + outputVelocity * Time.deltaTime;
    if( nextFramePosition.x <= m_BoundsTopLeft.x )
    {
      float xDistance = m_BoundsTopLeft.x - transform.position.x;
      outputVelocity.x = xDistance * ( 1 / Time.deltaTime );
    }
    if( nextFramePosition.y >= m_BoundsTopLeft.y )
    {
      float yDistance = m_BoundsTopLeft.y - transform.position.y;
      outputVelocity.y = yDistance * ( 1 / Time.deltaTime );
    }
    if( nextFramePosition.x >= m_BoundsBottomRight.x )
    {
      float xDistance = m_BoundsBottomRight.x - transform.position.x;
      outputVelocity.x = xDistance * ( 1 / Time.deltaTime );
    }
    if( nextFramePosition.y <= m_BoundsBottomRight.y )
    {
      float yDistance = m_BoundsBottomRight.y - transform.position.y;
      outputVelocity.y = yDistance * ( 1 / Time.deltaTime );
    }

    // Clamp velocity on returning
    if( returning )
    {
      Vector2 distanceToCenter = centerPos - transform.position;
      if( (outputVelocity.x > 0 && nextFramePosition.x > centerPos.x ) ||
          (outputVelocity.x < 0 && nextFramePosition.x < centerPos.x ) )
      {
        outputVelocity.x = distanceToCenter.x * (1 / Time.deltaTime);
      }
      if( (outputVelocity.y > 0 && nextFramePosition.y > centerPos.y ) ||
          (outputVelocity.y < 0 && nextFramePosition.y < centerPos.y ) )
      {
        outputVelocity.y = distanceToCenter.y * ( 1 / Time.deltaTime );
      }
    }

    m_CurVelocity = outputVelocity;

    // Catch an issue where improper friction caused us to move
    // opposite our x input
    if( Mathf.Sign(m_CurVelocity.x) != Mathf.Sign( moveDir.x ) )
    {
      Debug.LogError( "move sign x flipped!" );
    }
  }

  private void UpdatePositionFromVelocity()
  {
    if( !m_WaitingForMoveContinue )
    {
      Vector3 endPos = m_Rigidbody.position + ( m_CurVelocity * Time.deltaTime );

      if( d_AutoMatchXOnSwing )
      {
        endPos.x = NameRegistry.Instance.GameObjectRegistry[RegisteredGameObjectNames.GameBall].transform.position.x;
      }

      m_Rigidbody.MovePosition( endPos );
    }
  }

  private void AdjustAngle( Vector2 angleDir )
  {
    if( !m_WaitingForMoveContinue )
    {
      //transform.rotation = Quaternion.Euler( new Vector3( -angleDir.y * m_XYRotationConstraints,
      //                                      angleDir.x * m_XYRotationConstraints, 0f ) );

      // switching to rotation on the body so the position of body collider doesn't get wildly offset
      // during a swing
      m_Body.rotation = Quaternion.Euler( new Vector3( -angleDir.y * m_XYRotationConstraints,
                                            angleDir.x * m_XYRotationConstraints, 0f ) );
    }
  }

  public void InitiateSwing( float timeUntilReachRacquet, Vector3 impactPoint )
  {
    m_NextSwingDuration = timeUntilReachRacquet;
    m_BallArrivalTime = Time.time + timeUntilReachRacquet;
    m_NextSwingImpactPoint = impactPoint;

    m_BallHasTriggeredSwing = true;

    if( !m_PlayerActivatedSwing )
    {
      currentState = RacquetStates.WaitingToBeginSwing;
    }
  }

  public Vector3 GetBodyRotationEuler()
  {
    return m_Body.eulerAngles;
  }
}
