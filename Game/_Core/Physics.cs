using Godot;
using System.Collections.Generic;

public static class Physics
{
    public static class DebugColors
    {
        static DebugColors() => SetDefaults();

        public static Color Default, Hit, Contact, ContactNormal;
        public static void SetDefaults()
        {
            Hit = Colors.Red;
            Default = Colors.Green;
            Contact = Colors.DarkRed;
            ContactNormal = Colors.Cyan;
        }
    }

    static SceneTree tree = ((SceneTree)Godot.Engine.GetMainLoop());
    static Viewport viewport = tree.Root.GetViewport();
    static PhysicsDirectSpaceState3D space = viewport.World3D.DirectSpaceState;
    static Godot.Camera3D cam => viewport.GetCamera3D();
    static SphereShape3D sphere = new SphereShape3D();
    static PhysicsShapeQueryParameters2D shapeParams = new PhysicsShapeQueryParameters2D { };
    static PhysicsShapeQueryParameters3D volumeParams = new PhysicsShapeQueryParameters3D { Exclude = new Godot.Collections.Array<Rid>() };
    public static bool TryOverlapShape(PhysicsShapeQueryParameters3D parameters, List<Node> result_buffer, int max_results = 32)
    {
        result_buffer.Clear();
        var hits = space.IntersectShape(parameters, max_results);
        foreach (var item in hits)
        {
            result_buffer.Add((Node)item["collider"]);
        }
        return result_buffer.Count > 0;
    }

    public static bool TryOverlapSphere(Vector3 global_position, float radius, List<Node> result_buffer, int max_results = 32, uint mask = uint.MaxValue, bool body = true, bool area = false, bool debug = false, List<Godot.Node> exclude = default)
    {
        result_buffer.Clear();
        volumeParams.Exclude.Clear();
        if (exclude != null)
            foreach (var node in exclude)
                if (node is PhysicsBody3D pbody)
                    volumeParams.Exclude.Add(pbody.GetRid());

        sphere.Radius = radius;
        volumeParams.Shape = sphere;
        volumeParams.CollideWithBodies = body;
        volumeParams.CollideWithAreas = area;
        volumeParams.CollisionMask = mask;

        var transform = Transform3D.Identity;
        transform.Origin = global_position;
        volumeParams.Transform = transform;

        bool hit = TryOverlapShape(volumeParams, result_buffer, max_results);

        if (debug)
        {
            var color = result_buffer.Count > 0 ? DebugColors.Hit : DebugColors.Default;
            Debug.DrawCircle3D(global_position, radius, color);

            foreach (Node3D node in result_buffer)
            {
                var direction = node.GlobalTransform.Origin - global_position;
                if (direction.Length() > radius)
                    direction = direction.Normalized() * radius;

                Debug.DrawCircle3D(global_position + direction, .1f, DebugColors.Contact);
            }
        }
        return hit;
    }

    static bool TryShapeCast(Godot.PhysicsShapeQueryParameters3D parameters, out Result3D result, bool debug = false)
    {
        var original = parameters.Transform;
        var cast = space.CastMotion(parameters);
        bool hit = !(cast[0] == 1f && cast[1] == 1f);
        result = default;

        if (hit)
        {
            var transform = parameters.Transform;
            transform.Origin = transform.Origin + parameters.Motion * cast[1];
            parameters.Transform = transform;

            var info = space.GetRestInfo(parameters);
            hit = info.Count > 0;
            if (hit)
            {
                result.normal = (Vector3)info["normal"];
                result.point = (Vector3)info["point"];
                result.collider = Godot.GodotObject.InstanceFromId((ulong)info["collider_id"]) as Node;
            }
        }

        if (debug)
        {
            var color = hit ? DebugColors.Hit : DebugColors.Default;

            Debug.DrawArrow(original.Origin, parameters.Motion, color);

            var offset = original;
            offset.Origin = original.Origin + parameters.Motion * cast[1];

            switch (parameters.Shape)
            {
                case Godot.SphereShape3D sphere:
                    Debug.DrawCircle3D(original.Origin, cam.GlobalTransform.Basis.Z, sphere.Radius, DebugColors.Default);
                    Debug.DrawCircle3D(offset.Origin, cam.GlobalTransform.Basis.Z, sphere.Radius, color);
                    if (hit)
                    {
                        Debug.DrawLine3D(result.point, result.point + result.normal * sphere.Radius, DebugColors.ContactNormal);
                    }
                    break;

                case Godot.BoxShape3D box:
                    Debug.DrawCube(original, box.Size / 2f, DebugColors.Default);
                    Debug.DrawCube(offset, box.Size / 2f, color);
                    if (hit)
                    {
                        Debug.DrawLine3D(result.point, result.point + result.normal * sphere.Radius, DebugColors.ContactNormal);
                    }
                    break;

                default: throw new System.NotImplementedException();
            }
        }
        return hit;
    }

