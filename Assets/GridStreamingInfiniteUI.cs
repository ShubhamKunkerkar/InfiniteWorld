using UnityEngine;
using UnityEditor;
using System;
public partial class GridStreamingInfinite : MonoBehaviour
{
#if UNITY_EDITOR
    // Custom inspector to show WireGridThreshold only when enableVisualization is true
    [CustomEditor(typeof(GridStreamingInfinite))]
    public class Floor1Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty enableVisProp = serializedObject.FindProperty("enableVisualization");
            EditorGUILayout.PropertyField(enableVisProp);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("floor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cam"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tile_scale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("grid_size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("start_coordinates"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shift_radius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gridThreshold"));

            if (enableVisProp.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("WireGridThreshold"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    private void OnDrawGizmos()
    {
        if (!enableVisualization) return; // Only draw if enabled

        if (cam == null) return;

        // Colors
        Color gridColor = new Color(0.1f, 0.8f, 1f, 0.7f);         // Brighter cyan-blue for grid
        Color camToCellLineColor = new Color(1f, 0.3f, 0f, 1f);    // Strong orange for lines
        Color camPosColor = new Color(1f, 1f, 0.1f, 1f);           // Bright yellow for camera
        Color gridSnapColor = new Color(1f, 0f, 0.0f, 1f);       // Bright red for snapped grid pos
        Color radiusColor = new Color(1f, 0.1f, 1f, 0.9f);         // Bright magenta for radius

        // Calculate grid-aligned camera position
        Vector3 camPos = cam.transform.position;
        Vector3 gridPos = new Vector3(
            Mathf.Round((camPos.x - start_coordinates.x) / tile_scale) * tile_scale + start_coordinates.x,
            0,
            Mathf.Round((camPos.z - start_coordinates.y) / tile_scale) * tile_scale + start_coordinates.y
        );

        // Draw camera position
        Handles.color = camPosColor;
        Handles.SphereHandleCap(0, new Vector3(camPos.x, 0, camPos.z), Quaternion.identity, 0.5f, EventType.Repaint);
        Handles.Label(new Vector3(camPos.x, 0.6f, camPos.z), "<b>Camera</b>", new GUIStyle() { normal = { textColor = camPosColor }, fontStyle = FontStyle.Bold });

        // Draw grid as wire cubes
        matrix_offset_i = Math.Abs(Mathf.CeilToInt(-grid_size / 2));
        matrix_offset_j = Math.Abs(Mathf.CeilToInt(-grid_size / 2));
        int CamXcoordsInGridSpace = Mathf.RoundToInt((cam.transform.position.x - start_coordinates.x) / tile_scale);
        int CamZcoordsInGridSpace = Mathf.RoundToInt((cam.transform.position.z - start_coordinates.y) / tile_scale);
        int RadiusInGridUnits = Mathf.RoundToInt(radius / tile_scale);

        for (int i = CamXcoordsInGridSpace - (RadiusInGridUnits + WireGridThreshold); i <= CamXcoordsInGridSpace + RadiusInGridUnits + WireGridThreshold; i++)
        {
            for (int j = CamZcoordsInGridSpace - (RadiusInGridUnits + WireGridThreshold); j <= CamZcoordsInGridSpace + RadiusInGridUnits + WireGridThreshold; j++)
            {
                if (i < grid_size - matrix_offset_i && j < grid_size - matrix_offset_j && i >= -matrix_offset_i && j >= -matrix_offset_j)
                {
                    Vector3 center = new Vector3(
                        start_coordinates.x + i * tile_scale,
                        0,
                        start_coordinates.y + j * tile_scale
                    );
                    Handles.color = gridColor;
                    Handles.DrawWireCube(center, new Vector3(tile_scale, 0.05f, tile_scale));
                }
            }
        }

        // Draw radius disc at camera position
        Handles.color = radiusColor;
        Handles.DrawWireDisc(new Vector3(camPos.x, 0, camPos.z), Vector3.up, radius);

        // Highlight cells in spawn area and draw lines
        for (int i = CamXcoordsInGridSpace - (RadiusInGridUnits + gridThreshold); i <= CamXcoordsInGridSpace + RadiusInGridUnits + gridThreshold; i++)
        {
            for (int j = CamZcoordsInGridSpace - (RadiusInGridUnits + gridThreshold); j <= CamZcoordsInGridSpace + RadiusInGridUnits + gridThreshold; j++)
            {
                if (i < grid_size - matrix_offset_i && j < grid_size - matrix_offset_j && i >= -matrix_offset_i && j >= -matrix_offset_j)
                {
                    Vector3 cellCenter = new Vector3(
                        start_coordinates.x + i * tile_scale,
                        0,
                        start_coordinates.y + j * tile_scale
                        );

                    Handles.color = Color.red;
                    Handles.DrawWireCube(cellCenter, new Vector3(tile_scale, 0.05f, tile_scale));

                    float cellDistance = Vector3.Distance(new Vector3(camPos.x, 0, camPos.z), cellCenter);

                    if (cellDistance <= radius)
                    {
                        Handles.color = Color.Lerp(new Color(0f, 1f, 0f, 0.6f), new Color(1f, 0f, 0f, 0.6f), Mathf.InverseLerp(0, radius, cellDistance));
                        Handles.SphereHandleCap(0, cellCenter, Quaternion.identity, tile_scale * 0.4f, EventType.Repaint);

                        Handles.color = camToCellLineColor;
                        Handles.DrawDottedLine(new Vector3(camPos.x, 0, camPos.z), cellCenter, 2f);

                        Handles.Label(
                            cellCenter + Vector3.up * 0.3f,
                            $"({i},{j})\n{cellDistance:F1}",
                            new GUIStyle() { normal = { textColor = Color.white } }
                        );
                    }
                    else
                    {
                        Handles.color = Color.black;
                        Handles.SphereHandleCap(0, cellCenter, Quaternion.identity, tile_scale * 0.4f, EventType.Repaint);

                        Handles.color = camToCellLineColor;
                        Handles.DrawDottedLine(new Vector3(camPos.x, 0, camPos.z), cellCenter, 2f);

                        Handles.Label(
                            cellCenter + Vector3.up * 0.3f,
                            $"({i},{j})\n{cellDistance:F1}",
                            new GUIStyle() { normal = { textColor = Color.white } }
                        );
                    }
                }
            }
        }

        // Draw snapped grid position and line from camera to grid snap
        Handles.color = gridSnapColor;
        Handles.SphereHandleCap(0, gridPos, Quaternion.identity, 5.0f, EventType.Repaint);
        Handles.DrawLine(new Vector3(camPos.x, 0, camPos.z), gridPos, 3f);

        // Draw distance label
        float distance = Vector3.Distance(new Vector3(camPos.x, 0, camPos.z), gridPos);
        Handles.Label(gridPos + Vector3.forward * 15f + Vector3.left * 60f, $"Dist: {distance:F2} Active Grid : ({(gridPos.x - start_coordinates.x) / tile_scale},{(gridPos.z - start_coordinates.y) / tile_scale})", new GUIStyle() { normal = { textColor = Color.white }, fontStyle = FontStyle.Bold });

    }
#endif
}