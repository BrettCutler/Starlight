using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PulseColorOnBeat : MonoBehaviour
{
  [System.Serializable] public class OnBeatRenderer
  {
    public List<MeshRenderer> m_Renderers = new List<MeshRenderer>();
    public int m_BeatOffset;
    public List<StandardShaderProperties> m_ColorPropertiesToChange = new List<StandardShaderProperties>( );

    public List<Color> m_InitialMaterialColors
    {
      get { return m_InitialMaterialColorsIntern; }
      set { m_InitialMaterialColorsIntern = value; }
    }
    private List<Color> m_InitialMaterialColorsIntern;

    public List<HSVColor> m_InitialMaterialHSVColors
    {
      get { return m_InitialMaterialHSVColorsIntern; }
      set { m_InitialMaterialHSVColorsIntern = value; }
    }
    private List<HSVColor> m_InitialMaterialHSVColorsIntern;

    public List<int> m_ColorPropertyIDs
    {
      get { return m_ColorPropertyIDsIntern; }
      set { m_ColorPropertyIDsIntern = value; }
    }
    private List<int> m_ColorPropertyIDsIntern;
  }

  public List<OnBeatRenderer> m_ColorShiftRenderers = new List<OnBeatRenderer>( );
  
  /// <summary>
  /// Pulse color white when bar + offset hits
  /// </summary>
  public bool m_SaturationPulseOnBar
  {
    get { return m_SaturationPulseOnBarIntern; }
    set
    {
      // Clean up saturation when we turn off
      if( !value && (value != m_SaturationPulseOnBar ) )
      {
        ResetSaturation();
      }

      m_SaturationPulseOnBarIntern = value;
    }
  }
  /// <summary>
  /// Exposed to editor, but will not call setter when changed in Inspector.
  /// </summary>
  [Tooltip("Pulse color white when bar + offset hits")]
  [SerializeField] private bool m_SaturationPulseOnBarIntern;

  /// <summary>
  /// Pulse color shift briefly when bar + offset hits
  /// </summary>
  public bool m_HuePulseOnBar
  {
    get { return m_HuePulseOnBarIntern; }
    set
    {
      // Clean up hue when we turn off
      if( !value && ( value != m_HuePulseOnBar ) &&
          !m_RainbowHueShiftOnBar )
      {
        ResetHue();
      }

      m_HuePulseOnBarIntern = value;
    }
  }
  /// <summary>
  /// Exposed to editor, but will not call setter when changed in Inspector
  /// </summary>
  [Tooltip("Pulse color shift briefly when bar + offset hits.")]
  [SerializeField] private bool m_HuePulseOnBarIntern;

  /// <summary>
  /// Shift colors constantly, with a period of [Bar]. Offset by beatOffset.
  /// </summary>
  public bool m_RainbowHueShiftOnBar
  {
    get { return m_RainbowHueShiftOnBarIntern; }
    set
    {
      // Clean up hue when we turn off
      if( !value && ( value != m_RainbowHueShiftOnBar ) &&
          !m_HuePulseOnBarIntern )
      {
        ResetHue();
      }

      m_RainbowHueShiftOnBarIntern = value;
    }
  }
  /// <summary>
  /// Exposed to editor, but will not call setter when changed in Inspector
  /// </summary>
  [Tooltip("Shift colors constantly, with a period of [Bar]. Offset by beatOffset.")]
  [SerializeField] private bool m_RainbowHueShiftOnBarIntern;

  private int m_EmissionPropertyID;

  private float[] m_InitialEmissiveSaturationValues;

  public enum StandardShaderProperties
  {
    _Color,
    _EmissionColor,
    _TintColor,
  }

  void Awake()
  {
    for( int i = 0; i < m_ColorShiftRenderers.Count; ++i )
    {
      OnBeatRenderer colorShift = m_ColorShiftRenderers[i];

      int propertiesCount = colorShift.m_ColorPropertiesToChange.Count * colorShift.m_Renderers.Count;

      List<int> colorPropertyIDs = new List<int>( propertiesCount );
      List<Color> initialColors = new List<Color>( propertiesCount );
      List<HSVColor> initialHSVColors = new List<HSVColor>( propertiesCount );

      for( int j = 0; j < colorShift.m_Renderers.Count; ++j )
      {
        for( int k = 0; k < colorShift.m_ColorPropertiesToChange.Count; ++k )
        {
          int id = Shader.PropertyToID( colorShift.m_ColorPropertiesToChange[k].ToString() );
          colorPropertyIDs.Add( id );

          Color curColor = colorShift.m_Renderers[j].material.GetColor( colorPropertyIDs[colorPropertyIDs.Count -1] );
          initialColors.Add( curColor );
          initialHSVColors.Add( curColor.ToHSVColor() );
        }

      }

      colorShift.m_ColorPropertyIDs = colorPropertyIDs;
      colorShift.m_InitialMaterialColors = initialColors;
      colorShift.m_InitialMaterialHSVColors = initialHSVColors;
    }
  }

  void Update()
  {
    // early exit if we have no work to do
    if( m_SaturationPulseOnBar ||
        m_HuePulseOnBar ||
        m_RainbowHueShiftOnBar
        )
    {
      float timeLastOnBar = WaitForMusicManager.Instance.m_TimeLastOnBar;
      float barDuration = WaitForMusicManager.Instance.m_BarDuration;
      float beatDuration = WaitForMusicManager.Instance.m_BeatDuration;

      for( int i = 0; i < m_ColorShiftRenderers.Count; ++i )
      {
        OnBeatRenderer renderer = m_ColorShiftRenderers[i];

        for( int j = 0; j < renderer.m_Renderers.Count; ++j )
        {
          for( int k = 0; k < renderer.m_ColorPropertiesToChange.Count; ++k )
          {
            int propertyIndex = j * renderer.m_ColorPropertiesToChange.Count + k;

            Material thisMat = renderer.m_Renderers[j].material;

            Color currentColor = thisMat.GetColor( renderer.m_ColorPropertyIDs[propertyIndex] );
            HSVColor currentHSVColor = currentColor.ToHSVColor();

            // Clamp because no beat duration otherwise results in infinity, i.e., NaN adjusted hue
            float timeLastOnOffsetBar = timeLastOnBar + (beatDuration * renderer.m_BeatOffset);
            if( timeLastOnOffsetBar > Time.time )
            {
              timeLastOnOffsetBar -= barDuration;
            }

            float currentPercent = Mathf.Clamp(
              (Time.time - timeLastOnOffsetBar ) / barDuration,
              0f, float.MaxValue );

            float easedPercent = Easing.EaseOut( currentPercent, EasingType.Cubic );

            if( m_SaturationPulseOnBar )
            {
              // clamp saturation so we don't destroy color value
              float saturationValue = Mathf.Clamp( easedPercent * renderer.m_InitialMaterialHSVColors[propertyIndex].s,
              float.Epsilon, 1f - float.Epsilon );

              currentHSVColor.s = saturationValue;
            }

            if( m_HuePulseOnBar )
            {
              // Clamp just above and below zero so we don't get pure black or white, destroying color data
              currentHSVColor.h = Mathf.Clamp( ( easedPercent + renderer.m_InitialMaterialHSVColors[propertyIndex].h ) % 1f,
                float.Epsilon, 1f - float.Epsilon );
            }

            if( m_RainbowHueShiftOnBar )
            {
              // Clamp just above and below zero so we don't get pure black or white, destroying color data
              currentHSVColor.h = Mathf.Clamp( ( currentPercent + renderer.m_InitialMaterialHSVColors[propertyIndex].h ) % 1f,
                float.Epsilon, 1f - float.Epsilon );
            }

            currentColor = currentHSVColor.ToColor();

            thisMat.SetColor( renderer.m_ColorPropertyIDs[propertyIndex], currentColor );
          }
        }

      }
    }
  }

  private void ResetSaturation()
  {
    for( int i = 0; i < m_ColorShiftRenderers.Count; ++i )
    {
      OnBeatRenderer renderer = m_ColorShiftRenderers[i];

      for( int j = 0; j < m_ColorShiftRenderers[i].m_Renderers.Count; ++j )
      {
        for( int k = 0; k < m_ColorShiftRenderers[i].m_ColorPropertiesToChange.Count; ++k )
        {
          int propertyIndex = j * m_ColorShiftRenderers[i].m_ColorPropertiesToChange.Count + k;

          Material thisMat = renderer.m_Renderers[j].material;

          HSVColor currentColor = thisMat.GetColor( renderer.m_ColorPropertyIDs[propertyIndex] ).ToHSVColor();

          currentColor.s = renderer.m_InitialMaterialHSVColors[propertyIndex].s;

          thisMat.SetColor( renderer.m_ColorPropertyIDs[propertyIndex], currentColor.ToColor() );
        }
      }
    }
  }

  private void ResetHue()
  {
    for( int i = 0; i < m_ColorShiftRenderers.Count; ++i )
    {
      OnBeatRenderer renderer = m_ColorShiftRenderers[i];

      for( int j = 0; j < m_ColorShiftRenderers[i].m_Renderers.Count; ++j )
      { 
        for( int k = 0; k < m_ColorShiftRenderers[i].m_ColorPropertiesToChange.Count; ++k )
        {
          int propertyIndex = j * m_ColorShiftRenderers[i].m_ColorPropertiesToChange.Count + k;

          Material thisMat = renderer.m_Renderers[j].material;

          HSVColor currentColor = thisMat.GetColor( renderer.m_ColorPropertyIDs[propertyIndex] ).ToHSVColor();

          currentColor.h = renderer.m_InitialMaterialHSVColors[propertyIndex].h;

          thisMat.SetColor( renderer.m_ColorPropertyIDs[propertyIndex], currentColor.ToColor() );
        }
      }
    }
  }
}
