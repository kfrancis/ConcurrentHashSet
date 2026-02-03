using System.Reflection;
using ConcurrentCollections;

namespace ConcurrentHashSet.Tests;

/// <summary>
/// These tests use reflection to exercise internal code paths that are unreachable
/// through the public API under normal conditions. They target defensive guards
/// ported from ConcurrentDictionary that protect against extreme states.
/// </summary>
public class NullableAttributesCoverageTests
{
    [Test]
    public async Task MaybeNullWhenAttribute_Constructor_Sets_ReturnValue_True()
    {
        var assembly = typeof(ConcurrentHashSet<>).Assembly;
        var attrType = assembly.GetType("System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute");

        // On runtimes where the BCL already provides this attribute (netstandard2.1+),
        // the polyfill is not compiled into the assembly.
        if (attrType == null)
            return;

        var ctor = attrType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, [typeof(bool)], null)!;

        var instance = ctor.Invoke([true]);
        var returnValue = (bool)attrType.GetProperty("ReturnValue")!.GetValue(instance)!;

        await Assert.That(returnValue).IsTrue();
    }

    [Test]
    public async Task MaybeNullWhenAttribute_Constructor_Sets_ReturnValue_False()
    {
        var assembly = typeof(ConcurrentHashSet<>).Assembly;
        var attrType = assembly.GetType("System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute");

        if (attrType == null)
            return;

        var ctor = attrType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, [typeof(bool)], null)!;

        var instance = ctor.Invoke([false]);
        var returnValue = (bool)attrType.GetProperty("ReturnValue")!.GetValue(instance)!;

        await Assert.That(returnValue).IsFalse();
    }
}

public class InitializeFromCollectionCoverageTests
{
    [Test]
    public async Task InitializeFromCollection_Recalculates_Budget_When_Zero()
    {
        // The private constructor always sets _budget >= 1 (since capacity >= concurrencyLevel >= 1).
        // The _budget == 0 guard in InitializeFromCollection is defensive dead code ported from
        // ConcurrentDictionary. We exercise it via reflection.
        var set = new ConcurrentHashSet<int>();

        var budgetField = typeof(ConcurrentHashSet<int>)
            .GetField("_budget", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var initMethod = typeof(ConcurrentHashSet<int>)
            .GetMethod("InitializeFromCollection", BindingFlags.Instance | BindingFlags.NonPublic)!;

        // Force _budget to 0, then call InitializeFromCollection with an empty collection.
        // The foreach loop does nothing, then the _budget == 0 guard triggers.
        budgetField.SetValue(set, 0);
        initMethod.Invoke(set, [Array.Empty<int>()]);

        var newBudget = (int)budgetField.GetValue(set)!;
        await Assert.That(newBudget).IsGreaterThan(0);
    }

    [Test]
    public async Task InitializeFromCollection_Recalculates_Budget_When_Zero_With_Items()
    {
        var set = new ConcurrentHashSet<int>();

        var budgetField = typeof(ConcurrentHashSet<int>)
            .GetField("_budget", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var initMethod = typeof(ConcurrentHashSet<int>)
            .GetMethod("InitializeFromCollection", BindingFlags.Instance | BindingFlags.NonPublic)!;

        // Set _budget to 0, add items, then verify budget is recalculated
        budgetField.SetValue(set, 0);
        initMethod.Invoke(set, [new[] { 10, 20, 30 }]);

        var newBudget = (int)budgetField.GetValue(set)!;
        await Assert.That(newBudget).IsGreaterThan(0);
        await Assert.That(set.Count).IsEqualTo(3);
    }
}

public class GrowTableCoverageTests
{
    [Test]
    public async Task GrowTable_Budget_Overflow_To_Negative_Sets_IntMaxValue()
    {
        // When the bucket array is sparsely populated, GrowTable doubles the budget instead
        // of resizing. If _budget is near int.MaxValue/2, doubling overflows to negative.
        // The guard sets _budget = int.MaxValue. This state is unreachable through normal
        // API usage because not enough consecutive doublings can occur, but we exercise it
        // via reflection for coverage.
        var set = new ConcurrentHashSet<int>(128, 128);

        // Add a few items to make the set non-empty but very sparse
        // (approxCount will be << buckets.Length / 4, triggering budget doubling)
        set.Add(1);
        set.Add(2);

        var budgetField = typeof(ConcurrentHashSet<int>)
            .GetField("_budget", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var tablesField = typeof(ConcurrentHashSet<int>)
            .GetField("_tables", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var growMethod = typeof(ConcurrentHashSet<int>)
            .GetMethod("GrowTable", BindingFlags.Instance | BindingFlags.NonPublic)!;

        // Set budget to just above int.MaxValue/2 so that doubling wraps negative
        budgetField.SetValue(set, int.MaxValue / 2 + 1);

        var tables = tablesField.GetValue(set)!;
        growMethod.Invoke(set, [tables]);

        // After overflow, the guard should have set _budget to int.MaxValue
        var newBudget = (int)budgetField.GetValue(set)!;
        await Assert.That(newBudget).IsEqualTo(int.MaxValue);
    }

    [Test]
    public async Task GrowTable_Budget_Doubling_Path_Preserves_Items()
    {
        // Verify that after the budget-doubling early return in GrowTable,
        // the set still functions correctly
        var set = new ConcurrentHashSet<int>(128, 128);
        set.Add(1);
        set.Add(2);

        var budgetField = typeof(ConcurrentHashSet<int>)
            .GetField("_budget", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var tablesField = typeof(ConcurrentHashSet<int>)
            .GetField("_tables", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var growMethod = typeof(ConcurrentHashSet<int>)
            .GetMethod("GrowTable", BindingFlags.Instance | BindingFlags.NonPublic)!;

        budgetField.SetValue(set, int.MaxValue / 2 + 1);
        var tables = tablesField.GetValue(set)!;
        growMethod.Invoke(set, [tables]);

        // Set should still be usable after the overflow guard fires
        await Assert.That(set.Contains(1)).IsTrue();
        await Assert.That(set.Contains(2)).IsTrue();
        await Assert.That(set.Count).IsEqualTo(2);

        // Can still add and remove items (budget is int.MaxValue, so no more growth)
        set.Add(3);
        await Assert.That(set.Count).IsEqualTo(3);
        set.TryRemove(1);
        await Assert.That(set.Count).IsEqualTo(2);
    }

    // UNCOVERABLE: GrowTable lines 746-766 (6 lines, 2 branches)
    //
    // The maximizeTableSize guard in GrowTable activates when either:
    //   1. newLength (= Buckets.Length * 2 + 1) exceeds maxArrayLength (0x7FEFFFFF), or
    //   2. The checked multiplication Buckets.Length * 2 + 1 throws OverflowException
    //
    // Both require Buckets.Length > 1,073,217,535, meaning a Node?[] array of >1 billion
    // references (~8GB on 64-bit). This cannot be allocated in a test environment.
    // These guards are defensive code ported directly from .NET's ConcurrentDictionary
    // and protect against theoretical multi-billion-element hash tables.
}
