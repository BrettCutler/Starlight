using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TileSpawner : MonoBehaviour
{
  public GameObject m_TileToSpawn;
  public List<TileSpawnInfo> m_SpawnInfo;


  [System.Serializable]
  public struct TileSpawnInfo
  {
    public Vector3 position;
    public Vector3 scale;
    public TargetMovePattern movePattern;
    public TargetMoveSpeed moveSpeed;
    public bool isGlass;
  }

  private void Awake()
  {
    for( int i = 0; i < m_SpawnInfo.Count; ++i )
    {
      GameObject newObj = GameObject.Instantiate( m_TileToSpawn );
      newObj.transform.parent = transform;
      newObj.transform.localPosition = m_SpawnInfo[i].position;
      newObj.transform.localScale = m_SpawnInfo[i].scale;

      newObj.GetComponent<Collider>().isTrigger = m_SpawnInfo[i].isGlass;
    }
  }
}
public enum TargetMovePattern
{
  None,
  Horizontal,
  Vertical,
  Advance,
}

public enum TargetMoveSpeed
{
  Slow,
  Medium,
  Fast
}
