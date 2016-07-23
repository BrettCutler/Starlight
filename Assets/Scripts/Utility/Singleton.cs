using UnityEngine;
using System.Collections;

public interface ISingleton
{
  bool InstanceExists { get; }
  GameObject InstanceGameObject { get; }
}

public abstract class Singleton<T> : MonoBehaviour, ISingleton where T : Singleton<T>
{
  protected static T _instance;
  protected bool _isInitialized = false;

  public virtual bool DestroyOnLoad
  {
    get { return true; }
  }

  public static bool Exists
  {
    get { return _instance != null; }
  }

  public bool InstanceExists
  {
    get { return Exists; }
  }

  public GameObject InstanceGameObject
  {
    get { return Exists ? Instance.gameObject : null; }
  }

  public static T Instance
  {
    get
    {
      // Look for the instance if it hasn't already been set
      if( _instance == null && ( !_hasQuit || !Application.isPlaying ) )
      {
        _instance = GameObject.FindObjectOfType( typeof( T ) ) as T;
        // If one couldn't be found, create one
        if( _instance == null )
        {
          _instance = new GameObject( typeof( T ).ToString( ), typeof( T ) ).GetComponent<T>( );
        }
        else
        {
          _instance.Init( );
        }
      }
      return _instance;
    }
  }

  protected static bool _hasQuit = false;
  protected static bool _isDestroyed = false;
  public static bool isDestroyed
  {
    get { return _isDestroyed; }
  }

  private void Awake( )
  {
    _isDestroyed = false;

    if( _isInitialized )
      return;

    if( _instance == null )
    {
      //Debug.Log( "Generate Singleton " + name );
      _instance = this as T;
      _instance.Init( );
    }
    else
    {
      Debug.LogWarning( string.Format( "[Singleton]WARNING Tried to create two instances of Singleton '{0}' on GameObject '{1}'.", GetType( ).Name, name ) );
      Destroy( gameObject );
    }

    if( !DestroyOnLoad )
    {
      _instance.transform.parent = null;
      GameObject.DontDestroyOnLoad( _instance.gameObject );
    }
  }

  public virtual void Init( )
  {
    _isInitialized = true;

    if( _onSingletonReadyInternal != null )
      _onSingletonReadyInternal( );
  }

  protected virtual void OnApplicationQuit( )
  {
    _hasQuit = true;
    _isDestroyed = true;
    _instance = null;
  }

  public delegate void SingletonReadyEvent( );
  protected static event SingletonReadyEvent _onSingletonReadyInternal;
  public static event SingletonReadyEvent onSingletonReady
  {
    add
    {
      _onSingletonReadyInternal += value;
      if( Exists )
      {
        value( );
      }
    }
    remove
    {
      _onSingletonReadyInternal -= value;
    }
  }

  protected virtual void OnLevelWasLoaded( int level ) { }

  protected virtual void OnDestroy( )
  {
    if( !DestroyOnLoad )
      return;

    _isDestroyed = true;
    _instance = null;
  }
}
