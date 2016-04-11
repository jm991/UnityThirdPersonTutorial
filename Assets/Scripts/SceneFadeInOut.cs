using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Based on SceneFadeInOut.cs from Unity samples:
/// https://unity3d.com/learn/tutorials/projects/stealth/screen-fader
/// </summary>
[RequireComponent (typeof (GUITexture))]
public class SceneFadeInOut : MonoBehaviour 
{
    #region Variables (private)

    [SerializeField]
    private float fadeSpeed = 1.5f;
    private bool sceneStarting = true;
    private GUITexture guiTexture;

    #endregion


    #region Unity event functions

    void Awake()
    {
        guiTexture = GetComponent<GUITexture> ();
        guiTexture.pixelInset = new Rect (0f, 0f, Screen.width, Screen.height);
    }

    void Update()
    {
        if (sceneStarting)
        {
            StartScene ();
        }
    }

    #endregion


    #region Methods (private)

    private void FadeToClear()
    {
        guiTexture.color = Color.Lerp (guiTexture.color, Color.clear, fadeSpeed * Time.deltaTime);
    }

    private void FadeToBlack()
    {
        guiTexture.color = Color.Lerp (guiTexture.color, Color.black, fadeSpeed * Time.deltaTime);
    }

    private void StartScene()
    {
        FadeToClear ();

        if (guiTexture.color.a <= 0.05f)
        {
            guiTexture.color = Color.clear;
            guiTexture.enabled = false;
            sceneStarting = false;
        }
    }

    private void EndScene()
    {
        guiTexture.enabled = true;
        FadeToBlack ();

        if (guiTexture.color.a >= 0.95f)
        {
            SceneManager.LoadScene (1);
        }
    }

    #endregion
}
