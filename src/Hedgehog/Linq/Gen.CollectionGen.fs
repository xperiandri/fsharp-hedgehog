namespace Hedgehog.Linq

open System.Collections.Frozen
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel
open System.Runtime.CompilerServices
open Hedgehog
open Hedgehog.FSharp

/// <summary>
/// Generators of the collections of .NET, one for each type of collection. <c>Array</c>, <c>Enumerable</c> and <c>List</c>
/// of <see cref="T:Hedgehog.Linq.GenExtensions"/> generate arrays, enumerables and lists.
/// </summary>
/// <remarks>
/// <para>
/// All of them take a <c>Range</c> that determines the length of the collection, and a generator of its elements
/// (the generator of the keys is the one that the method extends, for a dictionary). They shrink like <c>List</c> does:
/// to the empty collection first, then by removing elements, then by shrinking the elements.
/// </para>
/// <para>
/// The range determines the number of the generated elements, not the size of the result. A set, a dictionary and the
/// other collections that keep an element only once collapse the elements that are equal, so the result can have fewer
/// elements than the lower bound of the range. When equal keys get different values, the value that was generated
/// last is kept, during generation and during shrinking. Equality and ordering are the default ones of the type.
/// </para>
/// </remarks>
[<AbstractClass; Sealed>]
type GenCollectionExtensions private () =

    //
    // Sets, dictionaries, queues and stacks
    //

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Generic.HashSet`1"/>.
    /// </summary>
    /// <param name="gen">Generates the elements of the set.</param>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    [<Extension>]
    static member HashSet<'T when 'T : equality> (gen : Gen<'T>, range : Range<int>) : Gen<HashSet<'T>> =
        CollectionGen.hashSet range gen

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Generic.SortedSet`1"/>.
    /// </summary>
    /// <param name="gen">Generates the elements of the set.</param>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    [<Extension>]
    static member SortedSet<'T when 'T : comparison> (gen : Gen<'T>, range : Range<int>) : Gen<SortedSet<'T>> =
        CollectionGen.sortedSet range gen

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Generic.Dictionary`2"/>.
    /// </summary>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    [<Extension>]
    static member Dictionary<'TKey, 'TValue when 'TKey : equality> (keyGen : Gen<'TKey>, valueGen : Gen<'TValue>, range : Range<int>) : Gen<Dictionary<'TKey, 'TValue>> =
        CollectionGen.dictionary range keyGen valueGen

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Generic.SortedDictionary`2"/>.
    /// </summary>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    [<Extension>]
    static member SortedDictionary<'TKey, 'TValue when 'TKey : comparison> (keyGen : Gen<'TKey>, valueGen : Gen<'TValue>, range : Range<int>) : Gen<SortedDictionary<'TKey, 'TValue>> =
        CollectionGen.sortedDictionary range keyGen valueGen

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Generic.Queue`1"/>. The first generated element is the first to be dequeued.
    /// </summary>
    /// <param name="gen">Generates the items in the queue.</param>
    /// <param name="range">Range determining the length of the queue.</param>
    [<Extension>]
    static member Queue (gen : Gen<'T>, range : Range<int>) : Gen<Queue<'T>> =
        CollectionGen.queue range gen

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Generic.Stack`1"/>. The last generated element is on the top of the stack.
    /// </summary>
    /// <param name="gen">Generates the items in the stack.</param>
    /// <param name="range">Range determining the length of the stack.</param>
    [<Extension>]
    static member Stack (gen : Gen<'T>, range : Range<int>) : Gen<Stack<'T>> =
        CollectionGen.stack range gen

    //
    // Immutable collections
    //

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableArray`1"/> using a 'Range' to determine the length.
    /// </summary>
    /// <param name="gen">Generates the items in the array.</param>
    /// <param name="range">Range determining the length of the array.</param>
    [<Extension>]
    static member ImmutableArray (gen : Gen<'T>, range : Range<int>) : Gen<ImmutableArray<'T>> =
        CollectionGen.immutableArray range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableList`1"/> using a 'Range' to determine the length.
    /// </summary>
    /// <param name="gen">Generates the items in the list.</param>
    /// <param name="range">Range determining the length of the list.</param>
    [<Extension>]
    static member ImmutableList (gen : Gen<'T>, range : Range<int>) : Gen<ImmutableList<'T>> =
        CollectionGen.immutableList range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableHashSet`1"/>.
    /// </summary>
    /// <param name="gen">Generates the elements of the set.</param>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    [<Extension>]
    static member ImmutableHashSet<'T when 'T : equality> (gen : Gen<'T>, range : Range<int>) : Gen<ImmutableHashSet<'T>> =
        CollectionGen.immutableHashSet range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableSortedSet`1"/>.
    /// </summary>
    /// <param name="gen">Generates the elements of the set.</param>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    [<Extension>]
    static member ImmutableSortedSet<'T when 'T : comparison> (gen : Gen<'T>, range : Range<int>) : Gen<ImmutableSortedSet<'T>> =
        CollectionGen.immutableSortedSet range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableDictionary`2"/>.
    /// </summary>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    [<Extension>]
    static member ImmutableDictionary<'TKey, 'TValue when 'TKey : equality> (keyGen : Gen<'TKey>, valueGen : Gen<'TValue>, range : Range<int>) : Gen<ImmutableDictionary<'TKey, 'TValue>> =
        CollectionGen.immutableDictionary range keyGen valueGen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableSortedDictionary`2"/>.
    /// </summary>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    [<Extension>]
    static member ImmutableSortedDictionary<'TKey, 'TValue when 'TKey : comparison> (keyGen : Gen<'TKey>, valueGen : Gen<'TValue>, range : Range<int>) : Gen<ImmutableSortedDictionary<'TKey, 'TValue>> =
        CollectionGen.immutableSortedDictionary range keyGen valueGen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableQueue`1"/>. The first generated element is the first to be dequeued.
    /// </summary>
    /// <param name="gen">Generates the items in the queue.</param>
    /// <param name="range">Range determining the length of the queue.</param>
    [<Extension>]
    static member ImmutableQueue (gen : Gen<'T>, range : Range<int>) : Gen<ImmutableQueue<'T>> =
        CollectionGen.immutableQueue range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.ImmutableStack`1"/>. The last generated element is on the top of the stack.
    /// </summary>
    /// <param name="gen">Generates the items in the stack.</param>
    /// <param name="range">Range determining the length of the stack.</param>
    [<Extension>]
    static member ImmutableStack (gen : Gen<'T>, range : Range<int>) : Gen<ImmutableStack<'T>> =
        CollectionGen.immutableStack range gen

    //
    // Frozen collections
    //

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Frozen.FrozenSet`1"/>.
    /// </summary>
    /// <param name="gen">Generates the elements of the set.</param>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    [<Extension>]
    static member FrozenSet<'T when 'T : equality> (gen : Gen<'T>, range : Range<int>) : Gen<FrozenSet<'T>> =
        CollectionGen.frozenSet range gen

    /// <summary>
    /// Generates a <see cref="T:System.Collections.Frozen.FrozenDictionary`2"/>.
    /// </summary>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    [<Extension>]
    static member FrozenDictionary<'TKey, 'TValue when 'TKey : equality> (keyGen : Gen<'TKey>, valueGen : Gen<'TValue>, range : Range<int>) : Gen<FrozenDictionary<'TKey, 'TValue>> =
        CollectionGen.frozenDictionary range keyGen valueGen

    //
    // Interfaces of collections
    //

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.ICollection`1"/> that is a <see cref="T:System.Collections.Generic.List`1"/>.
    /// </summary>
    /// <param name="gen">Generates the items in the collection.</param>
    /// <param name="range">Range determining the length of the collection.</param>
    [<Extension>]
    static member ICollection (gen : Gen<'T>, range : Range<int>) : Gen<ICollection<'T>> =
        CollectionGen.iCollection range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.IList`1"/> that is a <see cref="T:System.Collections.Generic.List`1"/>.
    /// </summary>
    /// <param name="gen">Generates the items in the list.</param>
    /// <param name="range">Range determining the length of the list.</param>
    [<Extension>]
    static member IList (gen : Gen<'T>, range : Range<int>) : Gen<IList<'T>> =
        CollectionGen.iList range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.IReadOnlyCollection`1"/> that is a
    /// <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1"/>.
    /// </summary>
    /// <param name="gen">Generates the items in the collection.</param>
    /// <param name="range">Range determining the length of the collection.</param>
    [<Extension>]
    static member IReadOnlyCollection (gen : Gen<'T>, range : Range<int>) : Gen<IReadOnlyCollection<'T>> =
        CollectionGen.iReadOnlyCollection range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.IReadOnlyList`1"/> that is a
    /// <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1"/>.
    /// </summary>
    /// <param name="gen">Generates the items in the list.</param>
    /// <param name="range">Range determining the length of the list.</param>
    [<Extension>]
    static member IReadOnlyList (gen : Gen<'T>, range : Range<int>) : Gen<IReadOnlyList<'T>> =
        CollectionGen.iReadOnlyList range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.ISet`1"/> that is a <see cref="T:System.Collections.Generic.HashSet`1"/>.
    /// </summary>
    /// <param name="gen">Generates the elements of the set.</param>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    [<Extension>]
    static member ISet<'T when 'T : equality> (gen : Gen<'T>, range : Range<int>) : Gen<ISet<'T>> =
        CollectionGen.iSet range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.IReadOnlySet`1"/> that is a <see cref="T:System.Collections.Generic.HashSet`1"/>.
    /// </summary>
    /// <param name="gen">Generates the elements of the set.</param>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    [<Extension>]
    static member IReadOnlySet<'T when 'T : equality> (gen : Gen<'T>, range : Range<int>) : Gen<IReadOnlySet<'T>> =
        CollectionGen.iReadOnlySet range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.IDictionary`2"/> that is a
    /// <see cref="T:System.Collections.Generic.Dictionary`2"/>.
    /// </summary>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    [<Extension>]
    static member IDictionary<'TKey, 'TValue when 'TKey : equality> (keyGen : Gen<'TKey>, valueGen : Gen<'TValue>, range : Range<int>) : Gen<IDictionary<'TKey, 'TValue>> =
        CollectionGen.iDictionary range keyGen valueGen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Generic.IReadOnlyDictionary`2"/> that is a
    /// <see cref="T:System.Collections.ObjectModel.ReadOnlyDictionary`2"/> of a <see cref="T:System.Collections.Generic.Dictionary`2"/>.
    /// </summary>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    [<Extension>]
    static member IReadOnlyDictionary<'TKey, 'TValue when 'TKey : equality> (keyGen : Gen<'TKey>, valueGen : Gen<'TValue>, range : Range<int>) : Gen<IReadOnlyDictionary<'TKey, 'TValue>> =
        CollectionGen.iReadOnlyDictionary range keyGen valueGen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.IImmutableList`1"/> that is an
    /// <see cref="T:System.Collections.Immutable.ImmutableList`1"/>.
    /// </summary>
    /// <param name="gen">Generates the items in the list.</param>
    /// <param name="range">Range determining the length of the list.</param>
    [<Extension>]
    static member IImmutableList (gen : Gen<'T>, range : Range<int>) : Gen<IImmutableList<'T>> =
        CollectionGen.iImmutableList range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.IImmutableSet`1"/> that is an
    /// <see cref="T:System.Collections.Immutable.ImmutableHashSet`1"/>.
    /// </summary>
    /// <param name="gen">Generates the elements of the set.</param>
    /// <param name="range">Range determining the number of the generated elements. Equal elements collapse, so the set can have fewer elements than the lower bound of the range.</param>
    [<Extension>]
    static member IImmutableSet<'T when 'T : equality> (gen : Gen<'T>, range : Range<int>) : Gen<IImmutableSet<'T>> =
        CollectionGen.iImmutableSet range gen

    /// <summary>
    /// Generates an <see cref="T:System.Collections.Immutable.IImmutableDictionary`2"/> that is an
    /// <see cref="T:System.Collections.Immutable.ImmutableDictionary`2"/>.
    /// </summary>
    /// <param name="keyGen">Generates the keys of the dictionary.</param>
    /// <param name="valueGen">Generates the values of the dictionary.</param>
    /// <param name="range">Range determining the number of the generated pairs. Pairs with equal keys collapse into the one that was generated last, so the dictionary can have fewer pairs than the lower bound of the range.</param>
    [<Extension>]
    static member IImmutableDictionary<'TKey, 'TValue when 'TKey : equality> (keyGen : Gen<'TKey>, valueGen : Gen<'TValue>, range : Range<int>) : Gen<IImmutableDictionary<'TKey, 'TValue>> =
        CollectionGen.iImmutableDictionary range keyGen valueGen
