using System.Collections;
using CesiumForUnity;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class RaySpawner : MonoBehaviour
{
    public static RaySpawner Instance;
    [Header("References")]
    [SerializeField] private XRRayInteractor rayInteractor;
    [SerializeField] private GameObject objectToSpawn;

    [Header("Input")]
    [SerializeField] private InputActionProperty spawnInput;

    private void OnEnable() => spawnInput.action?.Enable();

    private XRInteractorLineVisual lineVisual;
    private LineRenderer lineRenderer;
    [SerializeField] private bool rayEnabled = true;

    public Transform CesiumGlobe;
    public GameObject MarkerPrefab;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        lineRenderer = rayInteractor.GetComponent<LineRenderer>();
        lineVisual = rayInteractor.GetComponent<XRInteractorLineVisual>();
        DisableRay();

        if (MarkerPrefab != null)
        {
            MarkerPrefab = Resources.Load<GameObject>("Prefabs/Marker");
        }
    }
    private void Update()
    {
        if (rayEnabled && spawnInput.action != null && spawnInput.action.WasPerformedThisFrame())
        {
            StartCoroutine(TrySpawnObject());
            DisableRay();
        }
    }

    private IEnumerator TrySpawnObject()
    {
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            GameObject markerObj = Instantiate(MarkerPrefab);
 
            Transform modelTransform = markerObj.transform.GetChildWithName("Model");
            Instantiate(objectToSpawn, modelTransform);
 
            markerObj.AddComponent<CesiumGlobeAnchor>();
 
            markerObj.transform.SetParent(CesiumGlobe, true);
 
            // This was moved from Marker.Start() to fix a race condition.
            // It ensures the marker has the correct scale *before* we calculate its bounds for placement.
            // markerObj.transform.localScale = Vector3.one * GlobeManager.Instance.currentMarkerScale;
 
            // Initialize the marker logic (adds to list, sets up UI)
            Marker marker = markerObj.GetComponent<Marker>();
            if (marker != null) marker.InitializeMarker();

            // 2. Add Collider and get the "Pivot-to-Bottom" offset
            float liftAmount = AddColliderAndGetLiftAmount(markerObj);

            // 3. Rotate to match surface normal (Upward orientation)
            // Use LookRotation(hit.normal) if you want it sticking out like a dart
            // Use FromToRotation(Vector3.up, hit.normal) if you want it standing like a chair
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            markerObj.transform.rotation = targetRotation;

            // 4. Move to hit point + lift it up along the normal
            markerObj.transform.position = hit.point + (hit.normal * liftAmount);
 
            yield return new WaitForSeconds(0.2f);
 
            modelTransform.gameObject.SetLayerRecursively(LayerMask.NameToLayer("BlueTeam"));
        }
    }
 
    // Helper to calculate bounds and add BoxCollider
    private float AddColliderAndGetLiftAmount(GameObject obj)
    {
        // Get all meshes in children
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0) return 0f;

        // Initialize bounds with the first renderer to avoid zero-size errors
        Bounds combinedBounds = renderers[0].bounds;

        // Encapsulate all other renderers
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        // Add the BoxCollider
        BoxCollider boxCol = obj.AddComponent<BoxCollider>();
 
        // Apply the calculated center and size.
        boxCol.center = combinedBounds.center;
        boxCol.size = combinedBounds.size;
 
        // Calculate how much we need to lift the object
        return -combinedBounds.min.y;
    }
 
    public void SetObjectToSpawn(GameObject obj)
    {
        objectToSpawn = obj;
    }
    public void EnableRay()
    {
        lineRenderer.enabled = true;
        lineVisual.enabled = true;
        rayEnabled = true;
    }
    public void DisableRay()
    {
        lineRenderer.enabled = false;
        lineVisual.enabled = false;
        rayEnabled = false;
    }
}