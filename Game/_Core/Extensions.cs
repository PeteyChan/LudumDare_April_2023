using Godot;
using System;
using System.Collections.Generic;

public static partial class Extensions
{
    static Dictionary<Type, Func<object, string>> Formatters = new Dictionary<Type, Func<object, string>>()
    {
        {typeof(float), value => ((float)value).ToString("0.###")},
        {typeof(Vector2), value =>{ var vec = (Vector2)value;  return $"({vec.X.ToString("0.###")}x, {vec.Y.ToString("0.###")}y";}},
        {typeof(Vector3), value =>{ var vec = (Vector3)value;  return $"({vec.X.ToString("0.###")}x, {vec.Y.ToString("0.###")}y, {vec.Z.ToString("0.###")}z)";}}
    };

    public static void SetFormatString<T>(Func<T, string> formatter)
        => Formatters[typeof(Type)] = obj => { if (obj is T item) formatter?.Invoke(item); return "NULL"; };


    [ThreadStatic] static Queue<System.Text.StringBuilder> builders = new Queue<System.Text.StringBuilder>();

    /// <summary>
    /// converts object to string
    /// </summary>
    /// <param name="args">additional information appended to string</param>
    public static string AsString(this object target, params object[] args)
    {
        if (!builders.TryDequeue(out var builder))
            builder = new System.Text.StringBuilder();

        builder.Clear();

        builder.Append(Format(target));

        foreach (var item in args)
        {
            builder.Append(" ");
            builder.Append(Format(item));
        }
        builders.Enqueue(builder);
        return builder.ToString();

        string Format(object value)
        {
            if (value is null) return "null";
            if (value is Node node && !node.IsValid()) return "null";
            if (Formatters.TryGetValue(value.GetType(), out var formatter))
                return formatter.Invoke(value);
            return value.ToString();
        }
    }

    /// <summary>
    /// returns min value between target and supplied values
    /// </summary>
    public static int MinValue(this int target, int value)
        => target < value ? value : target;

    /// <summary>
    /// returns max value between target and supplied values
    /// </summary>
    public static int MaxValue(this int target, int value)
        => target > value ? value : target;

    /// <summary>
    /// clamps value between min and max
    /// </summary>
    public static int Clamp(this int target, int min, int max)
    {
        if (min > max) throw new Exception("min value is greater than max value");
        return target < min ? min : target > max ? max : target;
    }

    public static float MinValue(this float target, float value)
        => target < value ? value : target;

    public static float MaxValue(this float target, float value)
        => target > value ? value : target;

    public static int Round(this float target) => Mathf.RoundToInt(target);
    public static float Abs(this float target) => target < 0 ? -target : target;

    /// <summary>
    /// clamps value between values
    /// </summary>
    public static float Clamp(this float target, float min, float max)
    {
        if (min > max)
        {
            var temp = min;
            min = max;
            max = temp;
        }
        return target < min ? min : target > max ? max : target;
    }

    /// <summary>
    /// remaps value to a number between 0 and 1 depending on remap targets
    /// </summary>
    /// <param name="value">value to remap</param>
    /// <param name="zero_target">value that corresponds to zero</param>
    /// <param name="one_target">value that corresponds to one</param>
    /// <returns></returns>
    public static float Remap(this float value, float zero_target, float one_target)
    {
        bool inverse = zero_target > one_target;

        var min = inverse ? one_target : zero_target;
        var max = inverse ? zero_target : one_target;

        var dif = max - min;
        if (dif == 0) return 0;
        value = value - min;
        if (value == 0) return 0;
        value = value / dif;
        value = value > 1 ? 1 : value < 0 ? 0 : value;
        return inverse ? 1 - value : value;
    }

    static Dictionary<Type, List<Type>> implementors = new Dictionary<Type, List<Type>>();

    /// <summary>
    /// returns all types that inherit from target type that has parameterless constructors
    /// </summary>
    public static IReadOnlyList<Type> GetAllImplementors(this Type target_type)
    {
        if (!implementors.TryGetValue(target_type, out var types))
        {
            implementors[target_type] = types = new List<Type>();

            foreach (var type in System.Reflection.Assembly.GetCallingAssembly().GetTypes())
            {
                if (type.IsAbstract || type.IsGenericType) continue;
                if (target_type.IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null)
                    types.Add(type);
            }
            types.Sort((x, y) => x.FullName.CompareTo(y.FullName));
        }
        return types;
    }

    public static T Shuffle<T>(this T collection) where T : System.Collections.Generic.IList<T>
    {
        for (int i = 0; i < collection.Count; ++i)
        {
            var index = Random.Shared.Next(collection.Count);
            var temp = collection[index];
            collection[index] = collection[i];
            collection[i] = temp;
        }
        return collection;
    }

    public static T RandomElement<T>(this IList<T> collection)
        => collection[Random.Shared.Next(collection.Count)];

    public static float Range(this System.Random random, float min, float max)
    {
        if (min > max)
        {
            var temp = min;
            max = min;
            min = temp;
        }
        var range = max - min;
        var value = random.NextSingle() * range + min;
        return value;
    }

    public static List<T> Add<T>(this List<T> list, params T[] items)
    {
        list.AddRange(items);
        return list;
    }

    public static List<(int start, int end)> Split(this System.ReadOnlySpan<char> span, char separator, List<(int, int)> buffer = default)
    {
        if (buffer == null) buffer = new List<(int, int)>();
        buffer.Clear();
        int? start = null;

        for (int i = 0; i < span.Length; ++i)
        {
            if (span[i] == separator)
            {
                if (start.HasValue)
                {
                    buffer.Add((start.Value, i - start.Value));
                    start = null;
                }
                continue;
            }
            else if (!start.HasValue)
                start = i;
        }
        if (start.HasValue)
            buffer.Add((start.Value, span.Length - start.Value));

        return buffer;
    }
}
