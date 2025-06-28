#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class MeshDebug : MonoBehaviour
{
    public Color labelColor = Color.cyan;
    public float labelOffset = 0.02f;
    public bool showVertexIndices = true;

    private void OnDrawGizmos()
    {
        if (!showVertexIndices) return;

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;

        Handles.color = labelColor;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(vertices[i]);
            Handles.Label(worldPos + Vector3.up * labelOffset, i.ToString());
        }
    }
}
#endif
