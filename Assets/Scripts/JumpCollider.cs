using UnityEngine;
using System.Collections;

public class JumpCollider : MonoBehaviour
{

    public JumpColliderItem frontCollider;
    public JumpColliderItem farCollider;
    public JumpColliderItem nearCollider;

    //tests if a jump's direction has a JumpColliderItem and that the value of isTriggeredCount < 0
    //true => blocks the jump
    public bool CanJump(Vector3 direction, Vector3 frontVector)
    {
        if (direction.z > 0 && farCollider.isTriggeredCount > 0)
        {
            return false;
        }
        else if (direction.z < 0 && nearCollider.isTriggeredCount > 0)
        {
            return false;
        }
        else if (frontVector.x > 0 && direction.x < 0 && frontCollider.isTriggeredCount > 0)
        {
            return false;
        }
        else if (frontVector.x > 0 && direction.x > 0 && frontCollider.isTriggeredCount > 0)
        {
            return false;
        }
        return true;
    }
}
