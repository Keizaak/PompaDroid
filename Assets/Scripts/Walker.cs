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
using UnityEngine.AI;

[RequireComponent(typeof(Actor))] //requires an Actor reference. This Actor will be moved by the script
public class Walker : MonoBehaviour
{

    public NavMeshAgent navMeshAgent;
    private NavMeshPath navPath;
    private List<Vector3> corners;

    float currentSpeed;
    float speed;

    private Actor actor; //references the Actor that will walk 
    private System.Action didFinishWalk; //called when the Walker reaches its destination

    void Start()
    {
        //prevent the NavMeshAgent from updating this GameObject's transform (avoid conflicts with the RigidBody which can modify the transform too)
        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
        actor = GetComponent<Actor>();
    }

    //calculates how the walker should move to the requested targetPosition
    //returns whether it found a possible path or not
    public bool MoveTo(Vector3 targetPosition, System.Action callback = null)
    {
        //teleports the navMeshAgent to its current position
        navMeshAgent.Warp(transform.position);
        didFinishWalk = callback;
        speed = actor.speed;

        //calculates how to get to targetPosition using CalculatePath method
        navPath = new NavMeshPath();
        bool pathFound = navMeshAgent.CalculatePath(targetPosition, navPath);

        //if a path is found, it stores it and returns true
        if (pathFound)
        {
            //the calculated path (in the form of a list of positions) is stored. These positions represent the "corners" of the path towards the target position
            corners = navPath.corners.ToList();
            return true;
        }
        return false;
    }

    //clears the path and stops any movement from the Walker script
    public void StopMovement()
    {
        navPath = null;
        corners = null;
        currentSpeed = 0;
    }

    //manually moves the Walker toward the targetPosition
    protected void FixedUpdate()
    {
        bool canWalk = actor.CanWalk();

        //checks if canWalk() returns true and if a path exist
        if (canWalk && corners != null && corners.Count > 0)
        {
            currentSpeed = speed;
            //moves the Actor's position towards the first of the corners
            actor.body.MovePosition(Vector3.MoveTowards(transform.position, corners[0], Time.fixedDeltaTime * speed));

            //once the walker reaches a corner, removes that position from the list
            if (Vector3.SqrMagnitude(transform.position - corners[0]) < 0.6f)
            {
                corners.RemoveAt(0);
            }

            if (corners.Count > 0)
            {
                currentSpeed = speed;
                //flip the droid around when necessary based on the direction it's headed towards
                Vector3 direction = transform.position - corners[0];
                actor.FlipSprite(direction.x >= 0);
            }
            else
            {
                //when corners runs out of entries, the didFinishWalk callback is triggered
                currentSpeed = 0.0f;
                if (didFinishWalk != null)
                {
                    didFinishWalk.Invoke();
                    didFinishWalk = null;
                }
            }
        }
        actor.baseAnim.SetFloat("Speed", currentSpeed);
    }
}