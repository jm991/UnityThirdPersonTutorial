using UnityEngine;
using System.Collections;

public static class DebugDraw
{	
	#region Arrows
	
	public static void ArrowGizmo(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Gizmos.DrawRay(pos, direction);
		
		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180+arrowHeadAngle,0) * new Vector3(0,0,1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180-arrowHeadAngle,0) * new Vector3(0,0,1);
		Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
		Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
	}
	
	public static void ArrowGizmo(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Gizmos.color = color;
		Gizmos.DrawRay(pos, direction);
		
		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180+arrowHeadAngle,0) * new Vector3(0,0,1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180-arrowHeadAngle,0) * new Vector3(0,0,1);
		Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
		Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
	}
	
	public static void ArrowDebug(Vector3 pos, Vector3 direction, float length = 1f, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Debug.DrawRay(pos, direction * length);
		
		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180+arrowHeadAngle,0) * new Vector3(0,0,1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180-arrowHeadAngle,0) * new Vector3(0,0,1);
		Debug.DrawRay(pos + (direction * length), right * arrowHeadLength * length);
		Debug.DrawRay(pos + (direction * length), left * arrowHeadLength * length);
	}
	
	public static void ArrowDebug(Vector3 pos, Vector3 direction, Color color, float length = 1f, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Debug.DrawRay(pos, direction * length, color);
		
		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180+arrowHeadAngle,0) * new Vector3(0,0,1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180-arrowHeadAngle,0) * new Vector3(0,0,1);
		Debug.DrawRay(pos + (direction * length), right * arrowHeadLength * length, color);
		Debug.DrawRay(pos + (direction * length), left * arrowHeadLength * length, color);
	}
	
	#endregion
	
	
	#region Frustum methods
	
	private const int 	FRUSTUM_SIZE = 8;
	private const int 	BOTTOM_LEFT_POINT = 0;
	private const int 	TOP_LEFT_POINT = 1;
	private const int 	TOP_RIGHT_POINT = 2;
	private const int 	BOTTOM_RIGHT_POINT = 3;
	private const int 	BOTTOM_LEFT_VEC = 4;
	private const int 	TOP_LEFT_VEC = 5;
	private const int 	TOP_RIGHT_VEC = 6;
	private const int 	BOTTOM_RIGHT_VEC = 7;
	
	public static void DrawDebugFrustum(Vector3[] viewFrustum)
	{
		Color[] colors = new Color[] { Color.red, Color.green, Color.blue, Color.white, Color.cyan, Color.magenta, Color.yellow, Color.black };
		
		for (int i = 0; i < BOTTOM_LEFT_VEC; i++)
		{
			// Cast lines clockwise around near clipping plane bounds
			Debug.DrawLine(viewFrustum[i], viewFrustum[(i + 1) % BOTTOM_LEFT_VEC], colors[i]);
			Gizmos.DrawWireSphere(viewFrustum[i], 0.05f);
			
			// Cast rays clockwise out of near clipping plane bounds
			Debug.DrawRay(viewFrustum[i], viewFrustum[i + BOTTOM_LEFT_VEC], colors[i + BOTTOM_LEFT_VEC]);
		}
	}
	
	public static Vector3[] CalculateViewFrustum(Camera cam, ref Vector3 dimensions)
	{
		Vector3[] frustum = new Vector3[FRUSTUM_SIZE];
		
		// Near clipping plane bounds
		frustum[BOTTOM_LEFT_POINT] = cam.ViewportToWorldPoint(new Vector3(0f, 0f, cam.nearClipPlane));
		frustum[TOP_LEFT_POINT] = cam.ViewportToWorldPoint(new Vector3(0f, 1f, cam.nearClipPlane));
		frustum[BOTTOM_RIGHT_POINT] = cam.ViewportToWorldPoint(new Vector3(1f, 0f, cam.nearClipPlane));
		frustum[TOP_RIGHT_POINT] = cam.ViewportToWorldPoint(new Vector3(1f, 1f, cam.nearClipPlane));		
		
		// Clipping planes: 0/left, 1/right, 2/bottom, 3/top, 4/near, 5/far
		Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
		frustum[BOTTOM_LEFT_VEC] = Vector3.Cross(planes[0].normal, planes[2].normal);
		frustum[TOP_LEFT_VEC] = Vector3.Cross(planes[3].normal, planes[0].normal);
		frustum[TOP_RIGHT_VEC] = Vector3.Cross(planes[1].normal, planes[3].normal);
		frustum[BOTTOM_RIGHT_VEC] = Vector3.Cross(planes[2].normal, planes[1].normal);
		
		dimensions.x = (frustum[BOTTOM_LEFT_POINT] - frustum[BOTTOM_RIGHT_POINT]).magnitude;
		dimensions.y = (frustum[TOP_LEFT_POINT] - frustum[BOTTOM_LEFT_POINT]).magnitude;
		
		// Radius needed for sphere cast - distance from corner to center of viewport
		dimensions.z = (frustum[BOTTOM_LEFT_POINT] - cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, cam.nearClipPlane))).magnitude;
		
		return frustum;
	}
	
	#endregion
}