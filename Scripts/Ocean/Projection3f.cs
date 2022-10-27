using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ceto
{
  /// <summary>
  ///   Calculates the projection VP matrix and interpolation matrix.
  ///   The projector VP matrix is used by the overlay system to project
  ///   the overlays onto the ocean mesh. The interpolation matrix is
  ///   used to convert a screen space mesh position into the world
  ///   position on the projection plane.
  /// </summary>
  public class Projection3f : IProjection
  {
    /// <summary>
    ///   The corner points of a quad.
    /// </summary>
    private static readonly Vector4[] m_quad =
    {
      new(0, 0, 0, 1),
      new(1, 0, 0, 1),
      new(1, 1, 0, 1),
      new(0, 1, 0, 1)
    };

    /// <summary>
    ///   The corner points of a frustum box.
    /// </summary>
    private static readonly Vector4[] m_corners =
    {
      // near
      new(-1, -1, -1, 1),
      new(1, -1, -1, 1),
      new(1, 1, -1, 1),
      new(-1, 1, -1, 1),
      // far
      new(-1, -1, 1, 1),
      new(1, -1, 1, 1),
      new(1, 1, 1, 1),
      new(-1, 1, 1, 1)
    };

    /// <summary>
    ///   The indices of each line segment in
    ///   the frustum box.
    /// </summary>
    private static readonly int[,] m_indices =
    {
      { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
      { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
      { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
    };

    /// <summary>
    ///   The frustum corners in world space.
    /// </summary>
    private readonly Vector3[] m_frustumCorners;

    /// <summary>
    ///   A parent ocean object.
    /// </summary>
    private readonly Ocean m_ocean;

    /// <summary>
    /// </summary>
    private readonly List<Vector3> m_pointList;

    /// <summary>
    ///   The projector projection, view and inverse view projection matrix.
    /// </summary>
    private Matrix4x4 m_projectorP, m_projectorV;

    /// <summary>
    ///   The projector range and interpolation matrix.
    /// </summary>
    private Matrix4x4 m_projectorR, m_projectorI;

    /// <summary>
    /// </summary>
    public Projection3f(Ocean ocean)
    {
      m_ocean = ocean;

      m_projectorP = new Matrix4x4();
      m_projectorV = new Matrix4x4();
      m_projectorR = Matrix4x4.identity;
      m_projectorI = new Matrix4x4();

      m_pointList = new List<Vector3>(12);

      m_frustumCorners = new Vector3[8];
    }

    public bool IsDouble => false;

    public bool IsFlipped { get; set; }

    public bool TightFit { get; set; }

    /// <summary>
    ///   Update the projection data for this camera.
    ///   If this is the scene view camera you may not want to project
    ///   the grid from its point of view but instead from the main
    ///   cameras view so you can see how the mesh is being projected.
    /// </summary>
    public void UpdateProjection(Camera camera, CameraData data)
    {
      var cam = camera;

      if (cam == null || data == null) return;

      if (data.projection == null)
        data.projection = new ProjectionData();

      if (data.projection.IsViewUpdated(camera)) return;

      //Used to debug projection.
      if (Ocean.DISABLE_PROJECT_SCENE_VIEW && cam.name == "SceneCamera" && Camera.main != null)
        cam = Camera.main;

      //Aim the projector given the current camera position.
      //Find the most practical but visually pleasing projection.
      //Sets the m_projectorV and m_projectorP matrices.
      AimProjector(cam);

      //Create a view projection matrix.
      var projectorVP = m_projectorP * m_projectorV;

      //Create the m_projectorR matrix. 
      //Finds the range the projection must fit 
      //for the projected grid to cover the screen.
      CreateRangeMatrix(cam, projectorVP);
      //Create the inverse view projection range matrix.
      var IVP = projectorVP.inverse * m_projectorR;

      //Set the interpolation points based on IVP matrix.
      m_projectorI.SetRow(0, HProject(IVP, m_quad[0]));
      m_projectorI.SetRow(1, HProject(IVP, m_quad[1]));
      m_projectorI.SetRow(2, HProject(IVP, m_quad[2]));
      m_projectorI.SetRow(3, HProject(IVP, m_quad[3]));

      //Save a copy of the view projection range matrix and the interpolation matrix.
      data.projection.projectorVP = m_projectorR.inverse * projectorVP;
      data.projection.interpolation = m_projectorI;

      data.projection.SetViewAsUpdated(camera);
    }

    /// <summary>
    ///   The view matrix from the camera can not be used for the projection
    ///   because it is possible to be in a invalid direction where the
    ///   projection math does not work.
    ///   Instead create a new view matrix that re-aims the view to a
    ///   direction that is always valid. I found a simple view looking down
    ///   and a bit forward works best.
    /// </summary>
    private void AimProjector(Camera cam)
    {
      //Copy camera projection into projector projection
      m_projectorP = cam.projectionMatrix;

      var pos = cam.transform.position;
      var dir = cam.transform.forward;
      var lookAt = new Vector3();

      var level = m_ocean.level;

      var fit = TightFit ? 20.0f : 50.0f;

      var range = Math.Max(0.0f, m_ocean.FindMaxDisplacement(true)) + fit;

      if (Ocean.DISABLE_PROJECTION_FLIPPING)
      {
        //If flipping disabled keep the projection pos above the surface.
        //Make sure projection position is above the wave range.
        if (pos.y < level) pos.y = level;

        IsFlipped = false;
        pos.y = Math.Max(pos.y, level + range);
      }
      else
      {
        //If the camera is below the sea level then flip the projection.
        //Make sure projection position is above or below the wave range.
        if (pos.y < level)
        {
          IsFlipped = true;
          pos.y = Math.Min(pos.y, level - range);
        }
        else
        {
          IsFlipped = false;
          pos.y = Math.Max(pos.y, level + range);
        }
      }

      //Look a bit in front of view.
      lookAt = pos + dir * 50.0f;
      lookAt.y = m_ocean.level;

      LookAt(pos, lookAt, Vector3.up);
    }

    /// <summary>
    ///   Creates the range conversion matrix. The grid is projected
    ///   onto a plane but the ocean waves are not flat. They occupy
    ///   a min/max range. This must be accounted for or else the grid
    ///   will pull away from the screen. The range matrix will then modify
    ///   the projection matrix so that the projected grid always covers
    ///   the whole screen. This currently only takes the y displacement into
    ///   account, not the xz displacement. The xz is handled by another method.
    /// </summary>
    private void CreateRangeMatrix(Camera cam, Matrix4x4 projectorVP)
    {
      m_pointList.Clear();

      var V = cam.worldToCameraMatrix;

      //The inverse view projection matrix will transform
      //screen space verts to world space.
      var IVP = (m_projectorP * V).inverse;

      var UP = Vector3.up;
      var p = Vector4.zero;

      var level = m_ocean.level;
      var range = Mathf.Max(1.0f, m_ocean.FindMaxDisplacement(true));

      //Convert each screen vert to world space.
      for (var i = 0; i < 8; i++)
      {
        p = IVP * m_corners[i];
        //p /= p.w;
        p.x /= p.w;
        p.y /= p.w;
        p.z /= p.w;

        m_frustumCorners[i] = p;
      }

      //For each corner if its world space position is  
      //between the wave range then add it to the list.
      for (var i = 0; i < 8; i++)
        if (m_frustumCorners[i].y <= level + range && m_frustumCorners[i].y >= level - range)
          m_pointList.Add(m_frustumCorners[i]);

      //Now take each segment in the frustum box and check
      //to see if it intersects the ocean plane on both the
      //upper and lower ranges.
      for (var i = 0; i < 12; i++)
      {
        var p0 = m_frustumCorners[m_indices[i, 0]];
        var p1 = m_frustumCorners[m_indices[i, 1]];

        var max = new Vector3();
        var min = new Vector3();

        if (SegmentPlaneIntersection(p0, p1, UP, level + range, ref max)) m_pointList.Add(max);

        if (SegmentPlaneIntersection(p0, p1, UP, level - range, ref min)) m_pointList.Add(min);
      }

      var count = m_pointList.Count;

      //If list is empty the ocean can not be seen.
      if (count == 0)
      {
        m_projectorR[0, 0] = 1;
        m_projectorR[0, 3] = 0;
        m_projectorR[1, 1] = 1;
        m_projectorR[1, 3] = 0;
        return;
      }

      var xmin = float.PositiveInfinity;
      var ymin = float.PositiveInfinity;
      var xmax = float.NegativeInfinity;
      var ymax = float.NegativeInfinity;
      var q = Vector4.zero;

      //Now convert each world space position into
      //projector screen space. The min/max x/y values
      //are then used for the range conversion matrix.
      for (var i = 0; i < count; i++)
      {
        q.x = m_pointList[i].x;
        q.y = level;
        q.z = m_pointList[i].z;
        q.w = 1.0f;

        p = projectorVP * q;
        //p /= p.w;
        p.x /= p.w;
        p.y /= p.w;

        if (p.x < xmin) xmin = p.x;
        if (p.y < ymin) ymin = p.y;
        if (p.x > xmax) xmax = p.x;
        if (p.y > ymax) ymax = p.y;
      }

      //Create the range conversion matrix and return it.

      m_projectorR[0, 0] = xmax - xmin;
      m_projectorR[0, 3] = xmin;
      m_projectorR[1, 1] = ymax - ymin;
      m_projectorR[1, 3] = ymin;
    }

    /// <summary>
    ///   Project the corner point from projector space into
    ///   world space and find the intersection of the segment
    ///   with the ocean plane in homogeneous space.
    ///   The intersection is this grids corner's world pos.
    ///   This is done in homogeneous space so the corners
    ///   can be interpolated in the vert shader for the rest of
    ///   the points. Homogeneous space is the world space but
    ///   in 4D where the w value is the position on the infinity plane.
    /// </summary>
    private Vector4 HProject(Matrix4x4 ivp, Vector4 corner)
    {
      Vector4 a, b;

      corner.z = -1;
      a = ivp * corner;

      corner.z = 1;
      b = ivp * corner;

      var h = m_ocean.level;

      var ab = b - a;

      var t = (a.w * h - a.y) / (ab.y - ab.w * h);

      return a + ab * t;
    }

    /// <summary>
    ///   Find the intersection point of a plane and segment in world space.
    /// </summary>
    private bool SegmentPlaneIntersection(Vector3 a, Vector3 b, Vector3 n, float d, ref Vector3 q)
    {
      var ab = b - a;
      var t = (d - Vector3.Dot(n, a)) / Vector3.Dot(n, ab);

      if (t > -0.0f && t <= 1.0f)
      {
        q = a + t * ab;
        return true;
      }

      return false;
    }

    /// <summary>
    ///   Same as Unity's transform look.
    /// </summary>
    public void LookAt(Vector3 position, Vector3 target, Vector3 up)
    {
      var zaxis = (position - target).normalized;
      var xaxis = Vector3.Cross(up, zaxis).normalized;
      var yaxis = Vector3.Cross(zaxis, xaxis);

      m_projectorV[0, 0] = xaxis.x;
      m_projectorV[0, 1] = xaxis.y;
      m_projectorV[0, 2] = xaxis.z;
      m_projectorV[0, 3] = -Vector3.Dot(xaxis, position);

      m_projectorV[1, 0] = yaxis.x;
      m_projectorV[1, 1] = yaxis.y;
      m_projectorV[1, 2] = yaxis.z;
      m_projectorV[1, 3] = -Vector3.Dot(yaxis, position);

      m_projectorV[2, 0] = zaxis.x;
      m_projectorV[2, 1] = zaxis.y;
      m_projectorV[2, 2] = zaxis.z;
      m_projectorV[2, 3] = -Vector3.Dot(zaxis, position);

      m_projectorV[3, 0] = 0;
      m_projectorV[3, 1] = 0;
      m_projectorV[3, 2] = 0;
      m_projectorV[3, 3] = 1;

      //Must flip to match Unity's winding order.
      m_projectorV[0, 0] *= -1.0f;
      m_projectorV[0, 1] *= -1.0f;
      m_projectorV[0, 2] *= -1.0f;
      m_projectorV[0, 3] *= -1.0f;
    }
  }
}