using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

public class Marker : MonoBehaviour
{
    [SerializeField] private Transform canvasTransform;

    [Header("Locking")]
    public Button lockButton;
    [SerializeField] private bool isLocked = true;

    [SerializeField] private Sprite lockIcon, unlockIcon;
    [SerializeField] private Color lockedColor, unlockedColor;


    [Header("Team")]
    public TeamType teamType = TeamType.Blue;
    public Button teamButton;
    [SerializeField] private Sprite redTeamSprite, blueTeamSprite;
    [SerializeField] private Color redTeamColor, blueTeamColor;


    [Header("Scale")]
    public Button scaleButton;
    public TextMeshProUGUI scaleText;
    public Button increseScaleButton, decreaseScaleButton;
    public GameObject scalePanel;

    private bool _isInitialized = false;


    [Header("Move")]
    public Button moveButton;

    IEnumerator Start()
    {
        AssignListeners();
        // If RaySpawner initialized us, this flag will be true, so we skip double-init
        if (!_isInitialized) InitializeMarker();
        yield return null;
        AlignCanvas();
    }

    public void InitializeMarker()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        // add to markers
        GlobeManager.Instance.spawnedMarkers.Add(this);
        // set initial scale
        UpdateScaleText(GlobeManager.Instance.currentMarkerScale);
        transform.localScale = Vector3.one * GlobeManager.Instance.currentMarkerScale;
    }

    private void AlignCanvas()
    {
        //todo: based on the ModelInfo bounds, position the canvas above the model
        ModelInfo modelInfo = transform.GetChildWithName("Model").GetComponent<ModelInfo>();

        if (modelInfo != null)
        {
            modelInfo.CalculateBounds();
            "Aligning canvas position based on model bounds".Print();
            canvasTransform.localPosition = new Vector3(0, modelInfo.modelBounds.extents.y + 0.5f, 0);
        }
    }

    private void AssignListeners()
    {
        lockButton.onClick.AddListener(() =>
        {
            ToggleLock();
        });
        teamButton.onClick.AddListener(() =>
        {
            ToggleTeam();
        });
        scaleButton.onClick.AddListener(() =>
        {
            ToggleScalePanel();
        });
        increseScaleButton.onClick.AddListener(() =>
        {
            IncreaseScale();
        });
        decreaseScaleButton.onClick.AddListener(() =>
        {
            DecreaseScale();
        });
    }



    private void ToggleTeam()
    {
        teamType = teamType == TeamType.Red ? TeamType.Blue : TeamType.Red;
        var icon = teamButton.transform.GetChildWithName("Icon").GetComponent<Image>();
        icon.sprite = teamType == TeamType.Red ? redTeamSprite : blueTeamSprite;
        var bg = teamButton.GetComponent<Image>();
        bg.color = teamType == TeamType.Red ? redTeamColor : blueTeamColor;
        transform.GetChildWithName("Model").gameObject.SetLayerRecursively(teamType == TeamType.Red ? LayerMask.NameToLayer("RedTeam") : LayerMask.NameToLayer("BlueTeam"));
    }

    private void ToggleLock()
    {
        isLocked = !isLocked;
        var icon = lockButton.transform.GetChildWithName("Icon").GetComponent<Image>();
        icon.sprite = isLocked ? lockIcon : unlockIcon;
        var bg = lockButton.GetComponent<Image>();
        bg.color = isLocked ? lockedColor : unlockedColor;

        //todo : if unlocked, show other buttons
        teamButton.gameObject.SetActive(!isLocked);
        scaleButton.gameObject.SetActive(!isLocked);
        if (isLocked == true && scalePanel.activeSelf)
        {
            scalePanel.SetActive(false);
        }


    }


    private void ToggleScalePanel()
    {
        scalePanel.SetActive(!scalePanel.activeSelf);
    }

    public void UpdateScaleText(float worldScale)
    {
        if (scaleText != null)
        {
            scaleText.text = worldScale.ToString("0.0");
        }
    }

    private void IncreaseScale()
    {
        float newScale = GlobeManager.Instance.currentMarkerScale + 0.1f;
        GlobeManager.Instance.SetMarkerScale(newScale);
    }
    private void DecreaseScale()
    {
        float newScale = GlobeManager.Instance.currentMarkerScale - 0.1f;
        GlobeManager.Instance.SetMarkerScale(newScale);
    }

    private void MoveMarker()
    {
        // fade object
        // show ray
        // on click should move objec
    }

}


public enum TeamType
{
    Red,
    Blue,
}