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

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class Enemy : Actor
{

    public static int TotalEnemies; //static = class variable => all instances of Enemy share the same value
    public Walker walker;
    public bool stopMovementWhenHit = true; //determines if this Enemy should stop moving when it takes damage

    public EnemyAI ai;

    protected override void Start()
    {
        base.Start();
        lifeBar = GameObject.FindGameObjectWithTag("EnemyLifeBar").GetComponent<LifeBar>();
        lifeBar.SetProgress(currentLife / maxLife);
    }

    public void RegisterEnemy()
    {
        TotalEnemies++;
    }

    protected override void Die()
    {
        base.Die();
        ai.enabled = false;
        walker.enabled = false; //disable Walker
        TotalEnemies--;
    }

    public void MoveTo(Vector3 targetPosition)
    {
        walker.MoveTo(targetPosition);
    }

    //determines whether the enemy can walk to the right (positive offset) or the left (negative offset) of the targetPosition
    //allows the enemy to move to a free space next to the hero
    public void MoveToOffset(Vector3 targetPosition, Vector3 offset)
    {
        if (!walker.MoveTo(targetPosition + offset))
        {
            walker.MoveTo(targetPosition - offset);
        }
    }

    public void Wait()
    {
        walker.StopMovement();
    }

    //checks if the enemy's walk should be interrupted when it takes damage
    //(setting stopMovementWhenHit to true in the Inspector will stop the enemy's movement when it takes a punch)
    public override void TakeDamage(float value, Vector3 hitVector, bool knockdown = false)
    {
        if (stopMovementWhenHit)
        {
            walker.StopMovement();
        }
        base.TakeDamage(value, hitVector, knockdown);
    }

    //checks whether the hurt animation is currently playing since the enemy should not be able to walk when it's taking damage or getting up
    public override bool CanWalk()
    {
        return !baseAnim.GetCurrentAnimatorStateInfo(0).IsName("hurt") &&
          !baseAnim.GetCurrentAnimatorStateInfo(0).IsName("getup");
    }

}