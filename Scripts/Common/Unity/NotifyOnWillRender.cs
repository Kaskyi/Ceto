﻿namespace Razomy.Unity.Scripts.Common.Unity
{
  /// <summary>
  ///   Allows a list of functions to be added to a gameobject.
  ///   When the object gets rendered each function is called.
  ///   Allows for some custom code to run before rendering.
  /// </summary>
  public class NotifyOnWillRender : NotifyOnEvent
  {
    /// <summary>
    ///   Called when this gameobject gets rendered.
    /// </summary>
    private void OnWillRenderObject()
    {
      OnEvent();
    }
  }
}