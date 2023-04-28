namespace Utilities
{
    public class GridMap2D<T>
    {
        GridMap2D() { }

        public GridMap2D(int size = 32)
        {
            this.size = size;
            extents = size * 2 + 1;
            map = new T[extents * extents];
        }

        public readonly int size;
        readonly int extents;

        T[] map;
        public bool Set(int x, int y, in T value) => Set(x, y, value, out var old);
        public bool Set(int x, int y, in T new_value, out T old_value)
        {
            var index = GetIndex(x, y);
            if (index < 0 || index >= map.Length)
            {
                old_value = default;
                return false;
            }
            ref var terrain = ref map[index];
            old_value = terrain;
            terrain = new_value;
            return true;
        }

        int GetIndex(int x, int y) => (x + size) + (y + size) * extents;
        public T Get(int x, int y)
        {
            var index = GetIndex(x, y);
            if (index < 0 || index >= map.Length) return default;
            return map[index];
        }

        public void Foreach(System.Action<int, int, T> action)
        {
            for (int x = -size; x <= size; ++x)
                for (int y = -size; y <= size; ++y)
                    action(x, y, map[GetIndex(x, y)]);

        }
        public void Clear() => System.Array.Clear(map);
    }

    public class GridMap3D<T>
    {
        public T this[int x, int y, int z]
        {
            get => Get(x, y, z);
            set => Set(x, y, z, value);
        }

        GridMap3D() { }
        public GridMap3D(int size = 32, int height = 8)
        {
            this.size = size;
            this.height = height;
            int extents = size * 2 + 1;
            int height_extents = height * 2 + 1;
            map = new T[extents * extents * height_extents];
        }

        public readonly int size, height;
        T[] map;
        public bool Set(Godot.Vector3 position, in T value)
            => Set(position.X.Round(), position.Y.Round(), position.Z.Round(), value);
        public bool Set(int x, int y, int z, in T value) => Set(x, y, z, value, out var old);
        public bool Set(int x, int y, int z, in T new_value, out T old_value)
        {
            old_value = default;
            var index = GetIndex(x, y, z);
            if (index < 0 || index >= map.Length) return false;
            old_value = map[index];
            map[index] = new_value;
            return true;
        }

        int extents => size * 2;
        int GetIndex(int x, int y, int z) => (x + size) + (y + size) * extents + (z + height) * extents * extents;


        public T Get(int x, int y, int z)
        {
            var index = GetIndex(x, y, z);
            if (index < 0 || index > map.Length) return default;
            return map[index];
        }

        public void Foreach(System.Action<int, int, T> action)
        {
            for (int x = -size; x <= size; ++x)
                for (int y = -size; y <= size; ++y)
                    for (int z = -size; z <= size; ++z)
                        action(x, y, map[GetIndex(x, y, z)]);
        }

        public void Clear() => System.Array.Clear(map);
    }
}