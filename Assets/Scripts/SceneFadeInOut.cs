using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Based on SceneFadeInOut.cs from Unity samples:
/// https://unity3d.com/learn/tutorials/projects/stealth/screen-fader
/// </summary>
[RequireComponent (typeof (Image))]
public class SceneFadeInOut : MonoBehaviour 
{
    #region Variables (private)

    [SerializeField]
    private float fadeSpeed = 1.5f;
    private bool sceneStarting = true;
    private Image screenFaderImage;

    #endregion


    #region Unity event functions

    void Awake()
    {
        screenFaderImage = GetComponent<Image> ();
        screenFaderImage.color = Color.black;
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
        screenFaderImage.color = Color.Lerp (screenFaderImage.color, Color.clear, fadeSpeed * Time.deltaTime);
    }

    private void FadeToBlack()
    {
        screenFaderImage.color = Color.Lerp (screenFaderImage.color, Color.black, fadeSpeed * Time.deltaTime);
    }

    private void StartScene()
    {
        FadeToClear ();

        if (screenFaderImage.color.a <= 0.05f)
        {
            screenFaderImage.color = Color.clear;
            screenFaderImage.enabled = false;
            sceneStarting = false;
        }
    }

    private void EndScene()
    {
        screenFaderImage.enabled = true;
        FadeToBlack ();

        if (screenFaderImage.color.a >= 0.95f)
        {
            SceneManager.LoadScene (1);
        }
    }

    #endregion
}
