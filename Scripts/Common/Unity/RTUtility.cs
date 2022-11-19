using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Razomy.Unity.Scripts.Common.Unity
{
  public class RTSettings
  {
    public int ansioLevel = 1;
    public int depth = 0;
    public List<RenderTextureFormat> fallbackFormats = new();
    public FilterMode filer = FilterMode.Bilinear;
    public RenderTextureFormat format = RenderTextureFormat.ARGB32;
    public int height = 1;
    public bool mipmaps = false;

    public string name = "";
    public bool randomWrite = false;
    public RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
    public int width = 1;
    public TextureWrapMode wrap = TextureWrapMode.Clamp;
  }

  public static class RTUtility
  {
    public static void Blit(RenderTexture des, Material mat, int pass = 0)
    {
      //RenderTexture oldRT = RenderTexture.active;

      Graphics.SetRenderTarget(des);

      GL.PushMatrix();
      GL.LoadOrtho();

      mat.SetPass(pass);

      GL.Begin(GL.QUADS);
      GL.TexCoord2(0.0f, 0.0f);
      GL.Vertex3(0.0f, 0.0f, 0.1f);
      GL.TexCoord2(1.0f, 0.0f);
      GL.Vertex3(1.0f, 0.0f, 0.1f);
      GL.TexCoord2(1.0f, 1.0f);
      GL.Vertex3(1.0f, 1.0f, 0.1f);
      GL.TexCoord2(0.0f, 1.0f);
      GL.Vertex3(0.0f, 1.0f, 0.1f);
      GL.End();

      GL.PopMatrix();

      //RenderTexture.active = oldRT;
    }

    public static void Blit(RenderTexture des, Material mat, Vector3[] verts, int pass = 0)
    {
      //RenderTexture oldRT = RenderTexture.active;

      Graphics.SetRenderTarget(des);

      GL.PushMatrix();
      GL.LoadOrtho();

      mat.SetPass(pass);

      GL.Begin(GL.QUADS);
      GL.TexCoord2(0.0f, 0.0f);
      GL.Vertex(verts[0]);
      GL.TexCoord2(1.0f, 0.0f);
      GL.Vertex(verts[1]);
      GL.TexCoord2(1.0f, 1.0f);
      GL.Vertex(verts[2]);
      GL.TexCoord2(0.0f, 1.0f);
      GL.Vertex(verts[3]);
      GL.End();

      GL.PopMatrix();

      //RenderTexture.active = oldRT;
    }

    public static void Blit(RenderTexture des, Material mat, Vector3[] verts, Vector2[] uvs, int pass = 0)
    {
      //RenderTexture oldRT = RenderTexture.active;

      Graphics.SetRenderTarget(des);

      GL.PushMatrix();
      GL.LoadOrtho();

      mat.SetPass(pass);

      GL.Begin(GL.QUADS);
      GL.TexCoord(uvs[0]);
      GL.Vertex(verts[0]);
      GL.TexCoord(uvs[1]);
      GL.Vertex(verts[1]);
      GL.TexCoord(uvs[2]);
      GL.Vertex(verts[2]);
      GL.TexCoord(uvs[3]);
      GL.Vertex(verts[3]);
      GL.End();

      GL.PopMatrix();

      //RenderTexture.active = oldRT;
    }

    public static void MultiTargetBlit(IList<RenderTexture> des, Material mat, int pass = 0)
    {
      //RenderTexture oldRT = RenderTexture.active;

      var rb = new RenderBuffer[des.Count];

      for (var i = 0; i < des.Count; i++)
        rb[i] = des[i].colorBuffer;

      Graphics.SetRenderTarget(rb, des[0].depthBuffer);

      GL.PushMatrix();
      GL.LoadOrtho();

      mat.SetPass(pass);

      GL.Begin(GL.QUADS);
      GL.TexCoord2(0.0f, 0.0f);
      GL.Vertex3(0.0f, 0.0f, 0.1f);
      GL.TexCoord2(1.0f, 0.0f);
      GL.Vertex3(1.0f, 0.0f, 0.1f);
      GL.TexCoord2(1.0f, 1.0f);
      GL.Vertex3(1.0f, 1.0f, 0.1f);
      GL.TexCoord2(0.0f, 1.0f);
      GL.Vertex3(0.0f, 1.0f, 0.1f);
      GL.End();

      GL.PopMatrix();

      //RenderTexture.active = oldRT;
    }

    public static void MultiTargetBlit(RenderBuffer[] des_rb, RenderBuffer des_db, Material mat, int pass = 0)
    {
      //RenderTexture oldRT = RenderTexture.active;

      Graphics.SetRenderTarget(des_rb, des_db);

      GL.PushMatrix();
      GL.LoadOrtho();

      mat.SetPass(pass);

      GL.Begin(GL.QUADS);
      GL.TexCoord2(0.0f, 0.0f);
      GL.Vertex3(0.0f, 0.0f, 0.1f);
      GL.TexCoord2(1.0f, 0.0f);
      GL.Vertex3(1.0f, 0.0f, 0.1f);
      GL.TexCoord2(1.0f, 1.0f);
      GL.Vertex3(1.0f, 1.0f, 0.1f);
      GL.TexCoord2(0.0f, 1.0f);
      GL.Vertex3(0.0f, 1.0f, 0.1f);
      GL.End();

      GL.PopMatrix();

      //RenderTexture.active = oldRT;
    }

    public static void ClearColor(RenderTexture tex, Color col)
    {
      if (tex == null) return;

      //RenderTexture oldRT = RenderTexture.active;

      if (!SystemInfo.SupportsRenderTextureFormat(tex.format)) return;

      Graphics.SetRenderTarget(tex);
      GL.Clear(false, true, col);

      //RenderTexture.active = oldRT;
    }

    public static void Release(RenderTexture tex)
    {
      if (tex == null) return;
      tex.Release();
    }

    public static void Release(IList<RenderTexture> texList)
    {
      if (texList == null) return;

      var count = texList.Count;
      for (var i = 0; i < count; i++)
      {
        if (texList[i] == null) continue;
        texList[i].Release();
      }
    }

    public static void ReleaseAndDestroy(RenderTexture tex)
    {
      if (tex == null) return;
      tex.Release();
      Object.Destroy(tex);
    }

    public static void ReleaseAndDestroy(IList<RenderTexture> texList)
    {
      if (texList == null) return;

      var count = texList.Count;
      for (var i = 0; i < count; i++)
      {
        if (texList[i] == null) continue;
        texList[i].Release();
        Object.Destroy(texList[i]);
      }
    }

    public static void ReleaseTemporary(RenderTexture tex)
    {
      if (tex == null) return;
      RenderTexture.ReleaseTemporary(tex);
    }

    public static void ReleaseTemporary(IList<RenderTexture> texList)
    {
      if (texList == null) return;

      var count = texList.Count;
      for (var i = 0; i < count; i++)
      {
        if (texList[i] == null) continue;
        RenderTexture.ReleaseTemporary(texList[i]);
      }
    }

    private static RenderTextureFormat CheckFormat(RTSettings setting)
    {
      var format = setting.format;

      if (!SystemInfo.SupportsRenderTextureFormat(format))
      {
        Debug.Log("System does not support " + format + " render texture format.");

        var foundFallback = false;
        var count = setting.fallbackFormats.Count;
        for (var i = 0; i < count; i++)
          if (SystemInfo.SupportsRenderTextureFormat(setting.fallbackFormats[i]))
          {
            format = setting.fallbackFormats[i];
            Debug.Log("Found fallback format: " + format);
            foundFallback = true;
            break;
          }

        if (!foundFallback) throw new InvalidOperationException("Could not find fallback render texture format");
      }

      return format;
    }

    public static RenderTexture CreateRenderTexture(RTSettings setting)
    {
      if (setting == null)
        throw new NullReferenceException("RTSettings is null");

      var format = CheckFormat(setting);

      var tex = new RenderTexture(setting.width, setting.height, setting.depth, format, setting.readWrite);

      tex.name = setting.name;
      tex.wrapMode = setting.wrap;
      tex.filterMode = setting.filer;
      tex.useMipMap = setting.mipmaps;
      tex.anisoLevel = setting.ansioLevel;
      tex.enableRandomWrite = setting.randomWrite;

      return tex;
    }

    public static RenderTexture CreateTemporyRenderTexture(RTSettings setting)
    {
      if (setting == null)
        throw new NullReferenceException("RTSettings is null");

      var format = CheckFormat(setting);

      var tex = RenderTexture.GetTemporary(setting.width, setting.height, setting.depth, format, setting.readWrite);

      tex.name = setting.name;
      tex.wrapMode = setting.wrap;
      tex.filterMode = setting.filer;
      //tex.useMipMap = setting.mipmaps;
      tex.anisoLevel = setting.ansioLevel;
      //tex.enableRandomWrite = setting.randomWrite;

      return tex;
    }
  }
}