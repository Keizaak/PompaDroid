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
using UnityEngine;

//defines an attribute that adds additional behavior to methods, class and variables
//RequireComponent attribute requires any GameObject attached to this script to have a Camera component attached to it
//It ensures that certain components appear together on the same GameObject
[RequireComponent(typeof(Camera))]
public class CameraBounds : MonoBehaviour
{

    //define the virtual edges of the map
    public float minVisibleX;
    public float maxVisibleX;
    //store the actual calculated limits of the camera's-x-position
    private float minValue;
    private float maxValue;
    public float cameraHalfWidth; //stores the calculated half-width of the camera's view fustrum

    public Camera activeCamera; //stores a reference to the scene's MainCamera
    public Transform cameraRoot; //is the target transform that will move left and right to follow the player

    //references to the wall GameObjects
    public Transform leftBounds;
    public Transform rightBounds;

    public float offset;

    public Transform introWalkStart;
    public Transform introWalkEnd;
    public Transform exitWalkEnd;

    //moves the cameraRoot GameObject
    //moves the object's x-position to the x-parameter
    void Start()
    {
        //gets to the MainCamera and stores a reference
        activeCamera = Camera.main;

        //calculates half the width of the camera's view by transforming the screen's left-most and right-most points from screen space to world space equivalents using the camera's ScreenToWorldPoint method
        //then it takes the absolute distance between these points as the camera's half-view width
        cameraHalfWidth = Mathf.Abs(activeCamera.ScreenToWorldPoint(new Vector3(0, 0, 0)).x -
          activeCamera.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x) * 0.5f;
        minValue = minVisibleX + cameraHalfWidth;
        maxValue = maxVisibleX - cameraHalfWidth;

        //calculations to move the wall to the edge of the camera's view
        Vector3 position;
        position = leftBounds.transform.localPosition;
        //Substracts or adds the cameraHaldWidth value to the center of the camera to determine the edge
        position.x = transform.localPosition.x - cameraHalfWidth;
        //Moves the leftBoounds object to these edges
        leftBounds.transform.localPosition = position;

        position = rightBounds.transform.localPosition;
        position.x = transform.localPosition.x + cameraHalfWidth;
        rightBounds.transform.localPosition = position;

        //move the walk-in start marker two units to the left of the camera's left edge
        position = introWalkStart.transform.localPosition;
        position.x = transform.localPosition.x - cameraHalfWidth - 2.0f;
        introWalkStart.transform.localPosition = position;

        //move the walk-in end marker two units to the right of the camera's left edge
        position = introWalkEnd.transform.localPosition;
        position.x = transform.localPosition.x - cameraHalfWidth + 2.0f;
        introWalkEnd.transform.localPosition = position;

        //move the exit marker two units to the right of camera's right border
        position = exitWalkEnd.transform.localPosition;
        position.x = transform.localPosition.x + cameraHalfWidth + 2.0f;
        exitWalkEnd.transform.localPosition = position;

    }

    //moves the cameraRoot GameObject
    //moves the object's x-position to the x-parameter
    public void SetXPosition(float x)
    {
        Vector3 trans = cameraRoot.position;
        //limits the value of the x parameter (between min and max)
        trans.x = Mathf.Clamp(x + offset, minValue, maxValue);
        cameraRoot.position = trans;
    }

    //computes for the horizontal distance between the actorPosition parameter and the camera's x position
    //this distance becomes the value of the offset
    public void CalculateOffset(float actorPosition)
    {
        offset = cameraRoot.position.x - actorPosition;
        SetXPosition(actorPosition); //moves the camera to the actorPosition
        StartCoroutine(EaseOffset());
    }

    //gradually reduces the value of offset
    private IEnumerator EaseOffset()
    {
        //every loop decreases the offset until it reaches < 0.05 => offset = 0
        while (offset != 0)
        {
            offset = Mathf.Lerp(offset, 0, 0.1f);
            if (Mathf.Abs(offset) < 0.05f)
            {
                offset = 0;
            }
            yield return new WaitForFixedUpdate(); //forces this coroutine to pause and continue only after every call to FixedUpdate
        }
    }

    public void EnableBounds(bool isEnabled)
    {
        rightBounds.GetComponent<Collider>().enabled = isEnabled;
        leftBounds.GetComponent<Collider>().enabled = isEnabled;
    }
}