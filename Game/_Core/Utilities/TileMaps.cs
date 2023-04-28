using Godot;
using System.Collections.Generic;

namespace Utilities.Tilemaps
{
    public interface ITileMap
    {
        bool Add(Vector3I position);
        bool Remove(Vector3I position);
        void Clear();
        bool Generate();
    }

    public sealed class Multimesh_Slice25 : ITileMap
    {
        public Multimesh_Slice25(string tile_path, bool auto_add_to_scene = true)
        {
            var packed_scene = GD.Load<PackedScene>(tile_path).Instantiate();
            node = new Node3D { Name = packed_scene.Name };
            if (auto_add_to_scene)
                node.AddToScene();

            var all = new List<Godot.Node>(packed_scene.GetChildren());
            all.Sort((x, y) => x.Name.ToString().CompareTo(y.Name.ToString()));
            all.Insert(0, null);
            all.Insert(3, null);
            all.Insert(12, null);

            multimeshes = new (MultiMeshInstance3D, List<Vector3>)[all.Count];

            for (int i = 0; i < all.Count; ++i)
            {
                var asset = all[i];
                if (asset == null) continue;

                Sections section = (Sections)i;
                var multi_inst = new MultiMeshInstance3D { Name = asset.Name };
                var mesh_data = new MultiMesh();
                mesh_data.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
                mesh_data.InstanceCount = 32;
                mesh_data.VisibleInstanceCount = 0;
                mesh_data.Mesh = (asset as MeshInstance3D).Mesh;
                multi_inst.Multimesh = mesh_data;
                node.AddChild(multi_inst);

                multimeshes[i] = (multi_inst, new List<Vector3>());
            }
        }

        (MultiMeshInstance3D multi, List<Vector3> positions)[] multimeshes;
        HashSet<Godot.Vector3I> map = new();
        public Godot.Node node;
        public IEnumerable<Vector3I> positions => map;
        public bool Add(Godot.Vector3I position)
        {
            bool changed = map.Add(position);
            if (!_updated) _updated = changed;
            return changed;
        }
        public bool Remove(Godot.Vector3I position)
        {
            bool changed = map.Add(position);
            if (!_updated) _updated = changed;
            return changed;
        }
        public void Clear()
        {
            _updated = map.Count != 0;
            map.Clear();
        }

        public IEnumerable<Vector3> GetPositions(Sections section) => multimeshes[(int)section].positions;

        public enum Sections
        {
            none,
            left,
            right,
            //left_right,
            back = 4,
            back_left,
            back_right,
            back_left_right,
            front,
            front_left,
            front_right,
            front_left_right,
            //front_back,
            front_back_left = 13,
            front_back_right,
            front_back_left_right,

            corner_back_left,
            corner_back_right,
            corner_front_left,
            corner_front_right,

            fill,
            fill_2,
            fill_3,
            fill_4,
        }

