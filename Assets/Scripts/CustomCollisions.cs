using UnityEngine;
using System.Collections;

public static class CustomCollisions
{
  public static Vector3 ClosestPointOnSurface( Collider collider, Vector3 to )
  {
    if( collider is SphereCollider )
    {
      return ClosestPointOnSurface( (SphereCollider)collider, to );
    }
    else if( collider is BoxCollider )
    {
      return ClosestPointOnSurface( (BoxCollider)collider, to );
    }
    else if( collider is CapsuleCollider )
    {
      return ClosestPointOnSurface( (CapsuleCollider)collider, to );
    }

    Debug.LogError( "Collider " + collider + " is of a type not handled." );
    return Vector3.zero;
  }

  public static Vector3 ClosestPointOnSurface( SphereCollider collider, Vector3 to )
  {
    Vector3 outputPoint;

    outputPoint = to - collider.transform.position;
    outputPoint.Normalize();

    outputPoint *= collider.radius * collider.transform.localScale.x;

    return outputPoint;
  }

  public static Vector3 ClosestPointOnSurface( BoxCollider collider, Vector3 to )
  {
    // Cache the collider transform
    Transform colliderTransform = collider.transform;

    // First, transform the point into the space of the collider
    Vector3 local = colliderTransform.InverseTransformPoint( to );

    // Now, shift it to be the center of the box
    local -= collider.center;

    // Pre-multiply to save operations
    Vector3 halfSize = collider.size * 0.5f;

    // Clamp the points to the collider's extents
    Vector3 localNorm = new Vector3(
        Mathf.Clamp( local.x, -halfSize.x, halfSize.x ),
        Mathf.Clamp( local.y, -halfSize.y, halfSize.y ),
        Mathf.Clamp( local.z, -halfSize.z, halfSize.z )
      );

    // Calculate distances from each edge
    float dx = Mathf.Min( Mathf.Abs( halfSize.x - localNorm.x ), Mathf.Abs( -halfSize.x - localNorm.x ) );
    float dy = Mathf.Min( Mathf.Abs( halfSize.y - localNorm.y ), Mathf.Abs( -halfSize.y - localNorm.y ) );
    float dz = Mathf.Min( Mathf.Abs( halfSize.z - localNorm.z ), Mathf.Abs( -halfSize.z - localNorm.z ) );

    // Select a face to project on
    if( dx < dy && dx < dz )
    {
      localNorm.x = Mathf.Sign( localNorm.x ) * halfSize.x;
    }
    else if( dy < dx && dy < dz )
    {
      localNorm.y = Mathf.Sign( localNorm.y ) * halfSize.y;
    }
    else if( dz < dx && dz < dy )
    {
      localNorm.z = Mathf.Sign( localNorm.z ) * halfSize.z;
    }

    // Now we undo our transformations
    local = localNorm + collider.center;

    return colliderTransform.TransformPoint( local );
  }

  // Courtesy of Moodie
  public static Vector3 ClosestPointOnSurface( CapsuleCollider collider, Vector3 to )
  {
    // Cache the collider transform
    Transform colliderTransform = collider.transform;

    // length of cylindrical section between cap spheres' central point
    float lineLength = collider.height - collider.radius * 2;
    float halfLineLength = lineLength * 0.5f;

    Vector3 dir = Vector3.up;

    // In local space, position of upper & lower spheres
    Vector3 upperSphereCenter = dir * halfLineLength + collider.center;
    Vector3 lowerSphereCneter = -dir * halfLineLength + collider.center;

    // Controller position in localSpace
    Vector3 local = colliderTransform.InverseTransformPoint(to);

    Vector3 contactPoint = Vector3.zero;
    Vector3 contactPointReference = Vector3.zero; // point used to get a direction vector with controllerPoint

    if( local.y < halfLineLength && local.y > -halfLineLength ) // if touching cylinder
    {
      contactPointReference = dir * local.y + collider.center;
    }
    else if( local.y > halfLineLength ) // if touching top sphere
    {
      contactPointReference = upperSphereCenter;
    }
    else if( local.y < -halfLineLength ) // if touching bottom sphere
    {
      contactPointReference = lowerSphereCneter;
    }

    // Calculate contact point in local coordinates and return it in world coordinates
    contactPoint = local - contactPointReference;
    contactPoint.Normalize();
    contactPoint = contactPoint * collider.radius + contactPointReference;
    return colliderTransform.TransformPoint( contactPoint );
  }
}

public struct CustomCollisionEvent
{
  // Note: we can save any other info we want in here
  public GameObject gameObject;
  public Vector3 point;
  public Vector3 normal;
}
