using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;

public class MirrorPlane
{
    Camera reflectionCamera;    //������Ⱦ�����
    Camera refractionCamera;    //������Ⱦ�����

    public RenderTexture ReflectTex;
    public RenderTexture RefractTex;

    Vector3 planePos;
    Vector3 planeNormal;

    // for initialization
    public void Init()
    {
        // ��ӷ������
        GameObject goFle = new GameObject("ReflectionCamera");
        reflectionCamera = goFle.AddComponent<Camera>();

        // ����������
        GameObject goFra = new GameObject("RefacctionCamera");
        refractionCamera = goFra.AddComponent<Camera>();

        // ����RenderTarget
        ReflectTex = new RenderTexture(256, 256, 32);
        RefractTex = new RenderTexture(256, 256, 32);

        // �������RenderTarget
        reflectionCamera.targetTexture = ReflectTex;
        refractionCamera.targetTexture = RefractTex;

        reflectionCamera.transform.rotation = Camera.main.transform.rotation;
        reflectionCamera.transform.position = Camera.main.transform.position;

        refractionCamera.transform.rotation = Camera.main.transform.rotation;
        refractionCamera.transform.position = Camera.main.transform.position;

        planePos = new Vector3(0.0f, 0.0f, 0.0f);
        planeNormal = new Vector3(0.0f, 1.0f, 0.0f);
    }

    public void UpdateRenderTarget(Camera playerCam)
    {
        // ����
        {
            // ��һ��ƽ��Ĺ�ʽΪAx + By + Cz + D = 0������normal�����ƽ��ķ��ߣ�pos��ʾ�����÷��ߵ�һ����  
            // ��A��B��C�ֱ�Ϊnormal��x��y��zֵ��Ȼ�����pos�����  
            // ��normal.x * pos.x + normal.y * pos.y + normal.z * pos.z + D = 0  
            // ����DΪ -Vector3.Dot(normal, pos)  
            Vector3 pos = planePos;
            Vector3 normal = planeNormal; //��λ�������������  
            float D = -Vector3.Dot(normal, pos);
            Vector4 plane = new Vector4(normal.x, normal.y, normal.z, D);   //���ƽ��  

            // ������������  
            Matrix4x4 reflectionMatrix = Matrix4x4.zero;
            CalculateReflectionMatrix(ref reflectionMatrix, plane);

            // ���㷴����������������絽�������
            reflectionCamera.transform.position = reflectionMatrix.MultiplyPoint(playerCam.transform.position);  //ͨ���������������λ�� 

            {
                Vector3 p_0 = planePos;
                //a normal (defining the orientation of the plane), should be negative if we are firing the ray from above
                Vector3 n = -planeNormal;

                //A ray to point p can be defined as: l_0 + l * t = p, where:
                //the origin of the ray
                Vector3 l_0 = playerCam.transform.position;
                //l is the direction of the ray
                Vector3 l = playerCam.transform.forward;
                //t is the length of the ray, which we can get by combining the above equations:
                //t = ((p_0 - l_0) . n) / (l . n)

                //But there's a chance that the line doesn't intersect with the plane, and we can check this by first
                //calculating the denominator and see if it's not small. 
                //We are also checking that the denominator is positive or we are looking in the opposite direction
                float denominator = Vector3.Dot(l, n);

                if (denominator > 0.00001f)
                {
                    //The distance to the plane
                    float t = Vector3.Dot(p_0 - l_0, n) / denominator;

                    //Where the ray intersects with a plane
                    Vector3 p = l_0 + l * t;

                    Vector3 target = p;
                    reflectionCamera.transform.LookAt(target, -Vector3.up);
                }
            }
            
            // ���������  
            Vector4 clipPlane = CameraSpacePlane(reflectionCamera, pos, normal, 1.0f);
            Matrix4x4 projection = playerCam.projectionMatrix;
            CalculateObliqueMatrix(ref projection, clipPlane); //�������������ص�ͶӰ���󣬼������������ݲ���ʾ  
            reflectionCamera.projectionMatrix = projection;
        }

        // ����
        {
            Vector3 pos = planePos;
            Vector3 normal = -planeNormal; //��λ�������������  
            float D = -Vector3.Dot(normal, pos);
            Vector4 plane = new Vector4(normal.x, normal.y, normal.z, D);   //���ƽ��  

            // ����������������������絽�������
            refractionCamera.transform.rotation = playerCam.transform.rotation;
            refractionCamera.transform.position = playerCam.transform.position;


            // ���������  
            Vector4 clipPlane = CameraSpacePlane(refractionCamera, pos, normal, 1.0f);
            Matrix4x4 projection = playerCam.projectionMatrix;
            CalculateObliqueMatrix(ref projection, clipPlane); //�������������ص�ͶӰ���󣬼������������ݲ���ʾ  
            refractionCamera.projectionMatrix = projection;
        }
    }

    private static float sgn(float a)
    {
        if (a > 0.0f)
            return 1.0f;
        if (a < 0.0f)
            return -1.0f;
        return 0.0f;
    }

    public static void CalculateObliqueMatrix(ref Matrix4x4 projection, Vector4 clipPlane)
    {
        Vector4 q = projection.inverse * new Vector4(sgn(clipPlane.x), sgn(clipPlane.y), 1.0f, 1.0f);
        Vector4 c = clipPlane * (2.0F / (Vector4.Dot(clipPlane, q)));
        projection[2] = c.x - projection[3];
        projection[6] = c.y - projection[7];
        projection[10] = c.z - projection[11];
        projection[14] = c.w - projection[15];
    }

    public static Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * 0.02f;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    public static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
        reflectionMat.m01 = (-2F * plane[0] * plane[1]);
        reflectionMat.m02 = (-2F * plane[0] * plane[2]);
        reflectionMat.m03 = (-2F * plane[0] * plane[3]);

        reflectionMat.m10 = (-2F * plane[1] * plane[0]);
        reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
        reflectionMat.m12 = (-2F * plane[1] * plane[2]);
        reflectionMat.m13 = (-2F * plane[1] * plane[3]);

        reflectionMat.m20 = (-2F * plane[2] * plane[0]);
        reflectionMat.m21 = (-2F * plane[2] * plane[1]);
        reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
        reflectionMat.m23 = (-2F * plane[2] * plane[3]);

        reflectionMat.m30 = 0F;
        reflectionMat.m31 = 0F;
        reflectionMat.m32 = 0F;
        reflectionMat.m33 = 1F;
    }
}