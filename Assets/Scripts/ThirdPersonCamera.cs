/// <summary>
/// UnityTutorials - A Unity Game Design Prototyping Sandbox
/// <copyright>(c) John McElmurray and Julian Adams 2013</copyright>
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
using System.Collections;
using UnityEditor;


/// <summary>
/// Struct to hold data for aligning camera
/// </summary>
public struct CameraPosition 
{
	// Position to align camera to, probably somewhere behind the character
	// or position to point camera at, probably somewhere along character's axis
	private Vector3 position;
	// Transform used for any rotation
	private Transform xForm;
	
	public Vector3 Position { get { return position; } set { position = value; } }
	public Transform XForm { get { return xForm; } set { xForm = value; } }
	
	public void Init(string camName, Vector3 pos, Transform transform, Transform parent)
	{
		position = pos;
		xForm = transform;
		xForm.name = camName;
		xForm.parent = parent;
		xForm.localPosition = Vector3.zero;
		xForm.localPosition = position;
	}
}

/// <summary>
/// #DESCRIPTION OF CLASS#
/// </summary>
[RequireComponent (typeof (BarsEffect))]
public class ThirdPersonCamera : MonoBehaviour
{
	#region Variables (private)
	
	// Inspector serialized	
	[SerializeField]
	private Transform cameraXform;
	[SerializeField]
	private float distanceAway = 5.0f;
	[SerializeField]
	private float distanceAwayMultipler = 3f;
	[SerializeField]
	private float distanceUp = 1.5f;
	[SerializeField]
	private float distanceUpMultiplier = 4f;
	[SerializeField]
	private CharacterControllerLogic follow;
	[SerializeField]
	private Transform followXform;
	[SerializeField]
	private float widescreen = 0.2f;
	[SerializeField]
    private float targetingTime = 0.5f;
    [SerializeField]
    private float targetingTimer = 0.0f;
	[SerializeField]
	private float firstPersonLookSpeed = 3.0f;
	[SerializeField]
	private Vector2 firstPersonXAxisClamp = new Vector2(-70.0f, 90.0f);
	[SerializeField]
	private float fPSRotationDegreePerSecond = 120f;
	[SerializeField]
	private float firstPersonThreshold = 0.5f;
	[SerializeField]
	private float freeThreshold = -0.1f;
	[SerializeField]
	private Vector2 camMinDistFromChar = new Vector2(1f, -0.5f);
	[SerializeField]
	private float rightStickThreshold = 0.1f;
	[SerializeField]
	private const float freeRotationDegreePerSecond = -5f;
    [SerializeField]
    private float targetingRotationDegreePerSecond = 0.5f;
	[SerializeField]
	private float mouseWheelSensitivity = 3.0f;
	[SerializeField]
	private float compensationOffset = 0.2f;
	[SerializeField]
    private CamStates startingState = CamStates.Behind;
    [SerializeField]
    private TargetingSystem targetingSystem;
	
	
	// Smoothing and damping
    private Vector3 velocityCamSmooth = Vector3.zero;	
    private Vector3 lookCamSmooth = Vector3.zero;   
	[SerializeField]
    private float camSmoothDampTime = 0.1f;  
    [SerializeField]
    private float lookAtSmoothDampTime = 0.25f; 
    [SerializeField]
    private float targetSmoothDampTime = 1.0f;
    private Vector3 velocityLookDir = Vector3.zero;
	[SerializeField]
	private float lookDirDampTime = 0.1f;
    [SerializeField]
    private LayerMask wallhitMask;
	
	
	// Private global only
    private Vector3 lookDir;
	private Vector3 curLookDir;
	private BarsEffect barEffect;
    private CamStates camState = CamStates.Behind;	
	private float xAxisRot = 0.0f;
	public CameraPosition firstPersonCamPos;			
	public float lookWeight;
	private const float TARGETING_THRESHOLD = 0.01f;
	private Vector3 savedRigToGoal;
    private Vector3 savedRigToGoalDirection;
	private float distanceAwayFree;
	private float distanceUpFree;	
	private Vector2 rightStickPrevFrame = Vector2.zero;
	private float lastStickMin = float.PositiveInfinity;	// Used to prevent from zooming in when holding back on the right stick/scrollwheel
	private Vector3 nearClipDimensions = Vector3.zero; // width, height, radius
	private Vector3[] viewFrustum;
	private Vector3 characterOffset;
	private Vector3 targetPosition;	
    private Vector3 lookAt;
    private Vector3 lastLookAt;
    private float lookAtDampingThreshold = 0.3f;

