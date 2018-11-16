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
using UnityEngine;

[RequireComponent(typeof(Enemy))] //the required reference to Enemy (EnemyAI depends on the Enemy script for specific actions]
public class EnemyAI : MonoBehaviour
{

    public enum EnemyAction
    {
        None,
        Wait,
        Attack,
        Chase,
        Roam
    }

    //stores data for the weighted decision randomizer
    //contains a weight property and the corresponding action property
    public class DecisionWeight
    {
        public int weight;
        public EnemyAction action;
        public DecisionWeight(int weight, EnemyAction action)
        {
            this.weight = weight;
            this.action = action;
        }
    }

    Enemy enemy;
    GameObject heroObj;
    //will be used to calculate if the enemy will hit the hero
    public float attackReachMin;
    public float attackReachMax;
    public float personalSpace;

    public HeroDetector detector; //checks if the hero is nearby
    List<DecisionWeight> weights; // lists all possible actions when a decision is made
    public EnemyAction currentAction = EnemyAction.None; //the action that the AIis currently performing

    private float decisionDuration; //the time the AI must wait between decisions (determined by the most recent decision)

    void Start()
    {
        weights = new List<DecisionWeight>();
        enemy = GetComponent<Enemy>();
        heroObj = GameObject.FindGameObjectWithTag("Hero");
    }

    //moves the enemy into attack position
    //the AI will order the robot to go to the hero's position + an offset value, moving it to a point that allows for head-on attack
    private void Chase()
    {
        //set an offset vector to the normalized direction vector from the enemy towards the hero
        Vector3 directionVector = heroObj.transform.position - transform.position;
        directionVector.z = directionVector.y = 0; //only need x value to determine if the hero is to the robot's left or right
        directionVector.Normalize();
        //sets the robot's destination to a point in front of the hero so that the robot's punches land on the hero
        directionVector *= -1f;
        directionVector *= personalSpace;
        //generate a random value between (-0.4;0.4) & set it as the offset's z value (=> not always at the exact same point in front of the hero)
        directionVector.z += Random.Range(-0.4f, 0.4f);
        //determine the hero's position + a calculated offset and move the robot there
        enemy.MoveToOffset(heroObj.transform.position, directionVector);
        //waits for a random duration between (0.2;0.4)s before making another decision
        decisionDuration = Random.Range(0.2f, 0.4f);
    }

    private void Wait()
    {
        decisionDuration = Random.Range(0.2f, 0.5f);
        enemy.Wait();
    }

    private void Attack()
    {
        enemy.FaceTarget(heroObj.transform.position);
        enemy.Attack();
        decisionDuration = Random.Range(1.0f, 1.5f);
    }

    private void Roam()
    {
        float randomDegree = Random.Range(0, 360);
        Vector2 offset = new Vector2(Mathf.Sin(randomDegree), Mathf.Cos(randomDegree)); //provides a random vector
        float distance = Random.Range(20, 50);
        offset *= distance;
        Vector3 directionVector = new Vector3(offset.x, 0, offset.y);
        enemy.MoveTo(enemy.transform.position + directionVector);
        decisionDuration = Random.Range(0.3f, 0.6f);
    }

    private void DecideWithWeights(int attack, int wait, int chase, int move)
    {
        weights.Clear();

        if (attack > 0)
        {
            weights.Add(new DecisionWeight(attack, EnemyAction.Attack));
        }
        if (chase > 0)
        {
            weights.Add(new DecisionWeight(chase, EnemyAction.Chase));
        }
        if (wait > 0)
        {
            weights.Add(new DecisionWeight(wait, EnemyAction.Wait));
        }
        if (move > 0)
        {
            weights.Add(new DecisionWeight(move, EnemyAction.Roam));
        }

        int total = attack + chase + wait + move;
        int intDecision = Random.Range(0, total - 1);

        foreach (DecisionWeight weight in weights)
        {
            intDecision -= weight.weight; //substract the value of each possible EnemyAction weight in the weights list from the random index value until <= 0
            if (intDecision <= 0)
            {
                SetDecision(weight.action);
                break;
            }
        }
    }

    private void SetDecision(EnemyAction action)
    {
        currentAction = action;
        if (action == EnemyAction.Attack)
        {
            Attack();
        }
        else if (action == EnemyAction.Chase)
        {
            Chase();
        }
        else if (action == EnemyAction.Roam)
        {
            Roam();
        }
        else if (action == EnemyAction.Wait)
        {
            Wait();
        }
    }

    void Update()
    {
        //calculates the distance between the hero and enemy
        //no need the actual distance, only the squared distance, because the square root operation is expensive and unnecessary
        float sqrDistance = Vector3.SqrMagnitude(heroObj.transform.position - transform.position);
        //sets true when the distance between the hero and robot falls between attackReachMin and attackReachMax
        bool canReach = attackReachMin * attackReachMin < sqrDistance && sqrDistance < attackReachMax * attackReachMax;
        //is based on whether the two z positions are within 0.5 units of each other
        bool samePlane = Mathf.Abs(heroObj.transform.position.z - transform.position.z) < 0.5f;

        //if the droid could reach the hero but it's currently in a chase, this stops the chase and makes it wait
        if (canReach && currentAction == EnemyAction.Chase)
        {
            SetDecision(EnemyAction.Wait);
        }

        if (decisionDuration > 0.0f)
        {
            decisionDuration -= Time.deltaTime;
        }
        else
        {
            if (!detector.heroIsNearby)
            {
                DecideWithWeights(0, 20, 80, 0);
            }
            else
            {
                if (samePlane)
                {
                    if (canReach)
                    {
                        DecideWithWeights(70, 15, 0, 15);
                    }
                    else
                    {
                        DecideWithWeights(0, 10, 80, 10);
                    }
                }
                else
                {
                    DecideWithWeights(0, 20, 60, 20);
                }
            }
        }
    }

}