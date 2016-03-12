/// <summary>
/// UnityTutorials - A Unity Game Design Prototyping Sandbox
/// <copyright>(c) John McElmurray 20163</copyright>
/// 
/// UnityTutorials homepage: https://github.com/jm991/UnityTutorials
/// 
/// This software is provided 'as-is', without any express or implied
/// warranty.  In no event will the authors be held liable for any damages
/// arising from the use of this software.
///
/// Permission is granted to anyone to use this software for any purpose,
/// and to alter it and redistribute it freely, subject to the following restrictions:
///
/// 1. The origin of this software must not be misrepresented; you must not
/// claim that you wrote the original software. If you use this software
/// in a product, an acknowledgment in the product documentation would be
/// appreciated but is not required.
/// 2. Altered source versions must be plainly marked as such, and must not be
/// misrepresented as being the original software.
/// 3. This notice may not be removed or altered from any source distribution.
/// </summary>

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TargetingSystem : MonoBehaviour 
{
	#region Variables (private)

	// Inspector serialized
	[SerializeField]
	private CharacterControllerLogic player;
	[SerializeField]
	private GameObject[] targets;
	[SerializeField]
	private string targetTag = "Targetable";
    [SerializeField]
    private string playerTag = "Player";
    [SerializeField]
    private List<GameObject> visibleTargets;
    [SerializeField]
    private Animator animator;

    // Animator values
    [SerializeField]
    private string appearTrigger = "Locked";

	#endregion


    #region Unity event functions

	// Use this for initialization
	void Start () 
	{
        animator = GetComponent<Animator>();
        visibleTargets = new List<GameObject>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);

            if (playerObj != null)
            {
                player = playerObj.GetComponent<CharacterControllerLogic> ();
            }
        }

		// Find all targetable objects in the scene
		if (targets == null) 
		{
			targets = GameObject.FindGameObjectsWithTag(targetTag);
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
		// TODO: could optimize targets by adding a sphere collider to all objects and first checking if they collide and only considering that subset

        visibleTargets.Clear();

		// For all the targets in the scene, test to see if they are in the current frustrum, not occluded, and visible by the player (facing)
		foreach (GameObject target in targets) 
		{
            Debug.DrawLine(player.transform.position, target.transform.position, Color.blue);

			if (/*ThirdPersonCamera.IsVisibleFrom(target.GetComponent<Renderer>(), Camera.main)
			    && !ThirdPersonCamera.IsOccluded(target.GetComponent<Collider>(), Camera.main)
				&& */Vector3.Dot(player.transform.forward, (target.transform.position - player.transform.position).normalized) > 0f) 
			{
				visibleTargets.Add (target);
			}
		}

        if (visibleTargets.Count > 1)
        {
            // Sort by distance to player
            visibleTargets = visibleTargets.OrderBy(x => Vector2.Distance(player.transform.position, x.transform.position)).ToList();

            // Position the cursor above the closest target
            this.transform.position = visibleTargets[0].transform.position + new Vector3(0, visibleTargets[0].GetComponent<Collider>().bounds.size.y);
            animator.SetTrigger(appearTrigger);
        }
	}

    #endregion


	#region Methods (private)

	#endregion
}
