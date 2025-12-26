using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.UI;

public class ToggleManager : MonoBehaviour
{
    public SerializedDictionary<Toggle, GameObject> canvasDict = new SerializedDictionary<Toggle, GameObject>();

    private void Start()
    {
        foreach (var pair in canvasDict)
        {
            pair.Key.onValueChanged.AddListener(delegate { ToggleValueChanged(pair.Key); });
            pair.Value.SetActive(pair.Key.isOn);
        }
    }

    private void ToggleValueChanged(Toggle sender)
    {
        if (canvasDict.ContainsKey(sender))
        {
            canvasDict[sender].SetActive(sender.isOn);
        }
    }
}
