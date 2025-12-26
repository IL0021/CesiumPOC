using UnityEngine;
using CesiumForUnity;
using System.Threading.Tasks;
using TMPro;
using System;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Mathematics; // Required for double3
using System.Globalization; // Added for robust number parsing
using UnityEngine.Splines; // REQUIRED: To measure the polygon size

public class GlobeManager : MonoBehaviour
{

    public static GlobeManager Instance;
    
    [Header("Cesium Components")]
    public CesiumGeoreference cesiumGeoreference;
    public Cesium3DTileset[] cesium3DTilesets;

    [Header("Tabletop Components")]
    [Tooltip("Drag your CartographicPolygon object here. The script will keep it pinned to the table.")]
    public CesiumGlobeAnchor boundaryPolygonAnchor; 

    [Header("UI Elements")]
    public GameObject mainCanvas;
    
    // CHANGED: Made these public so you can assign them in Inspector (Recommended)
    // If left empty, the script will try to find them by name.
    public TMP_InputField scaleInputField;
    public TMP_InputField latitudeInputField;
    public TMP_InputField longitudeInputField;
    
    public Button cycleLocationBtn, recreateTilesetBtn;
    public Toggle buildingsToggle;
    public GameObject buildingsParent;

    [Header("Input")]
    public InputActionProperty XButtonPress;

    [Header("Tabletop Settings")]
    [Tooltip("Layer mask for the map terrain so raycast only hits the earth")]
    public LayerMask mapLayerMask = ~0; // Default to Everything
    [Tooltip("How high above origin to start looking for ground")]
    public float raycastHeight = 5000f;
    
    [Tooltip("The effective width of your tabletop in Unity Units. (Polygon Base Size * Polygon Scale).")]
    public double tabletopWidthUnity = 20.0; // Defaulted to 20 based on your screenshots (-10 to 10)

    // Internal state
    private double _scale;
    private int currentIndex = 0;
    private Vector3 _initialPolygonScale = Vector3.one; 

    // List of locations
    public List<Vector2> locations = new List<Vector2>();

    [Header("Marker Settings")]
    public float currentMarkerScale = 1.0f;
    public List<Marker> spawnedMarkers = new List<Marker>();

    private async void Awake()
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
        // Auto-get components if not assigned
        if(cesiumGeoreference == null) cesiumGeoreference = GetComponent<CesiumGeoreference>();
        if(cesium3DTilesets == null || cesium3DTilesets.Length == 0) cesium3DTilesets = GetComponentsInChildren<Cesium3DTileset>();
        
        // Capture the user's intended scale before we do anything
        if (boundaryPolygonAnchor != null)
        {
            _initialPolygonScale = boundaryPolygonAnchor.transform.localScale;
            
            // Fix: Auto-calculate the width from the Spline to ensure math is perfect
            CalculateTabletopWidth();
        }

        cesiumGeoreference.Initialize();

        // Initial setup
        await Task.Delay(1000);
        
        // Ensure we capture the scale correctly on start
        _scale = cesiumGeoreference.scale;

        // Set initial location
        // cesiumGeoreference.latitude = 28.409f;
        // cesiumGeoreference.longitude = 77.072f;
        // cesiumGeoreference.height = 0; // Reset height initially
        
        // Force polygon to start at this location
        SyncPolygonAnchor();

        await Task.Delay(1000);

        // 1. Assign Listeners FIRST to ensure UI is hooked up
        AssignListeners();
        
        RecreateTileset();

