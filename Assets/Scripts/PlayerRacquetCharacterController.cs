using UnityEngine;
using System.Collections;

public class PlayerRacquetCharacterController : BoxCharacterController
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
  
  public void Move( Vector2 moveDir, Vector2 aimDir )
  {
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

    Vector2 accelerationForce = moveDir * m_Acceleration * Time.deltaTime;
    Vector3 outputVelocity = new Vector3( accelerationForce.x, accelerationForce.y, 0f );

    // Cap at max velocity
    float outputVelocityMagnitude = Mathf.Min( outputVelocity.magnitude, m_MaxVelocity );

    // Apply friction; scale friction based on input magnitude (so it doesn't affect us at full throttle as much)
    float frictionScalar = m_FrictionAtInputMagnitude.Evaluate( moveDir.magnitude );
    float friction = 1f - (m_Friction * frictionScalar * Time.deltaTime );
    outputVelocityMagnitude *= friction;

    outputVelocity = outputVelocity.normalized * outputVelocityMagnitude;

    // Clamp velocity on returning
    Vector2 nextFramePosition = transform.position + outputVelocity * Time.deltaTime;
    if( returning )
    {
      Vector2 distanceToCenter = centerPos - transform.position;
      if( ( outputVelocity.x > 0 && nextFramePosition.x > centerPos.x ) ||
          ( outputVelocity.x < 0 && nextFramePosition.x < centerPos.x ) )
      {
        outputVelocity.x = distanceToCenter.x * ( 1 / Time.deltaTime );
      }
      if( ( outputVelocity.y > 0 && nextFramePosition.y > centerPos.y ) ||
          ( outputVelocity.y < 0 && nextFramePosition.y < centerPos.y ) )
      {
        outputVelocity.y = distanceToCenter.y * ( 1 / Time.deltaTime );
      }
    }

    m_CurVelocity = outputVelocity;
  }

  private void AdjustAngle( Vector2 aimDir )
  {
    transform.rotation = Quaternion.Euler( new Vector3( -aimDir.y * m_XYRotationConstraints,
                                          aimDir.x * m_XYRotationConstraints, 0f ) );
  }
}
