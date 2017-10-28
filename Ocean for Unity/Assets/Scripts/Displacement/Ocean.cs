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

    public MirrorPlane mirrorPlane;

    public Texture2D DisplacementTex;

    private Vector2 DisplacementTex_Offset;
    public Vector2 WaveDMapOffsetSpeed;
    public float Displacement;

    public Texture NormalTex;

    void Awake()
    {
        Water = gameObject;
        WaterFilters = Water.AddComponent<MeshFilter>();
        WaterRenderer = Water.AddComponent<MeshRenderer>();

        WaterFilters.mesh = CreateMesh(128, 128);

        WaterRenderer.material = Resources.Load("Displacement/Materials/Ocean", typeof(Material)) as Material;

        projection = new Projection();

        mirrorPlane = new MirrorPlane();
    }

    void Start()
    {
        if (mirrorPlane != null)
        {
            mirrorPlane.Init();
        }
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        projection.UpdateProjection(cam);

        DisplacementTex_Offset += -WaveDMapOffsetSpeed * Time.deltaTime;

        if (DisplacementTex_Offset.x >= 1.0f || DisplacementTex_Offset.x <= -1.0f)
            DisplacementTex_Offset.x = 0.0f;
        if (DisplacementTex_Offset.y >= 1.0f || DisplacementTex_Offset.y <= -1.0f)
            DisplacementTex_Offset.y = 0.0f;

        Shader.SetGlobalMatrix("Interpolation", projection.projectorI);
        Shader.SetGlobalTexture("ReflectTex", mirrorPlane.ReflectTex);
        Shader.SetGlobalTexture("RefractTex", mirrorPlane.RefractTex);
        Shader.SetGlobalTexture("DisplacementTex", DisplacementTex);
        Shader.SetGlobalVector("gWaveDMapOffset0", DisplacementTex_Offset);
        Shader.SetGlobalFloat("Displacement", Displacement);

        Shader.SetGlobalTexture("NormalTex", NormalTex);

        Shader.SetGlobalVector("eyePosW", cam.transform.position);
        Vector3 newdir = new Vector3(-100, 10, -100);
        Shader.SetGlobalVector("lightDirW", newdir);
    }

    public Mesh CreateMesh(int numVertRows, int numVertCols)
    {
        int numVertices = numVertRows * numVertCols;
        int numCellRows = numVertRows - 1;
        int numCellCols = numVertCols - 1;

        Vector3[] vertices = new Vector3[numVertices];
        Vector2[] texcoords = new Vector2[numVertices];
        int[] indices = new int[numVertices * 6];

        float dx = 0.5f;
        float dz = 0.5f;

        float width = (float)numVertCols * dx;
        float depth = (float)numVertRows * dz;

        float xOffset = -width * 0.5f;
        float zOffset = depth * 0.5f;

        int k = 0;
        for (float i = 0; i < numVertRows; ++i)
        {
            for (float j = 0; j < numVertCols; ++j)
            {
                vertices[k].x = j * dx + xOffset;
                vertices[k].z = -i * dz + zOffset;
                vertices[k].y = 0.0f;

//                 Vector2 uv = new Vector2((float)j / (float)(numVertCols - 1), (float)i / (float)(numVertRows - 1));
//                 texcoords[k] = new Vector2(uv.x, uv.y);

                ++k; // Next vertex
            }
        }

        for (int i = 0; i < numVertRows; ++i)
        {
            for (int j = 0; j < numVertCols; ++j)
            {
                int index = i * numVertCols + j;
                texcoords[index] = new Vector2((float)j / (numVertCols - 1), (float)i / (numVertRows - 1));
            }
        }

        k = 0;
        for (int i = 0; i < numCellRows; ++i)
        {
            for (int j = 0; j < numCellCols; ++j)
            {
                indices[k] = i * numVertCols + j;
                indices[k + 1] = (i + 1) * numVertCols + j;
                indices[k + 2] = i * numVertCols + j + 1;

                indices[k + 3] = (i + 1) * numVertCols + j;
                indices[k + 4] = (i + 1) * numVertCols + j + 1;
                indices[k + 5] = i * numVertCols + j + 1;

                // next quad
                k += 6;
            }
        }

/*
        for (int index = 0, x = 0; x < numVertexX - 1; x++)
        {
            for (int y = 0; y < numVertexY - 1; y++)
            {
                indices[index++] = x + y * numVertexX;
                indices[index++] = x + (y + 1) * numVertexX;
                indices[index++] = (x + 1) + y * numVertexX;

                indices[index++] = x + (y + 1) * numVertexX;
                indices[index++] = (x + 1) + (y + 1) * numVertexX;
                indices[index++] = (x + 1) + y * numVertexX;
            }
        }*/

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

    void OnWillRenderObject()
    {
        if (mirrorPlane != null)
        {
            mirrorPlane.UpdateRenderTarget(Camera.main);
        }
    }
}