using UnityEngine;
using System.Collections;

public class JumpColliderItem : MonoBehaviour {

    public int isTriggeredCount = 0;

    void OnTriggerEnter(Collider collider)
    {
        isTriggeredCount++;
    }

    void OnTriggerExit(Collider collider)
    {
        isTriggeredCount--;
    }
}
