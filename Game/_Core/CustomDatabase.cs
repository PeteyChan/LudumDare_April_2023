using System;
using System.Collections.Generic;

public partial class Database
{
    Dictionary<string, Dictionary<string, object>> database
        = new Dictionary<string, Dictionary<string, object>>();
    public Database(string resource_path) { this._path = resource_path; }
    string _path;
    public List<string> GetEntries() => new(database.Keys);
    public IEnumerable<(string field, object item)> GetFields(string entry)
    {
        if (database.TryGetValue(entry, out var fields))
            foreach (var field in fields)
                yield return (field.Key, field.Value);
    }

    /// <summary>
    /// returns true if entry was removed
    /// </summary>
    public bool RemoveEntry(string entry, out Dictionary<string, object> value)
        => database.Remove(entry, out value);

    /// <summary>
    /// returns true if entry was removed
    /// </summary>
    public bool RemoveEntry(string entry) => RemoveEntry(entry, out var value);

    /// <summary>
    /// removes field from all entries
    /// </summary>
    public void RemoveFields(string field)
    {
        foreach (var entry in database)
            entry.Value.Remove(field);
    }

    public bool TryGetValue(string entry, string field, out object value)
    {
        value = default;
        return database.TryGetValue(entry, out var data)
            && data.TryGetValue(field, out value);
    }

    public Database Set(string entry, string field, object value)
    {
        if (!database.TryGetValue(entry, out var data))
            database[entry] = data = new Dictionary<string, object>();
        if (field.Contains(' ')) throw new System.Exception("fields cannot contain spaces");
        data[field] = value;
        return this;
    }

    public Database RemoveField(string entry, string field)
    {
        if (database.TryGetValue(entry, out var data))
            data.Remove(field);
        return this;
    }

    public Database Save()
    {
        List<string> to_serialize = new List<string>();
        foreach (var entry in database)
        {
            to_serialize.Add(entry.Key);

            foreach (var field in entry.Value)
            {
                if (!Serializers.TrySerialize(field.Value, out var serialized_data))
                {
                    Debug.LogError(entry.Key, "Failed to serialize", field.Key);
                    continue;
                }
                to_serialize.Add($"{field.Key} {serialized_data}");
            }
            to_serialize.Add("");
        }

        using (var file = Godot.FileAccess.Open(_path, Godot.FileAccess.ModeFlags.Write))
            foreach (var line in to_serialize)
                file.StoreLine(line);
        return this;
    }

    public Database Load()
    {
        database.Clear();
        if (!Godot.FileAccess.FileExists(_path)) return this;

        string entry = default;
        using (var file = Godot.FileAccess.Open(_path, Godot.FileAccess.ModeFlags.Read))
            while (!file.EofReached())
            {
                var line = file.GetLine();
                if (line == "")
                    entry = default;
                else if (entry == default)
                    entry = line;
                else
                {
                    var options = line.Split(' ', 2);
                    if (options.Length != 2)
                    {
                        Debug.Log(options.Length, line);
                        continue;
                    }

                    var field = options[0];
                    var serialized_data = options[1];
                    if (!Serializers.TryDeserialize(serialized_data, out var value))
                        Debug.LogError(entry, "was unable to deserialize", new string(field));
                    else
                    {
                        Set(entry, field, value);
                    }
                }
            }
        return this;
    }

    public void Clear() => database.Clear();
}

/* testing
public partial class Database
{
    class Data
    {
        public float f = 0;
        public int i = 0;
        public string s = "";
        public Godot.Vector2 v2 = default;
        public Godot.Vector3 v3 = default;
        public bool b = false;
        public Guid guid = default;
        public Godot.Transform3D t = Godot.Transform3D.Identity;
        public Inputs input;
    }

    static void TestDatabase(Debug.Console args)
    {
        var db = new ClassDatabase("res://Databases/test.txt").Load();
        if (!db.TryGet(out Data data))
            data = new Data();

        var imgui = new ImmediateWindow();

        imgui.Window.OnUpdate(() =>
        {
            if (imgui.Button("Load DB"))
            {
                db.Load();
                if (db.TryGet(out Data stored))
                    data = stored;
            }

            if (imgui.Button("Save DB"))
            {
                db.Set(data);
                db.Save();
            }


            imgui.Separator();
            imgui.Property("data", ref data);
        });
    }

}
/**/

/// <summary>
/// simple database that stores unique classes
/// </summary>
public class ClassDatabase : System.Collections.IEnumerable
{
    public ClassDatabase(string path) => database = new Database(path);
    Database database;
    Dictionary<Type, object> stored = new Dictionary<Type, object>();

    public void Set<T>(T item) where T : new()
    {
        if (item is null) return;
        stored[typeof(T)] = item;
    }

    /// <summary>
    /// returns true if item was removed
    /// </summary>
    public bool Remove<T>() => stored.Remove(typeof(T));

