using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class HapticVelocity : MonoBehaviour
{
    [Range(0, 1)]
    public float intensity;
    public float speedRequirement;
    public GameObject tip;

    public XRBaseController leftController;
    public XRBaseController rightController;

    private Vector3 pos;

    private bool isHeld;
    private bool rightOrLeft;

    // Start is called before the first frame update
    void Start()
    {
        pos = tip.GetComponent<Transform>().position;

        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnEnterHand);
        grabInteractable.selectExited.AddListener(OnExitHand);
    }

    // Update is called once per frame
    void Update()
    {
        float speed = Speedometer(pos, tip.transform.position);
        pos = tip.transform.position;
        if (isHeld)
        {
            if (rightOrLeft)
            {
                TriggerHaptic(leftController, speed);
            }
            else
            {
                TriggerHaptic(rightController, speed);
            }
        }
    }

    public void OnEnterHand(BaseInteractionEventArgs args)
    {
        if (args.interactorObject is XRDirectInteractor)
        {
            if (args.interactorObject.transform.CompareTag("Left Hand"))
            {
                isHeld = true;
                rightOrLeft = true;
            }
            else if (args.interactorObject.transform.CompareTag("Right Hand"))
            {
                isHeld = true;
                rightOrLeft = false;
            }
        }
    }

    public void OnExitHand(BaseInteractionEventArgs args)
    {
        if (args.interactorObject is XRDirectInteractor)
        {
            isHeld = false;
        }
    }

    public void TriggerHaptic(XRBaseController controller, float speed)
    {
        if (speed > speedRequirement)
        {
            controller.SendHapticImpulse(intensity, Time.deltaTime);
        }
    }

    public float Speedometer(Vector3 previousPosition, Vector3 position) 
    {
        float speed = Mathf.Sqrt(Mathf.Pow((previousPosition.x - position.x), 2)+ Mathf.Pow((previousPosition.y - position.y), 2)+ Mathf.Pow((previousPosition.z - position.z), 2));
        return speed;
    }
}
