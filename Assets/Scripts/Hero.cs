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
using System;

public class Hero : Actor
{


    public float walkSpeed = 2; //walkSpeed is assigned to the speed value when the hero is walking
    public float runSpeed = 5;

    bool isRunning;
    bool isMoving;
    float lastWalk;
    public bool canRun = true;
    float tapAgainToRunTime = 0.2f;
    Vector3 lastWalkVector;

    Vector3 currentDir; // contains the actual direction the hero will be moving
    bool isFacingLeft; //is true when the hero faces left and false when he faces right

    //store whether the jump is currently playing or not
    bool isJumpLandAnim;
    bool isJumpingAnim;

    public InputHandler input; //reference to the InputHandler script and powers the input for the Hero script

    public float jumpForce = 1750; //controls how much force to add when the hero jumps
    private float jumpDuration = 0.2f; //detects higher jumps
    private float lastJumpTime; //the last time the hero jumped

    bool isAttackingAnim;
    float lastAttackTime;
    float attackLimit = 0.14f; //limit the player's ability to set up excessive attack queues

    public Walker walker; //enables the hero to be walked around automatically
    public bool isAutoPiloting; //flag that disables the processing of character movement when he's under walker's control
    public bool controllable = true; //flag the controls and enable/disable player input

    public bool canJumpAttack = true;
    //triggers attacks for the hero
    private int currentAttackChain = 1;
    public int evaluatedAttackChain = 0;

    public AttackData jumpAttack;

    bool isHurtAnim;

    public AttackData runAttack;
    public float runAttackForce = 1.8f;

    public AttackData normalAttack2; //2nd combo punch
    public AttackData normalAttack3; //final combo punch

    float chainComboTimer;
    public float chainComboLimit = 0.3f; //timeframe where the player can trigger the next combo attack
    const int maxCombo = 3;

    public float hurtTolerance; //the amount of damage the hero can take before collapsing
    public float hurtLimit = 20; //max value for hurtTolerance
    public float recoveryRate = 5; //amount by which hurtTolerance will be increased per second until it reaches the hurtLimit value

    bool isPickingUpAnim;
    bool weaponDropPressed = false; //whether the player pressed jump, which forces the hero to drop any equipped weapons
    public bool hasWeapon;

    public bool canJump = true;

    public SpriteRenderer powerupSprite;
    public Powerup nearbyPowerup;
    public Powerup currentPowerup;
    public GameObject powerupRoot;

    public AudioClip hit2Clip;

    public GameManager gameManager;

    public JumpCollider jumpCollider;

    protected override void Start()
    {
        base.Start();
        lifeBar = GameObject.FindGameObjectWithTag("HeroLifeBar").GetComponent<LifeBar>();
        lifeBar.SetProgress(currentLife / maxLife);
    }

