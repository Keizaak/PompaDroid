using UnityEngine;
using UnityEngine.UI;

public class LifeBar : MonoBehaviour {

    public Image fillImage; //progress bar
    public Image thumbnailImage; //hero's thumbnail

    public Sprite[] fillSprites; //array of sprites to color the progress bar (green, yellow, red)

    //sets the LifeBar to the full value of 1.0
    void Start()
    {
        SetProgress(1.0f);    
    }

    private Sprite SpriteForProgress(float progress)
    {
        if(progress >= 0.5f)
        {
            return fillSprites[0];
        }
        if(progress >= 0.25f)
        {
            return fillSprites[1];
        }
        return fillSprites[2];
    }

    public void SetThumbnail(Sprite image, Color color)
    {
        thumbnailImage.sprite = image;
        thumbnailImage.color = color;
    }

    //updates the fillImage progress bar fill
    public void SetProgress(float progress)
    {
        fillImage.fillAmount = progress;
        fillImage.sprite = SpriteForProgress(progress);
    }

    //toggle the life ar and its children on or off
    public void EnableLifeBar(bool enabled)
    {
        foreach(Transform tr in transform)
        {
            tr.gameObject.SetActive(enabled);
        }
    }
}
