using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public SerializedDictionary<int, List<GameObject>> inventoryItems = new();

    public GameObject itemCardUI;


    void Start()
    {
        AddInventoryUIItems();
        
    }

    private void AddInventoryUIItems()
    {
        Transform panelHolder = transform.GetChildWithName("PanelHolder");
        int i = 1;

        foreach (var item in inventoryItems)
        {
            Transform panel = panelHolder.GetChildWithName("Panel" + i);
            Transform content = panel.GetChildWithName("Content");
            foreach (var obj in item.Value)
            {
                GameObject itemCard = Instantiate(itemCardUI, content);
                Button button = itemCard.GetComponentInChildren<Button>();
                TextMeshProUGUI title = itemCard.GetComponentInChildren<TextMeshProUGUI>();
                title.text = obj.name;
                button.onClick.AddListener(() => {
                    RaySpawner.Instance.SetObjectToSpawn(obj);
                    RaySpawner.Instance.EnableRay();
                });
            }
            i++;
        }
    }
}
