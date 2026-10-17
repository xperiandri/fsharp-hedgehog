namespace Hedgehog.Benchmarks

open System.Collections.Generic
open System.Collections.Immutable
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs
open Hedgehog
open Hedgehog.FSharp

/// <summary>
/// The collections of 'CollectionGen' against what they replace, which is the baseline: the list or the array of
/// 'Gen.list' or 'Gen.array' that is mapped to the collection with 'Gen.map'. A collection is small or larger.
/// It is either drawn or starts to shrink.
/// </summary>
/// <remarks>
/// Run with
///
///     dotnet run -c Release --project tests/Hedgehog.Benchmarks -- --filter "*CollectionGenBenchmarks*"
///
/// The allocated memory per operation does not depend on the machine, the time does.
/// </remarks>
[<MemoryDiagnoser>]
[<SimpleJob(RuntimeMoniker.Net80, launchCount = 1, warmupCount = 3, iterationCount = 10)>]
[<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)>]
[<CategoriesColumn>]
type CollectionGenBenchmarks () =

    [<Params(5, 100)>]
    member val Length = 0 with get, set

    member val MappedImmutableArray = Unchecked.defaultof<Gen<ImmutableArray<int>>> with get, set
    member val MappedImmutableList = Unchecked.defaultof<Gen<ImmutableList<int>>> with get, set
    member val MappedHashSet = Unchecked.defaultof<Gen<HashSet<int>>> with get, set
    member val MappedDictionary = Unchecked.defaultof<Gen<Dictionary<int, int>>> with get, set
    member val CoreImmutableArray = Unchecked.defaultof<Gen<ImmutableArray<int>>> with get, set
    member val CoreImmutableList = Unchecked.defaultof<Gen<ImmutableList<int>>> with get, set
    member val CoreHashSet = Unchecked.defaultof<Gen<HashSet<int>>> with get, set
    member val CoreDictionary = Unchecked.defaultof<Gen<Dictionary<int, int>>> with get, set

    member val MappedImmutableArrayTree = Unchecked.defaultof<Tree<ImmutableArray<int>>> with get, set
    member val MappedImmutableListTree = Unchecked.defaultof<Tree<ImmutableList<int>>> with get, set
    member val MappedHashSetTree = Unchecked.defaultof<Tree<HashSet<int>>> with get, set
    member val MappedDictionaryTree = Unchecked.defaultof<Tree<Dictionary<int, int>>> with get, set
    member val CoreImmutableArrayTree = Unchecked.defaultof<Tree<ImmutableArray<int>>> with get, set
    member val CoreImmutableListTree = Unchecked.defaultof<Tree<ImmutableList<int>>> with get, set
    member val CoreHashSetTree = Unchecked.defaultof<Tree<HashSet<int>>> with get, set
    member val CoreDictionaryTree = Unchecked.defaultof<Tree<Dictionary<int, int>>> with get, set

    [<GlobalSetup>]
    member this.Setup () =
        let range = Range.singleton this.Length
        let element = Bench.element

        // How the collections were generated before: by mapping a list or an array.
        this.MappedImmutableArray <- Gen.array range element |> Gen.map ImmutableArray.CreateRange
        this.MappedImmutableList <- Gen.list range element |> Gen.map ImmutableList.CreateRange
        this.MappedHashSet <- Gen.list range element |> Gen.map (fun xs -> HashSet<int> xs)
        this.MappedDictionary <- Gen.zip element element |> Gen.list range |> Gen.map (fun kvps -> Dictionary (dict kvps))

        this.CoreImmutableArray <- CollectionGen.immutableArray range element
        this.CoreImmutableList <- CollectionGen.immutableList range element
        this.CoreHashSet <- CollectionGen.hashSet range element
        this.CoreDictionary <- CollectionGen.dictionary range element element

        this.MappedImmutableArrayTree <- Bench.draw this.MappedImmutableArray
        this.MappedImmutableListTree <- Bench.draw this.MappedImmutableList
        this.MappedHashSetTree <- Bench.draw this.MappedHashSet
        this.MappedDictionaryTree <- Bench.draw this.MappedDictionary
        this.CoreImmutableArrayTree <- Bench.draw this.CoreImmutableArray
        this.CoreImmutableListTree <- Bench.draw this.CoreImmutableList
        this.CoreHashSetTree <- Bench.draw this.CoreHashSet
        this.CoreDictionaryTree <- Bench.draw this.CoreDictionary

    //
    // Draw
    //

    [<Benchmark(Baseline = true); BenchmarkCategory("Draw ImmutableArray")>]
    member this.DrawImmutableArrayMapped () = Bench.draw this.MappedImmutableArray

    [<Benchmark; BenchmarkCategory("Draw ImmutableArray")>]
    member this.DrawImmutableArrayCore () = Bench.draw this.CoreImmutableArray

    [<Benchmark(Baseline = true); BenchmarkCategory("Draw ImmutableList")>]
    member this.DrawImmutableListMapped () = Bench.draw this.MappedImmutableList

    [<Benchmark; BenchmarkCategory("Draw ImmutableList")>]
    member this.DrawImmutableListCore () = Bench.draw this.CoreImmutableList

    [<Benchmark(Baseline = true); BenchmarkCategory("Draw HashSet")>]
    member this.DrawHashSetMapped () = Bench.draw this.MappedHashSet

    [<Benchmark; BenchmarkCategory("Draw HashSet")>]
    member this.DrawHashSetCore () = Bench.draw this.CoreHashSet

    [<Benchmark(Baseline = true); BenchmarkCategory("Draw Dictionary")>]
    member this.DrawDictionaryMapped () = Bench.draw this.MappedDictionary

    [<Benchmark; BenchmarkCategory("Draw Dictionary")>]
    member this.DrawDictionaryCore () = Bench.draw this.CoreDictionary

    //
    // Start to shrink
    //

    [<Benchmark(Baseline = true); BenchmarkCategory("Shrink ImmutableArray")>]
    member this.ShrinkImmutableArrayMapped () = Bench.startShrinking this.MappedImmutableArrayTree

    [<Benchmark; BenchmarkCategory("Shrink ImmutableArray")>]
    member this.ShrinkImmutableArrayCore () = Bench.startShrinking this.CoreImmutableArrayTree

    [<Benchmark(Baseline = true); BenchmarkCategory("Shrink ImmutableList")>]
    member this.ShrinkImmutableListMapped () = Bench.startShrinking this.MappedImmutableListTree

    [<Benchmark; BenchmarkCategory("Shrink ImmutableList")>]
    member this.ShrinkImmutableListCore () = Bench.startShrinking this.CoreImmutableListTree

    [<Benchmark(Baseline = true); BenchmarkCategory("Shrink HashSet")>]
    member this.ShrinkHashSetMapped () = Bench.startShrinking this.MappedHashSetTree

    [<Benchmark; BenchmarkCategory("Shrink HashSet")>]
    member this.ShrinkHashSetCore () = Bench.startShrinking this.CoreHashSetTree

    [<Benchmark(Baseline = true); BenchmarkCategory("Shrink Dictionary")>]
    member this.ShrinkDictionaryMapped () = Bench.startShrinking this.MappedDictionaryTree

    [<Benchmark; BenchmarkCategory("Shrink Dictionary")>]
    member this.ShrinkDictionaryCore () = Bench.startShrinking this.CoreDictionaryTree
