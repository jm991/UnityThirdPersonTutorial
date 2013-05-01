using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Image Effects/BarsEffect")]
public class BarsEffect : ImageEffectBase 
{
	public float	coverage = 0.1f;
	public Texture  barTexture;
	public static float NO_COVERAGE = -0.5f;
	public static float FULL_COVERAGE = 0.0f;

	// Called by camera to apply image effect
	void OnRenderImage(RenderTexture source, RenderTexture destination) 
	{
		material.SetTexture("_BarTex", barTexture);
		material.SetFloat("_Coverage", Mathf.Lerp(NO_COVERAGE, FULL_COVERAGE, coverage));
		Graphics.Blit(source, destination, material);
	}
}