    // Variables for explicitly setting camera modes
    // private CamStates forcedCamState = CamStates.Behind;  
    // private bool forceCamState = false;
    private float lastLeftTrigger = 0.0f;

    [SerializeField]
    private bool midSwitch = false;
    private int cycleCount = 0;
	
	#endregion
	
	
	#region Properties (public)	

	public Transform CameraXform
	{
		get
		{
			return this.cameraXform;
		}
	}

	public Vector3 LookDir
	{
		get
		{
			return this.curLookDir;
		}
	}

	public CamStates CamState
	{
		get
		{
			return this.camState;
		}
	}
	
	public enum CamStates
	{
		Behind,			// Single analog stick, Japanese-style; character orbits around camera; default for games like Mario64 and 3D Zelda series
		FirstPerson,	// Traditional 1st person look around
		Target,			// L-targeting variation on "Behind" mode
		Free,			// High angle; character moves relative to camera facing direction
        TargetingFree   // Most complex camera state; you can lock on using the L-targeting feature, and then move the right stick left, right, or down to have the camera in Free mode at the same time
	}

	public enum AnimatorLayers
	{
		// There is no programatic way to access the names of the layers in the Mecanim Animator controller
		// Therefore, try to keep this enum's names up to date with what's in the Animator tab
		// Layers are 0-indexed (Default "Base Layer" is always 0)
		BaseLayer = 0,
		Targeting = 1
	}

	public Vector3 RigToGoalDirection
	{
		get
		{
			// Move height and distance from character in separate parentRig transform since RotateAround has control of both position and rotation
			Vector3 rigToGoalDirection = Vector3.Normalize(characterOffset - this.transform.position);
			// Can't calculate distanceAway from a vector with Y axis rotation in it; zero it out
			rigToGoalDirection.y = 0f;

			return rigToGoalDirection;
		}
    }

    public Vector3 RigToTargetDirection
    {
        get
        {
            if (targetingSystem.CurrentTarget != null)
            {
                // Move height and distance from character in separate parentRig transform since RotateAround has control of both position and rotation
                Vector3 rigToTargetDirection = Vector3.Normalize(targetingSystem.CurrentTarget.transform.position - this.transform.position);
                // Can't calculate distanceAway from a vector with Y axis rotation in it; zero it out
                rigToTargetDirection.y = 0f;

                return rigToTargetDirection;
            }
            else
            {
                return RigToGoalDirection;
            }
        }
    }
	
	#endregion
	
	
	#region Unity event functions
	
	/// <summary>
	/// Use this for initialization.
	/// </summary>
	void Start ()
	{
        cycleCount = 0;

		cameraXform = this.transform;//.parent;
		if (cameraXform == null)
		{
			Debug.LogError("Parent camera to empty GameObject.", this);
		}
		
        if (targetingSystem == null)
        {
            targetingSystem = GameObject.FindObjectOfType<TargetingSystem>();
        }
		follow = GameObject.FindWithTag("Player").GetComponent<CharacterControllerLogic>();
		followXform = GameObject.FindWithTag("Player").transform;
		
		lookDir = followXform.forward;
		curLookDir = followXform.forward;
		
		barEffect = GetComponent<BarsEffect>();
		if (barEffect == null)
		{
			Debug.LogError("Attach a widescreen BarsEffect script to the camera.", this);
		}
		
		// Position and parent a GameObject where first person view should be
		firstPersonCamPos = new CameraPosition();
		firstPersonCamPos.Init
			(
				"First Person Camera",
				new Vector3(0.0f, 1.6f, 0.2f),
				new GameObject().transform,
				follow.transform
			);	

		camState = startingState;

		// Intialize values to avoid having 0s
		characterOffset = followXform.position + new Vector3(0f, distanceUp, 0f);
		distanceUpFree = distanceUp;
		distanceAwayFree = distanceAway;
		savedRigToGoal = RigToGoalDirection;
        savedRigToGoalDirection = RigToGoalDirection;
        // To align with lookAt and prevent camera movement at game start
        lastLookAt = characterOffset;

		// Initialize the Animator to the base layer 
		// If you have more than one layer, you'll need to do this for each layer
		follow.Animator.SetLayerWeight ((int) AnimatorLayers.Targeting, 0.0f);
	}
	
