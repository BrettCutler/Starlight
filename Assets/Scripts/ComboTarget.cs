using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls a target which bounces around arena, and, when hit, explodes, destryoing nearby targets.
/// </summary>
public class ComboTarget : MonoBehaviour
{
  public float m_VelocityScalar;
  public float m_ExplosionRadius;

  private Rigidbody m_Rigidbody;
  private Vector3 m_PreviousVelocity;

  private int m_ExplodeLayerMask;

  private const float k_TimeBetweenDestroyTargets = 0.1f;
  private const string k_TargetLayerName = "Target";

  void Awake()
  {
    m_Rigidbody = GetComponent<Rigidbody>();

    SetVelocity( new Vector3( Random.value * m_VelocityScalar,
                                          Random.value * m_VelocityScalar, 0f ) );

    m_ExplodeLayerMask = 1 << LayerMask.NameToLayer( k_TargetLayerName );
  }

  private void SetVelocity( Vector3 newVelocity )
  {
    m_Rigidbody.velocity = newVelocity;
    m_PreviousVelocity = m_Rigidbody.velocity;
  }

  void OnCollisionEnter( Collision collision )
  {
    // Manually reflect so we get a simplified bounce off our impact surface,
    // ensuring we don't lose velocity or stop
    SetVelocity( Vector3.Reflect( m_PreviousVelocity, collision.contacts[0].normal ) );
  }

  public void OnHitByBall()
  {
    Explode();
  }

  private void Explode()
  {
    // Trigger effects
    

    // Collect all nearby targets and destroy them
    Collider[] targetsHit = Physics.OverlapSphere( transform.position, m_ExplosionRadius,
      m_ExplodeLayerMask, QueryTriggerInteraction.Collide );

    for( int i = 0; i < targetsHit.Length; ++i )
    {
      if( targetsHit[i].gameObject == gameObject )
      {
        continue;
      }

      TargetManager.Instance.ScoreAndDestroyTarget( targetsHit[i].gameObject, i * k_TimeBetweenDestroyTargets );
    }

    // Destroy this
    Destroy( gameObject );
  }
}
