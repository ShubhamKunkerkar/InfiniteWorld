using UnityEngine;
using System.Collections.Generic;


/// Custom 64-bit vector for handling massive world coordinates

[System.Serializable]
public struct Vector2Long
{
    public long x, y;

    public Vector2Long(long x, long y)
    {
        this.x = x;
        this.y = y;
    }

    public static Vector2Long operator +(Vector2Long a, Vector2Long b)
    {
        return new Vector2Long(a.x + b.x, a.y + b.y);
    }

    public static Vector2Long operator -(Vector2Long a, Vector2Long b)
    {
        return new Vector2Long(a.x - b.x, a.y - b.y);
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, 0, y);
    }
}


/// Infinite world streaming system that loads and unloads chunks around the camera

public partial class GridStreamingInfinite : MonoBehaviour
{
    [Header("World Objects")]
    [SerializeField] private GameObject floor = null;
    [SerializeField] private Camera cam = null;

    [Header("Grid Configuration")]
    [SerializeField] private long grid_size = 10;
    [SerializeField] private int tile_scale = 1;
    [SerializeField] public Vector2Long start_coordinates;
    [SerializeField] private int radius = 5;
    [SerializeField] private int gridThreshold = 1;
    [SerializeField] private long shift_radius = 10000;

    [Header("Visualization")]
    [SerializeField] private bool enableVisualization = true;
    [SerializeField, Tooltip("Wire Grid Threshold (only visible when visualization is enabled)")]
    private int WireGridThreshold = 20;

    // World state tracking
    private Dictionary<(long, long), GameObject> spawnDictionary = new Dictionary<(long, long), GameObject>();
    private Vector2Long CamActiveGridPos = new Vector2Long();
    private int RadiusInGridUnits = 0;
    private double cam_distance = 0.0;
    private long loopShift = 0;
    private Vector2Long shift_coordinates = new Vector2Long(0, 0);
    private bool flag = false;

    void Start()
    {
        // Calculate grid center offset for boundary checking
        loopShift = (long)System.Math.Abs(System.Math.Ceiling((double)grid_size / 2));
    }

    void Update()
    {
        // Track camera distance from world origin
        cam_distance = new Vector2(cam.transform.position.x, cam.transform.position.z).sqrMagnitude;
        RadiusInGridUnits = Mathf.RoundToInt(radius / tile_scale);

        // Convert camera world position to grid coordinates
        CamActiveGridPos = new Vector2Long(
            (long)System.Math.Round((double)(cam.transform.position.x - start_coordinates.x) / tile_scale),
            (long)System.Math.Round((double)(cam.transform.position.z - start_coordinates.y) / tile_scale)
        );

        ChunkLoading();
        OriginShifting();
    }


    /// Handles origin shifting when camera gets too far from world origin

    void OriginShifting()
    {
        // Check if camera is far enough to trigger origin shift
        if (cam_distance > System.Math.Pow(shift_radius, 2))
        {
            // Calculate new shift coordinates based on camera position
            shift_coordinates = new Vector2Long(
                (CamActiveGridPos.x * tile_scale) + start_coordinates.x,
                (CamActiveGridPos.y * tile_scale) + start_coordinates.y
            );
            flag = true;
        }
    }


    /// Main chunk loading logic - spawns and destroys tiles based on camera position

    void ChunkLoading()
    {
        // Loop through all grid positions within loading radius
        for (long i = CamActiveGridPos.x - (RadiusInGridUnits + gridThreshold);
             i <= CamActiveGridPos.x + RadiusInGridUnits + gridThreshold; i++)
        {
            for (long j = CamActiveGridPos.y - (RadiusInGridUnits + gridThreshold);
                 j <= CamActiveGridPos.y + RadiusInGridUnits + gridThreshold; j++)
            {
                // Check if position is within world bounds
                if (i < grid_size - loopShift && j < grid_size - loopShift &&
                    i >= -loopShift && j >= -loopShift)
                {
                    // Calculate squared distance from camera to this grid position
                    double distance = System.Math.Pow(start_coordinates.x + i * tile_scale - cam.transform.position.x, 2) +
                                     System.Math.Pow(start_coordinates.y + j * tile_scale - cam.transform.position.z, 2);

                    // Spawn new tile if within radius and not already spawned
                    if (!spawnDictionary.ContainsKey((i, j)) && distance <= System.Math.Pow(radius, 2))
                    {
                        Vector3 spawnPos = new Vector3(start_coordinates.x + i * tile_scale, 0, start_coordinates.y + j * tile_scale);
                        spawnDictionary.Add((i, j), Instantiate(floor, spawnPos, Quaternion.identity));
                        spawnDictionary[(i, j)].transform.localScale = new Vector3(tile_scale, 1, tile_scale);

                        // Generate deterministic random color based on position
                        long seed = i + j * 317L;
                        seed ^= seed << 13;
                        seed ^= seed >> 17;
                        seed ^= seed << 5;
                        Random.InitState((int)(seed & 0x7FFFFFFF));
                        spawnDictionary[(i, j)].GetComponent<MeshRenderer>().material.color =
                            new Color(Random.value, Random.value, Random.value);
                    }

                    // Handle existing tiles
                    if (spawnDictionary.ContainsKey((i, j)))
                    {
                        // Remove tile if outside radius
                        if (distance > System.Math.Pow(radius, 2))
                        {
                            Destroy(spawnDictionary[(i, j)]);
                            spawnDictionary.Remove((i, j));
                        }
                        // Shift tile position if origin shifting is active
                        else if (flag && distance <= System.Math.Pow(radius, 2))
                        {
                            spawnDictionary[(i, j)].transform.position -= shift_coordinates.ToVector3();
                        }
                    }
                }
            }
        }

        // Apply origin shift if flagged
        if (flag)
        {
            // Adjust start coordinates after shifting
            start_coordinates -= shift_coordinates;
            // Reset camera position to the new origin
            Vector2 CamTrans = new Vector2((float)(double)(cam.transform.position.x - shift_coordinates.x), (float)(double)(cam.transform.position.z - shift_coordinates.y));
            cam.transform.position = new Vector3(CamTrans.x, cam.transform.position.y, CamTrans.y);
            // Reset flag to false after shifting
            flag = false;
        }
    }
}