module Tests

open System
open Xunit
open FsOptics.Core

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