    public static bool TrySphereCast(Node3D start, Node3D end, float radius, out Result3D result, uint mask = uint.MaxValue, bool body = true, bool area = false, bool debug = false, List<Godot.Node> exclude = default)
        => TrySphereCast(start.GlobalPosition, end.GlobalPosition - start.GlobalPosition, radius, out result, mask, body, area, debug, exclude);

    public static bool TrySphereCast(Vector3 global_position, Vector3 direction, float radius, out Result3D result, uint mask = uint.MaxValue, bool body = true, bool area = false, bool debug = false, List<Godot.Node> exclude = default)
    {
        volumeParams.Exclude.Clear();
        if (exclude != null)
            foreach (var node in exclude)
                if (node is PhysicsBody3D pbody)
                    volumeParams.Exclude.Add(pbody.GetRid());

        sphere.Radius = radius;
        volumeParams.CollideWithBodies = true;
        volumeParams.CollideWithAreas = true;
        volumeParams.Shape = sphere;
        volumeParams.CollisionMask = mask;
        volumeParams.Motion = direction;

        var transform = Transform3D.Identity;
        transform.Origin = global_position;
        volumeParams.Transform = transform;

        return TryShapeCast(volumeParams, out result, debug);
    }


    public static bool TryRayCast(Godot.PhysicsRayQueryParameters3D parameters, out Result3D result, bool debug = false)
    {
        var outcome = space.IntersectRay(parameters);
        result = new Result3D();
        var hit = outcome.Count > 0;
        if (hit)
        {
            result.normal = outcome["normal"].AsVector3();
            result.point = outcome["position"].AsVector3();
            result.collider = (Godot.Node)outcome["collider"];
        }

        if (debug)
        {
            var global_position = parameters.From;
            var direction = parameters.To - global_position;

            Debug.DrawArrow(global_position, direction, hit ? DebugColors.Hit : DebugColors.Default);

            if (hit)
            {
                Debug.DrawLine3D(result.point, result.point + result.normal / 4f, DebugColors.ContactNormal);
                Debug.DrawCircle3D(result.point, .1f, DebugColors.Contact);
            }
        }
        return hit;
    }

    static PhysicsRayQueryParameters3D ray3d_query_params = new PhysicsRayQueryParameters3D { Exclude = new Godot.Collections.Array<Rid>() };
    public static bool TryRayCast(Vector3 global_position, Vector3 direction, out Result3D result, uint mask = 1, bool body = true, bool area = false, bool hit_from_inside = true, bool debug = false, List<Godot.Node> exclude = default)
    {
        ray3d_query_params.Exclude.Clear();
        if (exclude != null)
            foreach (var item in exclude)
                if (item is Godot.PhysicsBody3D pbody)
                    ray3d_query_params.Exclude.Add(pbody.GetRid());

        ray3d_query_params.CollisionMask = mask;
        ray3d_query_params.CollideWithBodies = body;
        ray3d_query_params.CollideWithAreas = area;
        ray3d_query_params.From = global_position;
        ray3d_query_params.To = global_position + direction;
        ray3d_query_params.HitFromInside = hit_from_inside;
        return TryRayCast(ray3d_query_params, out result, debug);
    }


    public struct Result3D
    {
        public Vector3 normal;
        public Vector3 point;
        public Node collider;
    }

    // 2D
    public struct Result2D
    {
        public Vector2 normal;
        public Vector2 point;
        public Node collider;
    }

    static PhysicsDirectSpaceState2D space2D = viewport.World2D.DirectSpaceState;
    static CircleShape2D circle = new CircleShape2D();
    public static bool TryShapeCast2D(PhysicsShapeQueryParameters2D parameters, out Result2D result, bool debug = false)
    {
        var default_transform = parameters.Transform;
        var motion = parameters.Motion;
        var cast = space2D.CastMotion(parameters);
        bool hit = !(cast[0] == 1f && cast[1] == 1f);
        result = default;

        if (hit)
        {
            var trans = parameters.Transform;
            trans.Origin += parameters.Motion * cast[1];
            parameters.Transform = trans;
            parameters.Motion = default;
            
            var info = space2D.GetRestInfo(parameters);
            hit = info.Count > 0;
            if (hit)
            {
                result.normal = info["normal"].AsVector2();
                result.point = info["point"].AsVector2();
                result.collider = Godot.GodotObject.InstanceFromId((ulong)info["collider_id"]) as Node;
            }
        }

        if (debug)
        {
            var color = hit ? DebugColors.Hit : DebugColors.Default;
            Debug.DrawLine2D(default_transform.Origin, default_transform.Origin + motion, color);

            if (hit)
                Debug.DrawCircle2D(result.point, 4, DebugColors.Hit);

            var offset = default_transform;
            offset.Origin += motion * cast[1];

            switch (parameters.Shape)
            {
                case CircleShape2D circle:
                    Debug.DrawCircle2D(default_transform.Origin, circle.Radius, DebugColors.Default);
                    Debug.DrawCircle2D(offset.Origin, circle.Radius, color);
                    break;

                case RectangleShape2D rect:
                    break;

                default: throw new System.NotImplementedException();
            }
        }
        return hit;
    }

