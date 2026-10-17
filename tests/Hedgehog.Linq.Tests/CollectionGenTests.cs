using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace Hedgehog.Linq.Tests
{
    public class CollectionGenTests
    {
        private static readonly Gen<int> Element = Gen.Int32(Range.Constant(0, 100));

        // Out of a few values, so that equal elements and equal keys are common.
        private static readonly Gen<int> Few = Gen.Int32(Range.Constant(0, 4));

        private static void AssertAlways<T>(Gen<T> gen, Func<T, bool> predicate) =>
            gen.ForAll().Select(predicate).Check();

        private static bool InRange(int count, int lower, int upper) =>
            count >= lower && count <= upper;

        //
        // Sets, dictionaries, queues and stacks
        //

        [Fact]
        public void HashSet_CollapsesEqualElements() =>
            AssertAlways(Few.HashSet(Range.Constant(6, 9)), set => InRange(set.Count, 1, 5) && set.All(x => InRange(x, 0, 4)));

        [Fact]
        public void SortedSet_IsSortedAndCollapsesEqualElements() =>
            AssertAlways(Few.SortedSet(Range.Constant(6, 9)), set => InRange(set.Count, 1, 5) && set.SequenceEqual(set.OrderBy(x => x).Distinct()));

        [Fact]
        public void Dictionary_CollapsesEqualKeys() =>
            AssertAlways(Few.Dictionary(Element, Range.Constant(6, 9)), dictionary => InRange(dictionary.Count, 1, 5) && dictionary.Keys.All(x => InRange(x, 0, 4)) && dictionary.Values.All(x => InRange(x, 0, 100)));

        [Fact]
        public void Dictionary_KeepsOnePairOfEqualKeys() =>
            AssertAlways(Gen.Constant(7).Dictionary(Element, Range.Constant(3, 6)), dictionary => dictionary.Count == 1 && dictionary.ContainsKey(7));

        [Fact]
        public void SortedDictionary_IsSortedAndCollapsesEqualKeys() =>
            AssertAlways(Few.SortedDictionary(Element, Range.Constant(6, 9)), dictionary => InRange(dictionary.Count, 1, 5) && dictionary.Keys.SequenceEqual(dictionary.Keys.OrderBy(x => x)));

        [Fact]
        public void Queue_HasTheLengthOfTheRange() =>
            AssertAlways(Element.Queue(Range.Constant(3, 6)), queue => InRange(queue.Count, 3, 6));

        [Fact]
        public void Stack_HasTheLengthOfTheRange() =>
            AssertAlways(Element.Stack(Range.Constant(3, 6)), stack => InRange(stack.Count, 3, 6));

        //
        // Immutable collections
        //

        [Fact]
        public void ImmutableArray_HasTheLengthOfTheRange() =>
            AssertAlways(Element.ImmutableArray(Range.Constant(3, 6)), array => InRange(array.Length, 3, 6) && !array.IsDefault);

        [Fact]
        public void ImmutableArray_CanBeEmpty() =>
            AssertAlways(Element.ImmutableArray(Range.Singleton(0)), array => array.IsEmpty && !array.IsDefault);

        [Fact]
        public void ImmutableList_HasTheLengthOfTheRange() =>
            AssertAlways(Element.ImmutableList(Range.Constant(3, 6)), list => InRange(list.Count, 3, 6));

        [Fact]
        public void ImmutableHashSet_CollapsesEqualElements() =>
            AssertAlways(Few.ImmutableHashSet(Range.Constant(6, 9)), set => InRange(set.Count, 1, 5));

        [Fact]
        public void ImmutableSortedSet_IsSortedAndCollapsesEqualElements() =>
            AssertAlways(Few.ImmutableSortedSet(Range.Constant(6, 9)), set => InRange(set.Count, 1, 5) && set.SequenceEqual(set.OrderBy(x => x).Distinct()));

        [Fact]
        public void ImmutableDictionary_CollapsesEqualKeys() =>
            AssertAlways(Few.ImmutableDictionary(Element, Range.Constant(6, 9)), dictionary => InRange(dictionary.Count, 1, 5));

        [Fact]
        public void ImmutableSortedDictionary_CollapsesEqualKeys() =>
            AssertAlways(Few.ImmutableSortedDictionary(Element, Range.Constant(6, 9)), dictionary => InRange(dictionary.Count, 1, 5) && dictionary.Keys.SequenceEqual(dictionary.Keys.OrderBy(x => x)));

        [Fact]
        public void ImmutableQueue_HasTheLengthOfTheRange() =>
            AssertAlways(Element.ImmutableQueue(Range.Constant(3, 6)), queue => InRange(queue.Count(), 3, 6));

        [Fact]
        public void ImmutableStack_HasTheLengthOfTheRange() =>
            AssertAlways(Element.ImmutableStack(Range.Constant(3, 6)), stack => InRange(stack.Count(), 3, 6));

        //
        // Frozen collections
        //

        [Fact]
        public void FrozenSet_CollapsesEqualElements() =>
            AssertAlways(Few.FrozenSet(Range.Constant(6, 9)), set => InRange(set.Count, 1, 5));

        [Fact]
        public void FrozenDictionary_CollapsesEqualKeys() =>
            AssertAlways(Few.FrozenDictionary(Element, Range.Constant(6, 9)), dictionary => InRange(dictionary.Count, 1, 5));

        //
        // Interfaces of collections
        //

        [Fact]
        public void ICollection_IsAList() =>
            AssertAlways(Element.ICollection(Range.Constant(3, 6)), collection => collection is List<int> && InRange(collection.Count, 3, 6));

        [Fact]
        public void IList_IsAList() =>
            AssertAlways(Element.IList(Range.Constant(3, 6)), list => list is List<int> && InRange(list.Count, 3, 6));

        [Fact]
        public void IReadOnlyCollection_IsAReadOnlyCollection() =>
            AssertAlways(Element.IReadOnlyCollection(Range.Constant(3, 6)), collection => collection is ReadOnlyCollection<int> && InRange(collection.Count, 3, 6));

        [Fact]
        public void IReadOnlyList_IsAReadOnlyCollection() =>
            AssertAlways(Element.IReadOnlyList(Range.Constant(3, 6)), list => list is ReadOnlyCollection<int> && InRange(list.Count, 3, 6));

        [Fact]
        public void ISet_IsAHashSet() =>
            AssertAlways(Few.ISet(Range.Constant(6, 9)), set => set is HashSet<int> && InRange(set.Count, 1, 5));

        [Fact]
        public void IReadOnlySet_IsAHashSet() =>
            AssertAlways(Few.IReadOnlySet(Range.Constant(6, 9)), set => set is HashSet<int> && InRange(set.Count, 1, 5));

        [Fact]
        public void IDictionary_IsADictionary() =>
            AssertAlways(Few.IDictionary(Element, Range.Constant(6, 9)), dictionary => dictionary is Dictionary<int, int> && InRange(dictionary.Count, 1, 5));

        [Fact]
        public void IReadOnlyDictionary_IsAReadOnlyDictionary() =>
            AssertAlways(Few.IReadOnlyDictionary(Element, Range.Constant(6, 9)), dictionary => dictionary is ReadOnlyDictionary<int, int> && InRange(dictionary.Count, 1, 5));

        [Fact]
        public void IImmutableList_IsAnImmutableList() =>
            AssertAlways(Element.IImmutableList(Range.Constant(3, 6)), list => list is ImmutableList<int> && InRange(list.Count, 3, 6));

        [Fact]
        public void IImmutableSet_IsAnImmutableHashSet() =>
            AssertAlways(Few.IImmutableSet(Range.Constant(6, 9)), set => set is ImmutableHashSet<int> && InRange(set.Count, 1, 5));

        [Fact]
        public void IImmutableDictionary_IsAnImmutableDictionary() =>
            AssertAlways(Few.IImmutableDictionary(Element, Range.Constant(6, 9)), dictionary => dictionary is ImmutableDictionary<int, int> && InRange(dictionary.Count, 1, 5));

        //
        // A property that fails is reported, and the report can be rendered
        //

        [Fact]
        public void HashSet_FailingPropertyIsReported()
        {
            var property = Element.HashSet(Range.LinearInt32(0, 20)).ForAll().Select(set => set.Count < 3);

            var report = property.Report();

            Assert.True(report.Status.IsFailed);
            Assert.False(string.IsNullOrEmpty(report.Render()));
        }
    }
}
