using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;

public class Ocean : MonoBehaviour
{

    public MeshFilter WaterFilters { set; get; }
    public Renderer WaterRenderer { set; get; }
    public GameObject Water { set; get; }

    public Projection projection;

    void Awake()
	{
        Water = gameObject;
        WaterFilters = Water.AddComponent<MeshFilter>();
        WaterRenderer = Water.AddComponent<MeshRenderer>();

        WaterFilters.mesh = CreateMesh(1024, 1024, 32, 32);

        WaterRenderer.material = new Material(Shader.Find("Transparent/Diffuse"))
        {
            color = Color.blue
        };

        projection = new Projection();
    }

    void Start()
    {

    }

    private void Update()
    {
        Camera cam = Camera.main;
        projection.UpdateProjection(cam);

        Vector4 corner0 = projection.projectorI.GetRow(0);
        Vector4 corner1 = projection.projectorI.GetRow(1);
        Vector4 corner2 = projection.projectorI.GetRow(2);
        Vector4 corner3 = projection.projectorI.GetRow(3);

        int vertexCount = WaterFilters.mesh.vertexCount;
        Vector3[] newVertex = WaterFilters.mesh.vertices;
        for (int i=0;i<vertexCount;i++)
        {
            float u = WaterFilters.mesh.uv[i].x;
            float v = WaterFilters.mesh.uv[i].y;

            Vector4 p = Vector4.Lerp(Vector4.Lerp(corner0, corner1, u), Vector4.Lerp(corner3, corner2, u), v);
            p = p / p.w;

            newVertex[i].x = p.x;
            newVertex[i].y = 0.0f;
            newVertex[i].z = p.z;
        }
        WaterFilters.mesh.vertices = newVertex;
    }

    public Mesh CreateMesh(int width, int height, int titleWidth, int titleHeight)
    {
        int numVertexX = width / titleWidth + 1;
        int numVertexY = height / titleHeight + 1;
        Vector3[] vertices = new Vector3[numVertexX * numVertexY];
        Vector2[] texcoords = new Vector2[numVertexX * numVertexY];
        int[] indices = new int[numVertexX * numVertexY * 6];

        for (int x = 0; x < numVertexX; x++)
        {
            for (int y = 0; y < numVertexY; y++)
            {
                Vector2 uv = new Vector2(
                    (float)x / (float)(numVertexX - 1),
                    (float)y / (float)(numVertexY - 1));

                texcoords[x + y * numVertexX] = new Vector2(uv.x, uv.y);
                vertices[x + y * numVertexX] = new Vector3(uv.x, uv.y, 0.0f);
            }
        }

        int num = 0;
        for (int x = 0; x < numVertexX - 1; x++)
        {
            for (int y = 0; y < numVertexY - 1; y++)
            {
                indices[num++] = x + y * numVertexX;
                indices[num++] = x + (y + 1) * numVertexX;
                indices[num++] = (x + 1) + y * numVertexX;

                indices[num++] = x + (y + 1) * numVertexX;
                indices[num++] = (x + 1) + (y + 1) * numVertexX;
                indices[num++] = (x + 1) + y * numVertexX;
            }
        }

        Mesh mesh = new Mesh()
        {
            vertices = vertices,
            uv = texcoords,
            triangles = indices,
            name = "Projected Grid Mesh"
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}