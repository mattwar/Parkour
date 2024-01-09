using System.Collections;

namespace Parkour;

public abstract class ImmutableList<T> : IReadOnlyList<T>
{
    public static readonly ImmutableList<T> Empty = new EmptyList();

    private ImmutableList() { }

    public virtual T this[int index] => throw new ArgumentOutOfRangeException();
    public virtual int Count => 0;
    public virtual IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator();

    public abstract ImmutableList<T> Slice(int start, int length);

    public virtual ImmutableList<T> Insert(int index, T item) =>
        InsertRange(index, new ArrayList(new T[] { item }));

    public virtual ImmutableList<T> InsertRange(int index, IEnumerable<T> items) =>
        InsertRange(index, new ArrayList(items));

    public virtual ImmutableList<T> InsertRange(int index, params T[] items) =>
        InsertRange(index, new ArrayList(items));

    public virtual ImmutableList<T> InsertRange(int index, ImmutableList<T> list)
    {
        if (index == 0)
            return Concat(list, this);

        if (index == Count)
            return Concat(this, list);

        return Concat(
            Slice(0, index),
            list,
            Slice(index, Count - index)
            );
    }

    public ImmutableList<T> Append(T item) =>
        Insert(Count, item);

    public ImmutableList<T> AppendRange(IEnumerable<T> items) =>
        InsertRange(Count, items);

    public ImmutableList<T> AppendRange(params T[] items) =>
        InsertRange(Count, items);

    public ImmutableList<T> AppendRange(ImmutableList<T> list) => 
        InsertRange(Count, list);

    public ImmutableList<T> Prepend(T item) =>
        Insert(0, item);

    public ImmutableList<T> PrependRange(IEnumerable<T> list) =>
        InsertRange(0, list);

    public ImmutableList<T> PrependRange(params T[] items) =>
        InsertRange(0, items);

    public ImmutableList<T> PrependRange(ImmutableList<T> list) => 
        InsertRange(0, list);


    public virtual ImmutableList<T> RemoveRange(int start, int count)
    {
        if (start == 0)
            return Slice(count, this.Count - start);
        if (start + count == this.Count)
            return Slice(this.Count - (start + count), count);
        var before = Slice(0, start);
        var after = Slice(start, this.Count - start);
        return Concat(before, after);
    }

    public ImmutableList<T> RemoveAt(int index) => 
        RemoveRange(index, 1);

    public ImmutableList<T> ReplaceAt(int index, T item) =>
        RemoveAt(index)
        .Insert(index, item);

    public ImmutableList<T> ReplaceRange(int start, int count, T item) =>
        RemoveRange(start, count)
        .Insert(start, item);

    public ImmutableList<T> ReplaceRange(int start, int count, IEnumerable<T> items) =>
        RemoveRange(start, count)
        .InsertRange(start, items);

    public ImmutableList<T> ReplaceRange(int start, int count, params T[] items) =>
        RemoveRange(start, count)
        .InsertRange(start, items);

    public ImmutableList<T> ReplaceRange(int start, int count, ImmutableList<T> list) =>
        RemoveRange(start, count)
        .InsertRange(start, list);

    public void CopyTo(int start, Span<T> destination)
    {
        var amount = Math.Min(this.Count - start, destination.Length);
        for (int i = 0; i < amount; i++)
        {
            destination[i] = this[start + i];
        }
    }

    public int IndexOf(T value)
    {
        var comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < this.Count; i++)
        {
            if (comparer.Equals(this[i], value))
                return i;
        }

