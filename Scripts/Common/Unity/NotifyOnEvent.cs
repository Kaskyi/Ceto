using System;
using System.Collections.Generic;
using UnityEngine;

namespace Razomy.Unity.Scripts.Common.Unity
{
  /// <summary>
  ///   Allows a list of functions to be added to a gameobject.
  ///   When some event occurs each function is called.
  ///   Allows for some custom code to run before event.
  /// </summary>
  public abstract class NotifyOnEvent : MonoBehaviour
  {
    /// <summary>
    ///   Globally disable/enable the notification.
    ///   Used to prevent a recursive notifications
    ///   from happening.
    /// </summary>
    public static bool Disable;

    /// <summary>
    ///   The list of functions that will be called.
    /// </summary>
    private readonly IList<INotify> m_actions = new List<INotify>();

    /// <summary>
    ///   Call to execute actions.
    /// </summary>
    protected void OnEvent()
    {
      if (Disable) return;

      var count = m_actions.Count;
      for (var i = 0; i < count; i++)
      {
        var notify = m_actions[i];

        if (notify is Notify)
        {
          var n = notify as Notify;
          n.action(gameObject);
        }
        else if (notify is NotifyWithArg)
        {
          var n = notify as NotifyWithArg;
          n.action(gameObject, n.arg);
        }
      }
    }

    /// <summary>
    ///   Add a action with a argument.
    /// </summary>
    public void AddAction(Action<GameObject, object> action, object arg)
    {
      var notify = new NotifyWithArg();
      notify.action = action;
      notify.arg = arg;

      m_actions.Add(notify);
    }

    /// <summary>
    ///   Add a action with no argument.
    /// </summary>
    public void AddAction(Action<GameObject> action)
    {
      var notify = new Notify();
      notify.action = action;

      m_actions.Add(notify);
    }

    /// <summary>
    /// </summary>
    private interface INotify
    {}

    /// <summary>
    ///   Notification with a action.
    /// </summary>
    private class Notify : INotify
    {
      public Action<GameObject> action;
    }

    /// <summary>
    ///   Notification with a action and argument.
    /// </summary>
    private class NotifyWithArg : INotify
    {
      public Action<GameObject, object> action;
      public object arg;
    }
  }
}