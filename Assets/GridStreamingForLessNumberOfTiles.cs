using UnityEngine;
public partial class GridStreamingForLessNumberOfTiles : MonoBehaviour
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
    private GameObject[,] spawn_matrix;
    private Color[,] color_matrix;
    private Vector2Int CamActiveGridPos = new Vector2Int();
    private int matrix_offset_i;
    private int matrix_offset_j;
    private bool flag = false;
    private Vector2Int shift_coordinates = new Vector2Int(0, 0);

    void Start()
    {
        spawn_matrix = new GameObject[grid_size, grid_size];
        color_matrix = new Color[grid_size, grid_size];
        matrix_offset_i = System.Math.Abs(Mathf.CeilToInt(-grid_size / 2));
        matrix_offset_j = System.Math.Abs(Mathf.CeilToInt(-grid_size / 2));
        for (int i = 0; i < grid_size; i++)
        {
            for (int j = 0; j < grid_size; j++)
            {
                spawn_matrix[i, j] = null;
                color_matrix[i, j] = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            }
        }
    }
    void Update()
    {
        OriginShifting();
        ChunkLoading();
    }
    void OriginShifting()
    {
        float cam_distance = new Vector2(cam.transform.position.x, cam.transform.position.z).sqrMagnitude;
        CamActiveGridPos = new Vector2Int(
            Mathf.RoundToInt((cam.transform.position.x - start_coordinates.x) / tile_scale),
            Mathf.RoundToInt((cam.transform.position.z - start_coordinates.y) / tile_scale));
        if (cam_distance > System.Math.Pow(shift_radius, 2))
        {
            Vector3 shiftAmount = new Vector3(CamActiveGridPos.x * tile_scale + start_coordinates.x, 0, CamActiveGridPos.y * tile_scale + start_coordinates.y);
            cam.transform.position -= shiftAmount;
            shift_coordinates = new Vector2Int(
                (CamActiveGridPos.x * (int)tile_scale) + start_coordinates.x,
                (CamActiveGridPos.y * (int)tile_scale) + start_coordinates.y
            );
            start_coordinates -= shift_coordinates;
            flag = true;
        }
        
    }
    void ChunkLoading()
    {
        int RadiusInGridUnits = Mathf.RoundToInt(radius / tile_scale);
        for (int i = CamActiveGridPos.x - (RadiusInGridUnits + gridThreshold); i <= CamActiveGridPos.x + RadiusInGridUnits + gridThreshold; i++)
        {
            for (int j = CamActiveGridPos.y - (RadiusInGridUnits + gridThreshold); j <= CamActiveGridPos.y + RadiusInGridUnits + gridThreshold; j++)
            {
                if (i < grid_size - matrix_offset_i && j < grid_size - matrix_offset_j && i >= -matrix_offset_i && j >= -matrix_offset_j)
                {
                    float distance = Mathf.Pow(start_coordinates.x + i * tile_scale - cam.transform.position.x, 2) + Mathf.Pow(start_coordinates.y + j * tile_scale - cam.transform.position.z, 2);
                    if (distance <= System.Math.Pow(radius,2) && spawn_matrix[i + matrix_offset_i, j + matrix_offset_j] == null && !flag)
                    {
                        spawn_matrix[i + matrix_offset_i, j + matrix_offset_j] = Instantiate(floor, new Vector3(start_coordinates.x + i * tile_scale, 0, start_coordinates.y + j * tile_scale), Quaternion.identity);
                        spawn_matrix[i + matrix_offset_i, j + matrix_offset_j].GetComponent<Renderer>().material.color = color_matrix[i + matrix_offset_i, j + matrix_offset_j];
                        spawn_matrix[i + matrix_offset_i, j + matrix_offset_j].transform.localScale = new Vector3(tile_scale, 1, tile_scale);
                    }
                    else if (distance > System.Math.Pow(radius,2) && spawn_matrix[i + matrix_offset_i, j + matrix_offset_j] != null && !flag)
                    {
                        Destroy(spawn_matrix[i + matrix_offset_i, j + matrix_offset_j]);
                        spawn_matrix[i + matrix_offset_i, j + matrix_offset_j] = null;
                    }
                    if (flag)
                    {
                        if (spawn_matrix[i + matrix_offset_i, j + matrix_offset_j] != null)
                        {
                            if (distance > System.Math.Pow(radius,2))
                            {
                                Destroy(spawn_matrix[i + matrix_offset_i, j + matrix_offset_j]);
                                spawn_matrix[i + matrix_offset_i, j + matrix_offset_j] = null;
                            }
                            else
                            {
                                spawn_matrix[i + matrix_offset_i, j + matrix_offset_j].transform.localPosition -= new Vector3(shift_coordinates.x, 0, shift_coordinates.y);
                            }
                        }
                        else if (distance <= System.Math.Pow(radius,2) && spawn_matrix[i + matrix_offset_i, j + matrix_offset_j] == null)
                        {
                            spawn_matrix[i + matrix_offset_i, j + matrix_offset_j] = Instantiate(floor, new Vector3(start_coordinates.x + i * tile_scale, 0, start_coordinates.y + j * tile_scale), Quaternion.identity);
                            spawn_matrix[i + matrix_offset_i, j + matrix_offset_j].GetComponent<Renderer>().material.color = color_matrix[i + matrix_offset_i, j + matrix_offset_j];
                            spawn_matrix[i + matrix_offset_i, j + matrix_offset_j].transform.localScale = new Vector3(tile_scale, 1, tile_scale);
                        }
                    }
                }
            }
        }
        flag = false;
    }
}