# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ConcurrentHashSet is a thread-safe hash-based set (`ConcurrentHashSet<T>`) for .NET, modeled after `ConcurrentDictionary`. It lives in the `ConcurrentCollections` namespace with assembly name `ConcurrentCollections`. Published as the `ConcurrentHashSet` NuGet package.

## Build Commands

```bash
# Build (solution is under src/)
dotnet build src/ConcurrentCollections.sln -c Release

# Build specific framework target
dotnet build src/ConcurrentHashSet/ConcurrentHashSet.csproj -f netstandard2.0 -c Release
```

NuGet package is auto-generated on build (`GeneratePackageOnBuild=True`) and output to `src/ConcurrentHashSet/bin/Release/`.

## Test Commands

Tests use TUnit framework (`src/ConcurrentHashSet.Tests/`). TUnit uses Microsoft.Testing.Platform (not VSTest), so on .NET 10+ use `dotnet run`:

```bash
# Run all tests
dotnet run --project src/ConcurrentHashSet.Tests/ConcurrentHashSet.Tests.csproj -c Release

# On .NET 9 SDK, dotnet test also works
dotnet test src/ConcurrentHashSet.Tests/ConcurrentHashSet.Tests.csproj -c Release
```

## Build Constraints

Defined in `src/Directory.Build.props`:
- **TreatWarningsAsErrors** is enabled — all warnings are build errors
- **Nullable reference types** are enabled
- **Latest C# language version** is used

## Target Frameworks

Multi-targets: `netstandard1.0`, `netstandard2.0`, `net461`. Code uses conditional compilation (`#if`) for nullable attribute polyfills on older targets (see `NullableAttributes.cs`).

## Architecture

The entire implementation is in two files under `src/ConcurrentHashSet/`:

- **ConcurrentHashSet.cs** (~900 lines) — The single public type `ConcurrentHashSet<T>` implementing `IReadOnlyCollection<T>` and `ICollection<T>`. Uses lock-per-segment concurrency with linked-list buckets, mirroring `ConcurrentDictionary`'s internal design. Contains three nested types:
  - `Tables` (private class) — holds bucket array, lock array, and per-lock counts
  - `Node` (private class) — linked list node with cached hashcode
  - `Enumerator` (public struct) — allocation-free enumerator using a state machine with goto-based transitions

- **NullableAttributes.cs** — Polyfill for `MaybeNullWhenAttribute`, conditionally compiled only for targets lacking it.

## Key Design Decisions

- Concurrency model: lock-per-segment (up to 1024 locks), with `Volatile.Read/Write` for lock-free reads in hot paths like `Contains` and `TryGetValue`
- No set operations (union, intersect, etc.) — only per-item operations (`Add`, `TryRemove`, `Contains`, `TryGetValue`)
- Struct enumerator avoids heap allocation; does not snapshot — allows concurrent modification during iteration
- Default concurrency level is `Environment.ProcessorCount`; default capacity is 31 buckets