    public static bool TryOverlapShape2D(PhysicsShapeQueryParameters2D parameters, List<Node> results, int max_results = 32, bool debug = false)
    {
        results.Clear();
        var hits = space2D.IntersectShape(parameters, max_results);
        foreach (var item in hits)
            results.Add((Node)item["collider"]);

        if (debug)
        {
            var color = results.Count > 0 ? DebugColors.Hit : DebugColors.Default;
            switch (parameters.Shape)
            {
                case CircleShape2D circle:
                    Debug.DrawCircle2D(parameters.Transform.Origin, circle.Radius, color);
                    Debug.Label2D(parameters.Transform.Origin, results.Count);
                    break;
            }
        }
        return results.Count > 0;
    }

    public static bool TryRayCast2D(PhysicsRayQueryParameters2D parameters, out Result2D result, bool debug = false)
    {
        var outcome = space2D.IntersectRay(parameters);
        result = default;
        var hit = outcome.Count > 0;
        if (hit)
        {
            result.normal = outcome["normal"].AsVector2();
            result.point = outcome["position"].AsVector2();
            result.collider = (Godot.Node)outcome["collider"];
        }

        if (debug)
        {
            Debug.DrawCircle2D(parameters.From, 4f, DebugColors.Default);
            Debug.DrawLine2D(parameters.From, parameters.To, hit ? DebugColors.Hit : DebugColors.Default);
            if (hit)
            {
                Debug.DrawCircle2D(result.point, 2f, DebugColors.Hit);
                Debug.DrawLine2D(result.point, result.point + result.normal, DebugColors.ContactNormal);
            }
        }
        return hit;
    }

    static PhysicsRayQueryParameters2D ray2d = new PhysicsRayQueryParameters2D();
    public static bool TryRayCast2D(Vector2 global_origin, Vector2 direction, out Result2D result, List<Node> exclude = default, bool debug = false)
    {
        ray2d.Exclude.Clear();
        if (exclude != null)
            foreach (var node in exclude)
                if (node is PhysicsBody2D pbody)
                    ray2d.Exclude.Add(pbody.GetRid());

        ray2d.From = global_origin;
        ray2d.To = global_origin + direction;
        ray2d.CollisionMask = uint.MaxValue;
        ray2d.HitFromInside = true;
        return TryRayCast2D(ray2d, out result, debug);
    }




















    public static bool PointInTriangle(Godot.Vector2 point, Godot.Vector2 t0, Godot.Vector2 t1, Godot.Vector2 t2)
    {
        var s = (t0.X - t2.X) * (point.Y - t2.Y) - (t0.Y - t2.Y) * (point.X - t2.X);
        var t = (t1.X - t0.X) * (point.Y - t0.Y) - (t1.Y - t0.Y) * (point.X - t0.X);

        if ((s < 0) != (t < 0) && s != 0 && t != 0)
            return false;

        var d = (t2.X - t1.X) * (point.Y - t1.Y) - (t2.Y - t1.Y) * (point.X - t1.X);
        return d == 0 || (d < 0) == (s + t <= 0);
    }

    public static (Godot.Vector3 line1_nearest_point, Godot.Vector3 line2_nearest_point) NearestPointBetweenLines(Vector3 line1_start, Vector3 line1_end, Vector3 line2_start, Vector3 line2_end)
    {
        line1_start.X -= 0.0001f; // add a bit of noise so that parralel lines don't blow out to infinity
        line2_start.Y += 0.0001f;
        var pos_diff = line1_start - line2_start;
        var normal = (line1_end - line1_start).Normalized();
        var length = normal.Length();
        var ray_normal = (line2_end - line2_start).Normalized();
        var cross_normal = normal.Cross(ray_normal).Normalized();
        var rejection = pos_diff - pos_diff.Project(ray_normal) - pos_diff.Project(cross_normal);
        var distance_to_line_pos = rejection.Length() / normal.Dot(rejection.Normalized());
        var closest = line1_start - normal * distance_to_line_pos;

        var line_nearest = PointOnLineNearestToTarget(closest, line1_start, line1_end);
        var ray_nearest = PointOnLineNearestToTarget(line_nearest, line2_start, line2_end);
        line_nearest = PointOnLineNearestToTarget(ray_nearest, line1_start, line1_end);
        return (line_nearest, ray_nearest);
    }

    public static Godot.Vector3 PointOnLineNearestToTarget(Vector3 target, Vector3 line_start, Vector3 line_end)
    {
        var line = line_end - line_start;
        var len = line.Length();
        line = line.Normalized();

        var v = target - line_start;
        var d = v.Dot(line);
        d = d.Clamp(0f, len);
        return line_start + line * d;
    }
}

public struct Triangle
{
    public Godot.Vector2 p0, p1, p2;

    public bool IsInside(Godot.Vector3 position)
    => Physics.PointInTriangle(new Godot.Vector2(position.X, position.Z), p0, p1, p2);

    public bool IsInside(Godot.Vector2 position)
    => Physics.PointInTriangle(position, p0, p1, p2);
}
