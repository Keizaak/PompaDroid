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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    public Hero actor; //hold a reference to the Hero script
    public bool cameraFollows = true; //indicates if the camera should follow the hero
    public CameraBounds cameraBounds; //cameraBounds holds a reference to the CameraBounds script

    public LevelData currentLevelData; //references the data for the current level
    private BattleEvent currentBattleEvent; //references the active battle event from that data
    private int nextEventIndex; //index of all battle events that'll be use to fetch the next event
    public bool hasRemainingEvents; //determines whether there are available events in the level

    public List<GameObject> activeEnemies; //stores all enemies of that event
    public Transform[] spawnPositions; //stores the position in rows where these enemies will spawn

    public GameObject currentLevelBackground; //references the tile map for the level

    public GameObject robotPrefab;

    public Transform walkInStartTarget;
    public Transform walkInTarget;

    public Transform walkOutTarget; //hero's exit point

    public LevelData[] levels;
    public static int CurrentLevel = 0;

    public LifeBar enemyLifeBar;

    public GameObject goIndicator;

    public GameObject bossPrefab;

    public GameObject levelNamePrefab;
    public GameObject gameOverPrefab;

    public RectTransform uiTransform; //serve as the parent of all UI elements in the GameManager

    public GameObject loadingScreen;

    void Start()
    {

        cameraBounds.SetXPosition(cameraBounds.minVisibleX); //set the initial position of the camera to the minVisibleX

        //When GameManager executes its Start method, reset nextEventIndex & load the LevelData asset
        nextEventIndex = 0;
        StartCoroutine(LoadLevelData(levels[CurrentLevel]));
    }

    void Awake()
    {
        loadingScreen.SetActive(true);
    }

    void Update()
    {
        //checks if the game isn't running a battle event and if there are still events to play
        if (currentBattleEvent == null && hasRemainingEvents)
        {
            //checks if the next BattleEvent's column value is close enough to the hero to trigger it
            if (Mathf.Abs(currentLevelData.battleData[nextEventIndex].column - cameraBounds.activeCamera.transform.position.x) < 0.2f)
            {
                //loads the next event
                PlayBattleEvent(currentLevelData.battleData[nextEventIndex]);
            }
        }

        //checks if all robots are dead when there's an actively loaded battle event
        if (currentBattleEvent != null)
        {
            if (Robot.TotalEnemies == 0)
            {
                CompleteCurrentEvent();
            }
        }
        //move the camera's x-position to the player's x-position
        if (cameraFollows)
        {
            cameraBounds.SetXPosition(actor.transform.position.x);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private GameObject SpawnEnemy(EnemyData data)
    {
        //instantiates a boss when the data requires an EnemyType.Boss, otherwise, it generates a normal droid
        //Instantiate = creates a new GameObject that has all the data from that prefab
        GameObject enemyObj;
        if(data.type == EnemyType.Boss)
        {
            enemyObj = Instantiate(bossPrefab);
        } else
        {
            enemyObj = Instantiate(robotPrefab);
        }
        //calculate the spawn position using the row and offset values from EnemyData
        //the spawn point is outside the visible area so that the enemy spawns offscreen
        Vector3 position = spawnPositions[data.row].position;
        position.x = cameraBounds.activeCamera.transform.position.x + (data.offset * (cameraBounds.cameraHalfWidth + 1));
        enemyObj.transform.position = position;

        //check if the enemy is a robot and set its color
        if (data.type == EnemyType.Robot)
        {
            enemyObj.GetComponent<Robot>().SetColor(data.color);
        }
        enemyObj.GetComponent<Enemy>().RegisterEnemy();
        return enemyObj;
    }

    private void PlayBattleEvent(BattleEvent battleEventData)
    {
        currentBattleEvent = battleEventData;
        nextEventIndex++;

        //prevents the hero from escaping the battle event
        cameraFollows = false;
        cameraBounds.SetXPosition(battleEventData.column);

        //destroy remnants of prior battle events
        foreach (GameObject enemy in activeEnemies)
        {
            Destroy(enemy);
        }
        activeEnemies.Clear();
        Enemy.TotalEnemies = 0;

        //spawns enemies
        foreach (EnemyData enemyData in currentBattleEvent.enemies)
        {
            activeEnemies.Add(SpawnEnemy(enemyData));
        }
    }

    private void CompleteCurrentEvent()
    {
        currentBattleEvent = null;

        cameraFollows = true;
        cameraBounds.CalculateOffset(actor.transform.position.x); //calculates the offset at the end of every battle event
        hasRemainingEvents = currentLevelData.battleData.Count >
        nextEventIndex;

        enemyLifeBar.EnableLifeBar(false);

        if (!hasRemainingEvents)
        {
            StartCoroutine(HeroWalkout());
        } else
        {
            ShowGoIndicator();
        }
    }

    private IEnumerator LoadLevelData(LevelData data)
    {
        cameraFollows = false;
        currentLevelData = data;

        hasRemainingEvents = currentLevelData.battleData.Count > 0;
        activeEnemies = new List<GameObject>();

        yield return null; //pauses the method for one frame, allowing other scripts to run before executing the next line (here : CameraBounds script's Start())
        cameraBounds.SetXPosition(cameraBounds.minVisibleX);

        //prevent multiple instances of the map from being stacked atop one another
        if (currentLevelBackground != null)
        {
            Destroy(currentLevelBackground);
        }
        currentLevelBackground = Instantiate(currentLevelData.levelPrefab); //create a tile map

        cameraBounds.EnableBounds(false); //disable camera's edge colliders
        actor.transform.position = walkInStartTarget.transform.position; // move the hero to the walkInStartTarget position

        yield return new WaitForSeconds(0.1f); //pause the coroutine to give it time to initialize

        actor.UseAutopilot(true); //engage the hero's autopilot
        actor.AnimateTo(walkInTarget.transform.position, false, DidFinishIntro); //walk him toward the target position

        cameraFollows = true;

        ShowTextBanner(currentLevelData.levelName);

        loadingScreen.SetActive(false);
    }

    private void DidFinishIntro()
    {
        actor.UseAutopilot(false);
        actor.controllable = true;
        cameraBounds.EnableBounds(true);
        ShowGoIndicator();
    }

    private IEnumerator HeroWalkout()
    {
        cameraBounds.EnableBounds(false);
        cameraFollows = false;
        actor.UseAutopilot(true);
        actor.controllable = false;
        actor.AnimateTo(walkOutTarget.transform.position, true, DidFinishWalkout);
        yield return null;
    }

    private void DidFinishWalkout()
    {
        CurrentLevel++;
        if (CurrentLevel >= levels.Length)
        {
            Victory();
        }
        else
        {
            StartCoroutine(AnimateNextLevel());
        }

        cameraBounds.EnableBounds(true);
        cameraFollows = false;
        actor.UseAutopilot(false);
        actor.controllable = false;
    }

    private IEnumerator AnimateNextLevel()
    {
        ShowTextBanner(currentLevelData.levelName + " COMPLETED");
        yield return new WaitForSeconds(3.0f);
        SceneManager.LoadScene("Game");
    }

    //triggers the flicker by starting the FlickerGoIndicator coroutine
    private void ShowGoIndicator()
    {
        StartCoroutine(FlickerGoIndicator(4));
    }

    //animates the flicker by toggles the active state between on and off every 0.2s
    private IEnumerator FlickerGoIndicator(int count = 4)
    {
        while(count > 0)
        {
            goIndicator.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            goIndicator.SetActive(false);
            yield return new WaitForSeconds(0.2f);
            count--;
        }
    }

    private void ShowBanner(string bannerText, GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);
        obj.GetComponent<Text>().text = bannerText ;
        RectTransform rectTransform = obj.transform as RectTransform;
        rectTransform.SetParent(uiTransform);
        rectTransform.localScale = Vector3.one;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    public void GameOver()
    {
        ShowBanner("GAME OVER", gameOverPrefab);
    }

    public void Victory()
    {
        ShowBanner("YOU WON", gameOverPrefab);
    }

    public void ShowTextBanner(string levelName)
    {
        ShowBanner(levelName, levelNamePrefab);
    }
}