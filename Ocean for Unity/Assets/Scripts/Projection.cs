using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;

public class Projection
{
    public Matrix4x4 projectorR;
    public Matrix4x4 projectorI;

    public Matrix4x4 projectorP;
    public Matrix4x4 projectorV;

    float waterLevel;
    float displacementRange;

    readonly static Vector4[] quad =
    {
        new Vector4(0, 0, 0, 1),
        new Vector4(1, 0, 0, 1),
        new Vector4(1, 1, 0, 1),
        new Vector4(0, 1, 0, 1)
    };
    
    /// The corner points of a frustum box.
    readonly static Vector4[] corners =
    {
		// near
		new Vector4(-1, -1, -1, 1),
        new Vector4( 1, -1, -1, 1),
        new Vector4( 1,  1, -1, 1),
        new Vector4(-1,  1, -1, 1),
		// far
		new Vector4(-1, -1, 1, 1),
        new Vector4( 1, -1, 1, 1),
        new Vector4( 1,  1, 1, 1),
        new Vector4(-1,  1, 1, 1)
    };
    
    /// The indices of each line segment in
    /// the frustum box.
    readonly static int[,] indices =
    {
        {0,1}, {1,2}, {2,3}, {3,0},
        {4,5}, {5,6}, {6,7}, {7,4},
        {0,4}, {1,5}, {2,6}, {3,7}
    };

    public Projection()
    {
        projectorR = new Matrix4x4();
        projectorI = new Matrix4x4();
        projectorP = new Matrix4x4();
        projectorV = new Matrix4x4();

        waterLevel = 0;
        displacementRange = 5;
    }

    public void UpdateProjection(Camera renderCam)
    {
        //Aim the projector given the current camera position.
        //Find the most practical but visually pleasing projection.
        //Sets the m_projectorV and m_projectorP matrices.
        AimProjector(renderCam);

        //Create a view projection matrix.
        Matrix4x4 projectorVP = projectorP * projectorV;

        //Create the m_projectorR matrix. 
        //Finds the range the projection must fit 
        //for the projected grid to cover the screen.
        CreateRangeMatrix(renderCam, projectorVP);

        //Create the inverse view projection range matrix.
        Matrix4x4 IVP = (projectorVP).inverse * projectorR;

        //Set the interpolation points based on IVP matrix.
        projectorI = Matrix4x4.identity;
        projectorI.SetRow(0, HProject(IVP, quad[0]));
        projectorI.SetRow(1, HProject(IVP, quad[1]));
        projectorI.SetRow(2, HProject(IVP, quad[2]));
        projectorI.SetRow(3, HProject(IVP, quad[3]));
    }

    public void AimProjector(Camera renderCam)
    {
        projectorP = renderCam.projectionMatrix;

        Vector3 pos = renderCam.transform.position;
        Vector3 dir = renderCam.transform.forward;
        Vector3 lookAt = new Vector3();

        if (pos.y < waterLevel)
        {
            pos.y = waterLevel;
        }

        float offset = 40;
       
        pos.y = Math.Max(pos.y, waterLevel + displacementRange + offset);

        lookAt = pos + dir * 50.0f;
        lookAt.y = waterLevel;

        LookAt(pos, lookAt, Vector3.up);
    }