    //override = change the behavior of a parent class' method
    public override void Update()
    {
        base.Update(); //invoke Update() from Actor (base = refers to the superclass)

        if (!isAlive)
        {
            return; //prevents the hero from handling button presses when he's dead
        }

        isAttackingAnim = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("attack1") ||
          baseAnim.GetCurrentAnimatorStateInfo(0).IsName("attack2") ||
          baseAnim.GetCurrentAnimatorStateInfo(0).IsName("attack3") ||
          baseAnim.GetCurrentAnimatorStateInfo(0).IsName("jump_attack") ||
          baseAnim.GetCurrentAnimatorStateInfo(0).IsName("run_attack"); //detects when the hero is in the attack state
        isJumpLandAnim = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("jump_land");
        isJumpingAnim = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("jump_rise") ||
          baseAnim.GetCurrentAnimatorStateInfo(0).IsName("jump_fall");
        isHurtAnim = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("hurt");
        isPickingUpAnim = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("pickup");

        //prevents the script from perfoming actions during hero's entrance
        if (isAutoPiloting)
        {
            return;
        }

        float h = input.GetHorizontalAxis(); //Check for user input by storing the value of the horizontal input in the h variable
        float v = input.GetVerticalAxis(); //same for the vertical input in the v variable
        bool jump = input.GetJumpButtonDown();
        bool attack = input.GetAttackButtonDown();

        currentDir = new Vector3(h, 0, v); //store the h and v values to the currentDir Vector3
        //Horizontal input is translated to the hero's x-axis movement while vertical input is translated to movement along the z-axis 
        currentDir.Normalize();

        //limits the hero's movement when the attack animation is playing
        if (!isAttackingAnim)
        {
            //Check for the horizontal or vertical input
            if ((v == 0 && h == 0)) //no input is found => make the hero stop
            {
                Stop();
                isMoving = false;
            }
            else if (!isMoving && (v != 0 || h != 0)) //either of the two inputs is found => the hero walks
            {
                isMoving = true;
                float dotProduct = Vector3.Dot(currentDir, lastWalkVector); //determine if the user pressed the same direction twice. 1 = same way ; -1 = opposite directions ; 0 = perpendicular directions
                
                //A positive dotProduct means the same direction was pressed twice
                //If both inputs occur within the time interval set in tapAgainToRunTime, call Run()
                //makes the hero run whenever the same direction is rapidly pressed twice (double tap)
                if (canRun && Time.time < lastWalk + tapAgainToRunTime && dotProduct > 0)
                {
                    Run();
                }
                else
                {
                    Walk();
                    // only for horizontal movement
                    if (h != 0)
                    {
                        lastWalkVector = currentDir; //store the current movement direction
                        lastWalk = Time.time; //store the current time
                    }
                }
            }
        }

        //checks if the hero is performing a chainAttack
        if (chainComboTimer > 0)
        {
            chainComboTimer -= Time.deltaTime;

            //checks if the chainComboTimer is expired
            //resets then the timer and the animator 
            if (chainComboTimer < 0)
            {
                chainComboTimer = 0;
                currentAttackChain = 0;
                evaluatedAttackChain = 0;
                baseAnim.SetInteger("CurrentChain", currentAttackChain);
                baseAnim.SetInteger("EvaluatedChain", evaluatedAttackChain);
            }
        }

        //drop the powerup when the player presses jump
        if(jump && hasWeapon)
        {
            weaponDropPressed = true;
            DropWeapon();
        }

        if(weaponDropPressed && !jump)
        {
            weaponDropPressed = false;
        }

        //allows the hero to jump when the player presses the appropriate input
        //  AND the hero isn't knocked out and isn't in the midst of landing
        // AND isn't attacking and isn't picking up powerups and aren't droping powerups
        //  AND he isGrounded on the floor OR the jumpDuration hasn't expired
        if (canJump && jump && !isKnockedOut && jumpCollider.CanJump(currentDir, frontVector) &&
            !isJumpLandAnim && !isAttackingAnim && !isPickingUpAnim && !weaponDropPressed &&
            (isGrounded || (isJumpingAnim && Time.time < lastJumpTime + jumpDuration)))
        {
            Jump(currentDir);
        }

        if (attack && Time.time >= lastAttackTime + attackLimit && isGrounded && !isPickingUpAnim)
        {
            if(nearbyPowerup != null && nearbyPowerup.CanEquip())
            {
                lastAttackTime = Time.time;
                Stop();
                PickupWeapon(nearbyPowerup);
            }
        }

        if (attack && Time.time >= lastAttackTime + attackLimit && !isKnockedOut && !isPickingUpAnim)
        {
            lastAttackTime = Time.time;
            Attack();
        }

        //calculates the hurtTolerance value per second
        if (hurtTolerance < hurtLimit)
        {
            hurtTolerance += Time.deltaTime * recoveryRate;
            hurtTolerance = Mathf.Clamp(hurtTolerance, 0, hurtLimit);
        }

    }

    public void Stop()
    {
        speed = 0; // set the hero's speed to 0 to stop him from moving
        baseAnim.SetFloat("Speed", speed); // set his Animator's Speed parameter to 0 to ensure that the hero returns to the idle animation
        isRunning = false;
        baseAnim.SetBool("IsRunning", isRunning);
    }

    public void Walk()
    {
        speed = walkSpeed; //copy the value of walkSpeed to speed to make the hero move
        baseAnim.SetFloat("Speed", speed); //set his Animator's Speed parameter to the same value as speed to make his walk animation play later
        isRunning = false;
        baseAnim.SetBool("IsRunning", isRunning);
    }


    void FixedUpdate()
    {
        if (!isAlive)
        {
            return;
        }

        if (!isAutoPiloting)
        {
            Vector3 moveVector = currentDir * speed;
            if (isGrounded && !isAttackingAnim && !isJumpLandAnim && !isKnockedOut && !isHurtAnim)
            {
                body.MovePosition(transform.position + moveVector * Time.fixedDeltaTime);
                baseAnim.SetFloat("Speed", moveVector.magnitude);
            }

            if (moveVector != Vector3.zero && isGrounded && !isKnockedOut && !isAttackingAnim)
            {
                if (moveVector.x != 0)
                {
                    isFacingLeft = moveVector.x < 0;
                }
                FlipSprite(isFacingLeft);
            }
        }
    }


