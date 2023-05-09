module Tests

open System
open Xunit
open Focal.Core

[<Fact>]
let ``Get-Put Lens Law`` () =
    let x = (2,"asdf")
    Assert.Equal(fstLens.Set (fstLens.Get x) x, x)

[<Fact>]
let ``Put-Get Lens Law`` () =
    let x = (2,"asdf")
    Assert.Equal(fstLens.Get (fstLens.Set 3 x), 3)

[<Fact>]
let ``Put-Put Lens Law`` () =
    let x = (2,"asdf")
    Assert.Equal(fstLens.Set 4 (fstLens.Set 3 x), fstLens.Set 4 x)

[<Fact>]
let ``Nested Set operation`` () =
    let f n = System.Linq.Enumerable.Repeat(n, n) |> Seq.toList
    let x = Map.ofList [
        "foo", Set.ofList [1; 2; 3]
        "bar", Set.ofList [4; 5; 6] ]
    let expected = Map.ofList [
        "foo", Set.ofList [[1] ; [2;2]; [3;3;3] ]
        "bar", Set.ofList [[4;4;4;4]; [5;5;5;5;5]; [6;6;6;6;6;6]] ]
    let actual = x |> idLens<Map<_,Set<_>>,_>.EachValue().Each().Over f
    Assert.StrictEqual(expected, actual)

[<Fact>]
let ``Traverse nested structure`` () =
    let f n = System.Linq.Enumerable.Repeat(n, n) |> Seq.toList
    let x = [ Set.ofList [1; 2; 3]
              Set.ofList [4; 5; 6] ]
    let expected = [1; 2; 3; 4; 5; 6]
    let actual = x |> idLens<Set<_> list,_>.Each().Each().ToSeq |> Seq.toList
    Assert.StrictEqual(expected, actual)

[<Fact>]
let ``TryHead nested structure`` () =
    let f n = System.Linq.Enumerable.Repeat(n, n) |> Seq.toList
    let x = [ Set.ofList [1; 2; 3]
              Set.ofList [4; 5; 6] ]
    let expected = Some 1
    let actual = x |> idLens<Set<_> list,_>.Each().Each().TryHead()
    Assert.StrictEqual(expected, actual)

[<Fact>]
let ``Filtered nested structure`` () =
    let even x = (x % 2 = 0)
    let x = [ Set.ofList [1; 2; 3]
              Set.ofList [4; 5; 6] ]
    let expected = [2; 4; 6]
    let actual = x |> idLens<Set<_> list,_>.Each().Each().Filtered(even).ToSeq |> Seq.toList
    Assert.StrictEqual(expected, actual)

[<Fact>]
let ``Nested tuples`` () =
    let sq n = n * n
    let x = ("asdf", ([(1,2); (3,4)], "fdsa"))
    let expected = ("asdf", ([("1",2); ("9",4)], "fdsa"))
    let actual = x |> sndLens.ComposeWith(fstLens).ComposeWith(EachTraversal.eachOfList).ComposeWith(fstLens).Over(string << sq)
    Assert.StrictEqual(expected, actual)