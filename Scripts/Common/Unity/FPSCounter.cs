using UnityEngine;

namespace Razomy.Unity.Scripts.Common.Unity
{
  public class FPSCounter : MonoBehaviour
  {
    private readonly float updateInterval = 0.5f;
    private float accum; // FPS accumulated over the interval
    private float frames; // Frames drawn over the interval
    private float timeleft; // Left time for current interval

    public float FrameRate { get; set; }

    private void Start()
    {
      timeleft = updateInterval;
    }

    private void Update()
    {
      timeleft -= Time.deltaTime;
      accum += Time.timeScale / Time.deltaTime;
      ++frames;

      if (timeleft <= 0.0f)
      {
        FrameRate = accum / frames;
        timeleft = updateInterval;
        accum = 0;
        frames = 0;
      }
    }
  }
}