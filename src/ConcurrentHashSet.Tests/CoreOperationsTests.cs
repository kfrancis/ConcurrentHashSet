using ConcurrentCollections;

namespace ConcurrentHashSet.Tests;

public class AddTests
{
    [Test]
    public async Task Add_New_Item_Returns_True()
    {
        var set = new ConcurrentHashSet<int>();

        await Assert.That(set.Add(42)).IsTrue();
    }

    [Test]
    public async Task Add_Duplicate_Item_Returns_False()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(42);

        await Assert.That(set.Add(42)).IsFalse();
    }

    [Test]
    public async Task Add_Increments_Count()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(1);
        set.Add(2);
        set.Add(3);

        await Assert.That(set.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Add_Duplicate_Does_Not_Increment_Count()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(1);
        set.Add(1);

        await Assert.That(set.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Add_Multiple_Distinct_Items()
    {
        var set = new ConcurrentHashSet<string>();

        await Assert.That(set.Add("a")).IsTrue();
        await Assert.That(set.Add("b")).IsTrue();
        await Assert.That(set.Add("c")).IsTrue();
        await Assert.That(set.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Add_With_Custom_Comparer_Respects_Equality()
    {
        var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);

        await Assert.That(set.Add("Hello")).IsTrue();
        await Assert.That(set.Add("HELLO")).IsFalse();
        await Assert.That(set.Add("hello")).IsFalse();
        await Assert.That(set.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Add_Null_Item_For_Reference_Type()
    {
        var set = new ConcurrentHashSet<string?>(new NullSafeComparer());
        await Assert.That(set.Add(null)).IsTrue();
        await Assert.That(set.Add(null)).IsFalse();
        await Assert.That(set.Count).IsEqualTo(1);
    }
}

public class TryRemoveTests
{
    [Test]
    public async Task TryRemove_Existing_Item_Returns_True()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(42);

        await Assert.That(set.TryRemove(42)).IsTrue();
    }

    [Test]
    public async Task TryRemove_NonExisting_Item_Returns_False()
    {
        var set = new ConcurrentHashSet<int>();

        await Assert.That(set.TryRemove(42)).IsFalse();
    }

    [Test]
    public async Task TryRemove_Decrements_Count()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(1);
        set.Add(2);
        set.TryRemove(1);

        await Assert.That(set.Count).IsEqualTo(1);
    }

    [Test]
    public async Task TryRemove_Item_No_Longer_Contained()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(42);
        set.TryRemove(42);

        await Assert.That(set.Contains(42)).IsFalse();
    }

    [Test]
    public async Task TryRemove_Same_Item_Twice_Returns_False_Second_Time()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(42);

        await Assert.That(set.TryRemove(42)).IsTrue();
        await Assert.That(set.TryRemove(42)).IsFalse();
    }

    [Test]
    public async Task TryRemove_From_Empty_Set()
    {
        var set = new ConcurrentHashSet<int>();

        await Assert.That(set.TryRemove(1)).IsFalse();
    }

    [Test]
    public async Task TryRemove_With_Custom_Comparer()
    {
        var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
        set.Add("Hello");

        await Assert.That(set.TryRemove("HELLO")).IsTrue();
        await Assert.That(set.IsEmpty).IsTrue();
    }

    [Test]
    public async Task TryRemove_First_Node_In_Bucket()
    {
        // Use a small capacity to increase collision likelihood
        var set = new ConcurrentHashSet<int>(1, 1);
        set.Add(1);
        set.Add(2);

        await Assert.That(set.TryRemove(1)).IsTrue();
        await Assert.That(set.Contains(2)).IsTrue();
    }

    [Test]
    public async Task TryRemove_Middle_Node_In_Chain()
    {
        // Small capacity forces collisions in same bucket
        var set = new ConcurrentHashSet<int>(1, 1);
        set.Add(1);
        set.Add(2);
        set.Add(3);

        await Assert.That(set.TryRemove(2)).IsTrue();
        await Assert.That(set.Contains(1)).IsTrue();
        await Assert.That(set.Contains(3)).IsTrue();
    }
}

public class ContainsTests
{
    [Test]
    public async Task Contains_Existing_Item_Returns_True()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(42);

        await Assert.That(set.Contains(42)).IsTrue();
    }

    [Test]
    public async Task Contains_NonExisting_Item_Returns_False()
    {
        var set = new ConcurrentHashSet<int>();

        await Assert.That(set.Contains(42)).IsFalse();
    }

    [Test]
    public async Task Contains_After_Remove_Returns_False()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(42);
        set.TryRemove(42);

        await Assert.That(set.Contains(42)).IsFalse();
    }

    [Test]
    public async Task Contains_With_Custom_Comparer()
    {
        var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
        set.Add("Hello");

        await Assert.That(set.Contains("HELLO")).IsTrue();
        await Assert.That(set.Contains("hello")).IsTrue();
        await Assert.That(set.Contains("World")).IsFalse();
    }

    [Test]
    public async Task Contains_On_Empty_Set()
    {
        var set = new ConcurrentHashSet<string>();

        await Assert.That(set.Contains("anything")).IsFalse();
    }
}

