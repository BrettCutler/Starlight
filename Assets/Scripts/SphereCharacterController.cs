using UnityEngine;
using System.Collections;
using System;

public class SphereCharacterController : CustomCharacterController
{
  protected SphereCollider m_Collider;
  protected float m_Radius
  {
    get { return m_Collider.radius * Mathf.Max( transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z ); }
  }
  
  protected override void Awake()
  {
    m_Collider = GetComponent<SphereCollider>();

    base.Awake();
  }

  protected override float DistanceToClosestSurfaceFromInside( Vector3 point )
  {
    // distance to edge - distance of penetration
    return m_Radius - point.magnitude;
  }

  protected override float DistanceToClosestSurfaceFromOutside( Vector3 point )
  {
    // distance to edge + distance of penetration
    return m_Radius + point.magnitude;
  }

  protected override bool PointWithinShape( Vector3 point )
  {
    return Vector3.Distance( transform.position, point ) < m_Radius;
  }

  protected override int OverlapShapeNonAlloc()
  {
    return Physics.OverlapSphereNonAlloc(
      transform.position, m_Radius, m_OverlapShapeResults, m_CollideableLayers );
  }

  protected override void OnCollisionEnterCustom( CustomCollisionEvent collision )
  {
    Vector3 newMoveDir = Vector3.Reflect( m_CurVelocity, collision.normal );

    m_CurVelocity = newMoveDir;

    m_WaitingForMoveContinue = true;
  }
}
