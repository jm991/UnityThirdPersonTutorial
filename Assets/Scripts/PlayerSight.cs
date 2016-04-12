using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerSight : Sight 
{
    #region Variables (private)

    [SerializeField]
    private List<Targetable> targetsInRange;

    #endregion


    #region Properties (public)

    public List<Targetable> TargetsInRange { get { return targetsInRange; } }

    #endregion


    #region Unity event functions

    void Awake()
    {
        col = GetComponent<SphereCollider> ();
    }

    void OnTriggerEnter(Collider other)
    {
        Targetable target = other.gameObject.GetComponent<Targetable> ();

        if (target != null && !targetsInRange.Contains(target))
        {
            targetsInRange.Add (other.gameObject.GetComponent<Targetable>());
        }
    }

    void OnTriggerExit(Collider other)
    {
        Targetable target = other.gameObject.GetComponent<Targetable> ();

        if (target != null && targetsInRange.Contains(target))
        {
            targetsInRange.Remove (target);
        }
    }

    #endregion
}
