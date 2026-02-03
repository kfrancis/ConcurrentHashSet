using ConcurrentCollections;
using System.Collections;

namespace ConcurrentHashSet.Tests;

public class ICollectionAddTests
{
    [Test]
    public async Task ICollection_Add_Adds_Item()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();
        set.Add(42);

        await Assert.That(set.Count).IsEqualTo(1);
        await Assert.That(set.Contains(42)).IsTrue();
    }

    [Test]
    public async Task ICollection_Add_Duplicate_Does_Not_Throw()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();
        set.Add(42);
        set.Add(42); // Should not throw, just silently ignores

        await Assert.That(set.Count).IsEqualTo(1);
    }
}

public class ICollectionRemoveTests
{
    [Test]
    public async Task ICollection_Remove_Existing_Returns_True()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();
        set.Add(42);

        await Assert.That(set.Remove(42)).IsTrue();
    }

    [Test]
    public async Task ICollection_Remove_NonExisting_Returns_False()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();

        await Assert.That(set.Remove(42)).IsFalse();
    }
}

public class IsReadOnlyTests
{
    [Test]
    public async Task IsReadOnly_Returns_False()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();

        await Assert.That(set.IsReadOnly).IsFalse();
    }
}

public class CopyToTests
{
    [Test]
    public async Task CopyTo_Copies_All_Items()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();
        set.Add(1);
        set.Add(2);
        set.Add(3);

        var array = new int[3];
        set.CopyTo(array, 0);

        // Items may not be in insertion order, but all must be present
        var sorted = array.OrderBy(x => x).ToArray();
        await Assert.That(sorted[0]).IsEqualTo(1);
        await Assert.That(sorted[1]).IsEqualTo(2);
        await Assert.That(sorted[2]).IsEqualTo(3);
    }

    [Test]
    public async Task CopyTo_With_Offset()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();
        set.Add(10);
        set.Add(20);

        var array = new int[5];
        set.CopyTo(array, 2);

        // First two should be 0 (default), items at indices 2-3
        await Assert.That(array[0]).IsEqualTo(0);
        await Assert.That(array[1]).IsEqualTo(0);

        var copied = array.Skip(2).Where(x => x != 0).OrderBy(x => x).ToArray();
        await Assert.That(copied).Contains(10);
        await Assert.That(copied).Contains(20);
    }

    [Test]
    public async Task CopyTo_Null_Array_Throws()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();

        await Assert.That(() => set.CopyTo(null!, 0))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task CopyTo_Negative_Index_Throws()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();

        await Assert.That(() => set.CopyTo(new int[5], -1))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task CopyTo_Insufficient_Space_Throws()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();
        set.Add(1);
        set.Add(2);
        set.Add(3);

        await Assert.That(() => set.CopyTo(new int[2], 0))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task CopyTo_Insufficient_Space_Due_To_Offset_Throws()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();
        set.Add(1);
        set.Add(2);

        await Assert.That(() => set.CopyTo(new int[3], 2))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task CopyTo_Empty_Set_Does_Nothing()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();
        var array = new int[] { 99, 99, 99 };
        set.CopyTo(array, 0);

        await Assert.That(array[0]).IsEqualTo(99);
        await Assert.That(array[1]).IsEqualTo(99);
        await Assert.That(array[2]).IsEqualTo(99);
    }

    [Test]
    public async Task CopyTo_Exact_Size_Array()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();
        set.Add(1);
        set.Add(2);

        var array = new int[2];
        set.CopyTo(array, 0);

        var sorted = array.OrderBy(x => x).ToArray();
        await Assert.That(sorted[0]).IsEqualTo(1);
        await Assert.That(sorted[1]).IsEqualTo(2);
    }
}

public class IEnumerableInterfaceTests
{
    [Test]
    public async Task IEnumerable_Generic_GetEnumerator_Works()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });
        IEnumerable<int> enumerable = set;

        var items = new List<int>();
        foreach (var item in enumerable)
        {
            items.Add(item);
        }

        await Assert.That(items.Count).IsEqualTo(3);
        await Assert.That(items).Contains(1);
        await Assert.That(items).Contains(2);
        await Assert.That(items).Contains(3);
    }

    [Test]
    public async Task IEnumerable_NonGeneric_GetEnumerator_Works()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });
        IEnumerable enumerable = set;

        var items = new List<int>();
        foreach (int item in enumerable)
        {
            items.Add(item);
        }

        await Assert.That(items.Count).IsEqualTo(3);
        await Assert.That(items).Contains(1);
        await Assert.That(items).Contains(2);
        await Assert.That(items).Contains(3);
    }
}
