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
using UnityEditor;

public class TargetingSystem : MonoBehaviour 
{
	#region Variables (private)

	// Inspector serialized
	[SerializeField]
    private CharacterControllerLogic player;
    [SerializeField]
    private PlayerSight playerSight;
    [SerializeField]
    private ThirdPersonCamera gamecam;
	[SerializeField]
    private List<Targetable> targets;
    [SerializeField]
    private Targetable currentTarget;
	[SerializeField]
	private string targetTag = "Targetable";
    [SerializeField]
    private string playerTag = "Player";
    [SerializeField]
    private List<Targetable> visibleTargets;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private bool locked = false;
    private bool lastFrameLocked = false;
    private bool forceTargetChange = false;

    // Animator values
    [SerializeField]
    private string lockedTrigger = "Locked";
    [SerializeField]
    private string unlockedTrigger = "Unlocked";
    [SerializeField]
    private string appearTrigger = "Appear";
    [SerializeField]
    private string disappearTrigger = "Disappear";
    [SerializeField]
    private string disappearForcedTrigger = "DisappearForced";
    [SerializeField]
    private string appearForcedTrigger = "AppearForced";
    [SerializeField]
    private string lockingAnimation = "Locking";
    [SerializeField]
    private string appearAnimation = "Appear";
    [SerializeField]
    private string disappearAnimation = "Disappear";
    [SerializeField]
    private float targetingCamAngle = 30.0f;

    public static string[] triggers = new string[] 
    {
        "Locked",
        "Unlocked",
        "Appear",
        "Disappear",
        "DisappearForced",
        "AppearForced",
    };

    // Hashes
    private int m_DisappearId = 0;
    private int m_LockedId = 0;
    private int m_LockingId = 0;
    private int m_RedId = 0;
    private int m_DisappearTransId = 0;
    private int m_DisappearRedTransId = 0;
    private int m_LockedTransId = 0;
    private int m_LockingTransId = 0;
    private int m_RedTransId = 0;

    // Private global only
    private AnimatorStateInfo stateInfo;
    private AnimatorTransitionInfo transInfo;

	#endregion


    #region Properties (public)

    public bool ForceUnlock { get; set; }

    public float TargetingCamAngle { get { return targetingCamAngle; } }

    public bool HasTarget { get { return currentTarget != null; } } 

    // public bool IsTargetLocked { get { return IsTargetLocked != null; } } 

    public Targetable CurrentTarget { get { return currentTarget; } }

    #endregion


    #region Unity event functions

	// Use this for initialization
	void Start () 
	{
        animator = GetComponent<Animator>();
        visibleTargets = new List<Targetable>();
        ForceUnlock = false;

        if (gamecam == null)
        {
            gamecam = GameObject.FindObjectOfType<ThirdPersonCamera>();
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);

        if (player == null && playerObj != null)
        {
            player = playerObj.GetComponent<CharacterControllerLogic> ();
        }

        if (playerSight == null && playerObj != null)
        {
            playerSight = playerObj.GetComponent<PlayerSight> ();
        }


		// Find all targetable objects in the scene
		// if (targets == null) 
		// {
            targets.Clear();
			GameObject[] targetObjects = GameObject.FindGameObjectsWithTag(targetTag);

            foreach (GameObject target in targetObjects)
            {
                if (target.GetComponent<Targetable>())
                {
                    targets.Add (target.GetComponent<Targetable>());
                }
            }
		// }

        // Hash all animation names for performance
        m_DisappearId = Animator.StringToHash("Targeting.Disappear");
        m_LockedId = Animator.StringToHash ("Targeting.Locked");
        m_LockingId = Animator.StringToHash ("Targeting.Locking");
        m_RedId = Animator.StringToHash ("Targeting.Red");
        m_DisappearTransId = Animator.StringToHash("Targeting.Disappear -> Targeting.Yellow");
        m_DisappearRedTransId = Animator.StringToHash("Targeting.Disappear -> Targeting.Red");
        m_LockedTransId = Animator.StringToHash("Targeting.Locked -> Targeting.Disappear");
        m_LockingTransId = Animator.StringToHash("Targeting.Locking -> Targeting.Locked");
        m_RedTransId = Animator.StringToHash("Targeting.Red -> Targeting.Locking");
    }

