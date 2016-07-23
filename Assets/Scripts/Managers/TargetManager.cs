using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TargetManager : Singleton<TargetManager>
{
  public class SpawnedTarget
  {
    public GameObject m_GameObj;
    public TargetMovePattern m_MovePattern;
    public TargetMoveSpeed m_MoveSpeed;
    public bool m_MovingRight = true;
    public bool m_MovingUp = true;
  }

  private class TargetToDestroy
  {
    public GameObject m_GameObj;
    public float m_Lifetime;

    public TargetToDestroy( GameObject gameObj, float lifetime )
    {
      m_GameObj = gameObj;
      m_Lifetime = lifetime;
    }
  }

  [SerializeField] private bool m_DisableSpawnTargets;
  [SerializeField] private GameObject m_TargetPrefab;
  [SerializeField] private List<GameObject> m_TargetSpawnGroups;
  [SerializeField] private List<float> m_SpawnDepths;
  [SerializeField] private bool m_AdvanceAllTargets;
  [SerializeField] private float m_AdvanceAllVelocity;
  [SerializeField] private bool m_SpawnProcedurally;

  [SerializeField] private float m_slowSpeed;
  [SerializeField] private float m_mediumSpeed;
  [SerializeField] private float m_fastSpeed;

  private List<SpawnedTarget> m_ActiveTargets = new List<SpawnedTarget>( );
  private List<TargetToDestroy> m_QueuedTargetsToDestroy = new List<TargetToDestroy>( );

  private int m_MinBeatNextTileSpawn;

  [SerializeField] private bool d_DebugInitialSpawn;

  /// <summary>
  /// Flag used to stop movement while waiting for
  /// WaitForMusicController to allow us to continue
  /// </summary>
  protected bool m_WaitingForMoveContinue;
  [SerializeField] public List<TargetSpawnDefinition> m_TargetSpawns = new List<TargetSpawnDefinition>( );

  private static Vector2 k_MinTargetScale = new Vector2( 2.5f, 2.5f );
  private static Vector2 k_MaxTargetScale = new Vector2( 10f, 10f );
  private const float k_ZTargetScale = .1f;

  public override void Init()
  {
    base.Init();

    m_MinBeatNextTileSpawn = 15;

    WaitForMusicManager.Instance.OnDefaultContinue += OnMoveContinue;
    WaitForMusicManager.Instance.OnBeatContinue += OnBeatContinue;
    WaitForMusicManager.Instance.OnStopMovement += OnStopMovement;

    if( d_DebugInitialSpawn )
    {
      TileSpawner tileSpawner = m_TargetSpawnGroups[0].GetComponent<TileSpawner>();
      for( int i = 0; i < tileSpawner.m_SpawnInfo.Count; ++i )
      {
        SpawnTarget( tileSpawner.m_SpawnInfo[i], false );
      }
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

  private void OnBeatContinue()
  {
    if( !m_DisableSpawnTargets )
    {
      if( m_SpawnProcedurally )
      {
        ProceduralSpawnTileOnBeat();
      }
      else
      {
        SpawnTargetOnBeat();
      }
    }
  }

  private void ProceduralSpawnTileOnBeat()
  {
    if( AudioEventsCallbacksManager.Instance.m_BeatCount % 3 == 0 &&
        AudioEventsCallbacksManager.Instance.m_BeatCount >= m_MinBeatNextTileSpawn &&
        m_ActiveTargets.Count == 0
      )
    {
      int randomSelection = Random.Range( 0, m_TargetSpawnGroups.Count );

      TileSpawner tileSpawner = m_TargetSpawnGroups[randomSelection].GetComponent<TileSpawner>();
      for( int i = 0; i < tileSpawner.m_SpawnInfo.Count; ++i )
      {
        SpawnTarget( tileSpawner.m_SpawnInfo[i] );
      }

      m_MinBeatNextTileSpawn = AudioEventsCallbacksManager.Instance.m_BeatCount + Random.Range( 8, 16 );
    }
  }

  private void SpawnTargetOnBeat()
  {
    for( int i = 0; i < m_TargetSpawns.Count; ++i )
    {
      if( AudioEventsCallbacksManager.Instance.m_BeatCount == m_TargetSpawns[i].m_BeatNumber )
      {
        SpawnTile( new Vector3( 0f, 0f, GameManager.Instance.m_RoomDepth ),
                   m_TargetSpawns[i].m_TargetGroupSpawn );
      }
    }
  }

  public void  SpawnTarget( TileSpawner.TileSpawnInfo spawnInfo, bool depthRandom = true )
  {
    float zDepth = depthRandom ? m_SpawnDepths[ Random.Range(0, m_SpawnDepths.Count ) ] : m_SpawnDepths[0];

    SpawnedTarget newTarget = SpawnTarget( 
      spawnInfo.position + new Vector3( 0f, 0f, zDepth ), spawnInfo.scale );

    newTarget.m_GameObj.GetComponent<Collider>().isTrigger = spawnInfo.isGlass;
    newTarget.m_MovePattern = spawnInfo.movePattern;
    newTarget.m_MoveSpeed = spawnInfo.moveSpeed;
  }

  public SpawnedTarget SpawnTarget( Vector3 spawnPos, Vector3 spawnScale )
  {
    GameObject newTile = Instantiate( m_TargetPrefab,  spawnPos, Quaternion.identity ) as GameObject;
    newTile.transform.localScale = spawnScale;

    SpawnedTarget newTarget = new SpawnedTarget();
    newTarget.m_GameObj = newTile;

    m_ActiveTargets.Add( newTarget );

    return newTarget;
  }

  public void SpawnTile( Vector3 spawnPos, GameObject spawnGroupPrefab )
  {
    GameObject newObj = Instantiate( spawnGroupPrefab, spawnPos, Quaternion.identity ) as GameObject;

    SpawnedTarget newTarget = new SpawnedTarget();
    newTarget.m_GameObj = newObj;

    m_ActiveTargets.Add( newTarget );
  }

  private void Update()
  {
    if( !m_WaitingForMoveContinue )
    {
      MoveTargets();
      UpdateQueuedToDestroyTargets();
    }
  }

  private void MoveTargets()
  { 
    for( int i = 0; i < m_ActiveTargets.Count; ++i )
    {
      Vector3 endPos = m_ActiveTargets[i].m_GameObj.transform.position;

      if( m_AdvanceAllTargets )
      {
        endPos.z += m_AdvanceAllVelocity * Time.deltaTime;
      }

      float velocity = 0f;
      switch( m_ActiveTargets[i].m_MoveSpeed)
      {
        case TargetMoveSpeed.Slow:
          velocity = m_slowSpeed * Time.deltaTime;
          break;
        case TargetMoveSpeed.Medium:
          velocity = m_mediumSpeed * Time.deltaTime;
          break;
        case TargetMoveSpeed.Fast:
          velocity = m_fastSpeed * Time.deltaTime;
          break;
        default:
          break;
      }

      switch (m_ActiveTargets[i].m_MovePattern)
      {
        case TargetMovePattern.None:
          break;

        case TargetMovePattern.Horizontal:
          // We need to know size of collider to bounce off wall at proper position    
          float halfSizeX = m_ActiveTargets[i].m_GameObj.transform.lossyScale.x * 0.5f;

          // x pos of wall we're heading towards
          float targetEdgeXPos = m_ActiveTargets[i].m_MovingRight ?
            GameManager.Instance.m_BoundsBottomRight.x :
            GameManager.Instance.m_BoundsTopLeft.x;

          float distanceToXEdge = m_ActiveTargets[i].m_MovingRight ?
            targetEdgeXPos - endPos.x - halfSizeX :
            endPos.x - targetEdgeXPos - halfSizeX;

          // if we hit the wall, expend move energy to reach wall, bounce off
          if( distanceToXEdge < velocity )
          {
            float distanceFromEdge = velocity - distanceToXEdge;

            endPos.x = m_ActiveTargets[i].m_MovingRight ?
              targetEdgeXPos - distanceFromEdge - halfSizeX :
              targetEdgeXPos + distanceFromEdge + halfSizeX;

            m_ActiveTargets[i].m_MovingRight = !( m_ActiveTargets[i].m_MovingRight );
          }
          else // advance in current x direction
          {
            endPos.x += m_ActiveTargets[i].m_MovingRight ? velocity : -velocity;
          }

          break;

        case TargetMovePattern.Vertical:
          // We need to know size of collider to bounce off wall at proper position    
          float halfSizeY = m_ActiveTargets[i].m_GameObj.transform.lossyScale.y * 0.5f;

          // x pos of wall we're heading towards
          float targetEdgeYPos = m_ActiveTargets[i].m_MovingUp ?
            GameManager.Instance.m_BoundsTopLeft.y :
            GameManager.Instance.m_BoundsBottomRight.y;

          float distanceToYEdge = m_ActiveTargets[i].m_MovingUp ?
            targetEdgeYPos - endPos.y - halfSizeY :
            endPos.y - targetEdgeYPos - halfSizeY;

          // if we hit the wall, expend move energy to reach wall, bounce off
          if( distanceToYEdge < velocity )
          {
            float distanceFromEdge = velocity - distanceToYEdge;

            endPos.y = m_ActiveTargets[i].m_MovingUp ?
              targetEdgeYPos - distanceFromEdge - halfSizeY :
              targetEdgeYPos + distanceFromEdge + halfSizeY;

            m_ActiveTargets[i].m_MovingUp = !( m_ActiveTargets[i].m_MovingUp );
          }
          else // advance in current x direction
          {
            endPos.y += m_ActiveTargets[i].m_MovingUp ? velocity : -velocity;
          }

          break;

        case TargetMovePattern.Advance:
          endPos.z += -velocity;
          break;

        default:
          break;
      }
                
      m_ActiveTargets[i].m_GameObj.transform.position = endPos;

      // after move, so if we want this visible death anim plays at appropriate time
      if( endPos.z < GameManager.Instance.m_OutOfBoundsZ )
      {
        TileOutOfBounds( i );
      }
    }
  }

  private void TileOutOfBounds( int tileIter )
  {
    GameObject target = m_ActiveTargets[tileIter].m_GameObj;
    m_ActiveTargets.RemoveAt( tileIter );
    Destroy( target );
  }

  public void BallHitTarget( GameObject target )
  {
    ScoreAndDestroyTarget( target, 0f );
  }

  public void ScoreAndDestroyTarget( GameObject target, float delay )
  { 
    GameManager.Instance.TileScored();

    m_ActiveTargets.RemoveAt( FindActiveTargetIndexByGameObject( target ) );

    m_QueuedTargetsToDestroy.Add( new TargetToDestroy( target, delay ) );
  }

  private void UpdateQueuedToDestroyTargets()
  {
    for( int i = m_QueuedTargetsToDestroy.Count - 1; i > -1; i-- )
    {
      m_QueuedTargetsToDestroy[i].m_Lifetime -= Time.deltaTime;

      if( m_QueuedTargetsToDestroy[i].m_Lifetime < 0f )
      {
        Animation anim = m_QueuedTargetsToDestroy[i].m_GameObj.GetComponent<Animation>();
        anim.Play( "TargetTileExplode" );
        float duration = anim.clip.length;

        m_QueuedTargetsToDestroy[i].m_GameObj.GetComponent<Collider>().enabled = false;

        Destroy( m_QueuedTargetsToDestroy[i].m_GameObj, duration );

        m_QueuedTargetsToDestroy.RemoveAt( i );
      }
    }
  }

  public Vector3 GetRandomTargetScale()
  {
    return new Vector3(
      Random.Range( k_MinTargetScale.x, k_MaxTargetScale.x ),
      Random.Range( k_MinTargetScale.y, k_MaxTargetScale.y ),
      k_ZTargetScale
      );
  }

  private int FindActiveTargetIndexByGameObject( GameObject gameObj )
  {
    for( int i = 0; i < m_ActiveTargets.Count; ++i )
    {
      if( m_ActiveTargets[i].m_GameObj == gameObj )
      {
        return i;
      }
    }

    Debug.LogError( "Target of name " + gameObj.name + " cannot be found in m_ActiveTargets." );
    return -1;
  }
}
