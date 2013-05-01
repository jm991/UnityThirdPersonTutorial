/// <summary>
/// UnityTutorials
/// <copyright>(c) John McElmurray and Julian Adams 2013</copyright>
/// 
/// GitHub homepage: https://github.com/jm991/UnityTutorials
/// Fracture Studios homepage: http://www.fracture-studios.com/
/// 
/// This software is provided 'as-is', without any express or implied
/// warranty.  In no event will the authors be held liable for any damages
/// arising from the use of this software.
///
/// Permission is granted to anyone to use this software for any purpose
/// and to alter it and redistribute it freely, subject to the following 
/// restrictions:
///
/// 1. The origin of this software must not be misrepresented; you must not
/// claim that you wrote the original software. If you use this software
/// in a product, an acknowledgment in the product documentation would be
/// appreciated but is not required.
/// 2. This notice may not be removed or altered from any source distribution.
/// </summary>

using UnityEngine;
using System.Collections;
using UnityEditor;

/// <summary>
/// Editor class for widescreen bars effect.
/// </summary>
[CustomEditor (typeof(BarsEffect))]
public class BarsEffectEditor : Editor
{
	#region Variables (private)
	
	private BarsEffect effect;
	
	#endregion
	
	
	#region Unity event functions
	
	public void OnEnable()
	{
		effect = (BarsEffect) target;
	}
	
	public override void OnInspectorGUI()
	{		
		effect.coverage = EditorGUILayout.Slider(effect.coverage, 0f, 1f);
		
	   	this.DrawDefaultInspector();
	}
	
	#endregion
}