    public void CreateRangeMatrix(Camera renderCam, Matrix4x4 projectorVP)
    {
        List<Vector3> pointList = new List<Vector3>();
        Vector3[] frustumCorners = new Vector3[8];

        Matrix4x4 viewMatrix = renderCam.worldToCameraMatrix;

        //the inverse view projection matrix will transform
        //screen space verts to world space
        Matrix4x4 IVP = (projectorP * viewMatrix).inverse;
        
        //Convert each screen vert to world space
        for(int i=0;i<8;i++)
        {
            Vector4 p = IVP * corners[i];
            p /= p.w;

            frustumCorners[i] = p;
        }

        //For each corner if its world space position is
        //between the wave range then add it to the list
        for(int i=0;i<8;i++)
        {
            if(frustumCorners[i].y <= waterLevel + displacementRange && frustumCorners[i].y >= waterLevel - displacementRange)
            {
                pointList.Add(frustumCorners[i]);
            }
        }

        //Now take each segment in the frustum box and check
        //to see if it intersects the ocean plane on both the
        //upper and lower ranges
        Vector3 up = Vector3.up;

        for (int i=0;i<12;i++)
        {
            Vector3 p0 = frustumCorners[indices[i, 0]];
            Vector3 p1 = frustumCorners[indices[i, 1]];

            Vector3 max = new Vector3();
            Vector3 min = new Vector3();

            if(SegmentPlaneIntersection(p0, p1, up, waterLevel + displacementRange, ref max))
            {
                pointList.Add(max);
            }

            if(SegmentPlaneIntersection(p0, p1, up, waterLevel - displacementRange, ref min))
            {
                pointList.Add(min);
            }
        }

        int count = pointList.Count;

        //If list is empty the ocean can not be seen
        if(count == 0)
        {
            projectorR[0, 0] = 1;
            projectorR[1, 1] = 1;
            projectorR[0, 3] = 0;
            projectorR[1, 3] = 0;

            return;
        }

        float xmin = float.PositiveInfinity;
        float ymin = float.PositiveInfinity;
        float xmax = float.NegativeInfinity;
        float ymax = float.NegativeInfinity;

        //Now convert each world space position into
        //projector screen space. The min/max x/y values
        //are then used for the range conversion matrix.
        for(int i=0;i<count;i++)
        {
            Vector4 q = Vector4.zero;

            q.x = pointList[i].x;
            q.y = waterLevel;
            q.z = pointList[i].z;
            q.w = 1.0f;

            Vector4 p = projectorVP * q;
            p /= p.w;

            if (p.x < xmin) xmin = p.x;
            if (p.y < ymin) ymin = p.y;
            if (p.x > xmax) xmax = p.x;
            if (p.y > ymax) ymax = p.y;
        }

        //Create the range conversion matrix and return it.
        projectorR = Matrix4x4.identity;
        projectorR[0, 0] = xmax - xmin;
        projectorR[1, 1] = ymax - ymin;
        projectorR[0, 3] = xmin;
        projectorR[1, 3] = ymin;
    }

    public void LookAt(Vector3 position, Vector3 target, Vector3 up)
    {
        Vector3 zaxis = (position - target).normalized;
        Vector3 xaxis = Vector3.Cross(up, zaxis).normalized;
        Vector3 yaxis = Vector3.Cross(zaxis, xaxis);

        projectorV[0, 0] = xaxis.x;
        projectorV[0, 1] = xaxis.y;
        projectorV[0, 2] = xaxis.z;
        projectorV[0, 3] = -Vector3.Dot(xaxis, position);

        projectorV[1, 0] = yaxis.x;
        projectorV[1, 1] = yaxis.y;
        projectorV[1, 2] = yaxis.z;
        projectorV[1, 3] = -Vector3.Dot(yaxis, position);

        projectorV[2, 0] = zaxis.x;
        projectorV[2, 1] = zaxis.y;
        projectorV[2, 2] = zaxis.z;
        projectorV[2, 3] = -Vector3.Dot(zaxis, position);

        projectorV[3, 0] = 0;
        projectorV[3, 1] = 0;
        projectorV[3, 2] = 0;
        projectorV[3, 3] = 1;

        //Must flip to match Unity's winding order.
        projectorV[0, 0] *= -1.0f;
        projectorV[0, 1] *= -1.0f;
        projectorV[0, 2] *= -1.0f;
        projectorV[0, 3] *= -1.0f;
    }
    
    /// Find the intersection point of a plane and segment in world space.
    bool SegmentPlaneIntersection(Vector3 a, Vector3 b, Vector3 n, float d, ref Vector3 q)
    {
        Vector3 ab = b - a;
        float t = (d - Vector3.Dot(n, a)) / Vector3.Dot(n, ab);

        if (t > -0.0f && t <= 1.0f)
        {
            q = a + t * ab;
            return true;
        }

        return false;
    }

    /// Project the corner point from projector space into 
    /// world space and find the intersection of the segment
    /// with the ocean plane in homogeneous space.
    /// The intersection is this grids corner's world pos.
    /// This is done in homogeneous space so the corners
    /// can be interpolated in the vert shader for the rest of
    /// the points. Homogeneous space is the world space but
    /// in 4D where the w value is the position on the infinity plane.
    Vector4 HProject(Matrix4x4 ivp, Vector4 corner)
    {
        Vector4 a, b;

        corner.z = -1;
        a = ivp * corner;

        corner.z = 1;
        b = ivp * corner;

        float h = waterLevel;

        Vector4 ab = b - a;

        float t = (a.w * h - a.y) / (ab.y - ab.w * h);

        return a + ab * t;
    }
}