        bool _updated = false;
        HashSet<Vector3I> to_spawn = new();
        public bool Generate()
        {
            if (!_updated) return false;

            foreach (var item in multimeshes)
                item.positions?.Clear();

            foreach (var item in map)
            {
                Spawn(Sections.fill + RandomFromPosition(item, 0, 4), item); // spawn fill
                for (int x = -1; x <= 1; ++x)
                    for (int z = -1; z <= 1; ++z)
                    {
                        var pos = item;
                        pos.X += x;
                        pos.Z += z;
                        if (!map.Contains(pos))
                            to_spawn.Add(pos);
                    }
            }

            foreach (var pos in to_spawn)
            {
                Sections section = default;
                if (map.Contains(new Vector3I(pos.X - 1, pos.Y, pos.Z))) section |= Sections.left;
                if (map.Contains(new Vector3I(pos.X + 1, pos.Y, pos.Z))) section |= Sections.right;
                if (map.Contains(new Vector3I(pos.X, pos.Y, pos.Z - 1))) section |= Sections.back;
                if (map.Contains(new Vector3I(pos.X, pos.Y, pos.Z + 1))) section |= Sections.front;


                switch (section)
                {
                    case Sections.none: break;

                    case Sections.left | Sections.right:
                        Spawn(Sections.left, pos);
                        Spawn(Sections.right, pos);
                        break;

                    case Sections.front | Sections.back:
                        Spawn(Sections.front, pos);
                        Spawn(Sections.back, pos);
                        break;

                    default:
                        Spawn(section, pos);
                        break;
                }

                // spawn corner blocks
                if (!section.HasFlag(Sections.front) && !section.HasFlag(Sections.right) && map.Contains(new Vector3I(pos.X + 1, pos.Y, pos.Z + 1)))
                    Spawn(Sections.corner_front_right, pos);
                if (!section.HasFlag(Sections.front) && !section.HasFlag(Sections.left) && map.Contains(new Vector3I(pos.X - 1, pos.Y, pos.Z + 1)))
                    Spawn(Sections.corner_front_left, pos);
                if (!section.HasFlag(Sections.back) && !section.HasFlag(Sections.right) && map.Contains(new Vector3I(pos.X + 1, pos.Y, pos.Z - 1)))
                    Spawn(Sections.corner_back_right, pos);
                if (!section.HasFlag(Sections.back) && !section.HasFlag(Sections.left) && map.Contains(new Vector3I(pos.X - 1, pos.Y, pos.Z - 1)))
                    Spawn(Sections.corner_back_left, pos);
            }
            void Spawn(Sections section, Vector3I position)
                => multimeshes[(int)section].positions.Add(position);

            foreach (var (multi_inst, positions) in multimeshes) // generate meshes
            {
                if (positions == null) continue;

                var mesh = multi_inst.Multimesh;
                if (mesh.InstanceCount < positions.Count)
                    mesh.InstanceCount = positions.Count;
                mesh.VisibleInstanceCount = positions.Count;

                Transform3D transform = Transform3D.Identity;

                for (int i = 0; i < positions.Count; ++i)
                {
                    transform.Origin = positions[i];
                    mesh.SetInstanceTransform(i, transform);
                }
                multi_inst.Multimesh = mesh;
            }
            to_spawn.Clear();
            return true;
        }

        static int RandomFromPosition(Vector3I pos, int min, int max_exlusive, int seed = 1610612741)
        {
            var dif = max_exlusive - min;
            if (dif == 0) return min;

            unchecked
            {
                int val = seed;
                val += pos.X * 805306457;
                val += pos.Y * 37;
                val += pos.Z * 393241;
                val = Mathf.Abs(val) % dif;
                return (min > max_exlusive ? max_exlusive : min) + val;
            }
        }
    }

    public sealed class Multimesh_Path_16_Slice : ITileMap
    {
        public Multimesh_Path_16_Slice(string tile_path, bool auto_add_to_scene = true)
        {
            var packed_scene = GD.Load<PackedScene>(tile_path).Instantiate();
            node = new Node { Name = packed_scene.Name };
            if (auto_add_to_scene) node.AddToScene();
            var all = new List<Godot.Node>(GD.Load<PackedScene>(tile_path).Instantiate().GetChildren());
            all.Sort((x, y) => x.Name.ToString().CompareTo(y.Name.ToString()));

            multimeshes = new (MultiMeshInstance3D, List<Vector3>)[all.Count];

            for (int i = 0; i < all.Count; ++i)
            {
                var asset = all[i];
                if (asset == null) continue;

                var multi_inst = new MultiMeshInstance3D { Name = asset.Name };
                var mesh_data = new MultiMesh();
                mesh_data.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
                mesh_data.InstanceCount = 32;
                mesh_data.VisibleInstanceCount = 0;
                mesh_data.Mesh = (asset as MeshInstance3D).Mesh;
                multi_inst.Multimesh = mesh_data;
                node.AddChild(multi_inst);
                multimeshes[i] = (multi_inst, new List<Vector3>());
            }
        }

        public Node node;
        HashSet<Vector3I> map = new();
        (MultiMeshInstance3D multi, List<Vector3> positions)[] multimeshes;

        public bool Add(Vector3I position)
        {
            var changed = map.Add(position);
            if (!updated) updated = changed;
            return changed;
        }

        public bool Remove(Vector3I position)
        {
            var changed = map.Remove(position);
            if (!updated) updated = changed;
            return changed;
        }

