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
    
[<Fact>]
let ``Each works (array)`` () =
    let expected = [|2; 3; 4|]
    let result: int array = [|1; 2; 3|] |> idLens<int array,_>.Each().Over(fun x -> x + 1)
    Assert.Equal<int array>(expected, result)
    
[<Fact>]
let ``Each works (array, new type)`` () =
    let expected = [|"1"; "2"; "3"|]
    let result: string array = [|1; 2; 3|] |> idLens<int array,string array>.Each().Over(string)
    Assert.Equal<string array>(expected, result)
    
[<Fact>]
let ``Each works (map)`` () =
    let expected = Map.ofArray [|(1,"a"); (2,"b"); (3,"c")|]
    let result: Map<int,string> = Map.ofArray [|("1", 'a'); ("2", 'b'); ("3", 'c')|] |> idLens<Map<string,char>,_>.Each().Over(fun (k,v) -> (int k, string v))
    Assert.Equal<Map<int,string>>(expected, result)
    
[<Fact>]
let ``EachValue works (map)`` () =
    let expected = Map.ofArray [|(1,"a"); (2,"b"); (3,"c")|]
    let result: Map<int,string> = Map.ofArray [|(1, 'a'); (2, 'b'); (3, 'c')|] |> idLens<Map<int,char>,_>.EachValue().Over(fun v -> string v)
    Assert.Equal<Map<int,string>>(expected, result)
    
[<Fact>]
let ``Each works (seq)`` () =
    let expected = seq { yield 2; yield 3; yield 4 }
    let result = seq { yield 1; yield 2; yield 3 } |> idLens<int seq,_>.Each().Over(fun x -> x + 1)
    Assert.True(System.Linq.Enumerable.SequenceEqual(expected, result))
    
[<Fact>]
let ``Result prism works (Ok case, match)`` () =
    let expected = Ok 2
    let result = Ok 1 |> idLens.IfOk().Over(fun x -> x + 1)
    Assert.Equal(expected, result)
    
[<Fact>]
let ``Result prism works (Ok case, non-match)`` () =
    let expected = Error "asdf"
    let result = Error "asdf" |> idLens.IfOk().Over(fun x -> x + 1)
    Assert.Equal(expected, result)
    
[<Fact>]
let ``Result prism works (Error case, match)`` () =
    let expected = Error "error"
    let result = Error "failure" |> idLens.IfError().Set "error"
    Assert.Equal(expected, result)
    
[<Fact>]
let ``Result prism works (Error case, non-match)`` () =
    let expected = Ok 1
    let result = Ok 1 |> idLens.IfError().Set "error"
    Assert.Equal(expected, result)
    
[<Fact>]
let ``Result prism works (Error case, match, composed)`` () =
    let expected = Ok (Error "error")
    let result = Ok (Error "failure") |> (ResultPrism.ifOk :> IPrism<_,_,_,_>).IfError().Set "error"
    Assert.Equal(expected, result)

[<Fact>]
let ``Result prism works (Error case, non-match, composed)`` () =
    let expected = Error (Error "failure")
    let result = Error (Error "failure") |> (ResultPrism.ifOk :> IPrism<_,_,_,_>).IfError().Set "error"
    Assert.Equal(expected, result)

[<Fact>]
let ``Result prism ToSeq works (failure case)`` () =
    let expected = []
    let result = Error "failure" |> (ResultPrism.ifOk :> IPrism<_,_,int,_>).ToSeq |> List.ofSeq
    Assert.Equal<int list>(expected, result)

[<Fact>]
let ``Prism ComposeWith function works (success case)`` () =
    let expected = Ok (Some 6)
    let result = Ok (Some 5) |> idLens.IfOk().ComposeWith(OptionPrism.ifSome).Over(fun x -> x + 1)
    Assert.Equal(expected, result)

[<Fact>]
let ``Prism ComposeWith function works (failure case)`` () =
    let expected = Error (Ok None)
    let result = Ok None |> (ResultPrism.ifOk :> IPrism<_,_,_,_>).IfSome().Which
    Assert.Equal(expected, result) 

[<Fact>]
let ``Prism ComposeWith<IFold<_,_>...> function works`` () =
    let expected = [5]
    let result = Ok (Some 5) |> (ResultPrism.ifOk :> IPrism<_,_,_,_>).ComposeWith(OptionPrism.ifSome : IFold<_,_>).ToSeq |> List.ofSeq
    Assert.Equal<int list>(expected, result) 

[<Fact>]
let ``Prism ComposeWith<ISetter<_,_,_,_>...> function works`` () =
    let expected = Ok (Some 6)
    let result = Ok (Some 5) |> (ResultPrism.ifOk :> IPrism<_,_,_,_>).ComposeWith(OptionPrism.ifSome : ISetter<_,_,_,_>).Set 6
    Assert.Equal(expected, result) 

[<Fact>]
let ``Prism ComposeWith<ITraversal<_,_,_,_>...> function works`` () =
    let expected = Ok (Some 6)
    let result = Ok (Some 5) |> (ResultPrism.ifOk :> IPrism<_,_,_,_>).ComposeWith(OptionPrism.ifSome : ITraversal<_,_,_,_>).Set 6 
    Assert.Equal(expected, result) 

[<Fact>]
let ``fstLens works (setter)`` () =
    let expected = (2, "asdf")
    let result = (1, "asdf") |> fstLens.Over (fun x -> x+1) 
    Assert.Equal(expected, result) 

[<Fact>]
let ``fstLens works (setter w/ new type)`` () =
    let expected = (1L, "asdf")
    let result = (1, "asdf") |> fstLens.Over int64
    Assert.Equal(expected, result) 

[<Fact>]
let ``fstLens works (getter)`` () =
    let expected = 1
    let result = (1, "asdf") |> fstLens.Get
    Assert.Equal(expected, result) 

[<Fact>]
let ``sndLens works (setter)`` () =
    let expected = (1, "fdsa")
    let result = (1, "asdf") |> sndLens.Over (fun s -> new string(s.ToCharArray() |> Array.rev)) 
    Assert.Equal(expected, result) 

[<Fact>]
let ``sndLens works (setter w/ new type)`` () =
    let expected = (1, 4)
    let result = (1, "asdf") |> sndLens.Over String.length
    Assert.Equal(expected, result) 

[<Fact>]
let ``sndLens works (setter w/ new type, nested)`` () =
    let expected = ("qwerty", (1, 4))
    let result = ("qwerty", (1, "asdf")) |> sndLens.ComposeWith(sndLens).Over String.length
    Assert.Equal(expected, result) 

[<Fact>]
let ``sndLens works (setter w/ new type, nested, using '>>' operator)`` () =
    let expected = ("qwerty", (1, 4))
    let result = ("qwerty", (1, "asdf")) |> (sndLens.Over >> sndLens.Over) String.length 
    Assert.Equal(expected, result) 

[<Fact>]
let ``sndLens works (getter)`` () =
    let expected = "asdf"
    let result = (1, "asdf") |> sndLens.Get
    Assert.Equal(expected, result) 

[<Fact>]
let ``Exists works`` () =
    let input = [1; 2; 5]
    Assert.Equal(false, [] |> idLens<_ list,_>.Each().Exists()) 
    Assert.Equal(true, input |> idLens<_ list,_>.Each().Exists()) 
    Assert.Equal(true, input |> idLens<_ list,_>.Each().Exists(fun x -> x = 1)) 
    Assert.Equal(false, input |> idLens<_ list,_>.Each().Exists(fun x -> x = 3)) 