    public bool TryGet<T>(out T item) where T : new()
    {
        if (stored.TryGetValue(typeof(T), out var value))
        {
            item = (T)value;
            return item != null;
        }
        item = default;
        return false;
    }

    public T GetOrNew<T>() where T : new()
    {
        if (TryGet<T>(out var item)) return item;
        item = new T();
        stored[typeof(T)] = item;
        return item;
    }

    public ClassDatabase Save()
    {
        database.Clear();
        foreach (var item in stored)
        {
            var entry = item.Key.FullName;
            foreach (var field in item.Key.GetFields())
                database.Set(entry, field.Name, field.GetValue(item.Value));
        }
        database.Save();
        return this;
    }

    public ClassDatabase Load()
    {
        stored.Clear();
        database.Load();
        foreach (var entry in database.GetEntries())
        {
            var type = Type.GetType(entry);
            if (type == null) continue;
            var obj = System.Activator.CreateInstance(type);

            foreach (var field in type.GetFields())
            {
                if (database.TryGetValue(entry, field.Name, out var value))
                    field.SetValue(obj, value);
            }

            stored[type] = obj;
        }
        return this;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        foreach (var item in stored)
            yield return item.Value;
    }
}

public class TypeDatabase<T> where T : new()
{
    public TypeDatabase(string path) => database = new Database(path);
    Database database;
    Dictionary<string, T> stored = new Dictionary<string, T>();

    public bool TryGet(string entiry, out T data)
        => stored.TryGetValue(entiry, out data);
    public void Set(string entry, T data)
    {
        if (data != null)
            stored[entry] = data;
    }
    public bool Remove(string entry, out T data)
        => stored.Remove(entry, out data);

    public TypeDatabase<T> Clear()
    {
        stored.Clear();
        database.Clear();
        return this;
    }
    public TypeDatabase<T> Load()
    {
        stored.Clear();
        database.Load();

        foreach (var entry in database.GetEntries())
        {
            var data = new T();
            foreach (var (field, value) in database.GetFields(entry))
            {
                typeof(T).GetField(field).SetValue(data, value);
            }
            stored[entry] = data;
        }
        return this;
    }
    public TypeDatabase<T> Save()
    {
        database.Clear();
        var fields = typeof(T).GetFields();

        foreach (var item in stored)
        {
            var entry = item.Key;
            var data = item.Value;
            foreach (var field in fields)
            {
                database.Set(entry, field.Name, field.GetValue(data));
            }
        }
        database.Save();
        return this;
    }

    public IEnumerable<string> Entries => stored.Keys;
    public IEnumerable<T> Values => stored.Values;
}

static class Serializers
{
    public interface ISerializer
    {
        int Priority => GetPriority(this); // lower priority evaluated first
        bool TrySerialize(object data, out string value);
        bool TryDeserialize(string data, out object value);
    }

    static int GetPriority(ISerializer serializer)
        => serializer is Serializers.Default_Serializer ? 1000 : 0;

    public static bool TrySerialize(object obj, out string serialized_data)
    {
        foreach (var serialzier in serializers)
            if (serialzier.TrySerialize(obj, out serialized_data))
            {
                string type = serialzier.GetType().Name;
                serialized_data = $"{type} {serialized_data}";
                return true;
            }
        serialized_data = default;
        return false;
    }

    public static bool TryDeserialize(string serialized_data, out object value)
    {
        value = default;
        if (serialized_data == null) return false;
        var data = serialized_data.Split(" ", 2);
        if (data.Length != 2) return false;
        if (!string_to_serializer.TryGetValue(data[0], out var serializer)) return false;
        return serializer.TryDeserialize(data[1], out value);
    }

    static List<ISerializer> serializers = new List<ISerializer>();
    static Dictionary<string, ISerializer> string_to_serializer = new Dictionary<string, ISerializer>();
    static Serializers()
    {
        foreach (var item in typeof(ISerializer).GetAllImplementors())
        {
            var serializer = System.Activator.CreateInstance(item) as ISerializer;
            serializers.Add(serializer);
            string_to_serializer.Add(serializer.GetType().Name, serializer);
        }
        serializers.Sort((x, y) => x.Priority.CompareTo(y.Priority));
    }

    interface Default_Serializer : ISerializer { }

    class int32 : Default_Serializer
    {
        public bool TryDeserialize(string data, out object value)
        {
            value = default;
            return int.TryParse(data, out var val) && (value = val) != null;
        }

        public bool TrySerialize(object data, out string value)
        {
            value = default;
            return (data is int i) && (value = i.ToString()) != null;
        }
    }

    class f32 : Default_Serializer
    {
        public bool TryDeserialize(string data, out object value)
        {
            value = default;
            return float.TryParse(data, out var val) && (value = val) != null;
        }

        public bool TrySerialize(object data, out string value)
        {
            value = default;
            return (data is float f32) && (value = f32.ToString("0.###")) != null;
        }
    }

