module Hedgehog.AutoGen.Tests.CollectionTests

open System
open System.Collections.Frozen
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel
open Xunit
open Swensen.Unquote
open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Tests.TreeComparison

let private range = Range.constant 0 8
let private config = AutoGenConfig.defaults |> AutoGenConfig.setSeqRange range

/// The elements and the pairs the way Gen.auto makes them.
let private ints : Gen<int> = Gen.autoWith<int> config
let private pairs : Gen<int * string> = Gen.zip ints (Gen.autoWith<string> config)

let private sameAs (reference : Gen<'c>) (actual : Gen<'c>) (canonical : 'c -> 'e) : string option =
    firstDifference [ 0 .. 99 ] (fun seed ->
        let size = sizeOf seed
        let expected = draw seed size reference |> Tree.map canonical
        let actual = draw seed size actual |> Tree.map canonical
        difference [] 3 6 expected actual)

let private sorted (xs : seq<'a>) : 'a list = xs |> Seq.sort |> Seq.toList

let private pairsOf (kvps : seq<KeyValuePair<int, string>>) : (int * string) list =
    kvps |> Seq.map (fun kvp -> kvp.Key, kvp.Value) |> Seq.sortBy fst |> Seq.toList

// Gen.auto builds the collections that it knew from the generators of CollectionGen now. Whatever it generated before,
// for a seed, together with how it shrinks, is what it generates now.
[<Fact>]
let ``the collections that auto knew generate and shrink as they did`` () =
    let oldDictionary = pairs |> Gen.list range |> Gen.map (fun kvps -> Dictionary(dict kvps))
    let oldImmutableList = ints |> Gen.list range |> Gen.map ImmutableList.CreateRange
    let oldList = ints |> Gen.list range

    let differences =
        [ "ImmutableList", sameAs oldImmutableList (Gen.autoWith<ImmutableList<int>> config) Seq.toList
          "IImmutableList", sameAs (oldImmutableList |> Gen.map (fun x -> x :> IImmutableList<int>)) (Gen.autoWith<IImmutableList<int>> config) Seq.toList
          "ImmutableArray", sameAs (ints |> Gen.array range |> Gen.map ImmutableArray.CreateRange) (Gen.autoWith<ImmutableArray<int>> config) Seq.toList
          "ImmutableHashSet", sameAs (oldList |> Gen.map ImmutableHashSet.CreateRange) (Gen.autoWith<ImmutableHashSet<int>> config) sorted
          "ImmutableSortedSet", sameAs (oldList |> Gen.map ImmutableSortedSet.CreateRange) (Gen.autoWith<ImmutableSortedSet<int>> config) Seq.toList
          "IImmutableSet", sameAs (oldList |> Gen.map (fun xs -> ImmutableHashSet.CreateRange xs :> IImmutableSet<int>)) (Gen.autoWith<IImmutableSet<int>> config) sorted
          "Dictionary", sameAs oldDictionary (Gen.autoWith<Dictionary<int, string>> config) pairsOf
          "IDictionary", sameAs (oldDictionary |> Gen.map (fun x -> x :> IDictionary<int, string>)) (Gen.autoWith<IDictionary<int, string>> config) pairsOf
          "ReadOnlyDictionary", sameAs (oldDictionary |> Gen.map (fun x -> ReadOnlyDictionary(x))) (Gen.autoWith<ReadOnlyDictionary<int, string>> config) pairsOf
          "IReadOnlyDictionary", sameAs (oldDictionary |> Gen.map (fun x -> ReadOnlyDictionary(x) :> IReadOnlyDictionary<int, string>)) (Gen.autoWith<IReadOnlyDictionary<int, string>> config) pairsOf
          "list", sameAs oldList (Gen.autoWith<int list> config) id
          "List", sameAs (oldList |> Gen.map ResizeArray) (Gen.autoWith<List<int>> config) Seq.toList
          "IList", sameAs (oldList |> Gen.map (fun xs -> ResizeArray xs :> IList<int>)) (Gen.autoWith<IList<int>> config) Seq.toList
          "IReadOnlyList", sameAs (oldList |> Gen.map (fun xs -> ResizeArray(xs).AsReadOnly() :> IReadOnlyList<int>)) (Gen.autoWith<IReadOnlyList<int>> config) Seq.toList
          "seq", sameAs (oldList |> Gen.map Seq.ofList) (Gen.autoWith<seq<int>> config) Seq.toList
          "Set", sameAs (oldList |> Gen.map Set.ofList) (Gen.autoWith<Set<int>> config) Set.toList
          "Map", sameAs (pairs |> Gen.list range |> Gen.map Map.ofList) (Gen.autoWith<Map<int, string>> config) Map.toList ]
        |> List.choose (fun (name, difference) -> difference |> Option.map (sprintf "%s: %s" name))

    differences =! []

// F# structural equality collapses the keys of a dictionary, like it always did: the arrays with equal items are one key,
// and the last one of them wins.
[<Fact>]
let ``auto collapses the keys of a dictionary with the structural equality of F# as it did`` () =
    // Arrays are keys that are equal for F# and are not equal for .NET. A deep recursion limit lets the arrays be filled.
    let small = config |> AutoGenConfig.setSeqRange (Range.constant 0 3) |> AutoGenConfig.setRecursionDepth 5
    let keys = Gen.autoWith<int[]> small
    let values = Gen.autoWith<int> small
    let reference = Gen.zip keys values |> Gen.list (Range.constant 0 3) |> Gen.map (fun kvps -> Dictionary(dict kvps))
    let canonical (d : Dictionary<int[], int>) = d |> Seq.map (fun kvp -> List.ofArray kvp.Key, kvp.Value) |> Seq.sortBy fst |> Seq.toList
    sameAs reference (Gen.autoWith<Dictionary<int[], int>> small) canonical =! None
    Property.check <| property {
        let! d = Gen.autoWith<Dictionary<int[], int>> small
        let keys = d.Keys |> Seq.map List.ofArray |> Seq.toList
        test <@ List.length keys = List.length (List.distinct keys) @>
    }
//
// The collections that were not generated by Gen.auto before
//

let private guids = AutoGenConfig.defaults |> AutoGenConfig.setSeqRange (Range.constant 3 6) |> AutoGenConfig.setRecursionDepth 3

let private generates<'t> (isValid : 't -> bool) =
    Property.check <| property {
        let! x = Gen.autoWith<'t> guids
        test <@ isValid x @>
    }

