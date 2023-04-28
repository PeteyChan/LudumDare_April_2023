using Godot;
using System;
using System.Collections.Generic;

public static partial class GameExtensions
{
    partial class EventNode : Godot.Node
    {
        public EventNode() => Name = "CustomEvents";
        public Action<float> OnUpdate;
        public Action<float> OnPhysics;
        public Action OnDestroy;
        public Action<long> OnNotification;
        public override void _Notification(int what)
        {
            switch ((long)what)
            {
                case NotificationProcess:
                    OnUpdate?.Invoke((float)GetProcessDeltaTime());
                    break;

                case NotificationPhysicsProcess:
                    OnUpdate?.Invoke((float)GetPhysicsProcessDeltaTime());
                    break;

                case NotificationPredelete:
                    OnDestroy?.Invoke();
                    break;

                default:
                    OnNotification?.Invoke(what);
                    break;

            }
        }
    }

    public static T OnDestroy<T>(this T node, Action callback) where T : Godot.Node
    {
        if (!node.HasChild(out EventNode event_node))
            node.AddChild(event_node = new EventNode { });
        event_node.OnDestroy += callback;
        return node;
    }

    public static T OnPhysics<T>(this T node, Action callback) where T : Godot.Node
        => OnPhysics(node, delta => callback());

    public static T OnPhysics<T>(this T node, Action<float> callback) where T : Godot.Node
    {
        if (!node.TryFind(out EventNode event_node))
            node.AddChild(event_node = new EventNode { });
        event_node.OnPhysics += callback;
        event_node.SetPhysicsProcess(true);
        return node;
    }

    public static T OnUpdate<T>(this T node, Action callback) where T : Godot.Node
        => OnUpdate(node, delta => callback());

    public static T OnUpdate<T>(this T node, Action<float> update_method) where T : Godot.Node
    {
        if (!node.HasChild(out EventNode event_node))
            node.AddChild(event_node = new EventNode { });
        event_node.OnUpdate += update_method;
        event_node.SetProcess(true);
        return node;
    }

    public static T OnUpdate<T>(this T node, Action<T, float> update_method) where T : Godot.Node
        => OnUpdate(node, delta => update_method(node, delta));

    public static T OnNofication<T>(this T node, Action<long> callback) where T : Godot.Node
    {
        if (!node.HasChild(out EventNode event_node))
            node.AddChild(event_node = new EventNode());
        event_node.OnNotification += callback;
        return node;
    }

    public static T OnEnterTree<T>(this T node, Action callback) where T : Godot.Node
    {
        if (node.IsValid()) node.TreeEntered += callback;
        return node;
    }

    public static T OnExitTree<T>(this T node, Action callback) where T : Godot.Node
    {
        if (node.IsValid()) node.TreeExiting += callback;
        return node;
    }

    public static T AddToScene<T>(this T node, bool deffered = false) where T : Godot.Node
    {
        if (deffered) Scene.Current.CallDeferred("add_child", node);
        else Scene.Current.AddChild(node);
        return node;
    }

    public static T UnParent<T>(this T node) where T : Godot.Node
    {
        if (node?.GetParent() != null)
            node.GetParent().RemoveChild(node);
        return node;
    }

    public static void DestroyNode(this Godot.Node node)
    {
        if (Godot.Node.IsInstanceValid(node) && !node.IsQueuedForDeletion())
        {
            //if (node.IsInsideTree()) node.UnParent();
            node.QueueFree();
        }
    }

    public static T OnButtonDown<T>(this T button, Action<Godot.Button> action) where T : Godot.Button
        => OnButtonDown(button, () => action(button));

    public static T OnButtonDown<T>(this T button, Action action) where T : Godot.Button
    {
        button.Pressed += action;
        return button;
    }
}


public static partial class GodotExtensions
{
    public static T AddChild<T>(this T node, string resource_path) where T : Godot.Node
    {
        node.AddChild(GD.Load<PackedScene>(resource_path).Instantiate());
        return node;
    }

    public static T AddChild<T>(this T node, string resource_path, out Godot.Node child) where T : Godot.Node
    {
        child = GD.Load<PackedScene>(resource_path).Instantiate();
        node.AddChild(child);
        return node;
    }

    public static T AddChildDeffered<T>(this T node, Godot.Node child_node) where T : Godot.Node
    {
        if (!child_node.IsValid()) throw new Exception($"child node is invalid, cannot add set parent node {node}");
        if (!node.IsValid()) throw new Exception($"parent node is invalid, cannot be add child node {child_node}");
        node.CallDeferred("add_child", child_node);
        return node;
    }

    public static T SetParentDeffered<T>(this T node, Godot.Node parent_node) where T : Godot.Node
    {
        parent_node.AddChildDeffered(node);
        return node;
    }

