using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[System.Serializable]
public class Haptic
{
    [Range(0, 1)]
    public float intensity;
    public float duration;
    public void TriggerHaptic(BaseInteractionEventArgs eventArgs)
    {
        if (eventArgs.interactorObject is XRBaseControllerInteractor controllerInteractor)
        {
            TriggerHaptic(controllerInteractor.xrController);
        }
    }

    public void TriggerHaptic(XRBaseController controller)
    {
        if (intensity > 0)
        {
            controller.SendHapticImpulse(intensity, duration);
        }
    }
}

public class HapticIntractible : MonoBehaviour
{
    public Haptic HapticOnActivated;
    public Haptic HapticHoverEntered;
    public Haptic HapticHoverExited;
    public Haptic HapticSelectEntered;
    public Haptic HapticSelecExtited;

    void Start()
    {
        XRBaseInteractable interactable = GetComponent<XRBaseInteractable>();
        interactable.activated.AddListener(HapticOnActivated.TriggerHaptic);
        interactable.hoverEntered.AddListener(HapticHoverEntered.TriggerHaptic);
        interactable.hoverExited.AddListener(HapticHoverExited.TriggerHaptic);
        interactable.selectEntered.AddListener(HapticSelectEntered.TriggerHaptic);
        interactable.selectExited.AddListener(HapticSelecExtited.TriggerHaptic);


    }
}
