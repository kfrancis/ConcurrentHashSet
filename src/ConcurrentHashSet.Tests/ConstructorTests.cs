using ConcurrentCollections;

namespace ConcurrentHashSet.Tests;

public class ConstructorTests
{
    [Test]
    public async Task Default_Constructor_Creates_Empty_Set()
    {
        var set = new ConcurrentHashSet<int>();

        await Assert.That(set.Count).IsEqualTo(0);
        await Assert.That(set.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Default_Constructor_Uses_Default_Comparer()
    {
        var set = new ConcurrentHashSet<int>();

        await Assert.That(set.Comparer).IsEqualTo(EqualityComparer<int>.Default);
    }

    [Test]
    public async Task ConcurrencyLevel_And_Capacity_Constructor_Creates_Empty_Set()
    {
        var set = new ConcurrentHashSet<int>(4, 100);

        await Assert.That(set.Count).IsEqualTo(0);
        await Assert.That(set.IsEmpty).IsTrue();
    }

    [Test]
    public async Task ConcurrencyLevel_And_Capacity_Constructor_Uses_Default_Comparer()
    {
        var set = new ConcurrentHashSet<string>(4, 100);

        await Assert.That(set.Comparer).IsEqualTo(EqualityComparer<string>.Default);
    }

    [Test]
    public async Task ConcurrencyLevel_Less_Than_One_Throws()
    {
        await Assert.That(() => new ConcurrentHashSet<int>(0, 10))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Negative_Capacity_Throws()
    {
        await Assert.That(() => new ConcurrentHashSet<int>(1, -1))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Collection_Constructor_Copies_Elements()
    {
        var source = new[] { 1, 2, 3, 4, 5 };
        var set = new ConcurrentHashSet<int>(source);

        await Assert.That(set.Count).IsEqualTo(5);
        foreach (var item in source)
        {
            await Assert.That(set.Contains(item)).IsTrue();
        }
    }

    [Test]
    public async Task Collection_Constructor_Deduplicates()
    {
        var source = new[] { 1, 2, 2, 3, 3, 3 };
        var set = new ConcurrentHashSet<int>(source);

        await Assert.That(set.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Collection_Constructor_Null_Collection_Throws()
    {
        await Assert.That(() => new ConcurrentHashSet<int>((IEnumerable<int>)null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Comparer_Constructor_Uses_Provided_Comparer()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var set = new ConcurrentHashSet<string>(comparer);

        await Assert.That(set.Comparer).IsEqualTo(comparer);
    }

    [Test]
    public async Task Comparer_Constructor_Null_Comparer_Uses_Default()
    {
        var set = new ConcurrentHashSet<string>((IEqualityComparer<string>?)null);

        await Assert.That(set.Comparer).IsEqualTo(EqualityComparer<string>.Default);
    }

    [Test]
    public async Task Collection_And_Comparer_Constructor_Works()
    {
        var source = new[] { "foo", "FOO", "bar" };
        var set = new ConcurrentHashSet<string>(source, StringComparer.OrdinalIgnoreCase);

        await Assert.That(set.Count).IsEqualTo(2);
        await Assert.That(set.Comparer).IsEqualTo(StringComparer.OrdinalIgnoreCase);
    }

    [Test]
    public async Task Collection_And_Comparer_Constructor_Null_Collection_Throws()
    {
        await Assert.That(() => new ConcurrentHashSet<string>(null!, StringComparer.OrdinalIgnoreCase))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ConcurrencyLevel_Collection_Comparer_Constructor_Works()
    {
        var source = new[] { "a", "A", "b", "B" };
        var set = new ConcurrentHashSet<string>(4, source, StringComparer.OrdinalIgnoreCase);

        await Assert.That(set.Count).IsEqualTo(2);
        await Assert.That(set.Comparer).IsEqualTo(StringComparer.OrdinalIgnoreCase);
    }

    [Test]
    public async Task ConcurrencyLevel_Collection_Comparer_Constructor_Null_Collection_Throws()
    {
        await Assert.That(() => new ConcurrentHashSet<string>(4, null!, StringComparer.OrdinalIgnoreCase))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ConcurrencyLevel_Collection_Comparer_Constructor_Invalid_ConcurrencyLevel_Throws()
    {
        await Assert.That(() => new ConcurrentHashSet<string>(0, new[] { "a" }, StringComparer.Ordinal))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ConcurrencyLevel_Capacity_Comparer_Constructor_Works()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var set = new ConcurrentHashSet<string>(4, 50, comparer);

        await Assert.That(set.Count).IsEqualTo(0);
        await Assert.That(set.Comparer).IsEqualTo(comparer);
    }

    [Test]
    public async Task ConcurrencyLevel_Capacity_Comparer_Constructor_Invalid_ConcurrencyLevel_Throws()
    {
        await Assert.That(() => new ConcurrentHashSet<int>(0, 10, null))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ConcurrencyLevel_Capacity_Comparer_Constructor_Negative_Capacity_Throws()
    {
        await Assert.That(() => new ConcurrentHashSet<int>(1, -1, null))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ConcurrencyLevel_Capacity_Comparer_Constructor_Null_Comparer_Uses_Default()
    {
        var set = new ConcurrentHashSet<int>(4, 50, null);

        await Assert.That(set.Comparer).IsEqualTo(EqualityComparer<int>.Default);
    }

    [Test]
    public async Task Capacity_Less_Than_ConcurrencyLevel_Adjusts_Upward()
    {
        // capacity < concurrencyLevel should not throw; capacity is adjusted
        var set = new ConcurrentHashSet<int>(8, 2);

        await Assert.That(set.Count).IsEqualTo(0);
        // Should still work properly
        set.Add(1);
        await Assert.That(set.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Constructor_With_Empty_Collection()
    {
        var set = new ConcurrentHashSet<int>(Array.Empty<int>());

        await Assert.That(set.Count).IsEqualTo(0);
        await Assert.That(set.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Constructor_With_Large_Collection()
    {
        var source = Enumerable.Range(0, 10000).ToList();
        var set = new ConcurrentHashSet<int>(source);

        await Assert.That(set.Count).IsEqualTo(10000);
        foreach (var item in source)
        {
            await Assert.That(set.Contains(item)).IsTrue();
        }
    }

    [Test]
    public async Task Constructor_ConcurrencyLevel_One_Works()
    {
        var set = new ConcurrentHashSet<int>(1, 10);
        set.Add(1);
        set.Add(2);

        await Assert.That(set.Count).IsEqualTo(2);
    }
}
