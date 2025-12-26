using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

static class Extensions
{
    /// <summary>
    /// Returns child name with provided name.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Transform GetChildWithName(this Transform parent, string name)
    {
        Transform target = null;
        if (parent.name == name) return target;
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform result = GetChildWithName(child, name);
            if (result != null) return result;
        }
        return target;
    }

    public static Transform GetChildWithNameBFS(this Transform parent, string name)
    {
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(parent);
        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();
            if (current.name == name) return current;

            foreach (Transform child in current)
            {
                queue.Enqueue(child);
            }
        }
        return null;
    }
    public static Color FromHexCode(this Renderer renderer, string hexCode)
    {
        if (hexCode.Length < 6)
        {
            Debug.LogError($"Current hexCode: {hexCode}");
            throw new System.FormatException("Wrong Hex Code");
        }
        var r = hexCode.Substring(0, 2);
        var g = hexCode.Substring(2, 2);
        var b = hexCode.Substring(4, 2);
        string alpha;
        if (hexCode.Length >= 8)
        {
            alpha = hexCode.Substring(6, 2);
        }
        else alpha = "FF";

        return new Color(
            int.Parse(r, NumberStyles.HexNumber) / 255f,
            int.Parse(g, NumberStyles.HexNumber) / 255f,
            int.Parse(b, NumberStyles.HexNumber) / 255f,
            int.Parse(alpha, NumberStyles.HexNumber) / 255f);
    }
    public static Quaternion FromTransform(this Quaternion quaternion, Transform transform)
    {
        Vector3 euler = transform.rotation.eulerAngles;
        return Quaternion.Euler(euler);
    }
    public static void Print<T>(this T value, string message = "")
    {
        Debug.Log(message + value);
    }

    /// <summary>
    /// Prints all elements of the list to the Unity console.
    /// </summary>
    /// <param name="list">The list to print.</param>
    /// <param name="message">An optional message to print before each element.</param>
    public static void Print<T>(this List<T> list, string message = "")
    {
        for (int i = 0; i < list.Count; i++)
        {
            Debug.Log(message + list[i]);
        }
    }
    public static void Print<T>(this HashSet<T> list, string message = "")
    {
        for (int i = 0; i < list.Count; i++)
        {
            Debug.Log(message + list.ElementAt(i));
        }
    }


    public static GameObject CalculateCenterPoint(List<MeshRenderer> meshRenderers)
    {
        GameObject centrePoint = new GameObject("Center Point");
        Bounds combinedBounds = new Bounds();
        foreach (var mesh in meshRenderers)
        {
            if (mesh != null) combinedBounds.Encapsulate(mesh.bounds);
        }

        centrePoint.transform.position = combinedBounds.center;
        return centrePoint;
    }
}