        public void Clear()
        {
            updated = map.Count != 0;
            map.Clear();
        }
        bool updated;
        public bool Generate()
        {
            if (!updated) return false;

            foreach (var pos in map)
            {
                int left = 1, right = 2, back = 4, forward = 8;
                int index = 0;

                if (map.Contains(new Vector3I(pos.X - 1, pos.Y, pos.Z)))
                    index |= left;

                if (map.Contains(new Vector3I(pos.X + 1, pos.Y, pos.Z)))
                    index |= right;

                if (map.Contains(new Vector3I(pos.X, pos.Y, pos.Z - 1)))
                    index |= back;

                if (map.Contains(new Vector3I(pos.X, pos.Y, pos.Z + 1)))
                    index |= forward;

                multimeshes[index].positions.Add(pos);
            }

            foreach (var (multi_inst, positions) in multimeshes) // generate meshes
            {
                var mesh = multi_inst.Multimesh;
                if (mesh.InstanceCount < positions.Count)
                    mesh.InstanceCount = positions.Count;
                mesh.VisibleInstanceCount = positions.Count;

                Transform3D transform = Transform3D.Identity;

                for (int i = 0; i < positions.Count; ++i)
                {
                    transform.Origin = positions[i];
                    mesh.SetInstanceTransform(i, transform);
                }
                multi_inst.Multimesh = mesh;
                positions.Clear();
            }
            return true;
        }
    }


    public sealed class Instancer : ITileMap
    {
        public Instancer(string tile_path, bool auto_add_to_scene = true)
        {
            var packed_scene = GD.Load<PackedScene>(tile_path).Instantiate();
            node = new Node { Name = packed_scene.Name };
            if (auto_add_to_scene) node.AddToScene();

            multi_inst = new MultiMeshInstance3D { };
            var mesh_data = new MultiMesh();
            mesh_data.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
            mesh_data.InstanceCount = 32;
            mesh_data.VisibleInstanceCount = 0;

            if (packed_scene.TryFind(out MeshInstance3D mesh))
                mesh_data.Mesh = mesh.Mesh;

            multi_inst.Multimesh = mesh_data;
            node.AddChild(multi_inst);
        }

        public Node node;
        HashSet<Vector3I> map = new();
        MultiMeshInstance3D multi_inst;
        public bool Add(Vector3I position)
        {
            var changed = map.Add(position);
            if (!updated) updated = changed;
            return changed;
        }

        public bool Remove(Vector3I position)
        {
            var changed = map.Remove(position);
            if (!updated) updated = changed;
            return changed;
        }

        public void Clear()
        {
            updated = map.Count != 0;
            map.Clear();
        }
        bool updated;
        public bool Generate()
        {
            if (!updated) return false;

            var mesh = multi_inst.Multimesh;
            if (mesh.InstanceCount < map.Count)
                mesh.InstanceCount = map.Count;
            mesh.VisibleInstanceCount = map.Count;

            int index = 0;
            foreach (var position in map)
            {
                Transform3D transform = Transform3D.Identity;
                transform.Origin = position;
                mesh.SetInstanceTransform(index, transform);
                index++;
            }

            multi_inst.Multimesh = mesh;
            return true;
        }

        static int RandomFromPosition(Vector3I pos, int min, int max_exlusive, int seed = 1610612741)
        {
            var dif = max_exlusive - min;
            if (dif == 0) return min;

            unchecked
            {
                int val = seed;
                val += pos.X * 805306457;
                val += pos.Y * 37;
                val += pos.Z * 393241;
                val = Mathf.Abs(val) % dif;
                return (min > max_exlusive ? max_exlusive : min) + val;
            }
        }
    }
}

public static partial class Extensions
{
    public static bool Add(this Utilities.Tilemaps.ITileMap self, Vector3 position)
        => self.Add(new Vector3I(position.X.Round(), position.Y.Round(), position.Z.Round()));

    public static bool Add(this Utilities.Tilemaps.ITileMap self, int x, int y, int z)
        => self.Add(new Vector3I(x, y, z));
    public static bool Add(this Utilities.Tilemaps.ITileMap self, int x, int z) => Add(self, x, 0, z);
    public static bool Remove(this Utilities.Tilemaps.ITileMap self, int x, int z) => Remove(self, x, 0, z);
    public static bool Remove(this Utilities.Tilemaps.ITileMap self, Vector3 position)
        => self.Remove(new Vector3I(position.X.Round(), position.Y.Round(), position.Z.Round()));
    public static bool Remove(this Utilities.Tilemaps.ITileMap self, int x, int y, int z)
        => self.Remove(new Vector3(x, y, z));
}