[<Fact>]
let ``auto generates HashSet`` () =
    generates<HashSet<Guid>> (fun x -> x.Count >= 3 && x.Count <= 6)

[<Fact>]
let ``auto generates SortedSet`` () =
    generates<SortedSet<Guid>> (fun x -> x.Count >= 3 && x.Count <= 6)

[<Fact>]
let ``auto generates ISet as a HashSet`` () =
    generates<ISet<Guid>> (fun x -> x :? HashSet<Guid> && x.Count >= 3 && x.Count <= 6)

[<Fact>]
let ``auto generates ICollection as a List`` () =
    generates<ICollection<Guid>> (fun x -> x :? List<Guid> && x.Count >= 3 && x.Count <= 6)

[<Fact>]
let ``auto generates IReadOnlyCollection as a ReadOnlyCollection`` () =
    generates<IReadOnlyCollection<Guid>> (fun x -> x :? ReadOnlyCollection<Guid> && x.Count >= 3 && x.Count <= 6)

[<Fact>]
let ``auto generates ImmutableDictionary`` () =
    generates<ImmutableDictionary<Guid, int>> (fun x -> x.Count >= 3 && x.Count <= 6)

[<Fact>]
let ``auto generates ImmutableSortedDictionary`` () =
    generates<ImmutableSortedDictionary<Guid, int>> (fun x -> x.Count >= 3 && x.Count <= 6)

[<Fact>]
let ``auto generates ImmutableQueue`` () =
    generates<ImmutableQueue<int>> (fun x -> Seq.length x >= 3 && Seq.length x <= 6)