        return -1;
    }

    #region IReadOnlyList
    int IReadOnlyCollection<T>.Count => this.Count;
    T IReadOnlyList<T>.this[int index] => this[index];
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion

    private static readonly int IdealArrayLength = 50;

    public static ImmutableList<T> Concat(IEnumerable<ImmutableList<T>> lists)
    {
        var newList = new List<ImmutableList<T>>();
        foreach (var list in Flatten(lists))
        {
            if (list.Count == 0)
                continue;

            if (newList.Count > 0 
                && newList[^1].Count + list.Count <= IdealArrayLength)
            {
                // merge last list and next list together
                newList[^1] = new ArrayList(newList[^1].Concat(list));
            }
            else
            {
                newList.Add(list);
            }
        }

        if (newList.Count == 0)
            return ImmutableList<T>.Empty;
        else if (newList.Count == 1)
            return newList[0];
        else 
            return new CombinedList(newList.ToArray());
    }

    private static IEnumerable<ImmutableList<T>> Flatten(IEnumerable<ImmutableList<T>> lists)
    {
        foreach (var list in lists)
        {
            if (list.Count == 0)
                continue;

            if (list is CombinedList clist)
            {
                foreach (var cllist in clist.Lists)
                {
                    yield return cllist;
                }
            }
            else
            {
                yield return list;
            }
        }
    }

    public static ImmutableList<T> Concat(params ImmutableList<T>[] lists) =>
        Concat((IEnumerable<ImmutableList<T>>)lists);




    private sealed class EmptyList : ImmutableList<T>
    {
        public override int Count => 0;
        public override T this[int index] => throw new IndexOutOfRangeException();
        public override IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator();
        public override ImmutableList<T> InsertRange(int index, ImmutableList<T> list) => list;
        public override ImmutableList<T> Slice(int start, int count) => this;
    }

    private sealed class ArrayList : ImmutableList<T>
    {
        private readonly T[] _array;

        public ArrayList(IEnumerable<T> array)
        {
            _array = array.ToArray();
        }

        public override int Count => _array.Length;
        public override T this[int index] => _array[index];
        public override IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_array).GetEnumerator();

        public override ImmutableList<T> InsertRange(int index, ImmutableList<T> list)
        {
            return Concat(
                new ArrayList(_array.AsSpan().Slice(0, index).ToArray()),
                list,
                new ArrayList(_array.AsSpan().Slice(index, _array.Length - index).ToArray())
                );
        }

        public override ImmutableList<T> RemoveRange(int start, int count)
        {
            return Concat(
                new ArrayList(_array.AsSpan().Slice(0, start).ToArray()),
                new ArrayList(_array.AsSpan().Slice(start + count, _array.Length - (start + count)).ToArray())
                );
        }

        public override ImmutableList<T> Slice(int start, int count)
        {
            if (count <= IdealArrayLength)
            {
                return new ArrayList(_array.AsSpan().Slice(start, count).ToArray());
            }
            else
            {
                return new RangedList(this, start, count);
            }
        }
    }

    private sealed class RangedList : ImmutableList<T>
    {
        private readonly ImmutableList<T> _list;
        private readonly int _start;
        private readonly int _length;

        public RangedList(ImmutableList<T> list, int start, int length)
        {
            _list = list;
            _start = start;
            _length = length;
        }

        public override int Count => _length;
        public override T this[int index] => _list[index + _start];

        public override IEnumerator<T> GetEnumerator()
        {
            for (int i = _start, n = _start + _length; i < n; i++)
            {
                yield return _list[i];
            }
        }

        public override ImmutableList<T> Slice(int start, int count)
        {
            return _list.Slice(_start + start, count);
        }
    }

    private sealed class CombinedList : ImmutableList<T>
    {
        private readonly ImmutableList<T>[] _lists;
        private readonly int _totalCount;

        public CombinedList(ImmutableList<T>[] lists, int totalCount = 0)
        {
            _lists = lists;
            _totalCount = totalCount > 0 ? totalCount : CalculateTotalCount(lists);
        }

        public IReadOnlyList<ImmutableList<T>> Lists => _lists;

        private static int CalculateTotalCount(ImmutableList<T>[] lists)
        {
            int total = 0;

            foreach (var list in lists)
            {
                total += list.Count;
            }

            return total;
        }

        public override int Count => _totalCount;

        public override T this[int index]
        {
            get
            {
                for (int b = 0; b < _lists.Length; b++)
                {
                    var list = _lists[b];
                    int listLength = list.Count;
                    if (index < listLength)
                    {
                        return list[index];
                    }
                    index -= listLength;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public override IEnumerator<T> GetEnumerator()
        {
            foreach (var list in _lists)
            {
                foreach (var item in list)
                {
                    yield return item;
                }
            }
        }

        public override ImmutableList<T> Slice(int start, int count)
        {
            var newLists = new List<ImmutableList<T>>();

            foreach (var lst in _lists)
            {
                if (start > lst.Count)
                    continue;

                if (count == 0)
                    break;

                var amount = Math.Min(count, lst.Count);
                newLists.Add(lst.Slice(start, amount));
                start = Math.Max(0, start - amount);
                count -= amount;
            }

            return new CombinedList(newLists.ToArray());
        }
    }
}

public static class ImmutableListExtensions
{
    public static ImmutableList<T> ToImmutableList<T>(this IEnumerable<T> items)
    {
        if (items == null)
            return ImmutableList<T>.Empty;

        if (items is ImmutableList<T> immutable)
            return immutable;

        if ((items is IReadOnlyList<T> roList && roList.Count == 0)
            || !items.Any())
            return ImmutableList<T>.Empty;

        return ImmutableList<T>.Empty.AppendRange(items);
    }
}

public static class ImmutableList
{
    public static ImmutableList<T> Create<T>(IEnumerable<T> items) =>
        items.ToImmutableList();

    public static ImmutableList<T> Create<T>(params T[] items) =>
        items.ToImmutableList();
}
