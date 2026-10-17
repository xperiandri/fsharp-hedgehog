# Generating Collections

`Gen.list`, `Gen.array` and `Gen.seq` generate the collections of F#. The `CollectionGen` module generates the other collections of .NET: sets, dictionaries, queues and stacks, the immutable and the frozen collections, and the interfaces that they implement.

Every function takes a range that determines the length of the collection and a generator of its elements. A dictionary takes a generator of its keys and a generator of its values.

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

## Why not `Gen.list` followed by `Gen.map`?

A list can be turned into any collection with `Gen.map`:

```fsharp
Gen.alphaNum
|> Gen.array (Range.linear 0 5)
|> Gen.map ImmutableArray.CreateRange
```

The result is the same, but `Gen.map` copies the array into the collection, and it does it again for every node of the tree of shrinks that is walked when a property fails. The functions of `CollectionGen` build the collection straight from the generated elements, which allocates less, and a property that shrinks a lot runs faster.

Both give the same collections and the same shrinks for the same seed, so a recorded recheck works either way.

## The functions

| Kind | Functions |
| ---- | --------- |
| Lists and arrays | `list`, `array`, `resizeArray` |
| Sets, dictionaries, queues and stacks | `hashSet`, `sortedSet`, `dictionary`, `sortedDictionary`, `queue`, `stack` |
| Immutable collections | `immutableArray`, `immutableList`, `immutableHashSet`, `immutableSortedSet`, `immutableDictionary`, `immutableSortedDictionary`, `immutableQueue`, `immutableStack` |
| Frozen collections | `frozenSet`, `frozenDictionary` |
| Interfaces | `iEnumerable`, `iCollection`, `iList`, `iReadOnlyCollection`, `iReadOnlyList`, `iSet`, `iReadOnlySet`, `iDictionary`, `iReadOnlyDictionary`, `iImmutableList`, `iImmutableSet`, `iImmutableDictionary` |

`list`, `array` and `iEnumerable` are `Gen.list`, `Gen.array` and `Gen.seq`.

## Equal elements and equal keys

A set keeps an element once, and a dictionary keeps a key once. The range determines the number of the generated elements, not the size of the result, so a set or a dictionary can have fewer elements than the lower bound of the range:

```fsharp
// 5 elements are generated out of 3 values, so the set has 1, 2 or 3 elements.
CollectionGen.hashSet (Range.constant 5 5) (Gen.int32 (Range.constant 0 2))
```

When equal keys get different values, the value that was generated last is kept. This is true while shrinking as well, so a shrunk dictionary is always a dictionary that could have been generated.

Elements and keys are compared the way their type compares them by default: with `Equals` and `GetHashCode` for hash-based collections, and with `IComparable` for sorted ones.

## Order

A queue dequeues the elements in the order in which they were generated. A stack has the element that was generated last on the top.

## Interfaces

A function for an interface generates a collection of a certain type, and shrinks to collections of the same type.

| Function | Type of the collection |
| -------- | ---------------------- |
| `iEnumerable` | F# list |
| `iCollection`, `iList` | `List<'a>` |
| `iReadOnlyCollection`, `iReadOnlyList` | `ReadOnlyCollection<'a>` |
| `iSet`, `iReadOnlySet` | `HashSet<'a>` |
| `iDictionary` | `Dictionary<'k, 'v>` |
| `iReadOnlyDictionary` | `ReadOnlyDictionary<'k, 'v>` |
| `iImmutableList` | `ImmutableList<'a>` |
| `iImmutableSet` | `ImmutableHashSet<'a>` |
| `iImmutableDictionary` | `ImmutableDictionary<'k, 'v>` |

## Shrinking

All of them shrink the way `Gen.list` does: to the empty collection first (when the range allows it), then by removing elements, then by shrinking the elements one by one. A collection never gets shorter than the lower bound of the range.
