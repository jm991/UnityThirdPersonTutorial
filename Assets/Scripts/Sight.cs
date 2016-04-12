using UnityEngine;
using System.Collections;

[RequireComponent (typeof (SphereCollider))]
public abstract class Sight : MonoBehaviour 
{
    #region Variables (protected)

    [SerializeField]
    protected SphereCollider col;

    #endregion
}
