namespace Hedgehog.FSharp

open System.Collections.Frozen
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel
open System.Runtime.InteropServices
open Hedgehog

/// <summary>
/// Generators of collections, one for each type of collection.
/// </summary>
/// <remarks>
/// <para>
/// All of them take a <c>Range</c> that determines the length of the collection, and a generator of its elements
/// (of its keys and of its values, for a dictionary). They shrink like <c>Gen.list</c> does: to the empty collection
/// first, then by removing elements, then by shrinking the elements.
/// </para>
/// <para>
/// The collection is built straight from the generated elements. Mapping the result of <c>Gen.list</c> or <c>Gen.array</c>
/// to a collection copies it, and rebuilds every node of the tree of shrinks that is walked.
/// </para>
/// <para>
/// The range determines the number of the generated elements, not the size of the result. A set, a dictionary and the
/// other collections that keep an element only once collapse the elements that are equal, so the result can have fewer
/// elements than the lower bound of the range. When equal keys get different values, the value that was generated
/// last is kept, during generation and during shrinking. Equality and ordering are the default ones of the type.
/// </para>
/// </remarks>
[<RequireQualifiedAccess>]
module CollectionGen =

    let private collection (build : 'a[] -> 'c) (range : Range<int>) (gen : Gen<'a>) : Gen<'c> =
        ArrayCore.generate build range (Gen.toRandom gen)
        |> Gen.ofRandom

    let private pairs (build : ('k * 'v)[] -> 'c) (range : Range<int>) (keyGen : Gen<'k>) (valueGen : Gen<'v>) : Gen<'c> =
        collection build range (Gen.zip keyGen valueGen)

    let private toDictionary (pairs : ('k * 'v)[]) : Dictionary<'k, 'v> =
        let dictionary = Dictionary<'k, 'v>(pairs.Length)
        for key, value in pairs do
            dictionary.[key] <- value
        dictionary

    let private toSortedDictionary (pairs : ('k * 'v)[]) : SortedDictionary<'k, 'v> =
        let dictionary = SortedDictionary<'k, 'v>()
        for key, value in pairs do
            dictionary.[key] <- value
        dictionary

    let private toImmutableDictionary (pairs : ('k * 'v)[]) : ImmutableDictionary<'k, 'v> =
        let builder = ImmutableDictionary.CreateBuilder<'k, 'v>()
        for key, value in pairs do
            builder.[key] <- value
        builder.ToImmutable()

    let private toImmutableSortedDictionary (pairs : ('k * 'v)[]) : ImmutableSortedDictionary<'k, 'v> =
        let builder = ImmutableSortedDictionary.CreateBuilder<'k, 'v>()
        for key, value in pairs do
            builder.[key] <- value
        builder.ToImmutable()

    //
    // Lists and arrays
    //

    /// <summary>
    /// Generates an F# list using a 'Range' to determine the length. The same as <c>Gen.list</c>.
    /// </summary>
    /// <param name="range">Range determining the length of the list.</param>
    /// <param name="gen">Generates the items in the list.</param>
    let list (range : Range<int>) (gen : Gen<'a>) : Gen<'a list> =
        Gen.list range gen

    /// <summary>
    /// Generates an array using a 'Range' to determine the length. The same as <c>Gen.array</c>.
    /// </summary>
    /// <param name="range">Range determining the length of the array.</param>
    /// <param name="gen">Generates the items in the array.</param>
    let array (range : Range<int>) (gen : Gen<'a>) : Gen<'a[]> =
        Gen.array range gen

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Generic.List`1"/> using a 'Range' to determine the length.
    /// </summary>
    /// <param name="range">Range determining the length of the list.</param>
    /// <param name="gen">Generates the items in the list.</param>
    let resizeArray (range : Range<int>) (gen : Gen<'a>) : Gen<ResizeArray<'a>> =
        collection (fun xs -> ResizeArray<'a>(xs)) range gen

    //
    // Sets, dictionaries, queues and stacks
    //

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Generic.HashSet`1"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    /// <param name="gen">Generates the elements of the set.</param>
    let hashSet<'a when 'a : equality> (range : Range<int>) (gen : Gen<'a>) : Gen<HashSet<'a>> =
        collection (fun xs -> HashSet<'a>(xs)) range gen

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Generic.SortedSet`1"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    /// <param name="gen">Generates the elements of the set.</param>
    let sortedSet<'a when 'a : comparison> (range : Range<int>) (gen : Gen<'a>) : Gen<SortedSet<'a>> =
        collection (fun xs -> SortedSet<'a>(xs)) range gen

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Generic.Dictionary`2"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    let dictionary<'k, 'v when 'k : equality> (range : Range<int>) (keyGen : Gen<'k>) (valueGen : Gen<'v>) : Gen<Dictionary<'k, 'v>> =
        pairs toDictionary range keyGen valueGen

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Generic.SortedDictionary`2"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    let sortedDictionary<'k, 'v when 'k : comparison> (range : Range<int>) (keyGen : Gen<'k>) (valueGen : Gen<'v>) : Gen<SortedDictionary<'k, 'v>> =
        pairs toSortedDictionary range keyGen valueGen

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Generic.Queue`1"/>. The first generated element is the first to be dequeued.
    /// </summary>
    /// <param name="range">Range determining the length of the queue.</param>
    /// <param name="gen">Generates the items in the queue.</param>
    let queue (range : Range<int>) (gen : Gen<'a>) : Gen<Queue<'a>> =
        collection (fun xs -> Queue<'a>(xs)) range gen

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Generic.Stack`1"/>. The last generated element is on the top of the stack.
    /// </summary>
    /// <param name="range">Range determining the length of the stack.</param>
    /// <param name="gen">Generates the items in the stack.</param>
    let stack (range : Range<int>) (gen : Gen<'a>) : Gen<Stack<'a>> =
        collection (fun xs -> Stack<'a>(xs)) range gen

    //
    // Immutable collections
    //

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableArray`1"/> using a 'Range' to determine the length.
    /// </summary>
    /// <param name="range">Range determining the length of the array.</param>
    /// <param name="gen">Generates the items in the array.</param>
    let immutableArray (range : Range<int>) (gen : Gen<'a>) : Gen<ImmutableArray<'a>> =
        // The array of the outcomes is made for this collection only, so it is wrapped without a copy.
        collection (fun xs ->
            if xs.Length = 0 then ImmutableArray<'a>.Empty
            else ImmutableCollectionsMarshal.AsImmutableArray xs) range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableList`1"/> using a 'Range' to determine the length.
    /// </summary>
    /// <param name="range">Range determining the length of the list.</param>
    /// <param name="gen">Generates the items in the list.</param>
    let immutableList (range : Range<int>) (gen : Gen<'a>) : Gen<ImmutableList<'a>> =
        collection (fun xs -> ImmutableList.CreateRange xs) range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableHashSet`1"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    /// <param name="gen">Generates the elements of the set.</param>
    let immutableHashSet<'a when 'a : equality> (range : Range<int>) (gen : Gen<'a>) : Gen<ImmutableHashSet<'a>> =
        collection (fun xs -> ImmutableHashSet.CreateRange xs) range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableSortedSet`1"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    /// <param name="gen">Generates the elements of the set.</param>
    let immutableSortedSet<'a when 'a : comparison> (range : Range<int>) (gen : Gen<'a>) : Gen<ImmutableSortedSet<'a>> =
        collection (fun xs -> ImmutableSortedSet.CreateRange xs) range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableDictionary`2"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    let immutableDictionary<'k, 'v when 'k : equality> (range : Range<int>) (keyGen : Gen<'k>) (valueGen : Gen<'v>) : Gen<ImmutableDictionary<'k, 'v>> =
        pairs toImmutableDictionary range keyGen valueGen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableSortedDictionary`2"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    let immutableSortedDictionary<'k, 'v when 'k : comparison> (range : Range<int>) (keyGen : Gen<'k>) (valueGen : Gen<'v>) : Gen<ImmutableSortedDictionary<'k, 'v>> =
        pairs toImmutableSortedDictionary range keyGen valueGen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableQueue`1"/>. The first generated element is the first to be dequeued.
    /// </summary>
    /// <param name="range">Range determining the length of the queue.</param>
    /// <param name="gen">Generates the items in the queue.</param>
    let immutableQueue (range : Range<int>) (gen : Gen<'a>) : Gen<ImmutableQueue<'a>> =
        collection (fun xs -> ImmutableQueue.CreateRange xs) range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableStack`1"/>. The last generated element is on the top of the stack.
    /// </summary>
    /// <param name="range">Range determining the length of the stack.</param>
    /// <param name="gen">Generates the items in the stack.</param>
    let immutableStack (range : Range<int>) (gen : Gen<'a>) : Gen<ImmutableStack<'a>> =
        collection (fun xs -> ImmutableStack.CreateRange xs) range gen

    //
    // Frozen collections
    //

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Frozen.FrozenSet`1"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    /// <param name="gen">Generates the elements of the set.</param>
    let frozenSet<'a when 'a : equality> (range : Range<int>) (gen : Gen<'a>) : Gen<FrozenSet<'a>> =
        collection (fun xs -> FrozenSet.ToFrozenSet xs) range gen

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Frozen.FrozenDictionary`2"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    let frozenDictionary<'k, 'v when 'k : equality> (range : Range<int>) (keyGen : Gen<'k>) (valueGen : Gen<'v>) : Gen<FrozenDictionary<'k, 'v>> =
        pairs (fun kvps -> FrozenDictionary.ToFrozenDictionary (toDictionary kvps)) range keyGen valueGen

    //
    // Interfaces of collections
    //

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.IEnumerable`1"/> using a 'Range' to determine the length.
    /// The same as <c>Gen.seq</c>: the items are in an F# list.
    /// </summary>
    /// <param name="range">Range determining the length of the enumerable.</param>
    /// <param name="gen">Generates the items in the enumerable.</param>
    let iEnumerable (range : Range<int>) (gen : Gen<'a>) : Gen<IEnumerable<'a>> =
        Gen.seq range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.ICollection`1"/> that is a <see cref="T:System.Collections.Generic.List`1"/>.
    /// </summary>
    /// <param name="range">Range determining the length of the collection.</param>
    /// <param name="gen">Generates the items in the collection.</param>
    let iCollection (range : Range<int>) (gen : Gen<'a>) : Gen<ICollection<'a>> =
        collection (fun xs -> ResizeArray<'a>(xs) :> ICollection<'a>) range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.IList`1"/> that is a <see cref="T:System.Collections.Generic.List`1"/>.
    /// </summary>
    /// <param name="range">Range determining the length of the list.</param>
    /// <param name="gen">Generates the items in the list.</param>
    let iList (range : Range<int>) (gen : Gen<'a>) : Gen<IList<'a>> =
        collection (fun xs -> ResizeArray<'a>(xs) :> IList<'a>) range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.IReadOnlyCollection`1"/> that is a
    /// <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1"/>.
    /// </summary>
    /// <param name="range">Range determining the length of the collection.</param>
    /// <param name="gen">Generates the items in the collection.</param>
    let iReadOnlyCollection (range : Range<int>) (gen : Gen<'a>) : Gen<IReadOnlyCollection<'a>> =
        collection (fun xs -> ReadOnlyCollection<'a>(xs) :> IReadOnlyCollection<'a>) range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.IReadOnlyList`1"/> that is a
    /// <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1"/>.
    /// </summary>
    /// <param name="range">Range determining the length of the list.</param>
    /// <param name="gen">Generates the items in the list.</param>
    let iReadOnlyList (range : Range<int>) (gen : Gen<'a>) : Gen<IReadOnlyList<'a>> =
        collection (fun xs -> ReadOnlyCollection<'a>(xs) :> IReadOnlyList<'a>) range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.ISet`1"/> that is a <see cref="T:System.Collections.Generic.HashSet`1"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    /// <param name="gen">Generates the elements of the set.</param>
    let iSet<'a when 'a : equality> (range : Range<int>) (gen : Gen<'a>) : Gen<ISet<'a>> =
        collection (fun xs -> HashSet<'a>(xs) :> ISet<'a>) range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.IReadOnlySet`1"/> that is a <see cref="T:System.Collections.Generic.HashSet`1"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    /// <param name="gen">Generates the elements of the set.</param>
    let iReadOnlySet<'a when 'a : equality> (range : Range<int>) (gen : Gen<'a>) : Gen<IReadOnlySet<'a>> =
        collection (fun xs -> HashSet<'a>(xs) :> IReadOnlySet<'a>) range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.IDictionary`2"/> that is a
    /// <see cref="T:System.Collections.Generic.Dictionary`2"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    let iDictionary<'k, 'v when 'k : equality> (range : Range<int>) (keyGen : Gen<'k>) (valueGen : Gen<'v>) : Gen<IDictionary<'k, 'v>> =
        pairs (fun kvps -> toDictionary kvps :> IDictionary<'k, 'v>) range keyGen valueGen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.IReadOnlyDictionary`2"/> that is a
    /// <see cref="T:System.Collections.ObjectModel.ReadOnlyDictionary`2"/> of a <see cref="T:System.Collections.Generic.Dictionary`2"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    let iReadOnlyDictionary<'k, 'v when 'k : equality> (range : Range<int>) (keyGen : Gen<'k>) (valueGen : Gen<'v>) : Gen<IReadOnlyDictionary<'k, 'v>> =
        pairs (fun kvps -> ReadOnlyDictionary<'k, 'v>(toDictionary kvps) :> IReadOnlyDictionary<'k, 'v>) range keyGen valueGen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.IImmutableList`1"/> that is an
    /// <see cref="T:System.Collections.Immutable.ImmutableList`1"/>.
    /// </summary>
    /// <param name="range">Range determining the length of the list.</param>
    /// <param name="gen">Generates the items in the list.</param>
    let iImmutableList (range : Range<int>) (gen : Gen<'a>) : Gen<IImmutableList<'a>> =
        collection (fun xs -> ImmutableList.CreateRange xs :> IImmutableList<'a>) range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.IImmutableSet`1"/> that is an
    /// <see cref="T:System.Collections.Immutable.ImmutableHashSet`1"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    /// <param name="gen">Generates the elements of the set.</param>
    let iImmutableSet<'a when 'a : equality> (range : Range<int>) (gen : Gen<'a>) : Gen<IImmutableSet<'a>> =
        collection (fun xs -> ImmutableHashSet.CreateRange xs :> IImmutableSet<'a>) range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.IImmutableDictionary`2"/> that is an
    /// <see cref="T:System.Collections.Immutable.ImmutableDictionary`2"/>.
    /// </summary>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    let iImmutableDictionary<'k, 'v when 'k : equality> (range : Range<int>) (keyGen : Gen<'k>) (valueGen : Gen<'v>) : Gen<IImmutableDictionary<'k, 'v>> =
        pairs (fun kvps -> toImmutableDictionary kvps :> IImmutableDictionary<'k, 'v>) range keyGen valueGen
