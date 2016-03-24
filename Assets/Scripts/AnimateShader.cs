using UnityEngine;
using System.Collections;

/// <summary>
/// This class exists because there is currently no way to animate a shader directly.
/// Instead, this helper class animates it for us
/// http://answers.unity3d.com/questions/622005/legacy-animation-system-cannot-animate-colors.html
/// </summary>
public class AnimateShader : MonoBehaviour 
{
    /*
    /// <summary>
    /// Enum that will be used to control which color we transition to via the animation .anim clip
    /// </summary>
    private enum ColorValues
    {
        Yellow,
        Red,
        Transparent
    }

    /// <summary>
    /// Same as color, but for scale
    /// </summary>
    private enum ScaleValues
    {
        Normal,
        Big
    }

    [SerializeField]
    private ColorValues currentColor = ColorValues.Yellow;
    [SerializeField]
    private ScaleValues currentScale = ScaleValues.Normal;
    */

    [SerializeField]
    private Renderer matRenderer;
    /// <summary>
    /// Needs to be the name from the shader file's header block, not the filename
    /// </summary>
    [SerializeField]
    private string shaderName = "Sprites/SpriteBillboardSortTop";
    /// <summary>
    /// Needs to be the name of the property in the shader, not the string displayed in the UI
    /// </summary>
    [SerializeField]
    private string colorPropName = "_Color";
    /// <summary>
    /// Needs to be the name of the property in the shader, not the string displayed in the UI
    /// </summary>
    [SerializeField]
    private string scalePropName = "_Scale";
    [SerializeField]
    private Color animColor;
    [SerializeField]
    private float animScale;

    [SerializeField]
    private Color redColor = Color.red;
    [SerializeField]
    private Color yellowColor = Color.yellow;
    [SerializeField]
    private Color transparentColor = Color.clear;
    [SerializeField]
    private float normalScale = 1.0f;
    [SerializeField]
    private float bigScale = 2.5f;



	// Use this for initialization
	void Start () 
    {
        matRenderer = GetComponent<Renderer> ();
        matRenderer.material.shader = Shader.Find (shaderName);
	}
	
	// Update is called once per frame
	void Update () 
    {
        /*switch (currentColor)
        {
            case ColorValues.Yellow:
                animColor = Color.Lerp (animColor, yellowColor, Time.deltaTime);
                break;
            case ColorValues.Red:
                animColor = Color.Lerp (animColor, redColor, Time.deltaTime);
                break;
            case ColorValues.Transparent:
                animColor = Color.Lerp (animColor, transparentColor, Time.deltaTime);
                break;
            default:
                Debug.LogError ("Animating to invalid color.", this);
                break;
        }

        switch (currentScale)
        {
            case ScaleValues.Normal:
                animScale = Mathf.Lerp (animScale, normalScale, Time.deltaTime);
                break;
            case ScaleValues.Big:
                animScale = Mathf.Lerp (animScale, bigScale, Time.deltaTime);
                break;
            default:
                Debug.LogError ("Animating to invalid scale.", this);
                break;
        }*/
            
        matRenderer.material.SetColor (colorPropName, animColor);
        matRenderer.material.SetFloat (scalePropName, animScale);

	}
}
