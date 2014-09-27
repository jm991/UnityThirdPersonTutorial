using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof(ThirdPersonCamera))]
public class ThirdPersonCameraEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		EditorGUILayout.Space();
		ThirdPersonCamera camera = (ThirdPersonCamera) target;
		ReadOnlyField(camera.CamState.GetType().ToString(), camera.CamState);
	}
	
	private void ReadOnlyField(string title, object content)
	{
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel(title);
		EditorGUILayout.LabelField(content.ToString());
		EditorGUILayout.EndHorizontal();
	}
}