[<Fact>]
let ``auto generates ImmutableStack`` () =
    generates<ImmutableStack<int>> (fun x -> Seq.length x >= 3 && Seq.length x <= 6)

[<Fact>]
let ``auto generates FrozenSet`` () =
    generates<FrozenSet<Guid>> (fun x -> x.Count >= 3 && x.Count <= 6)

[<Fact>]
let ``auto generates FrozenDictionary`` () =
    generates<FrozenDictionary<Guid, int>> (fun x -> x.Count >= 3 && x.Count <= 6)

[<Fact>]
let ``auto generates the collections of collections`` () =
    generates<Dictionary<string, HashSet<ImmutableQueue<int>>>> (fun x -> x.Count <= 6)

[<Fact>]
let ``auto generates empty collections when there is no recursion left`` () =
    let noRecursion = AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth 0
    let isEmpty (xs : seq<'a>) = Seq.isEmpty xs
    // The quotation of 'test' does not take generic functions.
    let allEmpty a b c d e f g h i j k =
        isEmpty a && isEmpty b && isEmpty c && isEmpty d && isEmpty e && isEmpty f && isEmpty g && isEmpty h && isEmpty i && isEmpty j && isEmpty k
    Property.check <| property {
        let! a = Gen.autoWith<HashSet<int>> noRecursion
        let! b = Gen.autoWith<SortedSet<int>> noRecursion
        let! c = Gen.autoWith<ISet<int>> noRecursion
        let! d = Gen.autoWith<ICollection<int>> noRecursion
        let! e = Gen.autoWith<IReadOnlyCollection<int>> noRecursion
        let! f = Gen.autoWith<ImmutableDictionary<int, int>> noRecursion
        let! g = Gen.autoWith<ImmutableSortedDictionary<int, int>> noRecursion
        let! h = Gen.autoWith<ImmutableQueue<int>> noRecursion
        let! i = Gen.autoWith<ImmutableStack<int>> noRecursion
        let! j = Gen.autoWith<FrozenSet<int>> noRecursion
        let! k = Gen.autoWith<FrozenDictionary<int, int>> noRecursion
        let empty = allEmpty a b c d e f g h i j k
        test <@ empty @>
    }

[<Fact>]
let ``auto generates a new empty mutable collection each time when there is no recursion left`` () =
    let noRecursion = AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth 0
    // One generator, that is run twice.
    let hashSets = Gen.autoWith<HashSet<int>> noRecursion
    let lists = Gen.autoWith<List<int>> noRecursion
    Property.check <| property {
        let! a = hashSets
        let! b = hashSets
        let! c = lists
        let! d = lists
        let different = not (obj.ReferenceEquals (a, b)) && not (obj.ReferenceEquals (c, d))
        test <@ different @>
    }

// The types behind the interfaces are the ones that auto always used.
[<Fact>]
let ``auto backs the interfaces that it knew by the types that it knew`` () =
    let types =
        [ typeof<List<int>>, (Gen.autoWith<IList<int>> config |> Gen.map box)
          typeof<List<int>>, (Gen.autoWith<List<int>> config |> Gen.map box)
          typeof<ReadOnlyCollection<int>>, (Gen.autoWith<IReadOnlyList<int>> config |> Gen.map box)
          typeof<Dictionary<int, string>>, (Gen.autoWith<IDictionary<int, string>> config |> Gen.map box)
          typeof<ReadOnlyDictionary<int, string>>, (Gen.autoWith<IReadOnlyDictionary<int, string>> config |> Gen.map box)
          typeof<ReadOnlyDictionary<int, string>>, (Gen.autoWith<ReadOnlyDictionary<int, string>> config |> Gen.map box)
          typeof<ImmutableList<int>>, (Gen.autoWith<IImmutableList<int>> config |> Gen.map box)
          typeof<ImmutableHashSet<int>>, (Gen.autoWith<IImmutableSet<int>> config |> Gen.map box)
          typeof<int list>, (Gen.autoWith<seq<int>> config |> Gen.map box)
          // And the new interfaces.
          typeof<HashSet<int>>, (Gen.autoWith<ISet<int>> config |> Gen.map box)
          typeof<List<int>>, (Gen.autoWith<ICollection<int>> config |> Gen.map box)
          typeof<ReadOnlyCollection<int>>, (Gen.autoWith<IReadOnlyCollection<int>> config |> Gen.map box) ]
    let wrong =
        [ for expected, gen in types do
            for seed in 0 .. 19 do
                let tree = draw seed 30 gen
                // The outcome and its shrinks.
                for outcome in Tree.outcome tree :: (Tree.shrinks tree |> Seq.truncate 5 |> Seq.map Tree.outcome |> Seq.toList) do
                    if outcome.GetType() <> expected then
                        yield sprintf "%s instead of %s" (outcome.GetType().Name) expected.Name ]
    wrong =! []

//
// The recursion limit
//

type RecHashSet =
  {X: HashSet<RecHashSet>}
  member this.Depth =
    if this.X.Count = 0 then 0
    else this.X |> Seq.map (fun x -> x.Depth + 1) |> Seq.max

type RecImmutableDictionary =
  {X: ImmutableDictionary<RecImmutableDictionary, RecImmutableDictionary>}
  member this.Depth =
    if this.X.Count = 0 then 0
    else this.X |> Seq.collect (fun kvp -> seq { kvp.Key.Depth + 1; kvp.Value.Depth }) |> Seq.max

type RecFrozenSet =
  {X: FrozenSet<RecFrozenSet>}
  member this.Depth =
    if this.X.Count = 0 then 0
    else this.X |> Seq.map (fun x -> x.Depth + 1) |> Seq.max

type RecICollection =
  {X: ICollection<RecICollection>}
  member this.Depth =
    if this.X.Count = 0 then 0
    else this.X |> Seq.map (fun x -> x.Depth + 1) |> Seq.max

type RecImmutableQueue =
  {X: ImmutableQueue<RecImmutableQueue>}
  member this.Depth =
    if this.X.IsEmpty then 0
    else this.X |> Seq.map (fun x -> x.Depth + 1) |> Seq.max

[<Fact>]
let ``auto with recursive HashSet members does not cause stack overflow and respects max recursion depth`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<RecHashSet>
        return true
    }
    Property.check <| property {
        let! depth = Gen.int32 <| Range.exponential 0 5
        let! x = Gen.autoWith<RecHashSet> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 0 5))
        x.Depth <=! depth
    }

[<Fact>]
let ``auto with recursive ImmutableDictionary members does not cause stack overflow and respects max recursion depth`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<RecImmutableDictionary>
        return true
    }
    Property.check <| property {
        let! depth = Gen.int32 <| Range.exponential 0 5
        let! x = Gen.autoWith<RecImmutableDictionary> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 0 5))
        x.Depth <=! depth
    }

[<Fact>]
let ``auto with recursive FrozenSet members does not cause stack overflow and respects max recursion depth`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<RecFrozenSet>
        return true
    }
    Property.check <| property {
        let! depth = Gen.int32 <| Range.exponential 0 5
        let! x = Gen.autoWith<RecFrozenSet> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 0 5))
        x.Depth <=! depth
    }

[<Fact>]
let ``auto with recursive ICollection members does not cause stack overflow and respects max recursion depth`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<RecICollection>
        return true
    }
    Property.check <| property {
        let! depth = Gen.int32 <| Range.exponential 0 5
        let! x = Gen.autoWith<RecICollection> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 0 5))
        x.Depth <=! depth
    }

[<Fact>]
let ``auto with recursive ImmutableQueue members does not cause stack overflow and respects max recursion depth`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<RecImmutableQueue>
        return true
    }
    Property.check <| property {
        let! depth = Gen.int32 <| Range.exponential 0 5
        let! x = Gen.autoWith<RecImmutableQueue> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 0 5))
        x.Depth <=! depth
    }
