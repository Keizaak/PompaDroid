using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))] //requires a BoxCollider component for it to work
public class ActorCollider : MonoBehaviour {

    //needs values for the BoxCollider's center and size when the actor is standing
    public Vector3 standingColliderCenter;
    public Vector3 standingColliderSize;

    //or when it's knocked down
    public Vector3 downColliderCenter;
    public Vector3 downColliderSize;

    private BoxCollider actorCollider;

    void Awake()
    {
        actorCollider = GetComponent<BoxCollider>();    
    }

    //assigns appropriate values to the BoxCollider
    public void SetColliderStance(bool isStanding)
    {
        if (isStanding)
        {
            actorCollider.center = standingColliderCenter;
            actorCollider.size = standingColliderSize;
        } else
        {
            actorCollider.center = downColliderCenter;
            actorCollider.size = downColliderSize;
        }
    }
}
