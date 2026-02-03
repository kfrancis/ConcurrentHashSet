using ConcurrentCollections;

namespace ConcurrentHashSet.Tests;

public class ConcurrentAddTests
{
    [Test]
    public async Task Concurrent_Adds_All_Unique_Items_Present()
    {
        var set = new ConcurrentHashSet<int>();
        const int itemCount = 10000;

        var tasks = Enumerable.Range(0, itemCount)
            .Select(i => Task.Run(() => set.Add(i)));
        await Task.WhenAll(tasks);

        await Assert.That(set.Count).IsEqualTo(itemCount);
        for (var i = 0; i < itemCount; i++)
        {
            await Assert.That(set.Contains(i)).IsTrue();
        }
    }

    [Test]
    public async Task Concurrent_Adds_Duplicates_Return_Correctly()
    {
        var set = new ConcurrentHashSet<int>();
        const int threadCount = 100;
        var results = new bool[threadCount];

        // All threads try to add the same item
        var tasks = Enumerable.Range(0, threadCount)
            .Select(i => Task.Run(() => results[i] = set.Add(42)));
        await Task.WhenAll(tasks);

        // Exactly one should return true
        await Assert.That(results.Count(r => r)).IsEqualTo(1);
        await Assert.That(set.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Concurrent_Adds_From_Multiple_Threads()
    {
        var set = new ConcurrentHashSet<int>();
        const int itemsPerThread = 1000;
        const int threadCount = 8;

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
}

public class ConcurrentRemoveTests
{
    [Test]
    public async Task Concurrent_Removes_All_Succeed_For_Existing_Items()
    {
        const int itemCount = 10000;
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, itemCount));

        var tasks = Enumerable.Range(0, itemCount)
            .Select(i => Task.Run(() => set.TryRemove(i)));
        var results = await Task.WhenAll(tasks);

        await Assert.That(results.All(r => r)).IsTrue();
        await Assert.That(set.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Concurrent_Remove_Same_Item_Only_One_Succeeds()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(42);

        const int threadCount = 100;
        var results = new bool[threadCount];

        var tasks = Enumerable.Range(0, threadCount)
            .Select(i => Task.Run(() => results[i] = set.TryRemove(42)));
        await Task.WhenAll(tasks);

        await Assert.That(results.Count(r => r)).IsEqualTo(1);
        await Assert.That(set.IsEmpty).IsTrue();
    }
}

public class ConcurrentMixedOperationsTests
{
    [Test]
    public async Task Concurrent_Adds_And_Removes()
    {
        var set = new ConcurrentHashSet<int>();
        const int iterations = 5000;

        var addTasks = Enumerable.Range(0, iterations)
            .Select(i => Task.Run(() => set.Add(i)));
        var removeTasks = Enumerable.Range(0, iterations / 2)
            .Select(i => Task.Run(() => set.TryRemove(i)));

        await Task.WhenAll(addTasks.Concat(removeTasks));

        // We can't know the exact count due to ordering, but it should be non-negative
        await Assert.That(set.Count).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Concurrent_Adds_And_Contains()
    {
        var set = new ConcurrentHashSet<int>();
        const int itemCount = 5000;
        var addsDone = new TaskCompletionSource();

        var addTask = Task.Run(() =>
        {
            for (var i = 0; i < itemCount; i++)
            {
                set.Add(i);
            }
            addsDone.SetResult();
        });

        var containsTask = Task.Run(async () =>
        {
            // Keep checking contains while adds are happening
            while (!addsDone.Task.IsCompleted)
            {
                // Should not throw
                set.Contains(0);
                set.Contains(itemCount / 2);
                set.Contains(itemCount - 1);
                await Task.Yield();
            }
        });

        await Task.WhenAll(addTask, containsTask);

        await Assert.That(set.Count).IsEqualTo(itemCount);
    }

    [Test]
    public async Task Concurrent_Adds_Removes_Contains_Clear()
    {
        var set = new ConcurrentHashSet<int>();
        const int iterations = 2000;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var tasks = new List<Task>
        {
            Task.Run(() =>
            {
                for (var i = 0; i < iterations && !cts.Token.IsCancellationRequested; i++)
                    set.Add(i);
            }),
            Task.Run(() =>
            {
                for (var i = 0; i < iterations && !cts.Token.IsCancellationRequested; i++)
                    set.TryRemove(i % 100);
            }),
            Task.Run(() =>
            {
                for (var i = 0; i < iterations && !cts.Token.IsCancellationRequested; i++)
                    set.Contains(i % 500);
            }),
            Task.Run(() =>
            {
                for (var i = 0; i < 5 && !cts.Token.IsCancellationRequested; i++)
                {
                    Thread.Sleep(10);
                    set.Clear();
                }
            })
        };

        // Should complete without exceptions
        await Task.WhenAll(tasks);
        await Assert.That(set.Count).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Concurrent_Count_During_Modifications()
    {
        var set = new ConcurrentHashSet<int>();
        const int iterations = 2000;

        var modifyTask = Task.Run(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                set.Add(i);
                if (i % 3 == 0) set.TryRemove(i / 2);
            }
        });

        var countTask = Task.Run(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                var count = set.Count;
                // Count should always be non-negative
                if (count < 0) throw new Exception($"Count was negative: {count}");
            }
        });

        await Task.WhenAll(modifyTask, countTask);
        await Assert.That(set.Count).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Concurrent_IsEmpty_During_Modifications()
    {
        var set = new ConcurrentHashSet<int>();
        const int iterations = 2000;

        var modifyTask = Task.Run(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                set.Add(i % 10);
                set.TryRemove(i % 10);
            }
        });

