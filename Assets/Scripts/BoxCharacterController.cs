using UnityEngine;
using System.Collections;
using System;

public class BoxCharacterController : CustomCharacterController
{
  [SerializeField] private BoxCollider m_Collider;
  protected Vector3 m_SizeWorldSpace
  {
    get
    {
      return new Vector3( m_Collider.size.x * transform.lossyScale.x,
                          m_Collider.size.y * transform.lossyScale.y,
                          m_Collider.size.z * transform.lossyScale.z );
    }
  }
  
  protected override float DistanceToClosestSurfaceFromInside( Vector3 point )
  {
    // THIS IS GIVING ME THE CLOSEST POINT TO THE CONTACT POINT.
    // WHAT I REALLY WANT IS:
    // CLOSEST POINT ON SURFACE GOING FROM CONTACT POINT IN PUSHVECTOR DIRECTION
    // RAYCASTING AGAINST IT SEEMS TO WORK, NOW HOW DO I GUARANTEE IT STARTS FROM OUTSIDE SHAPE AND GOES IN?

    Debug.Log( "from inside, point = " + point.ToString("F4") );

    //float distanceToEdge = ClosestSurfacePointLocal( point ).magnitude;
    /// INSTEAD
    //Vector3 closestPointOnSurface = CustomCollisions.ClosestPointOnSurface( m_Collider, transform.position + point );
    //Vector3 pointToCenter = transform.position - closestPointOnSurface;
    //float distanceToEdge = pointToCenter.magnitude;
    //Debug.Log( "closest point on surface = " + closestPointOnSurface.ToString( "F5" ) );


    float distanceToEdge = DistanceToClosestSurfaceAlongVector( point );

    // distance to edge - distance of penetration
    Debug.Log( "result = distanceToEdge - point.magnitude || ( " +
      ( distanceToEdge - point.magnitude ) + " = " + distanceToEdge + " - " + point.magnitude + " ) " );
    return distanceToEdge - point.magnitude;
  }

  private float DistanceToClosestSurfaceAlongVector( Vector3 point )
  {
    Vector3 scaledPoint = point.normalized; // Generate a ray start point along vector point, at scale of collider + some
    scaledPoint.Scale( m_SizeWorldSpace );
    Vector3 rayStartPoint = transform.position + ( scaledPoint * 2f );

    RaycastHit hitInfo;

    bool didHit = m_Collider.Raycast( new Ray( rayStartPoint, -point ), out hitInfo, 15f );
    float distanceToEdge = (transform.position - hitInfo.point).magnitude;

    Debug.Log( "ray hit point = " + hitInfo.point.ToString( "F5" ) + ", didHit = " + didHit );

    return distanceToEdge;
  }

  protected override float DistanceToClosestSurfaceFromOutside( Vector3 point )
  {
    Debug.Log( "from outside, point = " + point.ToString("F4") );

    //float distanceToEdge = ClosestSurfacePointLocal( point ).magnitude;
    /// INSTEAD
    //Vector3 closestPointOnSurface = CustomCollisions.ClosestPointOnSurface( m_Collider, transform.position + point );
    //Vector3 pointToCenter = transform.position - closestPointOnSurface;
    //float distanceToEdge = pointToCenter.magnitude;
    //Debug.Log( "closest point on surface = " + closestPointOnSurface.ToString( "F5" ) );

    float distanceToEdge = DistanceToClosestSurfaceAlongVector( point );

    // distance to edge + distance of penetration
    Debug.Log( "result = distanceToEdge + point.magnitude || ( " +
      ( distanceToEdge + point.magnitude ) + " = " + distanceToEdge + " + " + point.magnitude + " ) " );
    return distanceToEdge + point.magnitude;
  }

  protected override int OverlapShapeNonAlloc()
  {
    return Physics.OverlapBoxNonAlloc(
      transform.position,
      m_SizeWorldSpace * 0.5f,
      m_OverlapShapeResults,
      transform.rotation,
      m_CollideableLayers
      );
  }

  protected override bool PointWithinShape( Vector3 point )
  {
    Vector3 localPoint = point - transform.position;
    Vector3 rotatedPoint = Quaternion.Inverse( transform.rotation ) * localPoint;

    return Mathf.Abs( rotatedPoint.x ) < ( m_SizeWorldSpace.x * 0.5f ) &&
           Mathf.Abs( rotatedPoint.y ) < ( m_SizeWorldSpace.y * 0.5f ) &&
           Mathf.Abs( rotatedPoint.z ) < ( m_SizeWorldSpace.z * 0.5f );
  }

  protected override void OnCollisionEnterCustom( CustomCollisionEvent collision )
  {
    Vector3 newMoveDir = Vector3.Reflect( m_CurVelocity, collision.normal );

    Debug.Log( "on collisionenter other tag = " + collision.gameObject.tag +
      ", frame = " + Time.frameCount +
      ", collisionNormal = " + collision.normal +
      ", newMoveDir = " + newMoveDir );

    m_CurVelocity = newMoveDir;

    m_WaitingForMoveContinue = true;
  }

  protected override void Awake()
  {
    base.Awake();
  }
}
