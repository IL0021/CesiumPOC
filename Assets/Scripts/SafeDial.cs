using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
public class SafeDial : XRBaseInteractable
{
    [SerializeField] private Transform m_AttachPoint;
    [SerializeField] private bool m_ClampedMotion = false;
    [Tooltip("When Clamped Motion is true, this value represents the rotation from 0 (0 degrees) to 1 (360 degrees).")]
    [SerializeField, Range(0f, 1f)] private float m_Value = 0f;

    private Quaternion _initialHandRotation;
    private Quaternion _initialLocalRotation; // CHANGE 1: Store Local rotation
    private float _currentAngle = 0f;


    public override Transform GetAttachTransform(IXRInteractor interactor)
    {
        return m_AttachPoint != null ? m_AttachPoint : base.GetAttachTransform(interactor);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        
        _initialHandRotation = args.interactorObject.transform.rotation;
        
        // CHANGE 2: Save the LOCAL rotation (relative to the safe parent)
        _initialLocalRotation = transform.localRotation;

        if (m_ClampedMotion)
        {
            _currentAngle = m_Value * 360f;
        }
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase phase)
    {
        base.ProcessInteractable(phase);

        if (isSelected)
        {
            if (phase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                if (m_ClampedMotion)
                {
                    Quaternion currentHandRotation = firstInteractorSelecting.GetAttachTransform(this).rotation;
                    Quaternion handRotationDifference = currentHandRotation * Quaternion.Inverse(_initialHandRotation);
                    float handTwist = handRotationDifference.eulerAngles.z;

                    _currentAngle = handTwist;
                    // Clamp the angle between 0 and 360
                    _currentAngle = Mathf.Clamp(_currentAngle, 0f, 360f);

                    m_Value = _currentAngle / 360f;
                    transform.localRotation = Quaternion.Euler(0, _currentAngle, 0);
                }
                else
                {
                    Quaternion currentHandRotation = firstInteractorSelecting.GetAttachTransform(this).rotation;
                    Quaternion handRotationDifference = currentHandRotation * Quaternion.Inverse(_initialHandRotation);

                    // CHANGE 3: The Magic Mapping 🎩
                    // Read the Hand's Z (Twist) ... and apply it to the Dial's Y (Spin)
                    float handTwist = handRotationDifference.eulerAngles.z;

                    // Create a rotation around the LOCAL Y axis
                    Quaternion targetRotation = Quaternion.Euler(0, handTwist, 0);

                    // Apply relative to the initial LOCAL state
                    transform.localRotation = targetRotation * _initialLocalRotation;
                }
            }
        }
    }
}