	// Update is called once per frame
	void Update ()
	{
        if (animator)
        {
            stateInfo = animator.GetCurrentAnimatorStateInfo (0);
            transInfo = animator.GetAnimatorTransitionInfo (0);
        }

		// TODO: could optimize targets by adding a sphere collider to all objects and first checking if they collide and only considering that subset

        visibleTargets.Clear();

		// For all the targets in the scene, test to see if they are in the current frustrum, not occluded, and visible by the player (facing)
        foreach (Targetable target in targets) 
		{
            Debug.DrawLine(player.transform.position, target.transform.position, Color.blue);

            if (ThirdPersonCamera.IsVisibleFrom(target.gameObject, Camera.main)
                && playerSight.TargetsInRange.Contains(target)
			    /*&& !ThirdPersonCamera.IsOccluded(target.GetComponent<Collider>(), Camera.main)*/
				&& Vector3.Dot(player.transform.forward, (target.transform.position - player.transform.position).normalized) > 0f) 
			{
				visibleTargets.Add (target);
			}
		}

        if (visibleTargets.Count > 0)
        {
            // Sort by distance to player
            visibleTargets = visibleTargets.OrderBy (x => Vector3.Distance(player.transform.position, x.transform.position)).ToList ();

            // Check and see if the target changed so we know whether to play the appearing animation
            bool targetChanged = false;

            if (visibleTargets [0] != currentTarget)
            {
                targetChanged = true;
                //Debug.Log ("Target changed!", this);
                //animator.ResetTrigger (appearTrigger);
                //animator.Play (appearAnimation);
                //animator.SetTriggerSafe (appearAnimation, appearForcedTrigger, 0);
            }

            // TODO: place check here for if the targeting script is locked or needs to unlock (if you leave the range)
            if (!locked && !forceTargetChange && !IsInDisappearAnimation())
            {
                // Don't change the target if we're already locked on
                currentTarget = visibleTargets [0];

                // TODO: (target switch feature) fix a bug here when cycling target to further away target and releasing
                // Debug.Log ("Target change forced");
            }

            if (forceTargetChange)
            {
                targetChanged = true;
                forceTargetChange = false;
            }

            if (currentTarget == null)
            {
                Debug.Log ("null target", this);
            }
            // Position the cursor above the closest target
            this.transform.position = currentTarget.transform.position + new Vector3 (0, currentTarget.GetComponent<Collider> ().bounds.size.y);
            //Debug.Log ("Updating position");

            // Only show targeting cursor if there is an available target and it is targeted

            if (targetChanged && currentTarget != null)// && gamecam.CamState == ThirdPersonCamera.CamStates.Target)
            {
                // Make sure to move the arrow to the original target before making it reappear
                if (transInfo.fullPathHash == m_DisappearTransId)
                {
                    currentTarget = visibleTargets [0];
                }

                //if (playerSight.TargetsInRange.Count == 0)
                {
                    animator.SetTriggerSafe (appearAnimation, appearForcedTrigger, 0);
                    // Debug.Log ("current tar 1: " + currentTarget, this);
                } 
                /*else
                {
                    animator.SetTriggerSafe (appearAnimation, appearTrigger, 0);
                    Debug.Log ("current tar 2: " + currentTarget, this);
                }*/

                //forceUnlock = false;
                //Debug.Log ("Appear trigger");
            } 

            if (!locked && lastFrameLocked)
            {
                // Debug.Log ("current tar 2: " + currentTarget, this);
                //animator.SetTriggerSafe (appearAnimation, appearTrigger, 0);

                animator.SetTriggerSafe (disappearAnimation, disappearTrigger, 0);
                //animator.SetTrigger (appearTrigger);

                forceTargetChange = true;
            } 

            // if (targetChanged)
            // {
            //    animator.SetTriggerSafe (appearAnimation, appearTrigger, 0);
            //    Debug.Log ("current tar 2: " + currentTarget, this);
            //}
        } 
        else // if (currentTarget != null)
        {            
            if (locked)
            {
                // If we're locked, unlock and switch back to Behind CamState when we don't have a target in range any longer
                Unlock ();
                Debug.Log ("Unlocked auto from locked state - behind cam", this);
                gamecam.ForceCameraState (ThirdPersonCamera.CamStates.Behind);
            } 
            else if (currentTarget == null)
            {
                // If there's no possible targets and we aren't locked, we should still hide the arrow
                animator.SetTriggerSafe (disappearAnimation, disappearForcedTrigger, 0);
            }

            // If no target is visible, we should reset the targeting   
            currentTarget = null;
        }

        if (gamecam.CamState == ThirdPersonCamera.CamStates.Target && currentTarget != null)
        {
            // Debug.Log ("Locking", this);
            animator.SetTriggerSafe (lockingAnimation, lockedTrigger, 0);
            locked = true;
            currentTarget.HasBeenLockedOnto = true;
        }

        lastFrameLocked = locked;
	}

