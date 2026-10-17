namespace Hedgehog.AutoGen

open System.Collections.Frozen
open System.Collections.Generic
open System.Collections.ObjectModel
open Hedgehog
open Hedgehog.FSharp
open System
open System.Collections.Immutable

module private DictionaryGenerators =

    /// Generates a dictionary out of the pairs of generated keys and values, and makes what 'build' makes of it.
    /// The pairs are collapsed by 'dict', that is by the structural equality of F# for the keys, and the last one of
    /// the equal keys wins. The dictionary that is made of them has the default comparer.
    let generate<'k, 'v, 'c when 'k : equality> (range : Range<int>) (keyGen : Gen<'k>) (valueGen : Gen<'v>) (build : Dictionary<'k, 'v> -> 'c) : Gen<'c> =
        ArrayCore.generate (fun kvps -> build (Dictionary(dict kvps))) range (Gen.toRandom (Gen.zip keyGen valueGen))
        |> Gen.ofRandom

[<Sealed>]
type DefaultGenerators =
    static member Byte() : Gen<byte> = Gen.byte <| Range.exponentialBounded ()
    static member Int16() : Gen<int16> = Gen.int16 <| Range.exponentialBounded ()
    static member UInt16() : Gen<uint16> = Gen.uint16 <| Range.exponentialBounded ()
    static member Int32() : Gen<int32> = Gen.int32 <| Range.exponentialBounded ()
    static member UInt32() : Gen<uint32> = Gen.uint32 <| Range.exponentialBounded ()
    static member Int64() : Gen<int64> = Gen.int64 <| Range.exponentialBounded ()
    static member UInt64() : Gen<uint64> = Gen.uint64 <| Range.exponentialBounded ()
    static member Single() : Gen<single> = Gen.double (Range.exponentialFrom 0. (float Single.MinValue) (float Single.MaxValue)) |> Gen.map single
    static member Double() : Gen<double> = Gen.double <| Range.exponentialBounded ()
    static member Decimal() : Gen<decimal> = Gen.double (Range.exponentialFrom 0. (float Decimal.MinValue) (float Decimal.MaxValue)) |> Gen.map decimal
    static member Bool() : Gen<bool> = Gen.bool
    static member Guid() : Gen<Guid> = Gen.guid
    static member Char() : Gen<char> = Gen.latin1
    static member String() : Gen<string> = Gen.string (Range.linear 0 50) Gen.latin1

    static member DateTime() : Gen<DateTime> = DefaultGenerators.DateTimeOffset() |> Gen.map _.DateTime
    static member DateTimeOffset() : Gen<DateTimeOffset> =
        let dateTimeRange =
            Range.exponentialFrom
                (DateTime(2000, 1, 1)).Ticks
                DateTime.MinValue.Ticks
                DateTime.MaxValue.Ticks
            |> Range.map DateTime
        Gen.dateTime dateTimeRange (Gen.constant DateTimeKind.Unspecified) |> Gen.map DateTimeOffset

    static member DateOnly() : Gen<DateOnly> =
        Range.exponentialFrom
            (DateOnly(2000, 1, 1)).DayNumber
            DateOnly.MinValue.DayNumber
            DateOnly.MaxValue.DayNumber
        |> Range.map DateOnly.FromDayNumber
        |> Gen.dateOnly

    // A linear range: an exponential one would keep the times of half of all sizes within the first tenth of a second
    // after midnight, because a day has 864 billion ticks.
    static member TimeOnly() : Gen<TimeOnly> =
        Range.linear TimeOnly.MinValue.Ticks TimeOnly.MaxValue.Ticks
        |> Range.map TimeOnly
        |> Gen.timeOnly

    static member ImmutableList<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<ImmutableList<'a>> =
        if context.CanRecurse then
            CollectionGen.immutableList context.CollectionRange valueGen
        else
            Gen.constant ImmutableList<'a>.Empty

    static member IImmutableList<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<IImmutableList<'a>> =
        if context.CanRecurse then
            CollectionGen.iImmutableList context.CollectionRange valueGen
        else
            Gen.constant (ImmutableList<'a>.Empty :> IImmutableList<'a>)

    static member ImmutableArray<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<ImmutableArray<'a>> =
        if context.CanRecurse then
            CollectionGen.immutableArray context.CollectionRange valueGen
        else
            Gen.constant ImmutableArray<'a>.Empty

    static member ImmutableHashSet<'a when 'a : equality>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<ImmutableHashSet<'a>> =
        if context.CanRecurse then
            CollectionGen.immutableHashSet context.CollectionRange valueGen
        else
            Gen.constant ImmutableHashSet<'a>.Empty

    static member ImmutableSet<'a when 'a : comparison>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<ImmutableSortedSet<'a>> =
        if context.CanRecurse then
            CollectionGen.immutableSortedSet context.CollectionRange valueGen
        else
            Gen.constant ImmutableSortedSet<'a>.Empty

    static member IImmutableSet<'a when 'a : equality>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<IImmutableSet<'a>> =
        if context.CanRecurse then
            CollectionGen.iImmutableSet context.CollectionRange valueGen
        else
            Gen.constant (ImmutableHashSet<'a>.Empty :> IImmutableSet<'a>)

    static member ImmutableDictionary<'k, 'v when 'k : equality>(context: AutoGenContext, keyGen: Gen<'k>, valueGen: Gen<'v>) : Gen<ImmutableDictionary<'k, 'v>> =
        if context.CanRecurse then
            CollectionGen.immutableDictionary context.CollectionRange keyGen valueGen
        else
            Gen.constant ImmutableDictionary<'k, 'v>.Empty

    static member ImmutableSortedDictionary<'k, 'v when 'k : comparison>(context: AutoGenContext, keyGen: Gen<'k>, valueGen: Gen<'v>) : Gen<ImmutableSortedDictionary<'k, 'v>> =
        if context.CanRecurse then
            CollectionGen.immutableSortedDictionary context.CollectionRange keyGen valueGen
        else
            Gen.constant ImmutableSortedDictionary<'k, 'v>.Empty

    static member ImmutableQueue<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<ImmutableQueue<'a>> =
        if context.CanRecurse then
            CollectionGen.immutableQueue context.CollectionRange valueGen
        else
            Gen.constant ImmutableQueue<'a>.Empty

    static member ImmutableStack<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<ImmutableStack<'a>> =
        if context.CanRecurse then
            CollectionGen.immutableStack context.CollectionRange valueGen
        else
            Gen.constant ImmutableStack<'a>.Empty

    static member FrozenSet<'a when 'a : equality>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<FrozenSet<'a>> =
        if context.CanRecurse then
            CollectionGen.frozenSet context.CollectionRange valueGen
        else
            Gen.constant FrozenSet<'a>.Empty

    static member FrozenDictionary<'k, 'v when 'k : equality>(context: AutoGenContext, keyGen: Gen<'k>, valueGen: Gen<'v>) : Gen<FrozenDictionary<'k, 'v>> =
        if context.CanRecurse then
            CollectionGen.frozenDictionary context.CollectionRange keyGen valueGen
        else
            Gen.constant FrozenDictionary<'k, 'v>.Empty

    // The mutable collections are new for every value, also when there is no recursion left, so that a test that
    // changes one of them does not change the others.
    static member HashSet<'a when 'a : equality>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<HashSet<'a>> =
        if context.CanRecurse then
            CollectionGen.hashSet context.CollectionRange valueGen
        else
            Gen.delay (fun () -> Gen.constant (HashSet<'a>()))

    static member SortedSet<'a when 'a : comparison>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<SortedSet<'a>> =
        if context.CanRecurse then
            CollectionGen.sortedSet context.CollectionRange valueGen
        else
            Gen.delay (fun () -> Gen.constant (SortedSet<'a>()))

    static member ISet<'a when 'a : equality>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<ISet<'a>> =
        if context.CanRecurse then
            CollectionGen.iSet context.CollectionRange valueGen
        else
            Gen.delay (fun () -> Gen.constant (HashSet<'a>() :> ISet<'a>))

    // The pairs are collapsed the way they always were: with the structural equality of F# ('dict'), the last one of
    // equal keys wins, and the dictionary that is made of them has the default comparer.
    static member Dictionary<'k, 'v when 'k: equality>(context: AutoGenContext, keyGen: Gen<'k>, valueGen: Gen<'v>): Gen<Dictionary<'k, 'v>> =
        if context.CanRecurse then
            DictionaryGenerators.generate context.CollectionRange keyGen valueGen id
        else
            Gen.constant (Dictionary<'k, 'v>())

    static member IDictionary<'k, 'v when 'k: equality>(context: AutoGenContext, keyGen: Gen<'k>, valueGen: Gen<'v>): Gen<IDictionary<'k, 'v>> =
        if context.CanRecurse then
            DictionaryGenerators.generate context.CollectionRange keyGen valueGen (fun x -> x :> IDictionary<'k, 'v>)
        else
            Gen.constant (Dictionary<'k, 'v>() :> IDictionary<'k, 'v>)

    static member ReadOnlyDictionary<'k, 'v when 'k: equality>(context: AutoGenContext, keyGen: Gen<'k>, valueGen: Gen<'v>): Gen<ReadOnlyDictionary<'k, 'v>> =
        if context.CanRecurse then
            DictionaryGenerators.generate context.CollectionRange keyGen valueGen (fun x -> ReadOnlyDictionary(x))
        else
            Gen.constant (ReadOnlyDictionary(Dictionary<'k, 'v>()))

    static member IReadOnlyDictionary<'k, 'v when 'k: equality>(context: AutoGenContext, keyGen: Gen<'k>, valueGen: Gen<'v>): Gen<IReadOnlyDictionary<'k, 'v>> =
        if context.CanRecurse then
            DictionaryGenerators.generate context.CollectionRange keyGen valueGen (fun x -> ReadOnlyDictionary(x) :> IReadOnlyDictionary<'k, 'v>)
        else
            Gen.constant (ReadOnlyDictionary(Dictionary<'k, 'v>()) :> IReadOnlyDictionary<'k, 'v>)

    static member FSharpList<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<'a list> =
        if context.CanRecurse then
            valueGen |> Gen.list context.CollectionRange
        else
            Gen.constant []

    static member List<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<List<'a>> =
        if context.CanRecurse then
            CollectionGen.resizeArray context.CollectionRange valueGen
        else
            Gen.delay (fun () -> Gen.constant (List<'a>()))

    static member IList<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<IList<'a>> =
        if context.CanRecurse then
            CollectionGen.iList context.CollectionRange valueGen
        else
            Gen.delay (fun () -> Gen.constant (List<'a>() :> IList<'a>))

    static member ICollection<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<ICollection<'a>> =
        if context.CanRecurse then
            CollectionGen.iCollection context.CollectionRange valueGen
        else
            Gen.delay (fun () -> Gen.constant (List<'a>() :> ICollection<'a>))

    static member IReadOnlyList<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<IReadOnlyList<'a>> =
        if context.CanRecurse then
            CollectionGen.iReadOnlyList context.CollectionRange valueGen
        else
            Gen.constant (List<'a>().AsReadOnly() :> IReadOnlyList<'a>)

    static member IReadOnlyCollection<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<IReadOnlyCollection<'a>> =
        if context.CanRecurse then
            CollectionGen.iReadOnlyCollection context.CollectionRange valueGen
        else
            Gen.constant (List<'a>().AsReadOnly() :> IReadOnlyCollection<'a>)

    static member Seq<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<seq<'a>> =
        if context.CanRecurse then
            CollectionGen.iEnumerable context.CollectionRange valueGen
        else
            Gen.constant (List.empty<'a> :> seq<'a>)

    static member Option<'a>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<'a option> =
        if context.CanRecurse then Gen.option valueGen
        else Gen.constant None

    static member Nullable<'a when 'a : struct and 'a : (new : unit -> 'a) and 'a :> ValueType>(context: AutoGenContext, valueGen: Gen<'a>): Gen<Nullable<'a>> =
        if context.CanRecurse
        then valueGen |> Gen.option |> Gen.map Option.toNullable
        else Gen.constant (Nullable<'a>())

    static member Set<'a when 'a : comparison>(context: AutoGenContext, valueGen: Gen<'a>) : Gen<Set<'a>> =
        if context.CanRecurse then
            ArrayCore.generate Set.ofArray context.CollectionRange (Gen.toRandom valueGen)
            |> Gen.ofRandom
        else
            Gen.constant Set.empty

    static member Map<'k, 'v when 'k : comparison>(context: AutoGenContext, keyGen: Gen<'k>, valueGen: Gen<'v>) : Gen<Map<'k, 'v>> =
        if context.CanRecurse then
            ArrayCore.generate Map.ofArray context.CollectionRange (Gen.toRandom (Gen.zip keyGen valueGen))
            |> Gen.ofRandom
        else
            Gen.constant Map.empty<'k, 'v>

    static member Uri : Gen<Uri> = Gen.uri

    static member IpAddress : Gen<System.Net.IPAddress> = Gen.ipAddress
