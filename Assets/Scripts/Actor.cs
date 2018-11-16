/*
 * Copyright (c) 2018 Razeware LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish, 
 * distribute, sublicense, create a derivative work, and/or sell copies of the 
 * Software in any work that is designed, intended, or marketed for pedagogical or 
 * instructional purposes related to programming, coding, application development, 
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works, 
 * or sale is expressly withheld.
 *    
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Actor : MonoBehaviour
{

    public Animator baseAnim; //references the animator created for the character's animation
    public Rigidbody body; //refers to the rigidbody that will handle the physics of the character
    public SpriteRenderer shadowSprite; //references the shadow beneath the character's feet

    public float speed = 2; //the current speed

    protected Vector3 frontVector; //contains the direction the character is facing (its value should be (X:-1, Y:0, Z:0) when the character faces left and (X:1, Y:0, Z:0) when he's facing right)
    public bool isGrounded; //detects if the character is in a collision with the floor

    public SpriteRenderer baseSprite; //references the baseSprite of any instance of the Actor class

    public bool isAlive = true;

    public float maxLife = 100.0f; //defines the maximum value of the actor's life
    public float currentLife = 100.0f; //instance's current life value

    public AttackData normalAttack;

    protected Coroutine knockdownRoutine; //animate the actor's knockdown
    public bool isKnockedOut;

    public GameObject hitSparkPrefab;

    public LifeBar lifeBar;
    public Sprite actorThumbnail;

    public GameObject hitValuePrefab;

    public AudioClip deathClip;
    public AudioClip hitClip;

    public AudioSource audioSource;

    protected ActorCollider actorCollider;

    protected bool canFlich = true; //whether the corresponding actor flinches when it takes damage

    protected virtual void Start()
    {
        currentLife = maxLife;
        isAlive = true;
        baseAnim.SetBool("IsAlive", isAlive);

        actorCollider = GetComponent<ActorCollider>();
        actorCollider.SetColliderStance(true);
    }

    //virtual = allows any derived class to override the method
    public virtual void Update()
    {
        //tell the shadow sprite to give the hero some space by staying at a y value of 0
        Vector3 shadowSpritePosition = shadowSprite.transform.position;
        shadowSpritePosition.y = 0;
        shadowSprite.transform.position = shadowSpritePosition;
    }

    //automatically called whenever another collider hits the attached collider
    protected virtual void OnCollisionEnter(Collision collision)
    {
        // detect when the hero collides with a GameObject named "Floor"
        if (collision.collider.name == "Floor")
        {
            isGrounded = true;
            baseAnim.SetBool("IsGrounded", isGrounded);
            DidLand();
        }
    }

    //automatically called whenever an attached collider detects that another collider has stopped colliding with it
    protected virtual void OnCollisionExit(Collision collision)
    {
        //detect when the hero is in the air
        if (collision.collider.name == "Floor")
        {
            isGrounded = false;
            baseAnim.SetBool("IsGrounded", isGrounded);
        }
    }

    //called when the hero collides with the floor and get the hero walking again
    protected virtual void DidLand()
    {
    }


    public void FlipSprite(bool isFacingLeft)
    {
        if (isFacingLeft)
        {
            //makes the hero face right by scaling it to (X:1, Y:1, Z:1)
            frontVector = new Vector3(-1, 0, 0);
            //flips the object horizontally by setting its Transform Scale to (X:-1, Y:1, Z:1)
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            frontVector = new Vector3(1, 0, 0);
            transform.localScale = new Vector3(1, 1, 1);
        }
    }

    public virtual void Attack()
    {
        baseAnim.SetTrigger("Attack");
    }

    //handles what happens when an actor hits another object that has a collider
    public virtual void DidHitObject(Collider collider, Vector3 hitPoint, Vector3 hitVector)
    {
        Actor actor = collider.GetComponent<Actor>();
        //checks if the hit object contains an Actor component and if it can be hit and it hasn't the same tag
        if (actor != null && actor.CanBeHit() && collider.tag != gameObject.tag)
        {
            if (collider.attachedRigidbody != null)
            {
                HitActor(actor, hitPoint, hitVector); //registers a hit via HitActor()
            }
        }
    }

    //contains instructions for what an actor should do when it hits another actor
    protected virtual void HitActor(Actor actor, Vector3 hitPoint, Vector3 hitVector)
    {
        actor.EvaluateAttackData(normalAttack, hitVector, hitPoint);
        PlaySFX(hitClip);
    }

    //kills the actor by setting isAlive to false and playing the death animation clip by setting IsAlive parameter to false
    protected virtual void Die()
    {
        if (knockdownRoutine != null)
        {
            StopCoroutine(knockdownRoutine);
        }

        isAlive = false;
        baseAnim.SetBool("IsAlive", isAlive);
        StartCoroutine(DeathFlicker());

        PlaySFX(deathClip);

        actorCollider.SetColliderStance(false);
    }

    //changes the opacity of the actor's sprite
    //creates a flickering effect when an actor dies
    protected virtual void SetOpacity(float value)
    {
        Color color = baseSprite.color;
        color.a = value;
        baseSprite.color = color;
    }

    //toggles the hero's sprite opacity between partially transparent and opaque
    private IEnumerator DeathFlicker()
    {
        int i = 5;
        //repeats this five times to get a flicker effect
        while (i > 0)
        {
            SetOpacity(0.5f); //changes the actor's baseSprite to 50% opaque
            yield return new WaitForSeconds(0.1f); //for 0.1 seconds (waits 0.1 seconds before continuing the method)
            SetOpacity(1.0f); //then to 100% opaque
            yield return new WaitForSeconds(0.1f); //for another 0.1seconds
            i--;
        }
    }

    public virtual void TakeDamage(float value, Vector3 hitVector, bool knockdown = false)
    {
        FlipSprite(hitVector.x > 0); //makes the actor face the direction from whence the damage came
        currentLife -= value;

        //evaluates currentLife and when that value reaches 0, it triggers the actor's Die method
        if (isAlive && currentLife <= 0)
        {
            Die();
        }
        else if (knockdown)
        {
            //checks if no knockdown animation is currently running
            if (knockdownRoutine == null)
            {
                //exerts backward and upward force
                Vector3 pushbackVector = (hitVector + Vector3.up * 0.75f).normalized;
                body.AddForce(pushbackVector * 250);
                //plays the actor's KnockdownRoutine coroutine
                knockdownRoutine = StartCoroutine(KnockdownRoutine());
            }
        }
        else if(canFlich)
        {
            baseAnim.SetTrigger("IsHurt"); //if the damage isn't enough to cause death, then it should play the hurt animation
        }

        lifeBar.EnableLifeBar(true); //shows the lifebar
        lifeBar.SetProgress(currentLife / maxLife); //sets the lifebar amount to the percentage value of the actor's life
        Color color = baseSprite.color; //gets the color of this actor's sprite
        //makes the actor semi-transparent when its health falls below 0
        if(currentLife < 0)
        {
            color.a = 0.75f;
        }
        lifeBar.SetThumbnail(actorThumbnail, color); //places the actor's thumbnail next to the lifebar and tint's it with the actor's color
    }

    public virtual bool CanWalk()
    {
        return true;
    }

    //changes the direction an actor faces based on its target point
    //calls FlipSprite to calculate which direction to face based on whether the target is to actor's left or right
    public virtual void FaceTarget(Vector3 targetPoint)
    {
        FlipSprite(transform.position.x - targetPoint.x > 0);
    }

    public virtual void EvaluateAttackData(AttackData data, Vector3 hitVector, Vector3 hitPoint)
    {
        body.AddForce(data.force * hitVector);
        TakeDamage(data.attackDamage, hitVector, data.knockdown);
        ShowHitEffects(data.attackDamage, hitPoint);
    }

    public void DidGetUp()
    {
        isKnockedOut = false;
    }

    public bool CanBeHit()
    {
        return isAlive && !isKnockedOut;
    }

    protected virtual IEnumerator KnockdownRoutine()
    {
        isKnockedOut = true;
        baseAnim.SetTrigger("Knockdown");
        actorCollider.SetColliderStance(false);
        yield return new WaitForSeconds(1.0f);
        actorCollider.SetColliderStance(true);
        baseAnim.SetTrigger("GetUp");
        knockdownRoutine = null; //clears the isKnockedOut flag
    }

    //takes the amount of damage and the position then creates an instance of the HitParticle prefab at the precise position of the hit
    protected void ShowHitEffects(float value, Vector3 position)
    {
        GameObject sparkObj = Instantiate(hitSparkPrefab);
        sparkObj.transform.position = position;

        //creates a new instance of hitValuePrefab and sets its text to the amount of damage taken
        //after 1s, triggers DestroyTimer script
        GameObject obj = Instantiate(hitValuePrefab);
        obj.GetComponent<Text>().text = value.ToString();
        obj.GetComponent<DestroyTimer>().EnableTimer(1.0f);

        GameObject canvas = GameObject.FindGameObjectWithTag("WorldCanvas");
        obj.transform.SetParent(canvas.transform); //makes it the child of the damage value
        //damage value is positioned in the place of the hit
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.transform.position = position;
    }

    public void PlaySFX(AudioClip clip)
    {
        audioSource.PlayOneShot(clip); //plays the sound clip once
    }

}

//make its variables visible in the inspector
[System.Serializable]
public class AttackData
{
    public float attackDamage = 10;
    public float force = 50;
    public bool knockdown = false;
}