        if (cycleLocationBtn != null) cycleLocationBtn.onClick.AddListener(CycleTileset);
        if (recreateTilesetBtn != null) recreateTilesetBtn.onClick.AddListener(RecreateTileset);
    }

    private void OnEnable()
    {
        if (XButtonPress != null && XButtonPress.action != null)
        {
            XButtonPress.action.Enable();
            XButtonPress.action.performed += OnXButtonPressed;
        }
    }

    private void OnDisable()
    {
        if (XButtonPress != null && XButtonPress.action != null)
        {
            XButtonPress.action.performed -= OnXButtonPressed;
        }
    }
    
    // ---------------------------------------------------------
    // AUTO CALCULATION
    // ---------------------------------------------------------

    [ContextMenu("Recalculate Width")]
    private void CalculateTabletopWidth()
    {
        if (boundaryPolygonAnchor != null)
        {
            // 1. Try SPLINE (Most Accurate for CesiumCartographicPolygon)
            SplineContainer spline = boundaryPolygonAnchor.GetComponentInChildren<SplineContainer>();
            if (spline != null && spline.Spline != null)
            {
                // Calculate bounds from knots
                float minX = float.MaxValue, maxX = float.MinValue;
                float minZ = float.MaxValue, maxZ = float.MinValue;

                foreach (var knot in spline.Spline.Knots)
                {
                    float3 pos = knot.Position;
                    if (pos.x < minX) minX = pos.x;
                    if (pos.x > maxX) maxX = pos.x;
                    if (pos.z < minZ) minZ = pos.z;
                    if (pos.z > maxZ) maxZ = pos.z;
                }

                float widthX = maxX - minX;
                float widthZ = maxZ - minZ;
                float baseSize = Mathf.Max(widthX, widthZ);

                // Multiply by transform scale
                float transformScale = Mathf.Max(boundaryPolygonAnchor.transform.lossyScale.x, boundaryPolygonAnchor.transform.lossyScale.z);
                
                tabletopWidthUnity = baseSize * transformScale;
                Debug.Log($"[GlobeManager] Auto-detected Width from Spline: {tabletopWidthUnity} (Base: {baseSize} * Scale: {transformScale})");
                return;
            }

            // 2. Fallback to MeshFilter
            MeshFilter mf = boundaryPolygonAnchor.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                float meshSize = Mathf.Max(mf.sharedMesh.bounds.size.x, mf.sharedMesh.bounds.size.z);
                float transformScale = Mathf.Max(boundaryPolygonAnchor.transform.lossyScale.x, boundaryPolygonAnchor.transform.lossyScale.z);
                
                double calculatedWidth = meshSize * transformScale;
                
                if (calculatedWidth > 0.1)
                {
                    tabletopWidthUnity = calculatedWidth;
                    Debug.Log($"[GlobeManager] Auto-detected Width from Mesh: {tabletopWidthUnity}");
                }
            }
        }
    }

    // ---------------------------------------------------------
    // CORE SYNC LOGIC
    // ---------------------------------------------------------
    
    public void SyncPolygonAnchor()
    {
        if (boundaryPolygonAnchor != null)
        {
            boundaryPolygonAnchor.longitudeLatitudeHeight = new double3(
                cesiumGeoreference.longitude,
                cesiumGeoreference.latitude,
                cesiumGeoreference.height
            );

            if (boundaryPolygonAnchor.transform.parent == cesiumGeoreference.transform)
            {
                 boundaryPolygonAnchor.transform.localPosition = Vector3.zero;
                 boundaryPolygonAnchor.transform.localScale = _initialPolygonScale;
            }
        }
    }

    // ---------------------------------------------------------
    // LOCATION LOGIC
    // ---------------------------------------------------------

    public void CycleTileset()
    {
        if (locations.Count == 0) return;

        currentIndex = (currentIndex + 1) % locations.Count;
        
        cesiumGeoreference.latitude = locations[currentIndex].x;
        cesiumGeoreference.longitude = locations[currentIndex].y;
        SyncPolygonAnchor();
        
        if(latitudeInputField != null) latitudeInputField.text = locations[currentIndex].x.ToString();
        if(longitudeInputField != null) longitudeInputField.text = locations[currentIndex].y.ToString();

        RecreateTileset();
    }

    [ContextMenu("Recreate Tileset")]
    public void RecreateTileset()
    {
        Debug.Log("Recreating Tileset & Snapping to Ground...");
        
        foreach (var tileSet in cesium3DTilesets)
        {
            if(tileSet != null) tileSet.RecreateTileset();
        }

        StopAllCoroutines();
        StartCoroutine(SnapMapToTableHeight());
    }

    // ---------------------------------------------------------
    // HEIGHT CORRECTION
    // ---------------------------------------------------------

    private IEnumerator SnapMapToTableHeight()
    {
        yield return new WaitForSeconds(0.5f);

        bool groundFound = false;
        int attempts = 0;
        int maxAttempts = 50; 

        Vector3 targetPosition = transform.position; 

        while (!groundFound && attempts < maxAttempts)
        {
            Vector3 rayOrigin = targetPosition + (Vector3.up * raycastHeight);
            
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastHeight * 2f, mapLayerMask))
            {
                groundFound = true;

                float heightDifference = hit.point.y - targetPosition.y;
                double realWorldHeightAdjustment = heightDifference / cesiumGeoreference.scale;

                cesiumGeoreference.height += realWorldHeightAdjustment;
                
                SyncPolygonAnchor();

                Debug.Log($"Ground found! Unity Diff: {heightDifference}, RealWorld Adj: {realWorldHeightAdjustment}m.");
            }
            else
            {
                attempts++;
                yield return new WaitForSeconds(0.1f);
            }
        }

        if (!groundFound)
        {
            SyncPolygonAnchor();
            Debug.LogWarning("Could not find ground to snap to.");
        }
    }

    // ---------------------------------------------------------
    // UI & HELPERS
    // ---------------------------------------------------------

    private void AssignListeners()
    {
        if (mainCanvas == null)
        {
            Debug.LogError("Main Canvas is not assigned in GlobeManager!");
            return;
        }

        if (scaleInputField == null) scaleInputField = FindDeepChild(mainCanvas.transform, "ScaleField")?.GetComponent<TMP_InputField>();
        if (latitudeInputField == null) latitudeInputField = FindDeepChild(mainCanvas.transform, "LatitudeField")?.GetComponent<TMP_InputField>();
        if (longitudeInputField == null) longitudeInputField = FindDeepChild(mainCanvas.transform, "LongitudeField")?.GetComponent<TMP_InputField>();
        if (buildingsToggle == null) buildingsToggle = FindDeepChild(mainCanvas.transform, "BuildingsToggle")?.GetComponent<Toggle>();

        // --- SCALE (KILOMETERS) ---
        if (scaleInputField != null)
        {
            scaleInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            scaleInputField.keyboardType = TouchScreenKeyboardType.DecimalPad;
            
            // CONVERSION: UnityWidth / Scale / 1000
            double initialKm = (tabletopWidthUnity / _scale) / 1000.0;
            scaleInputField.text = initialKm.ToString("0.##", CultureInfo.InvariantCulture);

            scaleInputField.onValueChanged.AddListener(val => {
                // Debug.Log($"[GlobeManager] View Width Input Changed: '{val}' KM");
                if(double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double kmInput) && kmInput > 0.001) 
                {
                    // CONVERSION: Scale = UnityWidth / (KM * 1000)
                    double targetMeters = kmInput * 1000.0;
                    scale = tabletopWidthUnity / targetMeters;
                }
            });
        }
        else Debug.LogError("Could not find 'ScaleField' InputField.");

        // --- LATITUDE ---
        if (latitudeInputField != null)
        {
            latitudeInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            latitudeInputField.keyboardType = TouchScreenKeyboardType.DecimalPad;

            latitudeInputField.text = cesiumGeoreference.latitude.ToString();
            latitudeInputField.onValueChanged.AddListener(val => {
                if (double.TryParse(val, out double lat))
                {
                    cesiumGeoreference.latitude = lat;
                    SyncPolygonAnchor(); 
                }
            });
        }

        // --- LONGITUDE ---
        if (longitudeInputField != null)
        {
            longitudeInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            longitudeInputField.keyboardType = TouchScreenKeyboardType.DecimalPad;

            longitudeInputField.text = cesiumGeoreference.longitude.ToString();
            longitudeInputField.onValueChanged.AddListener(val => {
                if (double.TryParse(val, out double lon))
                {
                    cesiumGeoreference.longitude = lon;
                    SyncPolygonAnchor(); 
                }
            });
        }

        if(buildingsToggle != null && buildingsParent != null)
        {
            buildingsToggle.onValueChanged.AddListener(val => buildingsParent.SetActive(val));
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public double scale
    {
        get { return _scale; }
        set
        {
            if (_scale != value)
            {
                _scale = value;
                OnScaleChanged();
            }
        }
    }
    [SerializeField] private Transform GLOBAL_SCALE;
    void OnScaleChanged()
    {
        cesiumGeoreference.scale = _scale;
        SyncPolygonAnchor();
        
        if (scaleInputField != null && !scaleInputField.isFocused)
        {
             double currentKm = (tabletopWidthUnity / _scale) / 1000.0;
             scaleInputField.text = currentKm.ToString("0.##", CultureInfo.InvariantCulture);
        }
        
        // UpdateAllMarkerScales(); // Reverted

        RecreateTileset();

        currentMarkerScale = GLOBAL_SCALE.localScale.x;

        SetMarkerScale(currentMarkerScale);

        //todo: check existing marker scale and set to currentMarkerScale
    }

    private void OnXButtonPressed(InputAction.CallbackContext context)
    {
        if(mainCanvas != null)
            mainCanvas.SetActive(!mainCanvas.activeSelf);
    }

    public void SetMarkerScale(float newScale)
    {
        currentMarkerScale = Mathf.Max(0.1f, newScale); // Prevent scale from being zero or negative
        
        foreach (Marker marker in spawnedMarkers)
        {
            if (marker != null)
            {
                marker.transform.localScale = Vector3.one * currentMarkerScale;
                marker.UpdateScaleText(currentMarkerScale);
            }
        }
    }
}