	/// <summary>
	/// Update is called once per frame.
	/// </summary>
	void Update ()
	{
		
	}
	
	/// <summary>
	/// Debugging information should be put here.
	/// </summary>
	void OnDrawGizmos ()
	{	
		if (EditorApplication.isPlaying && !EditorApplication.isPaused)
		{			
			DebugDraw.DrawDebugFrustum(viewFrustum);
		}
	}

    void OnAnimatorIK()
	{
		// Set the Look At Weight - amount to use look at IK vs using the head's animation
        follow.Animator.SetLookAtWeight(lookWeight);
        follow.Animator.SetLookAtPosition(firstPersonCamPos.XForm.position + firstPersonCamPos.XForm.forward);
	}
	
	void LateUpdate()
	{		
		viewFrustum = DebugDraw.CalculateViewFrustum(GetComponent<Camera>(), ref nearClipDimensions);

		// Pull values from controller/keyboard
		float rightX = Input.GetAxis("RightStickX");
		float rightY = Input.GetAxis("RightStickY");
		float leftX = Input.GetAxis("Horizontal");
		float leftY = Input.GetAxis("Vertical");
		float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
		float mouseWheelScaled = mouseWheel * mouseWheelSensitivity;
		float leftTrigger = Input.GetAxis("Target");
		bool bButtonPressed = Input.GetButton("ExitFPV");
		bool qKeyDown = Input.GetKey(KeyCode.Q);
		bool eKeyDown = Input.GetKey(KeyCode.E);
		bool lShiftKeyDown = Input.GetKey(KeyCode.LeftShift);

		// Abstraction to set right Y when using mouse
		if (mouseWheel != 0)
		{
			rightY = mouseWheelScaled;
		}
		if (qKeyDown)
		{
			rightX = 1;
		}
		if (eKeyDown)
		{
			rightX = -1;
		}
		if (lShiftKeyDown)
		{
			leftTrigger = 1;
        }
        if (leftTrigger <= TARGETING_THRESHOLD)
        {
            // Once trigger is let up again, we can reset this variable
            targetingSystem.ForceUnlock = false;
        }
		
		characterOffset = followXform.position + (distanceUp * followXform.up);
		
		targetPosition = Vector3.zero;


		// Determine camera state

        // Check to see if the trigger is depressed again within targetingTime seconds of leaving the Target camera state
        if (0f < targetingTimer && (lastLeftTrigger <= (1.0f - TARGETING_THRESHOLD) || leftTrigger <= (1.0f - TARGETING_THRESHOLD)))
        {
            // Indeterminate state where user has let off trigger momentarily, but can still retarget by pressing the trigger again quickly (within the bounds of targetingTime)
            targetingTimer -= Time.deltaTime;

            // If the trigger value increases during this period, send this state to TargetingSystem to cycle through the targets based on proximity
            if (leftTrigger > lastLeftTrigger && !midSwitch)
            {
                Debug.Log ("Cycle targets! " + cycleCount);
                targetingSystem.NextTarget ();
                cycleCount++;

                midSwitch = true;
            }
        }

        /*if (forceCamState)
        {
            camState = forcedCamState;
            forceCamState = false;
        } */
        if (leftTrigger > (1.0f - TARGETING_THRESHOLD) && !targetingSystem.ForceUnlock && !targetingSystem.ForceUnlock)
        {			
            TargetingSetup();

            // Reset timer/keep it at max value if we are in a state where the trigger is fully pressed
            targetingTimer = targetingTime;
            //midSwitch = false;
            // Debug.Log ("Reset cycle here?");
        } 
        else
        {	
            if (!targetingSystem.IsInDisappearAnimation())
            {
                // Debug.Log ("Cleared 2");
                targetingSystem.ClearLockedFlags ();
                // Debug.Log ("Decreasing bars... targetingTimer: " + targetingTimer + " lastLeftTrigger: " + lastLeftTrigger + " leftTrigger: " + leftTrigger);  
                barEffect.coverage = Mathf.SmoothStep (barEffect.coverage, 0f, targetingTime);
                follow.Animator.SetLayerWeight ((int)AnimatorLayers.Targeting, 0.0f);
                lookAt = characterOffset;
            }

		
            // * First Person *
            if (rightY > firstPersonThreshold && camState != CamStates.Free && !follow.IsInLocomotion ())
            {
                // Reset look before entering the first person mode
                xAxisRot = 0;
                lookWeight = 0f;
                camState = CamStates.FirstPerson;
            }

            // * Free *
            if (rightY < freeThreshold || Mathf.Abs(rightX) > Mathf.Abs(freeThreshold) || mouseWheel < 0f)// && System.Math.Round (follow.Speed, 2) == 0)
            {
                camState = CamStates.Free;
                savedRigToGoal = Vector3.zero;
                savedRigToGoalDirection = RigToGoalDirection;
            }

            // * Behind the back *
            if ((camState == CamStates.FirstPerson && bButtonPressed) ||
                (camState == CamStates.Target && leftTrigger <= TARGETING_THRESHOLD) ||
                targetingSystem.ForceUnlock)
            {
                camState = CamStates.Behind;
            }
        }
		
		// Execute camera state
		switch (camState)
		{
			case CamStates.Behind:
				ResetCamera();
			
				// Only update camera look direction if moving
                if (follow.Speed > follow.LocomotionThreshold && follow.IsInLocomotion() && !follow.IsInPivot())
				{
					lookDir = Vector3.Lerp(followXform.right * (leftX < 0 ? 1f : -1f), followXform.forward * (leftY < 0 ? -1f : 1f), Mathf.Abs(Vector3.Dot(this.transform.forward, followXform.forward)));
					Debug.DrawRay(this.transform.position, lookDir, Color.white);
				
					// Calculate direction from camera to player, kill Y, and normalize to give a valid direction with unit magnitude
					curLookDir = Vector3.Normalize(characterOffset - this.transform.position);
					curLookDir.y = 0;
					Debug.DrawRay(this.transform.position, curLookDir, Color.green);
				
					// Damping makes it so we don't update targetPosition while pivoting; camera shouldn't rotate around player
					curLookDir = Vector3.SmoothDamp(curLookDir, lookDir, ref velocityLookDir, lookDirDampTime);

                    Debug.DrawRay (followXform.position, curLookDir, Color.red);
                    Debug.DrawRay (followXform.position, lookDir, Color.blue);
				}				
				
                targetPosition = characterOffset + followXform.up * distanceUp - Vector3.Normalize(curLookDir) * distanceAway;
				
				break;
            case CamStates.Target:
                ResetCamera ();
                if (targetingTimer == targetingTime)
                {
                    midSwitch = false;
                }

                if (savedRigToGoal == Vector3.zero)
                {
                    // Debug.Log ("set savedrigtogoal");
                    savedRigToGoal = followXform.forward;
                    curLookDir = followXform.forward;
                }

                // If there's a target, the camera points between the two characters
                if (targetingSystem.HasTarget)
                {
                    Debug.DrawLine (followXform.position, targetingSystem.CurrentTarget.transform.position, Color.cyan);
                    Vector3 targetToPlayer = (targetingSystem.CurrentTarget.transform.position - followXform.position);
                    targetToPlayer.y += distanceUp;
                    Vector3 halfwayPoint = characterOffset + (targetToPlayer * 0.5f);
                    Vector3 targetToRig = (targetingSystem.CurrentTarget.transform.position - this.transform.position).normalized;
                    Debug.DrawRay (halfwayPoint, Vector3.up, Color.yellow);

                    // Find 30 degree left and right offset from targetToPlayer
                    Vector3 right = -1f * (Quaternion.LookRotation (targetToPlayer) * Quaternion.Euler (0, 180 + targetingSystem.TargetingCamAngle, 0) * new Vector3 (0, 0, 1));
                    Vector3 left = -1f * (Quaternion.LookRotation (targetToPlayer) * Quaternion.Euler (0, 180 - targetingSystem.TargetingCamAngle, 0) * new Vector3 (0, 0, 1));
                    Debug.DrawRay (targetingSystem.CurrentTarget.transform.position + Vector3.up, right * 2f, Color.black);
                    Debug.DrawRay (targetingSystem.CurrentTarget.transform.position + Vector3.up, left * 2f, Color.magenta);
                    Debug.DrawRay (this.transform.position, RigToGoalDirection * 2f, Color.green);  
                    Debug.DrawRay (this.transform.position, targetToRig, Color.green);


                    // characterOffset = halfwayPoint;


                    // ORIGINAL
                    //characterOffset = halfwayPoint + (distanceUp * followXform.up);
                    //Debug.Log ("dot: " + Vector3.Dot (followXform.forward, (targetingSystem.CurrentTarget.transform.position - this.transform.position).normalized));
                    float rightDot = Vector3.Dot ((targetingSystem.CurrentTarget.transform.position - this.transform.position).normalized, right);
                    float leftDot = Vector3.Dot ((targetingSystem.CurrentTarget.transform.position - this.transform.position).normalized, left);

                    float rightAngle = Vector3.Angle (targetToRig, right);
                    float leftAngle = Vector3.Angle (targetToRig, left);
                    // Debug.Log ("Right: " + rightAngle + " left: " + leftAngle, this);

                    Vector3 smallerAngle;

                    // See which is smaller
                    if (leftAngle < rightAngle)
                    {
                        smallerAngle = left;
                        // Debug.Log("chose left, left dot: " + leftDot + " right dot: " + rightDot);
                    } 
                    else
                    {
                        // Debug.Log("chose right, left dot: " + leftDot + " right dot: " + rightDot);
                        smallerAngle = right;
                    }

                    savedRigToGoal = smallerAngle;//Vector3.Lerp(savedRigToGoal, smallerAngle, targetSmoothDampTime * Time.deltaTime);

                    // Set curLookDir so that there is no jerkiness when returning to Behind CamState
                    curLookDir = savedRigToGoal;
                    curLookDir.y = 0f;

                    //savedRigToGoal = -1f * left;

                    // Flatten vectors so angle measurements are only in 2 movement dimensions
                    smallerAngle.y = 0;
                    Vector3 forwardTest = this.transform.forward;
                    forwardTest.y = 0;
                    float rotRemaining = Vector3.Angle (forwardTest, smallerAngle);


                    Vector3 axisSign = Vector3.Cross(smallerAngle, forwardTest);
                    float angleRootToMove = Vector3.Angle(forwardTest, smallerAngle) * (axisSign.y >= 0 ? -1f : 1f);

                    // The camera only rotates around the character if they are not moving when targeting
                    if (Mathf.Abs(leftX) <= rightStickThreshold && Mathf.Abs(leftY) <= rightStickThreshold)
                    {
                        // Debug.Log ("rotating, leftX: " + leftX + " leftY: " + leftY, this);
                        cameraXform.RotateAround (targetingSystem.CurrentTarget.transform.position, targetingSystem.CurrentTarget.transform.up, targetingRotationDegreePerSecond * Time.deltaTime * angleRootToMove);
                        //cameraXform.RotateAround (halfwayPoint, halfwayPoint + Vector3.up, targetingRotationDegreePerSecond * Time.deltaTime * angleRootToMove);
                    }
                    //Debug.Log ("angle to move remaining: " + angleRootToMove, this);
                    Debug.DrawRay (this.transform.position, forwardTest, Color.blue);
                    Debug.DrawRay (this.transform.position, smallerAngle, Color.red);

                    targetPosition = characterOffset + followXform.up - RigToGoalDirection * distanceAway;
                    // targetPosition = halfwayPoint + followXform.up - RigToGoalDirection * distanceAway;
                    // targetPosition = halfwayPoint + RigToGoalDirection * distanceAway;

                    //targetLookAt = targetingSystem.CurrentTarget.transform.position;
                    //lookAt = targetingSystem.CurrentTarget.transform.position;
                    lookAt = halfwayPoint;
                    //lookAt.y += distanceUp;
                    //lookAt = right;

                    //targetPosition = characterOffset + followXform.up - (targetingSystem.CurrentTarget.transform.position - followXform.position) * distanceAway;
                    //lookAt = Vector3.Lerp(lookAt, halfwayPoint, camSmoothDampTime * Time.deltaTime);


                    // Smoothly transition look direction towards firstPersonCamPos when entering first person mode
                    //lookAt = Vector3.Lerp(targetPosition + followXform.forward, this.transform.position + this.transform.forward, camSmoothDampTime * Time.deltaTime);

                    //cameraXform.RotateAround(halfwayPoint, followXform.up, freeRotationDegreePerSecond * 30f);
                } 
                else
                {
                    // distanceUp is not included in Target camera state - if you look at the original Zelda games, the camera goes directly level to the player's head
                    targetPosition = characterOffset + followXform.up - savedRigToGoal * distanceAway;
                    lookAt = characterOffset;
                }
								
				break;
			case CamStates.FirstPerson:	
				// Looking up and down
				// Calculate the amount of rotation and apply to the firstPersonCamPos GameObject
			    xAxisRot += (leftY * 0.5f * firstPersonLookSpeed);			
    			xAxisRot = Mathf.Clamp(xAxisRot, firstPersonXAxisClamp.x, firstPersonXAxisClamp.y); 
				firstPersonCamPos.XForm.localRotation = Quaternion.Euler(xAxisRot, 0, 0);
							
				// Superimpose firstPersonCamPos GameObject's rotation on camera
				Quaternion rotationShift = Quaternion.FromToRotation(this.transform.forward, firstPersonCamPos.XForm.forward);		
				this.transform.rotation = rotationShift * this.transform.rotation;		
				
				// Move character model's head
				lookWeight = Mathf.Lerp(lookWeight, 1.0f, Time.deltaTime * firstPersonLookSpeed);
				
				
				// Looking left and right
				// Similarly to how character is rotated while in locomotion, use Quaternion * to add rotation to character
				Vector3 rotationAmount = Vector3.Lerp(Vector3.zero, new Vector3(0f, fPSRotationDegreePerSecond * (leftX < 0f ? -1f : 1f), 0f), Mathf.Abs(leftX));
				Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
	        	follow.transform.rotation = (follow.transform.rotation * deltaRotation);
				
				// Move camera to firstPersonCamPos
				targetPosition = firstPersonCamPos.XForm.position;
			
				// Smoothly transition look direction towards firstPersonCamPos when entering first person mode
				lookAt = Vector3.Lerp(targetPosition + followXform.forward, this.transform.position + this.transform.forward, camSmoothDampTime * Time.deltaTime);
                Debug.DrawRay(this.transform.position, lookAt, Color.black);
                Debug.DrawRay(this.transform.position, targetPosition + followXform.forward, Color.white);	
                Debug.DrawRay(this.transform.position, firstPersonCamPos.XForm.position + firstPersonCamPos.XForm.forward, Color.cyan);
			
				// Choose lookAt target based on distance
				lookAt = (Vector3.Lerp(this.transform.position + this.transform.forward, lookAt, Vector3.Distance(this.transform.position, firstPersonCamPos.XForm.position)));
				break;
            case CamStates.Free:
                lookWeight = Mathf.Lerp (lookWeight, 0.0f, Time.deltaTime * firstPersonLookSpeed);

                Vector3 rigToGoal = characterOffset - cameraXform.position;
                rigToGoal.y = 0f;
                Debug.DrawRay (cameraXform.transform.position, rigToGoal, Color.red);
				
				// Panning in and out
				// If statement works for positive values; don't tween if stick not increasing in either direction; also don't tween if user is rotating
				// Checked against rightStickThreshold because very small values for rightY mess up the Lerp function
                if (rightY < lastStickMin && rightY < -1f * rightStickThreshold && rightY <= rightStickPrevFrame.y && Mathf.Abs (rightX) < rightStickThreshold)
                {
                    // Zooming out
                    distanceUpFree = Mathf.Lerp (distanceUp, distanceUp * distanceUpMultiplier, Mathf.Abs (rightY));
                    distanceAwayFree = Mathf.Lerp (distanceAway, distanceAway * distanceAwayMultipler, Mathf.Abs (rightY));
                    targetPosition = characterOffset + followXform.up * distanceUpFree - RigToGoalDirection * distanceAwayFree;
                    lastStickMin = rightY;
                    savedRigToGoalDirection = RigToGoalDirection;
                } else if (rightY > rightStickThreshold && rightY >= rightStickPrevFrame.y && Mathf.Abs (rightX) < rightStickThreshold)
                {
                    // Zooming in
                    // Subtract height of camera from height of player to find Y distance
                    distanceUpFree = Mathf.Lerp (Mathf.Abs (transform.position.y - characterOffset.y), camMinDistFromChar.y, rightY);
                    // Use magnitude function to find X distance	
                    distanceAwayFree = Mathf.Lerp (rigToGoal.magnitude, camMinDistFromChar.x, rightY);		
                    targetPosition = characterOffset + followXform.up * distanceUpFree - RigToGoalDirection * distanceAwayFree;		
                    lastStickMin = float.PositiveInfinity;
                    savedRigToGoalDirection = RigToGoalDirection;
                }				


                // Rotating around character
                cameraXform.RotateAround (characterOffset, followXform.up, freeRotationDegreePerSecond * (Mathf.Abs (rightX) > rightStickThreshold ? rightX : 0f));
                                
				// Store direction only if right stick inactive
                if ((rightX != 0 || rightY != 0) && Mathf.Abs (rightX) <= rightStickThreshold)
                {
                    // Fixes the issue where the camera keeps rotating even when stick is fully left or right
                    savedRigToGoal = savedRigToGoalDirection;//RigToGoalDirection;
                    Debug.DrawRay (this.transform.position, savedRigToGoal, Color.red);
                } 
                else
                {
                    savedRigToGoal = RigToGoalDirection;
                    savedRigToGoalDirection = RigToGoalDirection;
                }
								
				// Still need to track camera behind player even if they aren't using the right stick; achieve this by saving distanceAwayFree every frame
				if (targetPosition == Vector3.zero)
				{
					targetPosition = characterOffset + followXform.up * distanceUpFree - savedRigToGoal * distanceAwayFree;
				}

				break;
        }

        //Debug.DrawRay(followXform.position, targetPosition, Color.magenta);
        Debug.DrawRay(followXform.position, curLookDir, Color.green);
		

		CompensateForWalls(characterOffset, ref targetPosition);		
		SmoothPosition(cameraXform.position, targetPosition);	
        // The smoothDampTime is dependent on whether the difference between lastLookAt and lookAt is large or not
        float lookAtDiff = (lookAt - lastLookAt).sqrMagnitude;
        lastLookAt = Vector3.SmoothDamp(lastLookAt, lookAt, ref lookCamSmooth, lookAtDiff > lookAtDampingThreshold ? lookAtSmoothDampTime : lookAtDiff * lookAtSmoothDampTime); 
        // Debug.Log ("last look at: " + lastLookAt + " look at: " + lookAt + " difference: " + lookAtDiff);
        transform.LookAt(lastLookAt);   
        Debug.DrawRay (Vector3.zero, lookAt, Color.cyan);
        Debug.DrawRay (Vector3.zero, lastLookAt, Color.magenta);

		// Make sure to cache the unscaled mouse wheel value if using mouse/keyboard instead of controller
		rightStickPrevFrame = new Vector2(rightX, rightY);//mouseWheel != 0 ? mouseWheelScaled : rightY);

        lastLeftTrigger = leftTrigger;
	}
	