    public void Run()
    {
        speed = runSpeed;
        isRunning = true;
        baseAnim.SetBool("IsRunning", isRunning);
        baseAnim.SetFloat("Speed", speed);
    }

    void Jump(Vector3 direction)
    {
        if (!isJumpingAnim)
        {
            baseAnim.SetTrigger("Jump");
            lastJumpTime = Time.time;

            Vector3 horizontalVector = new Vector3(direction.x, 0, direction.z) * speed * 40;
            body.AddForce(horizontalVector, ForceMode.Force);
        }

        Vector3 verticalVector = Vector3.up * jumpForce * Time.deltaTime;
        body.AddForce(verticalVector, ForceMode.Force);
    }

    protected override void DidLand()
    {
        base.DidLand();
        Walk();
    }

    public override void Attack()
    {
        if (currentAttackChain <= maxCombo)
        {
            if (!isGrounded)
            {
                if (isJumpingAnim && canJumpAttack)
                {
                    canJumpAttack = false;
                    currentAttackChain = 1;
                    evaluatedAttackChain = 0;
                    baseAnim.SetInteger("EvaluatedChain", evaluatedAttackChain);
                    baseAnim.SetInteger("CurrentChain", currentAttackChain);
                    body.velocity = Vector3.zero;
                    body.useGravity = false;
                }
            }
            else
            {
                if (isRunning)
                {
                    body.AddForce((Vector3.up + (frontVector * 5)) * runAttackForce, ForceMode.Impulse);
                    currentAttackChain = 1;
                    evaluatedAttackChain = 0;
                    baseAnim.SetInteger("CurrentChain", currentAttackChain);
                    baseAnim.SetInteger("EvaluatedChain", evaluatedAttackChain);
                }
                else
                {
                    if (currentAttackChain == 0 || chainComboTimer == 0)
                    {
                        currentAttackChain = 1;
                        evaluatedAttackChain = 0;
                    }
                    baseAnim.SetInteger("EvaluatedChain", evaluatedAttackChain);
                    baseAnim.SetInteger("CurrentChain", currentAttackChain);
                }
            }
        }
    }

    public void DidChain(int chain)
    {
        evaluatedAttackChain = chain;
        baseAnim.SetInteger("EvaluatedChain", evaluatedAttackChain);
    }

    public void AnimateTo(Vector3 position, bool shouldRun, Action callback)
    {
        if (shouldRun)
        {
            Run();
        }
        else
        {
            Walk();
        }
        walker.MoveTo(position, callback);
    }

    public void UseAutopilot(bool useAutopilot)
    {
        isAutoPiloting = useAutopilot;
        walker.enabled = useAutopilot;
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        if (collision.collider.name == "Floor")
        {
            canJumpAttack = true;
        }
    }

    public void DidJumpAttack()
    {
        body.useGravity = true;
    }

    private void AnalyzeSpecialAttack(AttackData attackData, Actor actor, Vector3 hitPoint, Vector3 hitVector)
    {
        actor.EvaluateAttackData(attackData, hitVector, hitPoint);
        chainComboTimer = chainComboLimit;
    }

    protected override void HitActor(Actor actor, Vector3 hitPoint, Vector3 hitVector)
    {
        if (baseAnim.GetCurrentAnimatorStateInfo(0).IsName("attack1"))
        {
            //if the hasWeapon flag is true, the attackData1 of the powerup is used to calculate damage
            //otherwise, the game sticks with the normal attack
            AttackData attackData = hasWeapon ? currentPowerup.attackData1 : normalAttack; //ternary operator
            //handles the damage
            AnalyzeNormalAttack(attackData, 2, actor, hitPoint, hitVector);
            //when a powerup weapon is equipped, subtracts its uses
            PlaySFX(hitClip);
            if (hasWeapon)
            {
                currentPowerup.Use();
            }
        }
        else if (baseAnim.GetCurrentAnimatorStateInfo(0).IsName("attack2"))
        {
            AttackData attackData = hasWeapon ? currentPowerup.attackData2 : normalAttack2;
            AnalyzeNormalAttack(attackData, 3, actor, hitPoint, hitVector);
            PlaySFX(hitClip);
            if (hasWeapon)
            {
                currentPowerup.Use();
            }
        }
        else if (baseAnim.GetCurrentAnimatorStateInfo(0).IsName("attack3"))
        {
            AttackData attackData = hasWeapon ? currentPowerup.attackData3 : normalAttack3;
            AnalyzeNormalAttack(attackData, 1, actor, hitPoint, hitVector);
            PlaySFX(hit2Clip);
            if (hasWeapon)
            {
                currentPowerup.Use();
            }
        }
        else if (baseAnim.GetCurrentAnimatorStateInfo(0).IsName("jump_attack"))
        {
            AnalyzeSpecialAttack(jumpAttack, actor, hitPoint, hitVector);
            PlaySFX(hit2Clip);
        }
        else if (baseAnim.GetCurrentAnimatorStateInfo(0).IsName("run_attack"))
        {
            AnalyzeSpecialAttack(runAttack, actor, hitPoint, hitVector);
            PlaySFX(hit2Clip);
        }
    }