    class Bool : Default_Serializer
    {
        public bool TryDeserialize(string data, out object value)
        {
            value = default;
            return bool.TryParse(data, out var val) && (value = val) != null;
        }

        public bool TrySerialize(object data, out string value)
        {
            value = default;
            return (data is bool f32) && (value = f32.ToString()) != null;
        }
    }

    class guid : Default_Serializer
    {
        public bool TryDeserialize(string data, out object value)
        {
            value = default;
            return (Guid.TryParse(data, out var guid) && (value = guid) != null);
        }

        public bool TrySerialize(object data, out string value)
        {
            value = default;
            return (data is System.Guid guid) && (value = guid.ToString()) != null;
        }
    }

    class str : Default_Serializer
    {
        public bool TryDeserialize(string data, out object value)
        {
            value = data == null ? "" : data;
            return true;
        }

        public bool TrySerialize(object data, out string value)
        {
            value = default;
            return (data is string str) && (value = str) != null;
        }
    }

    class vec2 : Default_Serializer
    {
        public bool TryDeserialize(string data, out object value)
        {
            value = default;
            var floats = SplitFloats(data, 2);
            value = new Godot.Vector2(floats[0], floats[1]);
            return true;
        }

        public bool TrySerialize(object data, out string value)
        {
            value = default;
            if (data is not Godot.Vector2 vec2) return false;
            value = $"{vec2.X} {vec2.Y}";
            return true;
        }
    }

    class vec3 : Default_Serializer
    {
        public bool TryDeserialize(string data, out object value)
        {
            value = default;
            var floats = SplitFloats(data, 3);
            value = new Godot.Vector3(floats[0], floats[1], floats[2]);
            return true;
        }

        public bool TrySerialize(object data, out string value)
        {
            value = default;
            if (data is not Godot.Vector3 vec3) return false;
            value = $"{vec3.X} {vec3.Y} {vec3.Z}";
            return true;
        }
    }

    class color : Default_Serializer
    {
        public bool TryDeserialize(string data, out object value)
        {
            var floats = SplitFloats(data, 4);
            value = new Godot.Color(floats[0], floats[1], floats[2], floats[3]);
            return true;
        }

        public bool TrySerialize(object data, out string value)
        {
            value = default;
            if (data is not Godot.Color color) return false;
            value = $"{color.R} {color.G} {color.B} {color.A}";
            return true;
        }
    }

    class transform3d : Default_Serializer
    {
        public bool TrySerialize(object data, out string value)
        {
            value = default;
            if (data is not Godot.Transform3D transform) return false;
            var pos = transform.Origin;
            var basis = transform.Basis;

            value = serialize(pos);
            value += serialize(transform.Basis.Row0);
            value += serialize(transform.Basis.Row1);
            value += serialize(transform.Basis.Row2);
            return true;

            string serialize(Godot.Vector3 vec3) => $"{vec3.X} {vec3.Y} {vec3.Z} ";
        }

        public bool TryDeserialize(string data, out object result)
        {
            var value = Godot.Transform3D.Identity;
            var items = data.Split(" ");
            if (items.Length < 12)
            {
                result = value;
                return true;
            }

            value.Origin = try_deserilize(0, out var pos) ? pos : value.Origin;
            value.Basis.Row0 = try_deserilize(1, out var row0) ? row0 : value.Basis.Row0;
            value.Basis.Row1 = try_deserilize(2, out var row1) ? row1 : value.Basis.Row1;
            value.Basis.Row2 = try_deserilize(3, out var row2) ? row2 : value.Basis.Row2;
            result = value;
            return true;

            bool try_deserilize(int index, out Godot.Vector3 vec)
            {
                index *= 3;
                vec = new Godot.Vector3();

                if (float.TryParse(items[index], out vec.X)
                    && float.TryParse(items[index + 1], out vec.Y)
                    && float.TryParse(items[index + 2], out vec.Z))
                    return true;
                return false;
            }
        }
    }

    class Enum : Default_Serializer
    {
        public bool TryDeserialize(string data, out object value)
        {
            var items = data.Split(" ", 2);
            var enum_type = System.Type.GetType(items[0]);
            return System.Enum.TryParse(enum_type, items[1], true, out value);
        }

        public bool TrySerialize(object data, out string value)
        {
            value = default;
            if (data == null || !data.GetType().IsEnum) return false;
            value = $"{data.GetType()} {data.ToString()}";
            return true;
        }
    }

    static List<float> SplitFloats(string value, int count)
    {
        var data = value.Split(" ");
        List<float> results = new List<float>();
        int index = 0;
        for (; index < data.Length.MaxValue(count); ++index)
            if (float.TryParse(data[index], out float f32))
                results.Add(f32);
        for (; index < count; ++index)
            results.Add(0);
        return results;
    }
}