	#endregion
	

    #region Methods (public)

    /*public void ForceCameraState(CamStates forcedCamState)
    {
        this.forcedCamState = forcedCamState;
        forceCamState = true;
    }*/

    #endregion
	

    #region Methods (private)

    private void TargetingSetup()
    {
        if (camState != CamStates.Target)
        {
            savedRigToGoal = Vector3.zero;
            // If there is a target displayed on screen when entering this mode, we need to lock on it
            //targetingSystem.Lock();
        }
        camState = CamStates.Target;
        // Debug.Log ("increasing bars");
        barEffect.coverage = Mathf.SmoothStep (barEffect.coverage, widescreen, targetingTime);
        follow.Animator.SetLayerWeight ((int)AnimatorLayers.Targeting, 1.0f);
    }
	
	private void SmoothPosition(Vector3 fromPos, Vector3 toPos)
	{		
		// Making a smooth transition between camera's current position and the position it wants to be in
		cameraXform.position = Vector3.SmoothDamp(fromPos, toPos, ref velocityCamSmooth, camSmoothDampTime);
	}

	private void CompensateForWalls(Vector3 fromObject, ref Vector3 toTarget)
	{
		// Compensate for walls between camera
		RaycastHit wallHit = new RaycastHit();		
        if (Physics.Linecast(fromObject, toTarget, out wallHit, wallhitMask)) 
		{
			Debug.DrawRay(wallHit.point, wallHit.normal, Color.red);
			toTarget = wallHit.point;
		}		
		
		// Compensate for geometry intersecting with near clip plane
		Vector3 camPosCache = GetComponent<Camera>().transform.position;
		GetComponent<Camera>().transform.position = toTarget;
		viewFrustum = DebugDraw.CalculateViewFrustum(GetComponent<Camera>(), ref nearClipDimensions);
		
		for (int i = 0; i < (viewFrustum.Length / 2); i++)
		{
			RaycastHit cWHit = new RaycastHit();
			RaycastHit cCWHit = new RaycastHit();
			
			// Cast lines in both directions around near clipping plane bounds
            while (Physics.Linecast(viewFrustum[i], viewFrustum[(i + 1) % (viewFrustum.Length / 2)], out cWHit, wallhitMask) ||
                Physics.Linecast(viewFrustum[(i + 1) % (viewFrustum.Length / 2)], viewFrustum[i], out cCWHit, wallhitMask))
			{
				Vector3 normal = wallHit.normal;
				if (wallHit.normal == Vector3.zero)
				{
					// If there's no available wallHit, use normal of geometry intersected by LineCasts instead
					if (cWHit.normal == Vector3.zero)
					{
						if (cCWHit.normal == Vector3.zero)
						{
							Debug.LogError("No available geometry normal from near clip plane LineCasts. Something must be amuck.", this);
						}
						else
						{
							normal = cCWHit.normal;
						}
					}	
					else
					{
						normal = cWHit.normal;
					}
				}
				
				toTarget += (compensationOffset * normal);
				GetComponent<Camera>().transform.position += toTarget;
				
				// Recalculate positions of near clip plane
				viewFrustum = DebugDraw.CalculateViewFrustum(GetComponent<Camera>(), ref nearClipDimensions);
			}
		}
		
		GetComponent<Camera>().transform.position = camPosCache;
		viewFrustum = DebugDraw.CalculateViewFrustum(GetComponent<Camera>(), ref nearClipDimensions);
	}
	
