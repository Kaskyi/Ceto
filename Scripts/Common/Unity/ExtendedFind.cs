using System.Collections.Generic;
using UnityEngine;

namespace Ceto.Common.Unity.Utility
{
  /// <summary>
  ///   Extended find methods for getting components and interfaces from game objects.
  /// </summary>
  public static class ExtendedFind
  {
    /// <summary>
    ///   Get the first interface found attached to the gameobject
    ///   of type T and return it. Return null if none found.
    /// </summary>
    public static T GetInterface<T>(GameObject obj) where T : class
    {
      var components = obj.GetComponents<Component>();

      foreach (var c in components)
        if (c is T)
          return c as T;

      return null;
    }

    /// <summary>
    ///   Get the first interface found attached to a child of the gameobject
    ///   of type T and return it. Return null if none found.
    /// </summary>
    public static T GetInterfaceInChildren<T>(GameObject obj) where T : class
    {
      var components = obj.GetComponentsInChildren<Component>();

      foreach (var c in components)
        if (c is T)
          return c as T;

      return null;
    }

    /// <summary>
    ///   Get the first interface found attached to a immediate child of gameobject
    ///   of type T and return it. Return null if none found.
    /// </summary>
    public static T GetInterfaceImmediateChildren<T>(GameObject obj) where T : class
    {
      foreach (Transform child in obj.transform)
      {
        var components = child.GetComponents<Component>();

        foreach (var c in components)
          if (c is T)
            return c as T;
      }

      return null;
    }

    /// <summary>
    ///   Get all interfaces attached to gameobject of type T and return them.
    /// </summary>
    public static T[] GetInterfaces<T>(GameObject obj) where T : class
    {
      var components = obj.GetComponents<Component>();

      var list = new List<T>();

      foreach (var c in components)
        if (c is T)
          list.Add(c as T);

      return list.ToArray();
    }

    /// <summary>
    ///   Get all the interfaces attached to any of the children of gameobject
    ///   of type T and return them.
    /// </summary>
    public static T[] GetInterfacesInChildren<T>(GameObject obj) where T : class
    {
      var components = obj.GetComponentsInChildren<Component>();

      var list = new List<T>();

      foreach (var c in components)
        if (c is T)
          list.Add(c as T);

      return list.ToArray();
    }

    /// <summary>
    ///   Get all the interfaces attached to any of the immediate children of
    ///   gameobject of type T and return them.
    /// </summary>
    public static T[] GetInterfacesImmediateChildren<T>(GameObject obj) where T : class
    {
      var list = new List<T>();

      foreach (Transform child in obj.transform)
      {
        var components = child.GetComponents<Component>();

        foreach (var c in components)
          if (c is T)
            list.Add(c as T);
      }

      return list.ToArray();
    }

    /// <summary>
    ///   Get the first component found in the immediate parent of gameobject
    ///   of type T. Return null if none found.
    /// </summary>
    public static T GetComponetInImmediateParent<T>(GameObject obj) where T : Component
    {
      if (obj.transform.parent == null) return null;

      return obj.transform.parent.GetComponent<T>();
    }

    /// <summary>
    ///   Get all the components found in the immediate parent of gameobject of type T.
    /// </summary>
    public static T[] GetComponentsInImmediateParent<T>(GameObject obj) where T : Component
    {
      if (obj.transform.parent == null) return new T[0];

      return obj.transform.parent.GetComponents<T>();
    }

    /// <summary>
    ///   Get the first component found in the immediate children of gameobject
    ///   of type T. Return null if none found.
    /// </summary>
    public static T GetComponetInImmediateChildren<T>(GameObject obj) where T : Component
    {
      foreach (Transform child in obj.transform)
      {
        var component = child.GetComponent<T>();

        if (component != null) return component;
      }

      return null;
    }

    /// <summary>
    ///   Get all the components found in the immediate children of gameobject of type T.
    /// </summary>
    public static T[] GetComponetsInImmediateChildren<T>(GameObject obj) where T : Component
    {
      var list = new List<T>();

      foreach (Transform child in obj.transform)
      {
        var components = child.GetComponents<T>();

        foreach (var c in components) list.Add(c);
      }

      return list.ToArray();
    }

    /// <summary>
    ///   Returns the a component of type T on a named game object.
    ///   Returns null if no component found or no game object
    ///   called name exists.
    /// </summary>
    public static T FindComponentOnGameObject<T>(string name) where T : Component
    {
      var go = GameObject.Find(name);

      if (go == null) return null;

      return go.GetComponent<T>();
    }

    /// <summary>
    ///   Returns the all components of type T on a named game object.
    ///   Returns empty array if no component found or no game object
    ///   called name exists.
    /// </summary>
    public static T[] FindComponentsOnGameObject<T>(string name) where T : Component
    {
      var go = GameObject.Find(name);

      if (go == null) return new T[0];

      return go.GetComponents<T>();
    }

    /// <summary>
    ///   Returns the a interface of type T on a named game object.
    ///   Returns null if no interface found or no game object
    ///   called name exists.
    /// </summary>
    public static T FindInterfaceOnGameObject<T>(string name) where T : class
    {
      var go = GameObject.Find(name);

      if (go == null) return null;

      return GetInterface<T>(go);
    }

    /// <summary>
    ///   Returns the all interfaces of type T on a named game object.
    ///   Returns empty array if no interfaces found or no game object
    ///   called name exists.
    /// </summary>
    public static T[] FindInterfacesOnGameObject<T>(string name) where T : class
    {
      var go = GameObject.Find(name);

      if (go == null) return new T[0];

      return GetInterfaces<T>(go);
    }
  }
}