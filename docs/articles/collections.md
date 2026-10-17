# Generating Collections

`Gen.list`, `Gen.array` and `Gen.seq` (`List`, `Array` and `Enumerable` in C#) generate the collections of F#. The `CollectionGen` module generates the other collections of .NET: sets, dictionaries, queues and stacks, the immutable and the frozen collections, and the interfaces that they implement.

Every function takes a range that determines the length of the collection and a generator of its elements. A dictionary takes a generator of its keys and a generator of its values.

# [F#](#tab/fsharp)

```fsharp
open Hedgehog
open Hedgehog.FSharp

CollectionGen.immutableArray (Range.linear 0 5) Gen.alphaNum
// Gen<ImmutableArray<char>>

CollectionGen.hashSet (Range.linear 0 10) (Gen.int32 (Range.constant 0 100))
// Gen<HashSet<int>>

CollectionGen.dictionary (Range.linear 0 10) Gen.alpha (Gen.int32 (Range.constant 0 100))
// Gen<Dictionary<char, int>>
```

# [C#](#tab/csharp)

In C#, the generators are extension methods of `Gen<T>`. A dictionary extends the generator of its keys.

```csharp
using Hedgehog;
using Hedgehog.Linq;

Gen.AlphaNum.ImmutableArray(Range.LinearInt32(0, 5));
// Gen<ImmutableArray<char>>

Gen.Int32(Range.Constant(0, 100)).HashSet(Range.LinearInt32(0, 10));
// Gen<HashSet<int>>

Gen.Alpha.Dictionary(Gen.Int32(Range.Constant(0, 100)), Range.LinearInt32(0, 10));
// Gen<Dictionary<char, int>>
```

---

## Why not `Gen.list` followed by `Gen.map`?

A list can be turned into any collection with `Gen.map`:

# [F#](#tab/fsharp)

```fsharp
Gen.alphaNum
|> Gen.array (Range.linear 0 5)
|> Gen.map ImmutableArray.CreateRange
```

# [C#](#tab/csharp)

```csharp
Gen.AlphaNum
    .Array(Range.LinearInt32(0, 5))
    .Select(chars => ImmutableArray.CreateRange(chars));
```

---

The result is the same, but `Gen.map` copies the array into the collection, and it does it again for every node of the tree of shrinks that is walked when a property fails. The functions of `CollectionGen` build the collection straight from the generated elements, which allocates less, and a property that shrinks a lot runs faster.

Both give the same collections and the same shrinks for the same seed, so a recorded recheck works either way.

## The functions

| Kind | F# | C# |
| ---- | -- | -- |
| Lists and arrays | `list`, `array`, `resizeArray` | `List`, `Array` |
| Sets, dictionaries, queues and stacks | `hashSet`, `sortedSet`, `dictionary`, `sortedDictionary`, `queue`, `stack` | `HashSet`, `SortedSet`, `Dictionary`, `SortedDictionary`, `Queue`, `Stack` |
| Immutable collections | `immutableArray`, `immutableList`, `immutableHashSet`, `immutableSortedSet`, `immutableDictionary`, `immutableSortedDictionary`, `immutableQueue`, `immutableStack` | `ImmutableArray`, `ImmutableList`, `ImmutableHashSet`, `ImmutableSortedSet`, `ImmutableDictionary`, `ImmutableSortedDictionary`, `ImmutableQueue`, `ImmutableStack` |
| Frozen collections | `frozenSet`, `frozenDictionary` | `FrozenSet`, `FrozenDictionary` |
| Interfaces | `iEnumerable`, `iCollection`, `iList`, `iReadOnlyCollection`, `iReadOnlyList`, `iSet`, `iReadOnlySet`, `iDictionary`, `iReadOnlyDictionary`, `iImmutableList`, `iImmutableSet`, `iImmutableDictionary` | `Enumerable`, `ICollection`, `IList`, `IReadOnlyCollection`, `IReadOnlyList`, `ISet`, `IReadOnlySet`, `IDictionary`, `IReadOnlyDictionary`, `IImmutableList`, `IImmutableSet`, `IImmutableDictionary` |

`list`, `array` and `iEnumerable` are `Gen.list`, `Gen.array` and `Gen.seq`.

## Equal elements and equal keys

A set keeps an element once, and a dictionary keeps a key once. The range determines the number of the generated elements, not the size of the result, so a set or a dictionary can have fewer elements than the lower bound of the range:

# [F#](#tab/fsharp)

```fsharp
// 5 elements are generated out of 3 values, so the set has 1, 2 or 3 elements.
CollectionGen.hashSet (Range.constant 5 5) (Gen.int32 (Range.constant 0 2))
```

# [C#](#tab/csharp)

```csharp
// 5 elements are generated out of 3 values, so the set has 1, 2 or 3 elements.
Gen.Int32(Range.Constant(0, 2)).HashSet(Range.Constant(5, 5));
```

---

When equal keys get different values, the value that was generated last is kept. This is true while shrinking as well, so a shrunk dictionary is always a dictionary that could have been generated.

Elements and keys are compared the way their type compares them by default: with `Equals` and `GetHashCode` for hash-based collections, and with `IComparable` for sorted ones.

## Order

A queue dequeues the elements in the order in which they were generated. A stack has the element that was generated last on the top.

## Interfaces

A function for an interface generates a collection of a certain type, and shrinks to collections of the same type.

| Function | Type of the collection |
| -------- | ---------------------- |
| `iEnumerable` | F# list |
| `iCollection`, `iList` | `List<T>` |
| `iReadOnlyCollection`, `iReadOnlyList` | `ReadOnlyCollection<T>` |
| `iSet`, `iReadOnlySet` | `HashSet<T>` |
| `iDictionary` | `Dictionary<TKey, TValue>` |
| `iReadOnlyDictionary` | `ReadOnlyDictionary<TKey, TValue>` |
| `iImmutableList` | `ImmutableList<T>` |
| `iImmutableSet` | `ImmutableHashSet<T>` |
| `iImmutableDictionary` | `ImmutableDictionary<TKey, TValue>` |

## Shrinking

All of them shrink the way `Gen.list` does: to the empty collection first (when the range allows it), then by removing elements, then by shrinking the elements one by one. A collection never gets shorter than the lower bound of the range.
