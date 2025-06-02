using UnityEngine;
using System.Collections.Generic;
public partial class GridStreamingInfinite : MonoBehaviour
{
    // References
    [SerializeField] private GameObject floor = null;
    [SerializeField] private Camera cam = null;

    // Grid configuration
    [SerializeField] private uint tile_scale = 1;
    [SerializeField] private uint grid_size = 10;
    [SerializeField] public Vector2Int start_coordinates;
    [SerializeField] private int radius = 5;
    [SerializeField] private int gridThreshold = 1;
    [SerializeField] private int shift_radius = 10000;

    // Visualization
    [SerializeField] private bool enableVisualization = true; // Toggle for visualization
    [SerializeField, Tooltip("Wire Grid Threshold (only visible when visualization is enabled)")]
    private int WireGridThreshold = 20;

    // Internal state
    private Dictionary<(int, int), GameObject> spawnDictionary = new Dictionary<(int, int), GameObject>();
    private Vector2Int CamActiveGridPos = new Vector2Int();
    private bool flag = false;
    private Vector2Int shift_coordinates = new Vector2Int(0, 0);
    private int RadiusInGridUnits = 0;
    private float cam_distance = 0f;
    private int loopShift = 0;
    void Start()
    {
        loopShift = System.Math.Abs(Mathf.CeilToInt(-grid_size / 2));
    }

    void Update()
    {
        cam_distance = new Vector2(cam.transform.position.x, cam.transform.position.z).sqrMagnitude;
        RadiusInGridUnits = Mathf.RoundToInt(radius / tile_scale);
        CamActiveGridPos = new Vector2Int(
            Mathf.RoundToInt((cam.transform.position.x - start_coordinates.x) / tile_scale),
            Mathf.RoundToInt((cam.transform.position.z - start_coordinates.y) / tile_scale));
        ChunkLoading();
        OriginShifting();
    }
    void OriginShifting()
    {
        if (cam_distance > System.Math.Pow(shift_radius, 2))
        {
            shift_coordinates = new Vector2Int(
                (CamActiveGridPos.x * (int)tile_scale) + start_coordinates.x,
                (CamActiveGridPos.y * (int)tile_scale) + start_coordinates.y
            );
            flag = true;
        }
    }
    void ChunkLoading()
    {
        for (int i = CamActiveGridPos.x - (RadiusInGridUnits + gridThreshold); i <= CamActiveGridPos.x + RadiusInGridUnits + gridThreshold; i++)
        {
            for (int j = CamActiveGridPos.y - (RadiusInGridUnits + gridThreshold); j <= CamActiveGridPos.y + RadiusInGridUnits + gridThreshold; j++)
            {
                if (i < grid_size - loopShift && j < grid_size - loopShift && i >= -loopShift && j >= -loopShift)
                {
                    float distance = Mathf.Pow(start_coordinates.x + i * tile_scale - cam.transform.position.x, 2) + Mathf.Pow(start_coordinates.y + j * tile_scale - cam.transform.position.z, 2);

                    if (!spawnDictionary.ContainsKey((i, j)) && distance <= System.Math.Pow(radius, 2))
                    {
                        spawnDictionary.Add((i, j), Instantiate(floor, new Vector3(start_coordinates.x + i * tile_scale, 0, start_coordinates.y + j * tile_scale), Quaternion.identity));
                        spawnDictionary[(i, j)].transform.localScale = new Vector3(tile_scale, 1, tile_scale);
                        int seed = i + j * 317;
                        seed ^= seed << 13;
                        seed ^= seed >> 17;
                        seed ^= seed << 5;
                        UnityEngine.Random.InitState(seed);
                        spawnDictionary[(i, j)].GetComponent<MeshRenderer>().material.color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                    }
                    if (spawnDictionary.ContainsKey((i, j)))
                    {
                        if (distance > System.Math.Pow(radius, 2))
                        {
                            Destroy(spawnDictionary[(i, j)]);
                            spawnDictionary.Remove((i, j));
                        }
                        else if (flag && distance <= System.Math.Pow(radius, 2))
                        {
                            spawnDictionary[(i, j)].transform.position -= new Vector3(shift_coordinates.x, 0, shift_coordinates.y);
                        }
                    }
                }
            }
        }
        if (flag)
        {
            // Adjust start coordinates after shifting
            start_coordinates -= shift_coordinates;
            // Reset camera position to the new origin
            cam.transform.position -= new Vector3(shift_coordinates.x, 0, shift_coordinates.y);
            // Reset flag to false after shifting
            flag = false;
        }
    }
}
