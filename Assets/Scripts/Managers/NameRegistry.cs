using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NameRegistry : Singleton<NameRegistry>
{
  public Dictionary<RegisteredGameObjectNames, GameObject> GameObjectRegistry = new Dictionary<RegisteredGameObjectNames, GameObject>();
  public Dictionary<RegisteredSingularScripts, MonoBehaviour> ScriptsRegistry = new Dictionary<RegisteredSingularScripts, MonoBehaviour>();
}

public enum RegisteredGameObjectNames
{
  GameBall,
  Racquet,
  Camera,
}

public enum RegisteredSingularScripts
{
  GameBallScript,
  PlayerRacquetScript,
  CameraController,
}