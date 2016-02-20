
// pseudo image effect that displays useful info for your image effects

#pragma strict

@script ExecuteInEditMode
@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Camera Info")

class CameraInfo extends MonoBehaviour {

	// display current depth texture mode
	public var currentDepthMode : DepthTextureMode;
	// render path
	public var currentRenderPath : RenderingPath;
	// number of official image fx used
	public var recognizedPostFxCount : int = 0;
	
#if UNITY_EDITOR	
	function Start () {
		UpdateInfo ();		
	}

	function Update () {
		if (currentDepthMode != GetComponent.<Camera>().depthTextureMode)
			GetComponent.<Camera>().depthTextureMode = currentDepthMode;
		if (currentRenderPath != GetComponent.<Camera>().actualRenderingPath)
			GetComponent.<Camera>().renderingPath = currentRenderPath;
			
		UpdateInfo ();
	}
	
	function UpdateInfo () {
		currentDepthMode = GetComponent.<Camera>().depthTextureMode;
		currentRenderPath = GetComponent.<Camera>().actualRenderingPath;
		var fx : PostEffectsBase[] = gameObject.GetComponents.<PostEffectsBase> ();
		var fxCount : int = 0;
		for (var post : PostEffectsBase in fx) 
			if (post.enabled)
				fxCount++;
		recognizedPostFxCount = fxCount;		
	}
#endif
}
