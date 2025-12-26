using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private List<GameStep> steps;

    [SerializeField] private int _currentStepIndex = 0;
    [SerializeField] private int _currentInteractionIndex = 0;

    private BaseInteraction _activeInteraction;

    private void Start()
    {
        _currentStepIndex = 0;
        _currentInteractionIndex = 0;

        PlayNextInteraction();
    }

    public void PlayNextInteraction()
    {
        if (_currentStepIndex >= steps.Count)
        {
            Debug.Log("All Game Steps Completed.");
            return;
        }

        GameStep currentStep = steps[_currentStepIndex];

        if (_currentInteractionIndex >= currentStep.interactions.Count)
        {
            _currentStepIndex++;

            PlayNextInteraction();
            return;
        }

        BaseInteraction nextInteraction = currentStep.interactions[_currentInteractionIndex];

        if (nextInteraction != null)
        {
            if (_activeInteraction != null)
                _activeInteraction.OnComplete -= HandleInteractionComplete;

            _activeInteraction = nextInteraction;

            _activeInteraction.OnComplete += HandleInteractionComplete;

            Debug.Log($"Playing Step {_currentStepIndex} | Interaction {_currentInteractionIndex}");
            _activeInteraction.Process();
        }
        else
        {
            Debug.LogWarning($"Found empty interaction slot at Step {_currentStepIndex}, Index {_currentInteractionIndex}. Skipping.");
            HandleInteractionComplete(null);
        }
    }

    private void HandleInteractionComplete(BaseInteraction interaction)
    {
        if (interaction != null)
            interaction.OnComplete -= HandleInteractionComplete;

        _currentInteractionIndex++;

        PlayNextInteraction();
    }
}