    public static bool IsValid(this Godot.GodotObject node) => Godot.GodotObject.IsInstanceValid(node);
    public static T SetParent<T>(this T node, Godot.Node parent_node) where T : Godot.Node
    {
        parent_node.AddChild(node);
        return node;
    }

    public static bool TryFindParent<T>(this Godot.Node node, out T value) where T : class
    {
        if (node.IsValid())
        {
            if (node.GetParent() is T target)
            {
                value = target;
                return true;
            }
            return node.GetParent().TryFindParent(out value);
        }
        value = default;
        return false;
    }

    /// <summary>
    /// returns the first node of type in heirarchy that mataches the predicate 
    /// </summary>
    public static bool TryFind<T>(this Godot.Node node, out T target, Func<T, bool> predicate)
    {
        target = default;
        if (!node.IsValid()) return false;
        if (node is T value && predicate.Invoke(value))
        {
            target = value;
            return true;
        }

        foreach (var child in node.GetChildren())
            if (TryFind(child, out target, predicate))
                return true;
        return false;
    }

    /// <summary>
    /// Tries to find the first instance of T in node heirarchy starting from node
    /// </summary>
    public static bool TryFind<T>(this Godot.Node node, out T target) where T : class
    {
        target = default;
        if (!node.IsValid()) return false;
        if (node is T value)
        {
            target = value;
            return true;
        }

        foreach (var child in node.GetChildren())
            if (TryFind(child, out target))
                return true;
        return false;
    }

    /// <summary>
    /// returns true if node has a direct child of type
    /// </summary>
    public static bool HasChild<T>(this Godot.Node node, out T value) where T : class
    {
        if (node.IsValid())
            foreach (var child in node.GetChildren())
                if (child is T target)
                {
                    value = target;
                    return true;
                }
        value = default;
        return false;
    }


    public static bool HasFocusInHeirarchy(this Godot.Control node)
    {
        if (node.HasFocus()) return true;
        foreach (var child in node.GetChildren(true))
            if (child is Control control && control.HasFocusInHeirarchy())
                return true;
        return false;
    }


    /// <summary>
    /// Finds all instances of T in node heirarchy
    /// </summary>
    public static List<T> FindAll<T>(this Godot.Node node)
    {
        List<T> items = new List<T>();
        if (!node.IsValid()) return items;

        if (node is T target_value)
            items.Add(target_value);

        FindAll(node.GetChildren());
        return items;

        void FindAll(IEnumerable<Godot.Node> options)
        {
            foreach (var item in options)
            {
                if (item is T target)
                    items.Add(target);
                FindAll(item.GetChildren());
            }
        }
    }

    public static Vector2I ToVec2i(this Vector2 vec2) => new Vector2I(Mathf.RoundToInt(vec2.X), Mathf.RoundToInt(vec2.Y));
    public static Vector2 ToVec2(this Vector2I vec2) => new Vector2(vec2.X, vec2.Y);

    public static Godot.Vector3 GetForward(this Godot.Transform3D t) => t.Basis.Z;
    public static Godot.Vector3 GetBack(this Godot.Transform3D t) => -t.Basis.Z;
    public static Godot.Vector3 GetRight(this Godot.Transform3D t) => t.Basis.X;
    public static Godot.Vector3 GetLeft(this Godot.Transform3D t) => -t.Basis.X;
    public static Godot.Vector3 GetUp(this Godot.Transform3D t) => t.Basis.Y;
    public static Godot.Vector3 GetDown(this Godot.Transform3D t) => -t.Basis.Y;
    public static Godot.Vector3 GetRelativeInputDirection(this Godot.Transform3D t, float x, float y)
    {
        var forward = t.Basis.Z;
        var right = t.Basis.X;
        forward.Y = 0;
        right.Y = 0;
        forward = forward.Normalized();
        right = right.Normalized();
        forward *= y;
        right *= x;
        var target = forward * right;
        return (forward + right).Normalized();
    }

    public static Godot.AnimationPlayer SetLoop(this Godot.AnimationPlayer animator, string animation, bool loop = true)
    {
        animator.GetAnimation(animation).LoopMode = Animation.LoopModeEnum.Linear;
        return animator;
    }

    public static float GetTilt(this Godot.Vector2 vec2)
    {
        var val = Godot.Mathf.Sqrt(vec2.X * vec2.X + vec2.Y * vec2.Y);
        return val > 1 ? 1 : val;
    }

    public static Transform3D Lerp(this Transform3D transform, Transform3D target, float weight)
        => transform.InterpolateWith(target, weight);

    public static float Lerp(this float value, float target, float weight)
        => weight <= 0 ? value : weight >= 1 ? target : value + weight * (target - value);

    public static double Lerp(this double value, double target, double weight)
        => weight <= 0 ? value : weight >= 1 ? target : value + weight * (target - value);
}


