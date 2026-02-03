using ConcurrentCollections;
using System.Collections;

namespace ConcurrentHashSet.Tests;

public class StructEnumeratorTests
{
    [Test]
    public async Task GetEnumerator_Returns_Struct_Enumerator()
    {
        var set = new ConcurrentHashSet<int>();

        // GetEnumerator() returns the struct Enumerator directly
        var enumerator = set.GetEnumerator();

        await Assert.That(enumerator).IsTypeOf<ConcurrentHashSet<int>.Enumerator>();
    }

    [Test]
    public async Task Enumerator_Empty_Set_MoveNext_Returns_False()
    {
        var set = new ConcurrentHashSet<int>();
        var enumerator = set.GetEnumerator();

        await Assert.That(enumerator.MoveNext()).IsFalse();
    }

    [Test]
    public async Task Enumerator_Iterates_All_Items()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3, 4, 5 });
        var items = new List<int>();

        var enumerator = set.GetEnumerator();
        while (enumerator.MoveNext())
        {
            items.Add(enumerator.Current);
        }

        await Assert.That(items.Count).IsEqualTo(5);
        await Assert.That(items).Contains(1);
        await Assert.That(items).Contains(2);
        await Assert.That(items).Contains(3);
        await Assert.That(items).Contains(4);
        await Assert.That(items).Contains(5);
    }

    [Test]
    public async Task Enumerator_Current_Returns_Correct_Value()
    {
        var set = new ConcurrentHashSet<string>();
        set.Add("only");

        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();

        await Assert.That(enumerator.Current).IsEqualTo("only");
    }

    [Test]
    public async Task Enumerator_MoveNext_Returns_False_After_Last_Item()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(1);

        var enumerator = set.GetEnumerator();
        enumerator.MoveNext(); // first item

        await Assert.That(enumerator.MoveNext()).IsFalse();
    }

    [Test]
    public async Task Enumerator_MoveNext_Returns_False_Repeatedly_After_Exhaustion()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(1);

        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        enumerator.MoveNext(); // past end

        await Assert.That(enumerator.MoveNext()).IsFalse();
        await Assert.That(enumerator.MoveNext()).IsFalse();
    }

    [Test]
    public async Task Enumerator_Reset_Allows_ReEnumeration()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        var enumerator = set.GetEnumerator();

        // First pass
        var firstPass = new List<int>();
        while (enumerator.MoveNext())
        {
            firstPass.Add(enumerator.Current);
        }

        enumerator.Reset();

        // Second pass
        var secondPass = new List<int>();
        while (enumerator.MoveNext())
        {
            secondPass.Add(enumerator.Current);
        }

        await Assert.That(firstPass.Count).IsEqualTo(3);
        await Assert.That(secondPass.Count).IsEqualTo(3);
        await Assert.That(firstPass.OrderBy(x => x)).IsEquivalentTo(secondPass.OrderBy(x => x));
    }

    [Test]
    public async Task Enumerator_Dispose_Is_Safe()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });
        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        enumerator.Dispose(); // Should not throw
        // If we reached here, Dispose did not throw
    }

    [Test]
    public async Task Enumerator_IEnumerator_Current_Returns_Object()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(42);

        IEnumerator enumerator = ((IEnumerable)set).GetEnumerator();
        enumerator.MoveNext();

        await Assert.That(enumerator.Current).IsEqualTo((object)42);
    }

    [Test]
    public async Task Foreach_Works_With_Struct_Enumerator()
    {
        var set = new ConcurrentHashSet<int>(new[] { 10, 20, 30 });
        var items = new List<int>();

        foreach (var item in set)
        {
            items.Add(item);
        }

        await Assert.That(items.Count).IsEqualTo(3);
        await Assert.That(items).Contains(10);
        await Assert.That(items).Contains(20);
        await Assert.That(items).Contains(30);
    }

    [Test]
    public async Task Enumerator_Single_Item()
    {
        var set = new ConcurrentHashSet<string>();
        set.Add("only");

        var items = new List<string>();
        foreach (var item in set)
        {
            items.Add(item);
        }

        await Assert.That(items.Count).IsEqualTo(1);
        await Assert.That(items[0]).IsEqualTo("only");
    }

    [Test]
    public async Task Enumerator_Large_Set()
    {
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, 5000));
        var items = new List<int>();

        foreach (var item in set)
        {
            items.Add(item);
        }

        await Assert.That(items.Count).IsEqualTo(5000);
        await Assert.That(items.Distinct().Count()).IsEqualTo(5000);
    }

    [Test]
    public async Task LINQ_ToList_Works()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });
        // This uses IEnumerable<T>.GetEnumerator()
        var list = set.ToList();

        await Assert.That(list.Count).IsEqualTo(3);
        await Assert.That(list).Contains(1);
        await Assert.That(list).Contains(2);
        await Assert.That(list).Contains(3);
    }

    [Test]
    public async Task LINQ_Count_Works()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3, 4, 5 });

        await Assert.That(set.Count()).IsEqualTo(5);
    }

    [Test]
    public async Task LINQ_Any_Works()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        await Assert.That(set.Any(x => x == 2)).IsTrue();
        await Assert.That(set.Any(x => x == 99)).IsFalse();
    }
}
