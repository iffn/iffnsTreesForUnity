using System.Threading;
using UnityEngine;

public class PineTreeGenerator : MonoBehaviour
{
    [SerializeField] LineRenderer linkedLineRenderer;
    [SerializeField] int targetLineRenderer;

    [Header("Tree Structure")]
    const int maxChildren = 256;
    const int maxChildrenPerParent = 32;
    const int maxNodesPerChild = 32;

    // Hierarchy
    int[] parentIndices;
    int[][] parentChildIndices;
    int[][] parentChildNodeIndices;
    Vector3[] nodeDirections;
    Vector3[][] nodePositions;

    // Growth
    float[] growths;

    // Mesh data per node
    Vector3[][] vertices;
    int[][] triangles;
    Vector2[][] uvs;

    public int[] debugTriangles;

    void Start()
    {
        InitializeArrays();

        SetInitialTree();

        for(int i = 0; i < maxChildren; i++)
        {
            GrowBranch(i);
            GenerateMesh(i);
        }

        Mesh mesh = transform.GetComponent<MeshFilter>().sharedMesh; 
        if(mesh == null)
        {
            mesh = new Mesh();
            transform.GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        AssembleTreeMesh(mesh);

        linkedLineRenderer.positionCount = nodePositions[targetLineRenderer].Length;
        linkedLineRenderer.SetPositions(nodePositions[targetLineRenderer]);

        debugTriangles = mesh.triangles;
    }

    void SetInitialTree()
    {
        parentIndices[0] = -1;
        for(int i = 1; i < parentIndices.Length; i++)
        {
            parentIndices[i] = -2;
        }

        nodeDirections[0] = Vector3.up;
        growths[0] = 8;
    }

    void InitializeArrays()
    {
        parentIndices = new int[maxChildren];

        parentChildIndices = new int[maxChildren][];
        for (int i = 0; i < parentChildIndices.Length; i++)
            parentChildIndices[i] = new int[maxChildrenPerParent];

        parentChildNodeIndices = new int[maxChildren][];
        for(int i = 0; i<parentChildNodeIndices.Length; i++)
            parentChildNodeIndices[i] = new int[maxChildrenPerParent];

        nodePositions = new Vector3[maxChildren][];
        for(int i = 0;i<nodePositions.Length; i++)
        {
            nodePositions[i] = new Vector3[maxNodesPerChild];
        }

        nodeDirections = new Vector3[maxChildren];

        nodePositions = new Vector3[maxNodesPerChild][];
        
        growths = new float[maxChildren];

        vertices = new Vector3[maxChildren][];
        triangles = new int[maxChildren][];
        uvs = new Vector2[maxChildren][];
    }

    void GrowBranch(int index)
    {
        int parentIndex = parentIndices[index];

        // Root position
        Vector3 origin;

        if (parentIndex == -2)
            return;
        else if(parentIndex >= 0)
        {
            // Branch
            int[] searchInParent = parentChildIndices[parentIndex];

            for(int i = 0; i < searchInParent.Length; i++)
            {
                if(searchInParent[i] == index)
                {
                    int nodeIndex = parentChildNodeIndices[parentIndex][i];
                    origin = nodePositions[parentIndex][nodeIndex];
                    break;
                }
            }

            Debug.LogWarning($"Problem when creating tree: Node no found in {nameof(parentChildNodeIndices)}");
            origin = Vector3.zero;
        }
        else if(parentIndex == -1)
        {
            // Root
            origin = Vector3.zero;
        }
        else
        {
            Debug.LogWarning($"Problem when creating tree: Meaning of parent index {parentIndex} defined.");
            return;
        }

        // Node parameters
        float growth = growths[index];
        int numberOfNodes = System.Math.Clamp((int)growth, 2, maxNodesPerChild);
        float nodeLenght = growth * 0.1f;
        Vector3 nodeOffset = nodeDirections[index];

        //Node positions
        nodePositions[index] = new Vector3[numberOfNodes];
        nodePositions[index][0] = origin;

        Vector3 prevPosition = origin;

        for(int i =1; i < numberOfNodes; i++)
        {
            prevPosition += nodeOffset;
            nodePositions[index][i] = prevPosition + 0.1f * RandomPerpendicular(nodeOffset).normalized;
        }
    }

    void GenerateMesh(int index)
    {
        if (parentIndices[index] == -2)
            return;

        Vector3[] currentNodePositions = nodePositions[index];
        Vector3 direction = nodeDirections[index];

        int numberOfEdges = 4;
        float growth = growths[index];
        float offset = 0.1f * growth;

        Vector3[] offsets = GeneratePointsAroundNormal(direction, numberOfEdges);

        vertices[index] = new Vector3[(numberOfEdges + 1) * currentNodePositions.Length];
        
        uvs[index] = new Vector2[vertices[index].Length];
        triangles[index] = new int[(currentNodePositions.Length - 1) * numberOfEdges * 6];
        
        // Generate vertices and UVs
        for(int i = 0; i < currentNodePositions.Length; i++)
        {
            int baseIndex = i * (numberOfEdges + 1);
            vertices[index][baseIndex] = currentNodePositions[i];

            float verticalUVPosition = i; //ToDo: Scale y

            uvs[index][baseIndex] = new Vector2(0, verticalUVPosition);

            for (int j = 0; j < numberOfEdges; j++)
            {
                vertices[index][baseIndex + j + 1] = currentNodePositions[i] + offsets[j];

                uvs[index][baseIndex + j + 1] = new Vector2(1, verticalUVPosition);
            }
        }

        //Generate triangles
        int baseTriangleIndex = 0;

        for (int i = 0; i < currentNodePositions.Length - 1; i++)
        {
            int baseVertexIndex = i * (numberOfEdges + 1);

            for (int j = 0; j < numberOfEdges; j++)
            {
                triangles[index][baseTriangleIndex++] = baseVertexIndex;
                triangles[index][baseTriangleIndex++] = baseVertexIndex + 1 + j;
                triangles[index][baseTriangleIndex++] = baseVertexIndex + numberOfEdges + 1;

                triangles[index][baseTriangleIndex++] = baseVertexIndex + 1 + j;
                triangles[index][baseTriangleIndex++] = baseVertexIndex + numberOfEdges + 1;
                triangles[index][baseTriangleIndex++] = baseVertexIndex + numberOfEdges + 2 + j;
            }
        }
    }

    void AssembleTreeMesh(Mesh mesh)
    {
        int verticesAndUVCount = 0;
        int triangleCount = 0;

        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i] != null)
                verticesAndUVCount += vertices[i].Length;
        }

        for (int i = 0; i < triangles.Length; i++)
        {
            if (triangles[i] != null)
                triangleCount += triangles[i].Length;
        }

        Vector3[] meshVertices = new Vector3[verticesAndUVCount];
        Vector2[] meshUVs = new Vector2[verticesAndUVCount];
        int[] meshTriangles = new int[triangleCount];

        int vertCounter = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i] == null)
                continue;

            for (int j = 0; j < vertices[i].Length; j++)
            {
                meshVertices[vertCounter] = vertices[i][j];
                meshUVs[vertCounter] = uvs[i][j];
                vertCounter++;
            }
        }

        int triCounter = 0;
        int vertOffset = 0;
        for (int i = 0; i < triangles.Length; i++)
        {
            if (triangles[i] == null)
                continue;

            for (int j = 0; j < triangles[i].Length; j++)
            {
                meshTriangles[triCounter++] = triangles[i][j] + vertOffset;
            }
            if (vertices[i] != null)
                vertOffset += vertices[i].Length;
        }

        mesh.Clear();
        mesh.vertices = meshVertices;
        mesh.uv = meshUVs;
        mesh.triangles = meshTriangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    Vector3[] GeneratePointsAroundNormal(Vector3 normalDirection, int count)
    {
        Vector3 arbitrary = (Mathf.Abs(normalDirection.x) < 0.99f) ? Vector3.right : Vector3.up;

        Vector3 perpendicular = Vector3.Cross(normalDirection.normalized, arbitrary).normalized;

        Vector3[] returnArray = new Vector3[count];

        float perAngle = 360f / count;

        for(int i = 0; i < count; i++)
        {
            float angle = perAngle * i;

            Quaternion rotation = Quaternion.AngleAxis(angle, normalDirection.normalized);

            returnArray[i] = rotation * perpendicular;
        }

        return returnArray;
    }

    Vector3 RandomPerpendicular(Vector3 normalDirection)
    {
        // Step 1: Choose any vector not parallel to nodeOffset
        Vector3 arbitrary = (Mathf.Abs(normalDirection.x) < 0.99f) ? Vector3.right : Vector3.up;

        // Step 2: Take cross product to get perpendicular vector
        Vector3 perpendicular = Vector3.Cross(normalDirection.normalized, arbitrary).normalized;

        // Step 3: Optionally rotate around nodeOffset for random direction
        float angle = Random.Range(0f, 360f);
        Quaternion rotation = Quaternion.AngleAxis(angle, normalDirection.normalized);
        return rotation * perpendicular;
    }
}
