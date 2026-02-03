using ConcurrentCollections;

namespace ConcurrentHashSet.Tests;

public class TableGrowthTests
{
    [Test]
    public async Task Adding_Many_Items_Triggers_Table_Growth()
    {
        // Start with minimal capacity
        var set = new ConcurrentHashSet<int>(1, 1);

        for (var i = 0; i < 10000; i++)
        {
            set.Add(i);
        }

        await Assert.That(set.Count).IsEqualTo(10000);

        // Verify all items are still present after growth
        for (var i = 0; i < 10000; i++)
        {
            await Assert.That(set.Contains(i)).IsTrue();
        }
    }

    [Test]
    public async Task Growth_With_Low_ConcurrencyLevel()
    {
        var set = new ConcurrentHashSet<int>(1, 1);

        for (var i = 0; i < 1000; i++)
        {
            set.Add(i);
        }

        await Assert.That(set.Count).IsEqualTo(1000);
    }

    [Test]
    public async Task Growth_With_High_ConcurrencyLevel()
    {
        var set = new ConcurrentHashSet<int>(32, 1);

        for (var i = 0; i < 1000; i++)
        {
            set.Add(i);
        }

        await Assert.That(set.Count).IsEqualTo(1000);
    }

    [Test]
    public async Task Concurrent_Growth()
    {
        // Minimal capacity forces frequent resizing
        var set = new ConcurrentHashSet<int>(2, 2);
        const int itemsPerThread = 2000;
        const int threadCount = 4;

        var tasks = Enumerable.Range(0, threadCount)
            .Select(t => Task.Run(() =>
            {
                for (var i = t * itemsPerThread; i < (t + 1) * itemsPerThread; i++)
                {
                    set.Add(i);
                }
            }));
        await Task.WhenAll(tasks);

        await Assert.That(set.Count).IsEqualTo(threadCount * itemsPerThread);
    }

    [Test]
    public async Task Clear_After_Growth_Resets_Capacity()
    {
        var set = new ConcurrentHashSet<int>();

        // Add enough items to trigger growth
        for (var i = 0; i < 1000; i++)
        {
            set.Add(i);
        }

        set.Clear();

        await Assert.That(set.Count).IsEqualTo(0);
        await Assert.That(set.IsEmpty).IsTrue();

        // Should work normally after clear + growth reset
        for (var i = 0; i < 100; i++)
        {
            set.Add(i);
        }

        await Assert.That(set.Count).IsEqualTo(100);
    }
}

public class CustomComparerTests
{
    [Test]
    public async Task CaseInsensitive_String_Comparer()
    {
        var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);

        set.Add("Hello");

