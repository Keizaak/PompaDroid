using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))] //requires an image that will flicker
public class ImageFlicker : MonoBehaviour {

    private bool isShown = true;
    public float flickerDelay = 0.3f; //holds the flicker speed
    private Image image;

    void Start()
    {
        image = GetComponent<Image>();
        //behaves like a coroutine, except that this method calls ToggleImage for every flickerDelay duration forever, or until the Invoke is cancelled
        InvokeRepeating("ToggleImage", flickerDelay, flickerDelay); 
    }

    void ToggleImage()
    {
        image.enabled = isShown;
        isShown = !isShown;
    }
}
