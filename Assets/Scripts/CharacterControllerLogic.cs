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

/// <summary>
/// #DESCRIPTION OF CLASS#
/// </summary>
public class CharacterControllerLogic : MonoBehaviour 
{
	#region Variables (private)
	
	// Inspector serialized
	[SerializeField]
	private Animator animator;
	[SerializeField]
	private ThirdPersonCamera gamecam;
	[SerializeField]
	private float rotationDegreePerSecond = 120f;
	[SerializeField]
	private float directionSpeed = 1.5f;
	[SerializeField]
    private float directionDampTime = 0.25f;
    [SerializeField]
    private float rotationDampTime = 25f;
	[SerializeField]
	private float speedDampTime = 0.05f;
	[SerializeField]
	private float fovDampTime = 3f;
	[SerializeField]
	private float jumpMultiplier = 1f;
	[SerializeField]
	private CapsuleCollider capCollider;
	[SerializeField]
    private float jumpDist = 1f;
    [SerializeField]
    private TargetingSystem targetingSystem;
    	
	
	// Private global only
	private float leftX = 0f;
	private float leftY = 0f;
	private AnimatorStateInfo stateInfo;
	private AnimatorTransitionInfo transInfo;
	private float speed = 0f;
	private float direction = 0f;
	private float charAngle = 0f;
	private const float SPRINT_SPEED = 2.0f;	
	private const float SPRINT_FOV = 75.0f;
	private const float NORMAL_FOV = 60.0f;
	private float capsuleHeight;	
	
	
	// Hashes
    private int m_LocomotionId = 0;
	private int m_LocomotionPivotLId = 0;
	private int m_LocomotionPivotRId = 0;	
	private int m_LocomotionPivotLTransId = 0;	
	private int m_LocomotionPivotRTransId = 0;	
	
	#endregion
		
	
	#region Properties (public)

    public float targetDirection;
    public float targetSpeed;

	public Animator Animator
	{
		get
		{
			return this.animator;
		}
	}

	public float Speed
	{
		get
		{
			return this.speed;
		}
	}
	
	public float LocomotionThreshold { get { return 0.2f; } }
	
	#endregion
	
	
	#region Unity event functions
	
	/// <summary>
	/// Use this for initialization.
	/// </summary>
	void Start() 
	{
		animator = GetComponent<Animator>();
		capCollider = GetComponent<CapsuleCollider>();
        capsuleHeight = capCollider.height;

        if (targetingSystem == null)
        {
            targetingSystem = GameObject.FindObjectOfType<TargetingSystem>();
        }

		if(animator.layerCount >= 2)
		{
			animator.SetLayerWeight(1, 1);
		}		
		
		// Hash all animation names for performance
        m_LocomotionId = Animator.StringToHash("Base Layer.Locomotion");
		m_LocomotionPivotLId = Animator.StringToHash("Base Layer.LocomotionPivotL");
		m_LocomotionPivotRId = Animator.StringToHash("Base Layer.LocomotionPivotR");
		m_LocomotionPivotLTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotL");
		m_LocomotionPivotRTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotR");
	}
	
