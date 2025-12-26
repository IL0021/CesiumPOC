using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextHelper : MonoBehaviour
{

    [SerializeField] private GameObject sourceObject, targetObject;
    void OnDrawGizmos()
    {
        if (sourceObject == null || targetObject == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(sourceObject.transform.position, targetObject.transform.position);
#if UNITY_EDITOR
        float distance = Vector3.Distance(sourceObject.transform.position, targetObject.transform.position);
        Vector3 midPoint = Vector3.Lerp(sourceObject.transform.position, targetObject.transform.position, 0.5f);
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.green;
        UnityEditor.Handles.Label(midPoint, $"Distance: {distance:F2}", style);
#endif
    }



    //     public List<Transform> childTransforms = new List<Transform>();

    //     [ContextMenu("Fix Transforms")]
    //     public void GetAllChildTransforms()
    //     {
    //         int i = 0;
    //         foreach (Transform child in childTransforms)
    //         {
    //             child.transform.position = new Vector3(1, 1, i * 0.5f);
    // #if UNITY_EDITOR
    //             UnityEditor.EditorUtility.SetDirty(child);
    // #endif
    //             i++;
    //         }
    //     }
}
