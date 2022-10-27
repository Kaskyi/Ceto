using UnityEngine;

namespace Ceto
{
  public static class DrawCameraFrustum
  {
    private static readonly Vector4[] box =
    {
      new(-1, -1, -1, 1),
      new(-1, 1, -1, 1),
      new(1, 1, -1, 1),
      new(1, -1, -1, 1),

      new(-1, -1, 1, 1),
      new(-1, 1, 1, 1),
      new(1, 1, 1, 1),
      new(1, -1, 1, 1)
    };


    private static readonly int[,] indexs =
    {
      { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
      { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
      { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
    };

    public static void DrawFrustum(Matrix4x4 projectionView, Color col)
    {
      var positions = new Vector4[8];

      var IVP = projectionView.inverse;

      for (var i = 0; i < 8; i++)
      {
        positions[i] = IVP * box[i];
        positions[i] /= positions[i].w;
      }

      Gizmos.color = col;

      for (var i = 0; i < indexs.GetLength(0); i++)
        Gizmos.DrawLine(positions[indexs[i, 0]], positions[indexs[i, 1]]);
    }
  }
}