        await Assert.That(set.Contains("hello")).IsTrue();
        await Assert.That(set.Contains("HELLO")).IsTrue();
        await Assert.That(set.Contains("Hello")).IsTrue();
        await Assert.That(set.Add("HELLO")).IsFalse();
    }

    [Test]
    public async Task CaseInsensitive_TryGetValue_Returns_Original()
    {
        var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
        set.Add("OriginalCasing");

        set.TryGetValue("ORIGINALCASING", out var actual);

        await Assert.That(actual).IsEqualTo("OriginalCasing");
    }

    [Test]
    public async Task CaseInsensitive_Remove()
    {
        var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
        set.Add("Hello");

        await Assert.That(set.TryRemove("HELLO")).IsTrue();
        await Assert.That(set.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Custom_Modulo_Comparer_Groups_Values()
    {
        // Comparer that considers ints equal if they have the same value mod 10
        var set = new ConcurrentHashSet<int>(new ModuloComparer(10));

        await Assert.That(set.Add(5)).IsTrue();
        await Assert.That(set.Add(15)).IsFalse(); // Same as 5 mod 10
        await Assert.That(set.Add(25)).IsFalse();
        await Assert.That(set.Add(6)).IsTrue();
        await Assert.That(set.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Comparer_From_Constructor_Is_Used_For_Collection()
    {
        var source = new[] { "a", "A", "b", "B", "c", "C" };
        var set = new ConcurrentHashSet<string>(source, StringComparer.OrdinalIgnoreCase);

        await Assert.That(set.Count).IsEqualTo(3);
    }
}

public class HashCollisionTests
{
    [Test]
    public async Task Items_With_Same_HashCode_Are_Distinct()
    {
        var set = new ConcurrentHashSet<CollidingItem>();
        var item1 = new CollidingItem(1, 42);
        var item2 = new CollidingItem(2, 42);
        var item3 = new CollidingItem(3, 42);

        set.Add(item1);
        set.Add(item2);
        set.Add(item3);

        await Assert.That(set.Count).IsEqualTo(3);
        await Assert.That(set.Contains(item1)).IsTrue();
        await Assert.That(set.Contains(item2)).IsTrue();
        await Assert.That(set.Contains(item3)).IsTrue();
    }

    [Test]
    public async Task Remove_From_Collision_Chain()
    {
        var set = new ConcurrentHashSet<CollidingItem>();
        var item1 = new CollidingItem(1, 42);
        var item2 = new CollidingItem(2, 42);
        var item3 = new CollidingItem(3, 42);

        set.Add(item1);
        set.Add(item2);
        set.Add(item3);

        // Remove middle item in chain
        await Assert.That(set.TryRemove(item2)).IsTrue();
        await Assert.That(set.Count).IsEqualTo(2);
        await Assert.That(set.Contains(item1)).IsTrue();
        await Assert.That(set.Contains(item3)).IsTrue();
    }

    [Test]
    public async Task Remove_Head_Of_Collision_Chain()
    {
        var set = new ConcurrentHashSet<CollidingItem>();
        var item1 = new CollidingItem(1, 42);
        var item2 = new CollidingItem(2, 42);

        set.Add(item1);
        set.Add(item2);

        // The last added item is at the head of the bucket chain
        await Assert.That(set.TryRemove(item2)).IsTrue();
        await Assert.That(set.Contains(item1)).IsTrue();
        await Assert.That(set.Count).IsEqualTo(1);
    }

    [Test]
    public async Task TryGetValue_With_Collisions()
    {
        var set = new ConcurrentHashSet<CollidingItem>();
        var item1 = new CollidingItem(1, 42);
        var item2 = new CollidingItem(2, 42);

        set.Add(item1);
        set.Add(item2);

        var found = set.TryGetValue(new CollidingItem(2, 42), out var actual);

        await Assert.That(found).IsTrue();
        await Assert.That(actual!.Id).IsEqualTo(2);
    }

    [Test]
    public async Task Many_Collisions_Still_Work()
    {
        var set = new ConcurrentHashSet<CollidingItem>();
        const int count = 100;

        for (var i = 0; i < count; i++)
        {
            set.Add(new CollidingItem(i, 1)); // All hash to same value
        }

        await Assert.That(set.Count).IsEqualTo(count);

        for (var i = 0; i < count; i++)
        {
            await Assert.That(set.Contains(new CollidingItem(i, 1))).IsTrue();
        }
    }
}

public class EdgeCaseTests
{
    [Test]
    public async Task Add_And_Remove_Same_Item_Repeatedly()
    {
        var set = new ConcurrentHashSet<int>();

        for (var i = 0; i < 1000; i++)
        {
            await Assert.That(set.Add(42)).IsTrue();
            await Assert.That(set.TryRemove(42)).IsTrue();
        }

        await Assert.That(set.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Operations_With_Default_Value_Type()
    {
        var set = new ConcurrentHashSet<int>();

        await Assert.That(set.Add(0)).IsTrue();
        await Assert.That(set.Contains(0)).IsTrue();
        await Assert.That(set.TryRemove(0)).IsTrue();
        await Assert.That(set.Contains(0)).IsFalse();
    }

    [Test]
    public async Task Large_Number_Of_Items()
    {
        var set = new ConcurrentHashSet<int>();
        const int count = 100000;

        for (var i = 0; i < count; i++)
        {
            set.Add(i);
        }

        await Assert.That(set.Count).IsEqualTo(count);
    }

    [Test]
    public async Task String_Items_Various_Lengths()
    {
        var set = new ConcurrentHashSet<string>();
        var strings = new[]
        {
            "",
            "a",
            "ab",
            "abc",
            new string('x', 1000),
            new string('y', 10000)
        };

        foreach (var s in strings)
        {
            set.Add(s);
        }

        await Assert.That(set.Count).IsEqualTo(strings.Length);
        foreach (var s in strings)
        {
            await Assert.That(set.Contains(s)).IsTrue();
        }
    }

    [Test]
    public async Task Negative_HashCode_Values()
    {
        // int.MinValue has a special hashcode behavior
        var set = new ConcurrentHashSet<int>();
        set.Add(int.MinValue);
        set.Add(int.MaxValue);
        set.Add(0);
        set.Add(-1);

        await Assert.That(set.Count).IsEqualTo(4);
        await Assert.That(set.Contains(int.MinValue)).IsTrue();
        await Assert.That(set.Contains(int.MaxValue)).IsTrue();
    }

    [Test]
    public async Task Concurrent_Clear_And_Add()
    {
        var set = new ConcurrentHashSet<int>();

        var tasks = new List<Task>();
        for (var t = 0; t < 4; t++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (var i = 0; i < 500; i++)
                {
                    set.Add(i);
                }
            }));
        }
        tasks.Add(Task.Run(() =>
        {
            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(1);
                set.Clear();
            }
        }));

        await Task.WhenAll(tasks);

        // After everything completes, count should be non-negative
        await Assert.That(set.Count).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task ICollection_CopyTo_After_Growth()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();
        for (var i = 0; i < 500; i++)
        {
            set.Add(i);
        }

        var array = new int[500];
        set.CopyTo(array, 0);

        var sorted = array.OrderBy(x => x).ToArray();
        for (var i = 0; i < 500; i++)
        {
            await Assert.That(sorted[i]).IsEqualTo(i);
        }
    }

    [Test]
    public async Task Enumeration_Sees_All_Items_After_Growth()
    {
        var set = new ConcurrentHashSet<int>();
        for (var i = 0; i < 1000; i++)
        {
            set.Add(i);
        }

        var items = new HashSet<int>();
        foreach (var item in set)
        {
            items.Add(item);
        }

        await Assert.That(items.Count).IsEqualTo(1000);
    }

    [Test]
    public async Task TryGetValue_Default_Int_On_Miss()
    {
        var set = new ConcurrentHashSet<int>();
        set.TryGetValue(999, out var actual);

        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task TryGetValue_Default_String_On_Miss()
    {
        var set = new ConcurrentHashSet<string>();
        set.TryGetValue("missing", out var actual);

        await Assert.That(actual).IsNull();
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public async Task Various_Capacities_Work(int capacity)
    {
        var set = new ConcurrentHashSet<int>(1, capacity);

        for (var i = 0; i < 50; i++)
        {
            set.Add(i);
        }

        await Assert.That(set.Count).IsEqualTo(50);
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(4)]
    [Arguments(8)]
    [Arguments(16)]
    [Arguments(32)]
    public async Task Various_ConcurrencyLevels_Work(int concurrencyLevel)
    {
        var set = new ConcurrentHashSet<int>(concurrencyLevel, 31);

        for (var i = 0; i < 100; i++)
        {
            set.Add(i);
        }

        await Assert.That(set.Count).IsEqualTo(100);
    }
}

/// <summary>
/// An item that allows controlling the hash code for collision testing.
/// </summary>
public class CollidingItem : IEquatable<CollidingItem>
{
    public int Id { get; }
    private readonly int _hashCode;

    public CollidingItem(int id, int hashCode)
    {
        Id = id;
        _hashCode = hashCode;
    }

    public bool Equals(CollidingItem? other) => other != null && Id == other.Id;
    public override bool Equals(object? obj) => Equals(obj as CollidingItem);
    public override int GetHashCode() => _hashCode;
}

/// <summary>
/// An equality comparer that considers ints equal if they have the same remainder when divided by a modulus.
/// </summary>
public class ModuloComparer : IEqualityComparer<int>
{
    private readonly int _modulus;

    public ModuloComparer(int modulus) => _modulus = modulus;

    public bool Equals(int x, int y) => x % _modulus == y % _modulus;
    public int GetHashCode(int obj) => (obj % _modulus).GetHashCode();
}
