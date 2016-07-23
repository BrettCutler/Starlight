using UnityEngine;
using System.Collections;

// Right now, only one player character controller
[RequireComponent( typeof( PlayerRacquet ) )]
public class PlayerMoveControl : MonoBehaviour
{
  private PlayerRacquet m_CharacterController;

  private void Awake()
  {
    // Get referenes, the SLOW way
    m_CharacterController = GetComponent<PlayerRacquet>();
  }

  // NOTE that the example MoveControl script reads button presses in Update, saves their state, and
  // passes it in during FixedUpdate.
  // This appears to be in case Update hits 2+ times between FixedUpdate, to bias towards confirming
  // a jump.
  private void FixedUpdate()
  {
    Vector2 moveDir = new Vector2( Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    Vector2 aimDir = new Vector2( Input.GetAxis("RightHorizontal"), Input.GetAxis("RightVertical") );

    //Debug.Log( "moveDir = " + moveDir + ", aimDir = " + aimDir );
        
    // If on keyboard, holding diagonal will create a vector of magnitude > 1, so clamp
    moveDir = Vector2.ClampMagnitude( moveDir, 1f );
    aimDir = Vector2.ClampMagnitude( aimDir, 1f );

    m_CharacterController.Move( moveDir, aimDir );
    
    if( Input.GetButtonDown( "Swing" ) )
    {
      m_CharacterController.PlayerRequestSwing( );
    }
  }
}
