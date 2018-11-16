using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadMainMenu : MonoBehaviour {

	public void GameOverDone()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