    public override void TakeDamage(float value, Vector3 hitVector, bool knockdown = false)
    {
        hurtTolerance -= value;
        if (hurtTolerance <= 0 || !isGrounded)
        {
            hurtTolerance = hurtLimit;
            knockdown = true;
        }
        if (hasWeapon)
        {
            DropWeapon();
        }
        base.TakeDamage(value, hitVector, knockdown);
    }

    public override bool CanWalk()
    {
        return (isGrounded && !isAttackingAnim && !isJumpLandAnim && !isKnockedOut && !isHurtAnim);
    }

    protected override IEnumerator KnockdownRoutine()
    {
        body.useGravity = true;
        return base.KnockdownRoutine();
    }


    private void AnalyzeNormalAttack(AttackData attackData, int attackChain, Actor actor, Vector3 hitPoint, Vector3 hitVector)
    {
        actor.EvaluateAttackData(attackData, hitVector, hitPoint);
        currentAttackChain = attackChain;
        chainComboTimer = chainComboLimit;
    }

    public void PickupWeapon(Powerup powerup)
    {
        baseAnim.SetTrigger("PickupPowerup");
    }

    //picks up any powerups that are near the hero
    public void DidPickupWeapon()
    {
        if(nearbyPowerup != null && nearbyPowerup.CanEquip())
        {
            Powerup powerup = nearbyPowerup;
            hasWeapon = true;
            currentPowerup = powerup;
            nearbyPowerup = null;
            powerupRoot = currentPowerup.rootObject;
            powerup.user = this;

            //prevents the rigidbody of the powerup from moving and hides the powerup in the scene
            currentPowerup.body.velocity = Vector3.zero;
            powerupRoot.SetActive(false);
            //constrains the hero's motion to walking only
            Walk();

            powerupSprite.enabled = true;
            canRun = false;
            canJump = false;
        }
    }

    public void DropWeapon()
    {
        //enables the powerupRoot again
        powerupRoot.SetActive(true);
        //causes the hero to toss the glove sprite upward
        powerupRoot.transform.position = transform.position + Vector3.up;
        currentPowerup.body.AddForce(Vector3.up * 100);

        //reset
        powerupRoot = null;
        currentPowerup.user = null;
        currentPowerup = null;
        nearbyPowerup = null;

        powerupSprite.enabled = false;
        canRun = true;
        hasWeapon = false;
        canJump = true;
    }

    void OnTriggerEnter(Collider collider)
    {
        //checks if a colliding trigger collider is a member of the powerup layer
        if (collider.gameObject.layer == LayerMask.NameToLayer("Powerup"))
        {
            Powerup powerup = collider.gameObject.GetComponent<Powerup>();
            if(powerup != null)
            {
                nearbyPowerup = powerup;
            }
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if(collider.gameObject.layer == LayerMask.NameToLayer("Powerup"))
        {
            Powerup powerup = collider.gameObject.GetComponent<Powerup>();
            if(powerup == nearbyPowerup)
            {
                nearbyPowerup = null;
            }
        }
    }

    public override void DidHitObject(Collider collider, Vector3 hitPoint, Vector3 hitVector)
    {
        Container containerObject = collider.GetComponent<Container>();

        if(containerObject != null)
        {
            containerObject.Hit(hitPoint);
            PlaySFX(hitClip);
            if(containerObject.CanBeOpened() && collider.tag != gameObject.tag)
            {
                containerObject.Open(hitPoint);
            }
        } else
        {
            base.DidHitObject(collider, hitPoint, hitVector);
        }
    }

    protected override void Die()
    {
        base.Die();
        gameManager.GameOver();
    }

}
