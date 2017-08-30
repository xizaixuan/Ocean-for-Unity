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

    void Awake()
	{
        Water = gameObject;
        WaterFilters = Water.AddComponent<MeshFilter>();
        WaterRenderer = Water.AddComponent<MeshRenderer>();

        WaterFilters.mesh = CreateMesh(1024, 768, 32, 24);
    }

    void Start()
    {

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

                texcoords[x + y * numVertexX] = new Vector2(0, 0);
                vertices[x + y * numVertexY] = new Vector3(uv.x, uv.y, 0.0f);
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