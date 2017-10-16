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

    public Texture DisplacementTex;

    private Vector2 DisplacementTex_Offset;
    public Vector2 DisplacementTex_Speed;
    public float Displacement;

    void Awake()
	{
        Water = gameObject;
        WaterFilters = Water.AddComponent<MeshFilter>();
        WaterRenderer = Water.AddComponent<MeshRenderer>();

        WaterFilters.mesh = CreateMesh(1024, 1024, 32, 32);
        
        WaterRenderer.material = Resources.Load("Materials/Ocean", typeof(Material)) as Material;

        projection = new Projection();

        mirrorPlane = new MirrorPlane();
    }

    void Start()
    {
        if(mirrorPlane != null)
        {
            mirrorPlane.Init();
        }
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        projection.UpdateProjection(cam);

        DisplacementTex_Offset += DisplacementTex_Speed * Time.deltaTime;

        Shader.SetGlobalMatrix("Interpolation", projection.projectorI);
        Shader.SetGlobalTexture("ReflectTex", mirrorPlane.ReflectTex);
        Shader.SetGlobalTexture("RefractTex", mirrorPlane.RefractTex);
        Shader.SetGlobalTexture("DisplacementTex", DisplacementTex);
        Shader.SetGlobalVector("DisplacementTex_Offset", DisplacementTex_Offset);
        Shader.SetGlobalFloat("Displacement", Displacement);
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

    void OnWillRenderObject()
    {
        if (mirrorPlane != null)
        {
            mirrorPlane.UpdateRenderTarget(Camera.main);
        }
    }
}