        var isEmptyTask = Task.Run(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                // Should not throw
                _ = set.IsEmpty;
            }
        });

        await Task.WhenAll(modifyTask, isEmptyTask);
        // If we reached here, no exceptions occurred during concurrent IsEmpty checks
    }
}

public class ConcurrentEnumerationTests
{
    [Test]
    public async Task Enumerate_During_Concurrent_Adds()
    {
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, 100));
        var addsDone = new TaskCompletionSource();

        var addTask = Task.Run(() =>
        {
            for (var i = 100; i < 1000; i++)
            {
                set.Add(i);
            }
            addsDone.SetResult();
        });

        var enumerateTask = Task.Run(async () =>
        {
            while (!addsDone.Task.IsCompleted)
            {
                // Enumeration should not throw during concurrent modification
                foreach (var _ in set) { }
                await Task.Yield();
            }
        });

        await Task.WhenAll(addTask, enumerateTask);
        await Assert.That(set.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Enumerate_During_Concurrent_Removes()
    {
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, 1000));
        var removesDone = new TaskCompletionSource();

        var removeTask = Task.Run(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                set.TryRemove(i);
            }
            removesDone.SetResult();
        });

        var enumerateTask = Task.Run(async () =>
        {
            while (!removesDone.Task.IsCompleted)
            {
                foreach (var _ in set) { }
                await Task.Yield();
            }
        });

        await Task.WhenAll(removeTask, enumerateTask);
        await Assert.That(set.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Multiple_Concurrent_Enumerations()
    {
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, 100));

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() =>
            {
                var items = new List<int>();
                foreach (var item in set)
                {
                    items.Add(item);
                }
                return items.Count;
            }));

        var counts = await Task.WhenAll(tasks);

        // All enumerations should see 100 items (no concurrent modification)
        foreach (var count in counts)
        {
            await Assert.That(count).IsEqualTo(100);
        }
    }
}

public class ConcurrentTryGetValueTests
{
    [Test]
    public async Task Concurrent_TryGetValue_During_Adds()
    {
        var set = new ConcurrentHashSet<int>();
        const int itemCount = 5000;

        var addTask = Task.Run(() =>
        {
            for (var i = 0; i < itemCount; i++)
            {
                set.Add(i);
            }
        });

        var getTask = Task.Run(() =>
        {
            for (var i = 0; i < itemCount; i++)
            {
                set.TryGetValue(i, out _); // Should never throw
            }
        });

        await Task.WhenAll(addTask, getTask);
        await Assert.That(set.Count).IsEqualTo(itemCount);
    }
}
