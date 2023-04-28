namespace Utilities
{

    using System.Collections;
    using System.Collections.Generic;

    public class TypeDictionary<Context>
    {
        static Dictionary<System.Type, int> type_to_index = new Dictionary<System.Type, int>();
        object[] items = new object[16];

        int CreateIndex(System.Type type)
        {
            if (!type_to_index.TryGetValue(type, out var index))
                index = type_to_index[type] = type_to_index.Count;
            return index;
        }

        public ref T Get<T>()
        {
            var index = Item<T>.Index;
            if (index < 0
                || index >= items.Length
                || items[index] == null)
                throw new System.Exception(typeof(T).AsString("was never set"));
            return ref ((Item<T>)items[index]).item;
        }

        public void Set<T>(T value)
        {
            var index = Item<T>.Index;
            if (index < 0) index = Item<T>.Index = CreateIndex(typeof(T));
            if (index >= items.Length) System.Array.Resize(ref items, items.Length * 2);
            if (items[index] == null) items[index] = new Item<T>();
            ((Item<T>)items[index]).item = value;
        }
        public bool TryGet<T>(out T value)
        {
            value = default;
            var index = Item<T>.Index;
            if (index < 0 || index >= items.Length) return false;
            if (items[index] == null) return false;
            value = ((Item<T>)items[index]).item;
            return true;
        }

        public void Remove<T>()
        {
            var index = Item<T>.Index;
            if (index >= 0 && index < items.Length)
                items[index] = default;
        }

        class Item<T>
        {
            public static int Index = -1;
            public T item;
        }
    }


    /// <summary>
    /// managed data that can be deleted
    /// </summary>
    public struct Managed<T>
    {
        public static IEnumerable<Managed<T>> All
        {
            get
            {
                for (int i = 0; i < data.Length; ++i)
                    if (data[i].version > 0)
                        yield return new Managed<T> { index = i, version = data[i].version };
            }
        }

        static (int version, T item)[] data = new (int version, T item)[8];
        static int next_index;
        static Queue<int> free_index = new Queue<int>();
        public Managed(T value)
        {
            index = free_index.Count > 0 ? free_index.Dequeue() : next_index++;

            if (index == data.Length)
                System.Array.Resize(ref data, index * 2);

            version = data[index].version = -data[index].version + 1;
            data[index].item = value;
        }

        public static void DeleteAll()
        {
            for (int i = 0; i < next_index; ++i)
            {
                if (data[i].version > 0) data[i].version = -data[i].version;
                data[i].item = default;
            }
            free_index.Clear();
            next_index = 0;
        }

        public void Delete()
        {
            if (this)
            {
                free_index.Enqueue(index);
                data[index].version = -version;
                data[index].item = default;
            }
        }

        int version;
        int index;
        public ref T Value
        {
            get
            {
                if (this)
                    return ref data[index].item;
                throw new System.NullReferenceException();
            }
        }

        public static implicit operator bool(Managed<T> item)
            => item.version > 0 && data[item.index].version == item.version;

        public override string ToString()
        {
            if (this) return data[index].item.AsString();
            else return "null";
        }
    }
}

