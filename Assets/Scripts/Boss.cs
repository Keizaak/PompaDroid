using System.Collections;
using UnityEngine;

public class Boss : Enemy {

    protected override void Start()
    {
        base.Start();
        canFlich = false;
    }

    public override void TakeDamage(float value, Vector3 hitVector, bool knockdown = false)
    {
        base.TakeDamage(value, hitVector, false);
    }
}