	/// <summary>
	/// Reset local position of camera inside of parentRig and resets character's look IK.
	/// </summary>
	private void ResetCamera()
	{
		lookWeight = Mathf.Lerp(lookWeight, 0.0f, Time.deltaTime * firstPersonLookSpeed);
		transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime);
	}
	
	#endregion Methods


	#region Utilities (static)

    public static bool IsVisibleFrom(GameObject check, Camera camera)
	{
        // Based on solution found here: 11949463 stackoverflow
        // Now you have a center, calculate the bounds by creating a zero sized 'Bounds', 
        Bounds bounds = new Bounds(check.transform.position, Vector3.zero);

        // Find all children of the object that are Renderers/SkinnedMeshRenderers and add them to the bounds
        foreach (Renderer curRenderer in check.GetComponentsInChildren<Renderer>()) 
        {
            bounds.Encapsulate(curRenderer.bounds);  
        }

		// Can also consider using renderer.isVisible based on performance and shadow settings
		Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        bool test = GeometryUtility.TestPlanesAABB(planes, bounds);
        return test;
	}

	public static bool IsOccluded(Collider target, Camera camera)
	{
		// Note: this is a fairly inaccurate measure of occlusion since it assumes the object is a single point
		// Proper queries fill objects with colour, then perform a full render and check the render for the color of the object in question
		// Improvements to raycasting method use a Monte Carlo approach where a ray is cast to a differnet point on the object every update
		RaycastHit hit;
		bool hitSomething = false;

		// Calculate Ray direction
		Vector3 direction = camera.transform.position - target.transform.position; 	
		DebugDraw.ArrowDebug(camera.transform.position, direction);

		if (Physics.Raycast(target.transform.position, direction, out hit))
		{
			if (hit.collider.tag != "MainCamera") //hit something else before the camera
			{
				hitSomething = true;
			}
		}

		return hitSomething;
	}

	#endregion
}
