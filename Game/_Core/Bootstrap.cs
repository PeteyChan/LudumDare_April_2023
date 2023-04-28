using Godot;
using System;
using System.Reflection;
using System.Collections.Generic;

/// <summary>
/// marks classes that should be auto loaded at startup
/// </summary>
public class Bootstrap : Attribute
{
    public struct Process { public Process(float delta) => this.delta = delta; public readonly float delta; }
    public struct Physics { public Physics(float delta) => this.delta = delta; public readonly float delta; }
    public struct Ready { }
    public static float seconds_since_bootstrap => (float)Internal.Bootstrap.timer.Elapsed.TotalSeconds;
}

namespace Internal
{
    public partial class Bootstrap : CanvasLayer
    {
        public Bootstrap()
        {
            Node = (!Node.IsValid()) ? this : throw new Exception("Cannot bootstrap application more than ocnce");
        }

        public void Quit()
        {
            this.GetTree().Root.PropagateNotification((int)Godot.Node.NotificationWMCloseRequest);
            this.GetTree().Quit();
        }

        public static Viewport Viewport { get; private set; }
        public static System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        public static Bootstrap Node { get; private set; }
        Dictionary<Type, object> all = new Dictionary<Type, object>();
        List<Action<T>> GetCallbacks<T>()
        {
            if (!all.TryGetValue(typeof(T), out var list))
            {
                var values = new List<Action<T>>();
                all[typeof(T)] = list = values;

                foreach (var type in typeof(global::Bootstrap).Assembly.GetTypes())
                {
                    if (type.IsGenericType) continue;
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        var param = method.GetParameters();
                        if (param.Length != 1) continue;

                        if (param[0].ParameterType == typeof(T))
                            values.Add(method.CreateDelegate(typeof(Action<T>)) as Action<T>);
                    }
                }
            }
            return (List<Action<T>>)list;
        }

        public override void _Ready()
        {
            Viewport = GetViewport();
            Layer = int.MaxValue;

            var items = GetCallbacks<global::Bootstrap.Ready>();
            for (int i = 0; i < items.Count; ++i)
                items[i].Invoke(default);
        }

        public override void _Process(double delta)
        {
            var delta_time = new global::Bootstrap.Process((float)delta);
            var items = GetCallbacks<global::Bootstrap.Process>();
            for (int i = 0; i < items.Count; ++i)
                items[i].Invoke(delta_time);
        }

        public override void _PhysicsProcess(double delta)
        {
            var delta_time = new global::Bootstrap.Physics((float)delta);
            var items = GetCallbacks<global::Bootstrap.Physics>();
            for (int i = 0; i < items.Count; ++i)
                items[i].Invoke(delta_time);
        }
    }
}
