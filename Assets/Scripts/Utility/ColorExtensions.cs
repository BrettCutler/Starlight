using UnityEngine;
using System.Collections;

/// <summary>
/// Based on http://wiki.unity3d.com/index.php/Colorx
/// </summary>
public static class ColorExtensions
{
  /// <summary>
  /// Interpolate between colors with fewer artifacts than Color.Lerp.
  /// Converts to HSVColor, then Lerps.
  /// </summary>
  public static Color Slerp( this Color a, Color b, float t )
  {
    return HSVColor.Lerp( HSVColor.FromColor( a ), HSVColor.FromColor( b ), t ).ToColor( );
  }

  public static Color H( this Color c, int hue0to360 )
  {
    HSVColor temp = HSVColor.FromColor(c);
    temp.h = ( hue0to360 / 360.0f );
    return HSVColor.ToColor( temp );
  }

  public static Color H( this Color c, float hue0to1 )
  {
    HSVColor temp = HSVColor.FromColor(c);
    temp.h = hue0to1;
    return HSVColor.ToColor( temp );
  }

  public static Color S( this Color c, float saturation0to1 )
  {
    HSVColor temp = HSVColor.FromColor(c);
    temp.s = saturation0to1;
    return HSVColor.ToColor( temp );
  }

  public static Color V( this Color c, float value0To1 )
  {
    HSVColor temp = HSVColor.FromColor(c);
    temp.v = value0To1;
    return HSVColor.ToColor( temp );
  }

  public static HSVColor ToHSVColor( this Color c )
  {
    return HSVColor.FromColor( c );
  }
}
