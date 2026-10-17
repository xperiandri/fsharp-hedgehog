module Hedgehog.Tests.CollectionGenTests

open Hedgehog
open Hedgehog.FSharp
open TestDsl

#if !FABLE_COMPILER
open System.Collections.Frozen
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel
open System.Runtime.InteropServices
open TreeComparison

// The collections of CollectionGen are built straight from the generated elements. They have to be what mapping the
// result of Gen.list to the collection would give: the same collections, and the same shrinks, for the same seeds.

/// The elements of the sets are drawn out of a few values, so that equal elements are common.
let private few : Gen<int> =
    Gen.int32 (Range.constant 0 4)

let private many : Gen<int> =
    Gen.int32 (Range.constant 0 100)

/// The keys are drawn out of a few values, so that equal keys are common.
let private keys : Gen<int> =
    Gen.int32 (Range.constant 0 4)

let private values : Gen<int> =
    Gen.int32 (Range.constant 0 100)

let private ranges : (string * Range<int>) list =
    [ "constant", Range.constant 0 8
      "linear from 0", Range.linear 0 12
      "linear with a minimum", Range.linear 3 15 ]

/// A collection of elements has to be what 'expected' makes of the list that Gen.list generates, whatever 'canonical'
/// makes of the collection and of the expected value to compare them.
let private sameAsList
        (name : string)
        (element : Gen<int>)
        (generator : Range<int> -> Gen<int> -> Gen<'c>)
        (canonical : 'c -> 'e)
        (expected : int list -> 'e) =
    testCase (sprintf "%s generates what Gen.list generates, as a collection" name) <| fun _ ->
        for rangeName, range in ranges do
            let referenceGen = Gen.list range element
            let actualGen = generator range element
            let found =
                firstDifference [ 0 .. 149 ] (fun seed ->
                    let size = sizeOf seed
                    let expectedTree = draw seed size referenceGen |> Tree.map expected
                    let actualTree = draw seed size actualGen |> Tree.map canonical
                    difference [] 3 6 expectedTree actualTree)
            (rangeName, found) =! (rangeName, None)

let private sorted (xs : seq<'a>) : 'a list = xs |> Seq.sort |> Seq.toList
let private distinctSorted (xs : int list) : int list = xs |> List.distinct |> List.sort

/// The same for the dictionaries, out of the pairs that Gen.list generates, where the last one of equal keys wins.
let private sameAsPairs
        (name : string)
        (generator : Range<int> -> Gen<int> -> Gen<int> -> Gen<'c>)
        (canonical : 'c -> (int * int) list) =
    testCase (sprintf "%s generates what Gen.list of pairs generates, as a dictionary" name) <| fun _ ->
        for rangeName, range in ranges do
            let referenceGen = Gen.list range (Gen.zip keys values)
            let actualGen = generator range keys values
            let found =
                firstDifference [ 0 .. 149 ] (fun seed ->
                    let size = sizeOf seed
                    let expectedTree = draw seed size referenceGen |> Tree.map (fun pairs -> pairs |> Map.ofList |> Map.toList)
                    let actualTree = draw seed size actualGen |> Tree.map canonical
                    difference [] 3 6 expectedTree actualTree)
            (rangeName, found) =! (rangeName, None)

let private pairsOf (kvps : seq<KeyValuePair<int, int>>) : (int * int) list =
    kvps |> Seq.map (fun kvp -> kvp.Key, kvp.Value) |> Seq.sortBy fst |> Seq.toList

/// Whatever is not empty shrinks to the empty collection first, for a few seeds.
let private firstShrinkIsEmpty<'c when 'c :> seq<int>> (name : string) (gen : Gen<'c>) : unit =
    let isEmpty (collection : 'c) = Seq.isEmpty (collection :> seq<int>)
    let notEmpty = [ 0 .. 29 ] |> List.filter (fun seed -> not (isEmpty (Tree.outcome (draw seed 50 gen))))
    // There has to be something to check.
    Expect.isTrue (not (List.isEmpty notEmpty))
    for seed in notEmpty do
        let first = Tree.shrinks (draw seed 50 gen) |> Seq.head |> Tree.outcome
        (name, isEmpty first) =! (name, true)
/// What a generator generates is of the type that is documented.
let private isA<'t> (value : obj) : bool =
    value :? 't

let collectionGenTests = testList "CollectionGen tests" [

    //
    // Lists and arrays
    //

    sameAsList "list" many CollectionGen.list id id
    sameAsList "array" many CollectionGen.array Array.toList id
    sameAsList "resizeArray" many CollectionGen.resizeArray Seq.toList id

    //
    // Sets, dictionaries, queues and stacks
    //

    sameAsList "hashSet" few CollectionGen.hashSet sorted distinctSorted
    sameAsList "sortedSet" few CollectionGen.sortedSet Seq.toList distinctSorted
    sameAsPairs "dictionary" CollectionGen.dictionary pairsOf
    sameAsPairs "sortedDictionary" CollectionGen.sortedDictionary (fun d -> d |> Seq.toList |> pairsOf)
    sameAsList "queue" many CollectionGen.queue Seq.toList id
    // A stack is enumerated from the top, that is the last generated element.
    sameAsList "stack" many CollectionGen.stack Seq.toList List.rev

    //
    // Immutable collections
    //

    sameAsList "immutableArray" many CollectionGen.immutableArray Seq.toList id
    sameAsList "immutableList" many CollectionGen.immutableList Seq.toList id
    sameAsList "immutableHashSet" few CollectionGen.immutableHashSet sorted distinctSorted
    sameAsList "immutableSortedSet" few CollectionGen.immutableSortedSet Seq.toList distinctSorted
    sameAsPairs "immutableDictionary" CollectionGen.immutableDictionary pairsOf
    sameAsPairs "immutableSortedDictionary" CollectionGen.immutableSortedDictionary pairsOf
    sameAsList "immutableQueue" many CollectionGen.immutableQueue Seq.toList id
    sameAsList "immutableStack" many CollectionGen.immutableStack Seq.toList List.rev

    //
    // Frozen collections
    //

    sameAsList "frozenSet" few CollectionGen.frozenSet sorted distinctSorted
    sameAsPairs "frozenDictionary" CollectionGen.frozenDictionary pairsOf

    //
    // Interfaces of collections
    //

    sameAsList "iEnumerable" many CollectionGen.iEnumerable Seq.toList id
    sameAsList "iCollection" many CollectionGen.iCollection Seq.toList id
    sameAsList "iList" many CollectionGen.iList Seq.toList id
    sameAsList "iReadOnlyCollection" many CollectionGen.iReadOnlyCollection Seq.toList id
    sameAsList "iReadOnlyList" many CollectionGen.iReadOnlyList Seq.toList id
    sameAsList "iSet" few CollectionGen.iSet sorted distinctSorted
    sameAsList "iReadOnlySet" few CollectionGen.iReadOnlySet sorted distinctSorted
    sameAsPairs "iDictionary" CollectionGen.iDictionary pairsOf
    sameAsPairs "iReadOnlyDictionary" CollectionGen.iReadOnlyDictionary pairsOf
    sameAsList "iImmutableList" many CollectionGen.iImmutableList Seq.toList id
    sameAsList "iImmutableSet" few CollectionGen.iImmutableSet sorted distinctSorted
    sameAsPairs "iImmutableDictionary" CollectionGen.iImmutableDictionary pairsOf

    //
    // The types of the interfaces
    //

    testCase "the interfaces are backed by the documented types" <| fun _ ->
        let range = Range.constant 1 5
        let outcome gen = Tree.outcome (draw 3 30 gen) |> box
        Expect.isTrue (isA<ResizeArray<int>> (outcome (CollectionGen.iCollection range many)))
        Expect.isTrue (isA<ResizeArray<int>> (outcome (CollectionGen.iList range many)))
        Expect.isTrue (isA<ReadOnlyCollection<int>> (outcome (CollectionGen.iReadOnlyCollection range many)))
        Expect.isTrue (isA<ReadOnlyCollection<int>> (outcome (CollectionGen.iReadOnlyList range many)))
        Expect.isTrue (isA<HashSet<int>> (outcome (CollectionGen.iSet range many)))
        Expect.isTrue (isA<HashSet<int>> (outcome (CollectionGen.iReadOnlySet range many)))
        Expect.isTrue (isA<Dictionary<int, int>> (outcome (CollectionGen.iDictionary range many many)))
        Expect.isTrue (isA<ReadOnlyDictionary<int, int>> (outcome (CollectionGen.iReadOnlyDictionary range many many)))
        Expect.isTrue (isA<ImmutableList<int>> (outcome (CollectionGen.iImmutableList range many)))
        Expect.isTrue (isA<ImmutableHashSet<int>> (outcome (CollectionGen.iImmutableSet range many)))
        Expect.isTrue (isA<ImmutableDictionary<int, int>> (outcome (CollectionGen.iImmutableDictionary range many many)))
        // The same as Gen.seq.
        Expect.isTrue (isA<int list> (outcome (CollectionGen.iEnumerable range many)))

    testCase "every shrink of an interface has the type of the interface too" <| fun _ ->
        let range = Range.constant 2 5
        let tree = draw 5 30 (CollectionGen.iReadOnlyList range many)
        Expect.isTrue (Tree.shrinks tree |> Seq.truncate 20 |> Seq.forall (fun shrink -> isA<ReadOnlyCollection<int>> (Tree.outcome shrink)))
        let tree = draw 5 30 (CollectionGen.iDictionary range many many)
        Expect.isTrue (Tree.shrinks tree |> Seq.truncate 20 |> Seq.forall (fun shrink -> isA<Dictionary<int, int>> (Tree.outcome shrink)))

    //
    // Sizes
    //

    testCase "collections that keep equal elements have the length that the range determines" <| fun _ ->
        let range = Range.constant 4 7
        for seed in 0 .. 99 do
            let size = sizeOf seed
            let lengthOf gen = (Tree.outcome (draw seed size gen))
            let n = Seq.length (lengthOf (CollectionGen.resizeArray range many))
            Expect.isTrue (n >= 4 && n <= 7)
            (lengthOf (CollectionGen.array range many)).Length =! n
            (lengthOf (CollectionGen.immutableArray range many)).Length =! n
            (lengthOf (CollectionGen.immutableList range many)).Count =! n
            (lengthOf (CollectionGen.queue range many)).Count =! n
            (lengthOf (CollectionGen.stack range many)).Count =! n
            (lengthOf (CollectionGen.immutableQueue range many) |> Seq.length) =! n
            (lengthOf (CollectionGen.immutableStack range many) |> Seq.length) =! n

    testCase "sets and dictionaries collapse equal elements and have at most as many as the range allows" <| fun _ ->
        let range = Range.constant 6 9
        for seed in 0 .. 99 do
            let size = sizeOf seed
            let outcome gen = Tree.outcome (draw seed size gen)
            let set = outcome (CollectionGen.hashSet range few)
            Expect.isTrue (set.Count >= 1 && set.Count <= 5)
            Expect.isTrue ((outcome (CollectionGen.sortedSet range few)).Count <= 5)
            Expect.isTrue ((outcome (CollectionGen.immutableHashSet range few)).Count <= 5)
            Expect.isTrue ((outcome (CollectionGen.immutableSortedSet range few)).Count <= 5)
            Expect.isTrue ((outcome (CollectionGen.frozenSet range few)).Count <= 5)
            Expect.isTrue ((outcome (CollectionGen.dictionary range keys values)).Count <= 5)
            Expect.isTrue ((outcome (CollectionGen.sortedDictionary range keys values)).Count <= 5)
            Expect.isTrue ((outcome (CollectionGen.immutableDictionary range keys values)).Count <= 5)
            Expect.isTrue ((outcome (CollectionGen.immutableSortedDictionary range keys values)).Count <= 5)
            Expect.isTrue ((outcome (CollectionGen.frozenDictionary range keys values)).Count <= 5)

    //
    // The last of equal keys wins
    //

    testCase "when keys are equal the value that was generated last is kept, by every dictionary" <| fun _ ->
        let range = Range.constant 3 6
        let constantKey = Gen.constant 7
        for seed in 0 .. 99 do
            let size = sizeOf seed
            let pairs = Tree.outcome (draw seed size (Gen.list range (Gen.zip constantKey values)))
            let last = pairs |> List.last |> snd
            let check (kvps : seq<KeyValuePair<int, int>>) =
                (kvps |> Seq.map (fun kvp -> kvp.Key, kvp.Value) |> Seq.toList) =! [ 7, last ]
            check (Tree.outcome (draw seed size (CollectionGen.dictionary range constantKey values)))
            check (Tree.outcome (draw seed size (CollectionGen.sortedDictionary range constantKey values)))
            check (Tree.outcome (draw seed size (CollectionGen.immutableDictionary range constantKey values)))
            check (Tree.outcome (draw seed size (CollectionGen.immutableSortedDictionary range constantKey values)))
            check (Tree.outcome (draw seed size (CollectionGen.frozenDictionary range constantKey values)))
            check (Tree.outcome (draw seed size (CollectionGen.iDictionary range constantKey values)))
            check (Tree.outcome (draw seed size (CollectionGen.iReadOnlyDictionary range constantKey values)))
            check (Tree.outcome (draw seed size (CollectionGen.iImmutableDictionary range constantKey values)))

    //
    // Shrinks
    //

    testCase "the first shrink of a collection that is not empty is the empty collection, when the range allows it" <| fun _ ->
        let range = Range.constant 0 6
        firstShrinkIsEmpty "hashSet" (CollectionGen.hashSet range many)
        firstShrinkIsEmpty "sortedSet" (CollectionGen.sortedSet range many)
        firstShrinkIsEmpty "immutableArray" (CollectionGen.immutableArray range many)
        firstShrinkIsEmpty "immutableList" (CollectionGen.immutableList range many)
        firstShrinkIsEmpty "immutableQueue" (CollectionGen.immutableQueue range many)
        firstShrinkIsEmpty "frozenSet" (CollectionGen.frozenSet range many)
        firstShrinkIsEmpty "queue" (CollectionGen.queue range many)
        firstShrinkIsEmpty "stack" (CollectionGen.stack range many)
        firstShrinkIsEmpty "iReadOnlyList" (CollectionGen.iReadOnlyList range many)

    testCase "a collection does not shrink below the lower bound of the range" <| fun _ ->
        let rec noShorterThan (minimum : int) (depth : int) (tree : Tree<ImmutableArray<int>>) =
            (Tree.outcome tree).Length >= minimum
            && (depth = 0 || Tree.shrinks tree |> Seq.truncate 8 |> Seq.forall (noShorterThan minimum (depth - 1)))
        let range = Range.linear 2 10
        for seed in 0 .. 59 do
            let size = sizeOf seed
            Expect.isTrue (noShorterThan (Range.lowerBound size range) 3 (draw seed size (CollectionGen.immutableArray range many)))

    //
    // The array of the outcomes
    //

    testCase "an ImmutableArray does not share its array with the shrinks" <| fun _ ->
        let gen = CollectionGen.immutableArray (Range.singleton 4) (Gen.int32 (Range.constant 0 9))
        let untouched = draw 7 30 gen |> Tree.map Seq.toList
        let tree = draw 7 30 gen
        let overwrite (xs : ImmutableArray<int>) =
            // The array behind an ImmutableArray is exposed for this test only.
            let array = ImmutableCollectionsMarshal.AsArray xs
            for i in 0 .. array.Length - 1 do
                array.[i] <- -1
        overwrite (Tree.outcome tree)
        for shrink in Tree.shrinks tree |> Seq.truncate 10 do
            overwrite (Tree.outcome shrink)
        let expectedShrinks = Tree.shrinks untouched |> Seq.toList
        let actualShrinks = Tree.shrinks tree |> Seq.map (Tree.map Seq.toList) |> Seq.toList
        List.length actualShrinks =! List.length expectedShrinks
        for expected, actual in List.zip expectedShrinks actualShrinks do
            difference [] 2 20 expected actual =! None

    testCase "an empty ImmutableArray is the empty one" <| fun _ ->
        let tree = draw 1 30 (CollectionGen.immutableArray (Range.singleton 0) many)
        let array = Tree.outcome tree
        Expect.isFalse array.IsDefault
        Expect.isTrue array.IsEmpty
        Expect.isTrue (array = ImmutableArray<int>.Empty)

    //
    // Nesting
    //

    testCase "collections of collections shrink" <| fun _ ->
        let gen =
            CollectionGen.immutableList (Range.constant 1 3)
                (CollectionGen.hashSet (Range.constant 1 3) many)
        let tree = draw 2 30 gen
        Expect.isTrue (Tree.shrinks tree |> Seq.isEmpty |> not)

]
#else
let collectionGenTests = testList "CollectionGen tests" []
#endif