	/// <summary>
	/// Update is called once per frame.
	/// </summary>
	void Update() 
	{
		if (animator && gamecam.CamState != ThirdPersonCamera.CamStates.FirstPerson)
		{
			stateInfo = animator.GetCurrentAnimatorStateInfo(0);
			transInfo = animator.GetAnimatorTransitionInfo(0);
			
			// Press A to jump
			if (Input.GetButton("Jump"))
			{
				animator.SetBool("Jump", true);
			}
			else
			{
				animator.SetBool("Jump", false);
			}	
			
			// Pull values from controller/keyboard
			leftX = Input.GetAxis("Horizontal");
			leftY = Input.GetAxis("Vertical");			
			
			charAngle = 0f;
			direction = 0f;	
			float charSpeed = 0f;
		
			// Translate controls stick coordinates into world/cam/character space
			StickToWorldspace(this.transform, gamecam.transform, ref direction, ref charSpeed, ref charAngle, IsInPivot());		
			
			// Press B to sprint
			if (Input.GetButton("Sprint"))
			{
				speed = Mathf.Lerp(speed, SPRINT_SPEED, Time.deltaTime);
				gamecam.GetComponent<Camera>().fieldOfView = Mathf.Lerp(gamecam.GetComponent<Camera>().fieldOfView, SPRINT_FOV, fovDampTime * Time.deltaTime);
			}
			else
			{
				speed = charSpeed;
				gamecam.GetComponent<Camera>().fieldOfView = Mathf.Lerp(gamecam.GetComponent<Camera>().fieldOfView, NORMAL_FOV, fovDampTime * Time.deltaTime);		
			}

            if (gamecam.CamState != ThirdPersonCamera.CamStates.Target)
            {
                animator.SetFloat ("Direction", direction, directionDampTime, Time.deltaTime);
            } 
            else
            {
                Vector3 stickVec = new Vector3(leftX, 0, leftY);

                // Convert joystick input in Worldspace coordinates
                //Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(this.transform.forward));
                // Get camera rotation
                Vector3 CameraDirection = gamecam.transform.forward;
                CameraDirection.y = 0.0f; // kill Y
                Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(CameraDirection));
                Vector3 moveDirection = referentialShift * stickVec;

                // In targeting mode, speed is equal to the stick vector magnitude with a sign determined by whether the character and stick are facing the same direction
                // It is then multiplied by the dot product of the two vectors to make it approach 0 when the vector being compared (stick vector) approaches the x axis)

                float speedSign = (Vector3.Dot(this.transform.forward, stickVec) >= 0f ? 1f : -1f);
                float testSpeed = stickVec.sqrMagnitude * speedSign;// * Vector3.Dot(this.transform.forward, stickVec);

                //speed = testSpeed;

                //Debug.Log ("Speed sign: " + speedSign + " speed val: " + testSpeed);

                float testAngle = Vector3.Angle (this.transform.forward, stickVec);

                Vector3 speedAxisSign = Vector3.Cross(this.transform.forward, stickVec) * -1f;
                Vector3 directionAxisSign = Vector3.Cross(moveDirection, this.transform.forward) * -1f;
                float testDirection = (testAngle / 100.0f) * (directionAxisSign.y >= 0f ? -1f : 1f);
                Vector3 newSpeedAxisSign = new Vector3 (0f, Vector3.Dot (this.transform.forward, moveDirection), 0f);


                Vector3 debugPoint = new Vector3 (this.transform.position.x, this.transform.position.y + 2f, this.transform.position.z);
                Debug.DrawRay(debugPoint, directionAxisSign, Color.red);
                //Debug.DrawRay(debugPoint, speedAxisSign, Color.black);
                Debug.DrawRay(debugPoint, newSpeedAxisSign, Color.black);
                Debug.DrawRay (debugPoint, new Vector3(testDirection, 0f, testSpeed), Color.green);
                Debug.DrawRay(debugPoint, moveDirection, Color.blue);
                Debug.DrawRay(debugPoint, this.transform.forward, Color.magenta);


                Vector3 baseVec = this.transform.forward;
                if (Vector3.Dot (baseVec, moveDirection) < 0f)
                {
                    // Reversing vector to measure angle to account for the X values that correspond to angles higher than 90 degrees (trying to map angles to 2D blend tree)
                    baseVec *= -1f;
                }
                testAngle = (Vector3.Angle (baseVec, moveDirection) / 100.0f);
                float directionAxisSignVal = (directionAxisSign.y >= 0 ? 1f : -1f);
                //Debug.Log ("Angle sign: " + directionAxisSign + " angle val: " + (testAngle / 100.0f) + " final val: " + testAngle * directionAxisSignVal);

                //direction = testDirection;
                //animator.SetFloat("TargetSpeed", testSpeed, speedDampTime, Time.deltaTime);

                targetSpeed = (moveDirection.sqrMagnitude * (newSpeedAxisSign.y >= 0 ? 1f : -1f));
                targetDirection = testAngle * directionAxisSignVal;
                // THIS IS THE CORRECT VALUE FOR Y and X
                animator.SetFloat ("TargetSpeed", targetSpeed, speedDampTime, Time.deltaTime);
                animator.SetFloat ("TargetDirection", targetDirection, directionDampTime, Time.deltaTime);

                //animator.SetFloat ("TargetDirection", testDirection, directionDampTime, Time.deltaTime);
                //animator.SetFloat("TargetDirection", (Vector3.Angle(this.transform.forward, moveDirection) / 100.0f) * (directionAxisSign.y >= 0 ? 1f : -1f), directionDampTime, Time.deltaTime);
            }

            animator.SetFloat("Speed", speed, speedDampTime, Time.deltaTime);
			animator.SetFloat("XStick", leftX, speedDampTime, Time.deltaTime);
			animator.SetFloat("YStick", leftY, directionDampTime, Time.deltaTime);
			animator.SetFloat("StickAngle", Mathf.Atan2(leftX, leftY) * Mathf.Rad2Deg);
			
			if (speed > LocomotionThreshold)	// Dead zone
			{
				if (!IsInPivot())
				{
					Animator.SetFloat("Angle", charAngle);
				}
			}
			if (speed < LocomotionThreshold && Mathf.Abs(leftX) < 0.05f)    // Dead zone
			{
				animator.SetFloat("Direction", 0f);
				animator.SetFloat("Angle", 0f);
			}		
		} 
	}
	
	/// <summary>
	/// Any code that moves the character needs to be checked against physics
	/// </summary>
	void FixedUpdate()
	{							
		// Rotate character model if stick is tilted right or left, but only if character is moving in that direction
		if (IsInLocomotion() && gamecam.CamState == ThirdPersonCamera.CamStates.Behind  && !IsInPivot() && ((direction >= 0 && leftX >= 0) || (direction < 0 && leftX < 0)))
		{
//			Debug.Log ("behind Rotating");
			Vector3 rotationAmount = Vector3.Lerp(Vector3.zero, new Vector3(0f, rotationDegreePerSecond * (leftX < 0f ? -1f : 1f), 0f), Mathf.Abs(leftX));
			Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
        	this.transform.rotation = (this.transform.rotation * deltaRotation);
		}		

        if (gamecam.CamState == ThirdPersonCamera.CamStates.Target /*&& ((direction >= 0 && leftX >= 0) || (direction < 0 && leftX < 0))*/ && targetingSystem.HasTarget)
        {
            //Debug.Log ("target Rotating");
            //this.transform.rotation.SetFromToRotation(this.transform.rotation.eulerAngles, targetingSystem.CurrentTarget.transform.position - this.transform.position);


            Vector3 lookPos = targetingSystem.CurrentTarget.transform.position - this.transform.position;
            lookPos.y = 0;
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            //float falloff = Vector3.Dot (gamecam.transform.forward, lookPos);

            // Makes character face target
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, rotation/* Quaternion.Euler(rotation.eulerAngles * falloff)*/, Time.deltaTime * rotationDampTime);

            //Vector3 rotationAmount = Vector3.Lerp(Vector3.zero, new Vector3(0f, -rotationDegreePerSecond * (leftX < 0f ? -1f : 1f), 0f), Mathf.Abs(leftX));
            //Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
            //this.transform.rotation = (this.transform.rotation * deltaRotation);            
        }

		
		if (IsInJump())
		{
			float oldY = transform.position.y;
			transform.Translate(Vector3.up * jumpMultiplier * animator.GetFloat("JumpCurve"));
			if (IsInLocomotionJump())
			{
				transform.Translate(Vector3.forward * Time.deltaTime * jumpDist);
			}
			capCollider.height = capsuleHeight + (animator.GetFloat("CapsuleCurve") * 0.5f);
			if (gamecam.CamState != ThirdPersonCamera.CamStates.Free)
			{
				gamecam.transform.Translate(Vector3.up * (transform.position.y - oldY));
			}
		}
	}
	
	/// <summary>
	/// Debugging information should be put here.
	/// </summary>
	void OnDrawGizmos()
	{	
	
	}
	
	#endregion
	
	
	#region Methods
	
	public bool IsInJump()
	{
		return (IsInIdleJump() || IsInLocomotionJump());
	}

	public bool IsInIdleJump()
	{
		return animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.IdleJump");
	}
	
	public bool IsInLocomotionJump()
	{
		return animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.LocomotionJump");
	}
	
	public bool IsInPivot()
	{
		return stateInfo.fullPathHash == m_LocomotionPivotLId || 
			stateInfo.fullPathHash == m_LocomotionPivotRId || 
			transInfo.nameHash == m_LocomotionPivotLTransId || 
			transInfo.nameHash == m_LocomotionPivotRTransId;
	}

    public bool IsInLocomotion()
    {		
		return stateInfo.fullPathHash == m_LocomotionId;
    }
	
	public void StickToWorldspace(Transform root, Transform camera, ref float directionOut, ref float speedOut, ref float angleOut, bool isPivoting)
    {
        Vector3 rootDirection = root.forward;
				
        Vector3 stickDirection = new Vector3(leftX, 0, leftY);
		
		speedOut = stickDirection.sqrMagnitude;

        // Get camera rotation
        Vector3 CameraDirection = camera.forward;
        CameraDirection.y = 0.0f; // kill Y
        Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(CameraDirection));
        Quaternion charReferentialShift = Quaternion.FromToRotation (Vector3.forward, Vector3.Normalize (rootDirection));

        // Convert joystick input in Worldspace coordinates
        Vector3 moveDirection = referentialShift * stickDirection;
        Vector3 targetMoveDirection = charReferentialShift * stickDirection;
		Vector3 axisSign = Vector3.Cross(moveDirection, rootDirection);
		
        //Vector3 debugPoint = new Vector3 (root.position.x, root.position.y + 2f, root.position.z);
		//Debug.DrawRay(debugPoint, moveDirection, Color.green);
		//Debug.DrawRay(debugPoint, rootDirection, Color.magenta);
        //Debug.DrawRay (debugPoint, targetMoveDirection, Color.cyan);
		//Debug.DrawRay(debugPoint, stickDirection, Color.blue);
        //Debug.DrawRay(debugPoint, axisSign, Color.red);
		
		float angleRootToMove = Vector3.Angle(rootDirection, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);
		if (!isPivoting)
		{
			angleOut = angleRootToMove;
		}
		angleRootToMove /= 180f;
		
		directionOut = angleRootToMove * directionSpeed;
	}	
	
	#endregion Methods
}