    void LateUpdate()
    {
        if (gamecam.CamState != ThirdPersonCamera.CamStates.Target && locked)
        {
            // Release any targets
            Unlock (); 
        }
    }

    void OnDrawGizmos()
    {
        foreach (Targetable target in visibleTargets)
        {
            GUIStyle textStyle = new GUIStyle ();
            textStyle.normal.textColor = Color.green;

            float distance = Vector3.Distance (player.transform.position, target.transform.position);
            float magnitude = (player.transform.position - target.transform.position).magnitude;
            Handles.Label (target.transform.position, "dist.: " + distance + " mag.: " + magnitude, textStyle);
        }
    }

    #endregion


	#region Methods (private)

    private void Unlock()
    {  
        if (HasTarget)
        {
            // If we don't have a target anymore, we should unlock
            locked = false;
            //currentTarget = null;
            ForceUnlock = true;

            //animator.ResetTrigger (lockedTrigger);
            animator.SetTriggerSafe(disappearAnimation, disappearTrigger, 0);
            //animator.ResetTrigger (disappearTrigger);
            //Debug.Log ("Disappear trigger");
        }
    }


    #endregion


    #region Methods (public)

    public void ClearLockedFlags()
    {
        foreach (Targetable target in visibleTargets)
        {
            target.HasBeenLockedOnto = false;
        }
    }

    public bool IsInDisappearAnimation()
    {
        return stateInfo.fullPathHash == m_DisappearId ||
            stateInfo.fullPathHash == m_LockedId ||
            stateInfo.fullPathHash == m_LockingId ||
            stateInfo.fullPathHash == m_RedId ||
            transInfo.fullPathHash == m_DisappearTransId ||
            transInfo.fullPathHash == m_DisappearRedTransId ||
            transInfo.fullPathHash == m_LockedTransId ||
            transInfo.fullPathHash == m_LockingTransId ||
            transInfo.fullPathHash == m_RedTransId;
    }

    public void NextTarget()
    {
        // Using FirstOrDefault vs. First since we need to get a null value if nothing is targeted (FirstOrDefault), rather than an Exception (First)
        Targetable firstUntargeted = visibleTargets.FirstOrDefault (x => !x.HasBeenLockedOnto);

        if (firstUntargeted == null)
        {
            Debug.Log ("Cleared 1");
            ClearLockedFlags ();

            if (visibleTargets.Count > 0)
            {
                currentTarget = visibleTargets [0];
            }
        }
        else
        {
            // This all works because visibleTargets is sorted every Update by distance from player
            int index = visibleTargets.IndexOf (currentTarget);

            // Iterate to next target with wraparound logic back to the first target
            currentTarget = visibleTargets[(index + 1) % visibleTargets.Count];
            currentTarget.HasBeenLockedOnto = true;
        }

        forceTargetChange = true;

        Debug.Log ("Target change complete?");
    }

    #endregion
}