public class TryGetValueTests
{
    [Test]
    public async Task TryGetValue_Existing_Item_Returns_True_And_Value()
    {
        var set = new ConcurrentHashSet<string>();
        set.Add("hello");

        var found = set.TryGetValue("hello", out var actual);

        await Assert.That(found).IsTrue();
        await Assert.That(actual).IsEqualTo("hello");
    }

    [Test]
    public async Task TryGetValue_NonExisting_Returns_False_And_Default()
    {
        var set = new ConcurrentHashSet<string>();

        var found = set.TryGetValue("missing", out var actual);

        await Assert.That(found).IsFalse();
        await Assert.That(actual).IsNull();
    }

    [Test]
    public async Task TryGetValue_Returns_Stored_Reference()
    {
        // Case-insensitive comparer: lookup with different casing returns the originally stored string
        var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
        set.Add("Hello");

        var found = set.TryGetValue("HELLO", out var actual);

        await Assert.That(found).IsTrue();
        await Assert.That(actual).IsEqualTo("Hello");
        await Assert.That(ReferenceEquals(actual, "Hello")).IsTrue();
    }

    [Test]
    public async Task TryGetValue_On_Empty_Set()
    {
        var set = new ConcurrentHashSet<int>();

        var found = set.TryGetValue(42, out var actual);

        await Assert.That(found).IsFalse();
        await Assert.That(actual).IsEqualTo(default(int));
    }
}

public class ClearTests
{
    [Test]
    public async Task Clear_Removes_All_Items()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3, 4, 5 });
        set.Clear();

        await Assert.That(set.Count).IsEqualTo(0);
        await Assert.That(set.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Clear_On_Empty_Set_Is_Noop()
    {
        var set = new ConcurrentHashSet<int>();
        set.Clear();

        await Assert.That(set.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_Allows_ReAdding_Items()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(1);
        set.Clear();

        await Assert.That(set.Add(1)).IsTrue();
        await Assert.That(set.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Clear_Multiple_Times()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });
        set.Clear();
        set.Clear();

        await Assert.That(set.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_Then_Add_Many_Items()
    {
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, 1000));

        await Assert.That(set.Count).IsEqualTo(1000);

        set.Clear();

        await Assert.That(set.Count).IsEqualTo(0);

        foreach (var i in Enumerable.Range(0, 500))
        {
            set.Add(i);
        }

        await Assert.That(set.Count).IsEqualTo(500);
    }
}

public class CountTests
{
    [Test]
    public async Task Count_Empty_Set_Returns_Zero()
    {
        var set = new ConcurrentHashSet<int>();

        await Assert.That(set.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Count_After_Adds()
    {
        var set = new ConcurrentHashSet<int>();
        for (var i = 0; i < 100; i++)
        {
            set.Add(i);
        }

        await Assert.That(set.Count).IsEqualTo(100);
    }

    [Test]
    public async Task Count_After_Adds_And_Removes()
    {
        var set = new ConcurrentHashSet<int>();
        for (var i = 0; i < 100; i++)
        {
            set.Add(i);
        }
        for (var i = 0; i < 50; i++)
        {
            set.TryRemove(i);
        }

        await Assert.That(set.Count).IsEqualTo(50);
    }
}

public class IsEmptyTests
{
    [Test]
    public async Task IsEmpty_New_Set_Returns_True()
    {
        var set = new ConcurrentHashSet<int>();

        await Assert.That(set.IsEmpty).IsTrue();
    }

    [Test]
    public async Task IsEmpty_After_Add_Returns_False()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(1);

        await Assert.That(set.IsEmpty).IsFalse();
    }

    [Test]
    public async Task IsEmpty_After_Add_And_Remove_Returns_True()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(1);
        set.TryRemove(1);

        await Assert.That(set.IsEmpty).IsTrue();
    }

    [Test]
    public async Task IsEmpty_After_Clear_Returns_True()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });
        set.Clear();

        await Assert.That(set.IsEmpty).IsTrue();
    }
}

public class ComparerPropertyTests
{
    [Test]
    public async Task Comparer_Returns_Default_When_None_Specified()
    {
        var set = new ConcurrentHashSet<int>();

        await Assert.That(set.Comparer).IsEqualTo(EqualityComparer<int>.Default);
    }

    [Test]
    public async Task Comparer_Returns_Specified_Comparer()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var set = new ConcurrentHashSet<string>(comparer);

        await Assert.That(set.Comparer).IsEqualTo(comparer);
    }
}

/// <summary>
/// A comparer that handles null values safely for testing purposes.
/// </summary>
public class NullSafeComparer : IEqualityComparer<string?>
{
    public bool Equals(string? x, string? y) => string.Equals(x, y);
    public int GetHashCode(string? obj) => obj?.GetHashCode() ?? 0;
}
