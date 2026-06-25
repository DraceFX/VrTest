using UnityEngine;
using UnityEngine.XR;

public class ToggleObjectByXRButton : MonoBehaviour
{
    [SerializeField] GameObject targetObject;

    InputDevice leftController;
    InputDevice rightController;

    bool lastState = false;

    void Start()
    {
        leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    void Update()
    {
        bool leftPressed = false;
        bool rightPressed = false;

        if (leftController.isValid)
            leftController.TryGetFeatureValue(CommonUsages.primaryButton, out leftPressed);

        if (rightController.isValid)
            rightController.TryGetFeatureValue(CommonUsages.primaryButton, out rightPressed);

        bool currentState = leftPressed || rightPressed;

        // реагируем только на нажатие (а не удержание)
        if (currentState && !lastState)
        {
            ToggleObject();
        }

        lastState = currentState;
    }

    void ToggleObject()
    {
        if (targetObject == null)
            return;

        targetObject.SetActive(!targetObject.activeSelf);
    }
}