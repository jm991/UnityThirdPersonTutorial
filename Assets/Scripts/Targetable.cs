using System;
using UnityEngine;

[RequireComponent (typeof (SphereCollider))]
public abstract class Targetable : MonoBehaviour
{
    public bool HasBeenLockedOnto = false;

    void OnDrawGizmos()
    {
        if (HasBeenLockedOnto)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere (this.transform.position + Vector3.up, 0.5f);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere (this.transform.position + Vector3.up, 0.5f);
        }
    }
}

