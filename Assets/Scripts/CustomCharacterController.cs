using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class CustomCharacterController : MonoBehaviour
{
  /// <summary>
  /// Each layer we can register collisions against.
  /// Unlike PhysX, collisions do not have to be reciprocal.
  /// </summary>
  [SerializeField] protected LayerMask m_CollideableLayers;

  /// <summary>
  /// Applied each update
  /// </summary>
  [SerializeField] protected Vector3 m_GravityAcceleration;

  /// <summary>
  /// Collisions to be processed
  /// </summary>
  private List<CustomCollisionEvent> m_CollisionData;

  /// <summary>
  /// Flag used to stop movement while waiting for
  /// WaitForMusicController to allow us to continue
  /// </summary>
  protected bool m_WaitingForMoveContinue;

  /// <summary>
  /// Objects we collide with will be temporarily set
  /// to this layer to ensure raycast tests succeed.
  /// Filled with k_TemporaryLayerName.
  /// </summary>
  protected int m_TemporaryLayerIndex;
  
  /// <summary>
  /// Recorded value of physics timestamp;
  /// so we can break a long frame into smaller chunks
  /// </summary>
  protected float m_DeltaTime;

  /// <summary>
  /// Equivalent to rigidbody.velocity
  /// Allow children to override getter/setter
  /// functions if other behavior must happen
  /// (for example, guaranteeing speed follows tempo)
  /// </summary>
  public Vector3 m_CurVelocity
  {
    get { return m_CurVelocityIntern; }
    set { m_CurVelocityIntern = value; }
  }
  private Vector3 m_CurVelocityIntern;

  /// <summary>
  /// Container for OverlapShape physics call, to avoid garbage generation
  /// </summary>
  protected Collider[] m_OverlapShapeResults = new Collider[10];

  /// <summary>
  /// Max number of steps to call RecursivePushback
  /// </summary>
  protected static int k_MaxPushbackIterations = 2;
  /// <summary>
  /// When testing against other colliders, use a sphere collider of this size
  /// to smooth over small bumps.
  /// This may result in pushback in the wrong direction!
  /// Experimentally, though, it seems to resolve itself after a few iterations,
  /// before any time passes
  /// </summary>
  protected static float k_SmallTolerance = .01f;

  // Layer names we will look up when we need them
  protected static string k_TemporaryLayerName = "RaycastTest";

  [SerializeField] protected bool m_DebugLog;

  public int gravityTicks = 0;
  public int gravityTicksWhileGoingUp = 0;
  public int gravityTicksWhileGoingDown = 0;
  protected float maxYPos = 0f;

  protected virtual void Awake()
  {
    m_CollisionData = new List<CustomCollisionEvent>();
    m_TemporaryLayerIndex = LayerMask.NameToLayer( k_TemporaryLayerName );

    WaitForMusicManager.Instance.OnDefaultContinue += OnMoveContinue;
  }

  protected virtual void Update()
  {
    // Run update steps until we've caught up to dt
    // TODO: implement this on a fixed update
    // TODO: throw ANGRY warnings if velocity is ever high enough that we might
    // start tunneling (i.e., velocity * dt > radius )
    // Or, run more iterations at radius size (*and* angrily warn)
    if( !m_WaitingForMoveContinue )
    {
      m_DeltaTime = Time.deltaTime;
      StepUpdate();
    }
  }

  public virtual void SetPosition( Vector3 newPosition )
  {
    transform.position = newPosition;
  }

  public void SetVelocity( Vector3 newVelocity )
  {
    m_CurVelocity = newVelocity;
  }

  protected abstract int OverlapShapeNonAlloc();
  protected abstract bool PointWithinShape( Vector3 point );
  protected abstract float DistanceToClosestSurfaceFromInside( Vector3 point );
  protected abstract float DistanceToClosestSurfaceFromOutside( Vector3 point );
  protected abstract void OnCollisionEnterCustom( CustomCollisionEvent collision );

  private void OnMoveContinue()
  {
    m_WaitingForMoveContinue = false;
  }

  protected virtual void StepUpdate()
  {
    // 1)MOVEMENT
    // 2)PUSHBACK
    // 3)RESOLUTION

    // Pushback before movement, because no other objects create collisions
    // This way, we can generate collisions if objects move into us and we 
    // would otherwise move out of their way
    RecursivePushback( 0, 1 );

    // MOVEMENT
    if( m_CurVelocity.y < 0 ) { gravityTicksWhileGoingDown++; }
    else { gravityTicksWhileGoingUp++; }
    gravityTicks++;
    maxYPos = Mathf.Max( transform.position.y, maxYPos );

    m_CurVelocity += m_GravityAcceleration * m_DeltaTime;
    transform.position += m_CurVelocity * m_DeltaTime;

    m_CollisionData.Clear();

    // PUSHBACK
    RecursivePushback( 0, k_MaxPushbackIterations );

    ProcessCollisions();
  }

  private void RecursivePushback( int depth, int maxDepth )
  {
    bool didContact = false;

    int collisionCount = OverlapShapeNonAlloc( );
    for( int i = 0; i < collisionCount; ++i )
    {
      if( m_OverlapShapeResults[i].isTrigger )
        continue;

      Vector3 contactPoint = CustomCollisions.ClosestPointOnSurface(
                                m_OverlapShapeResults[i], transform.position );
      if( m_DebugLog ) {
        Debug.Log( "contactPoint = " + contactPoint.ToString( "F5" ) + 
          ", other obj = " + m_OverlapShapeResults[i].gameObject.name ); }

      if( contactPoint != Vector3.zero )
      {
        Vector3 pushVector = contactPoint - transform.position;

        if( pushVector != Vector3.zero )
        {
          // Temporarily change the other object's layer so we can cast against it
          int otherLayer = m_OverlapShapeResults[i].gameObject.layer;
          m_OverlapShapeResults[i].gameObject.layer = m_TemporaryLayerIndex;

          // Check which side of the normal we are on
          bool facingNormal = Physics.SphereCast(
            new Ray( transform.position, pushVector.normalized ),
            k_SmallTolerance,
            pushVector.magnitude + k_SmallTolerance,
            1 << m_TemporaryLayerIndex 
            );

          // now put the layer back
          m_OverlapShapeResults[i].gameObject.layer = otherLayer;

          // Orient and scale our vector based on which side of the normal we are situated
          if( facingNormal )
          {
            if( PointWithinShape( contactPoint ) )
            {
              pushVector = pushVector.normalized * DistanceToClosestSurfaceFromInside( pushVector ) * -1;
              // implemented in example as:
              // pushVector = pushVector.normalized * ( radius - pushVector.magnitude ) * -1;
              // trying it in DistanceToClosestSurface as (radius + vector.magnitude), look for problems
            }
            else // a previously resolved collision has had a side effect that moved  us outside this collider
            {
              continue;
            }
          }
          else
          {
            pushVector = pushVector.normalized * DistanceToClosestSurfaceFromOutside( pushVector );
            // implemented in example as:
            // pushVector = pushVector.normalized * ( radius + pushVector.magnitude );
          }

          didContact = true;

          // Move out of collision with object
          if( m_DebugLog )
          {
            Debug.Log( "pushback: result = transform.position + pushVector || ( " +
            ( transform.position + pushVector ).ToString( "F3" ) + " = " +
            transform.position.ToString( "F3" ) + " + " + pushVector.ToString( "F3" ) + " ) " );
          }

          transform.position += pushVector;

          // Record a collision event, at the point outside the object we have resolved with
          m_OverlapShapeResults[i].gameObject.layer = m_TemporaryLayerIndex;
          RaycastHit normalHit;
          //Physics.SphereCast(
          //  new Ray( transform.position, contactPoint - transform.position ),
          //  k_SmallTolerance,
          //  out normalHit,
          //  1 << m_TemporaryLayerIndex );
          Physics.Raycast(
            new Ray( transform.position, contactPoint - transform.position ),
            out normalHit,
            1 << m_TemporaryLayerIndex );

          m_OverlapShapeResults[i].gameObject.layer = otherLayer;
          CustomCollisionEvent collision = new CustomCollisionEvent()
          {
            gameObject = m_OverlapShapeResults[i].gameObject,
            point = contactPoint,
            normal = normalHit.normal
          };

          if( m_DebugLog )
          {
            Debug.Log( "new collision, gameObject = " +
              collision.gameObject +
              ", contactPoint = " + collision.point +
              ", normal = " + collision.normal +
              ", ray hit gameObject = " + normalHit.collider.gameObject +
              ", ray start point = " + transform.position +
              ", ray direction = " + ( contactPoint - transform.position ) );
          }

          m_CollisionData.Add( collision );
        }
      }
    }

    if( depth < maxDepth && didContact )
    {
      RecursivePushback( depth + 1, maxDepth );
    }
  }

  private void ProcessCollisions()
  {
    for( int i = 0; i < m_CollisionData.Count; ++i )
    {
      // Send it to ourselves
      gameObject.SendMessage( "OnCollisionEnterCustom", m_CollisionData[i], SendMessageOptions.DontRequireReceiver );
      // Then send it to what we hit
      CustomCollisionEvent otherCollision = new CustomCollisionEvent()
      {
        gameObject = gameObject,
        point = m_CollisionData[i].point,
        normal = m_CollisionData[i].normal
      };
      m_CollisionData[i].gameObject.SendMessage( "OnCollisionEnterCustom", otherCollision, SendMessageOptions.DontRequireReceiver );
    }
  }
}
