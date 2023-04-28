using System.Collections;
using System.Collections.Generic;
namespace Utilities
{
    public sealed class EnumMap<E, T> : System.Collections.Generic.IEnumerable<(E Item, T Value)>
    where E : struct, System.Enum
    {
        static EnumMap()
        {
            var values = System.Enum.GetValues<E>();
            for (int i = 0; i < values.Length; ++i)
            {
                if (System.Runtime.CompilerServices.Unsafe.As<E, int>(ref values[i]) != i)
                    throw new System.Exception("Enum map enum values must have no values defined");
            }
        }

        public ref T this[E index] => ref values[System.Runtime.CompilerServices.Unsafe.As<E, int>(ref index)];
        static int Length = System.Enum.GetValues<E>().Length;
        T[] values = new T[Length];

        public EnumMap() { }
        public EnumMap(EnumMap<E, T> to_copy)
        {
            foreach (var value in System.Enum.GetValues<E>())
                this[value] = to_copy[value];
        }

        public bool TrySet(string item, object value)
        {
            if (value is T type && System.Enum.TryParse<E>(item, out E result))
            {
                this[result] = type;
                return true;
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<(E, T)>)this).GetEnumerator();

        IEnumerator<(E Item, T Value)> IEnumerable<(E Item, T Value)>.GetEnumerator()
        {
            foreach (var item in System.Enum.GetValues<E>())
                yield return (item, this[item]);
        }
    }
}