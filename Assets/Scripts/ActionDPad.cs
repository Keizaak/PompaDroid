using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActionDPad : UIBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler {

	public enum ActionPadDirection
    {
        Up = 1,
        UpRight = 2,
        Right = 3,
        DownRight,
        Down,
        DownLeft,
        Left,
        UpLeft,
        None = 999
    };

    [SerializeField]
    float radius = 1; //determines the max radius for touches to register

    [HideInInspector]
    bool isHeld; //sends up a flag when a touch happens

    [SerializeField]
    Sprite[] directionalSprites; //holds the d-pad sprites for the button presses

    //processes d-pad movemenet for the UI's EventSystem
    [Serializable]
    public class JoystickMoveEvent : UnityEvent<ActionPadDirection> { }
    public JoystickMoveEvent OnValueChange;

    private ActionPadDirection UpdateTouchSprite(Vector2 direction)
    {
        //calculates the angle of the direction vector and converts it to degrees using the Mathf.Atan
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        //Normalizes the angle to a value between (0;360)
        if(angle < 0)
        {
            angle += 360;
        }

        ActionPadDirection currentPadDirection = ActionPadDirection.None;
        if(angle <=22.5f || angle > 337.5f)
        {
            currentPadDirection = ActionPadDirection.Up;
        }
        else if (angle > 22.5 && angle <= 67.5)
        {
            currentPadDirection = ActionPadDirection.UpRight;
        }
        else if (angle > 67.5 && angle <= 112.5)
        {
            currentPadDirection = ActionPadDirection.Right;
        }
        else if (angle > 112.5 && angle <= 157.5)
        {
            currentPadDirection = ActionPadDirection.DownRight;
        }
        else if (angle > 157.5 && angle <= 202.5)
        {
            currentPadDirection = ActionPadDirection.Down;
        }
        else if (angle > 202.5 && angle <= 247.5)
        {
            currentPadDirection = ActionPadDirection.DownLeft;
        }
        else if (angle > 247.5 && angle <= 292.5)
        {
            currentPadDirection = ActionPadDirection.Left;
        }
        else if (angle > 292.5 && angle <= 337.5)
        {
            currentPadDirection = ActionPadDirection.UpLeft;
        }

        //Updates the Image using the directionalSprites array
        int index = 0;
        if (currentPadDirection != ActionPadDirection.None)
        {
            index = (int)currentPadDirection;
        }
        GetComponent<Image>().sprite = directionalSprites[index];
        return currentPadDirection;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsActive())
        {
            return;
        }

        //converts the touch position to a local Vector2
        RectTransform thisRect = transform as RectTransform;
        Vector2 touchDir;
        bool didConvert = RectTransformUtility.ScreenPointToLocalPointInRectangle(thisRect, eventData.position, eventData.enterEventCamera, out touchDir);

        //if the magnitude of the calculated vector < radius variable
        //then vector reset and between (0;1)
        //then updates the d-pad sprite and invokes the OnValueChange event
        if(touchDir.sqrMagnitude > radius * radius)
        {
            touchDir.Normalize();
            isHeld = true;
            ActionPadDirection currentDirection = UpdateTouchSprite(touchDir);
            OnValueChange.Invoke(currentDirection);
        }
    }

    //handles updates when the drag action is complete
    public void OnEndDrag(PointerEventData eventData)
    {
        OnValueChange.Invoke(ActionPadDirection.None);
        GetComponent<Image>().sprite = directionalSprites[0];
    }

    //handles drag actions that go accross the screen
    public void OnDrag(PointerEventData eventData)
    {
        //isHeld = true => processes the touch like in OnBeginDrag()
        if (isHeld)
        {
            RectTransform thisRect = transform as RectTransform;
            Vector2 touchDir;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(thisRect, eventData.position, eventData.enterEventCamera, out touchDir);
            touchDir.Normalize();

            ActionPadDirection currentDirection = UpdateTouchSprite(touchDir);
            OnValueChange.Invoke(currentDirection);
        }
    }

    //converts the touch position to the local position on the ActionDPad RectTransform
    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransform thisRect = transform as RectTransform;
        Vector2 touchDir;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(thisRect, eventData.position, eventData.enterEventCamera, out touchDir);
        touchDir.Normalize();

        //updates the d-pad sprite
        ActionPadDirection currentDirection = UpdateTouchSprite(touchDir);
        OnValueChange.Invoke(currentDirection);
    }

    //does the same thing as OnEndDrag (returns the d-pad to its neutral state)
    public void OnPointerUp(PointerEventData eventData)
    {
        OnValueChange.Invoke(ActionPadDirection.None);
        GetComponent<Image>().sprite = directionalSprites[0];
    }
}
