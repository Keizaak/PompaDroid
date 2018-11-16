using UnityEngine;

public class AudioManager : MonoBehaviour {

    //static => can asset it from any class with AudioManager.Instance
    public static AudioManager Instance; //points the only existing instance of AudioManager

    //checks if the static variable is assigned
    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); //save the AudioManager from destruction when the game scene changes
        } else
        {
            if(Instance != this)
            {
                Destroy(gameObject);
            }
        }
    }

}
