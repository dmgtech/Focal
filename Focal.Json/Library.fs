module Focal.Json 

open Focal.Core
open FSharp.Data

let jsonRecordPrism : Prism<_,_,_,_> =
    { 
        _which = function
            | JsonValue.Record r -> Ok r
            | x -> Error x
        _unto = fun (xs: (string * JsonValue) array) -> JsonValue.Record xs
    }

let jsonArrayPrism : Prism<_,_,_,_> =
    { 
        _which = function
            | JsonValue.Array arr -> Ok arr
            | x -> Error x
        _unto = fun (xs: JsonValue array) -> JsonValue.Array xs
    }

let jsonStringPrism : Prism<_,_,_,_> =
    { 
        _which = function
            | JsonValue.String str -> Ok str
            | x -> Error x
        _unto = fun (str: string) -> JsonValue.String str
    }

let jsonNumberPrism : Prism<_,_,_,_> =
    { 
        _which = function
            | JsonValue.Number n -> Ok n
            | x -> Error x
        _unto = fun (n: decimal) -> JsonValue.Number n
    }

let jsonFloatPrism : Prism<_,_,_,_> =
    { 
        _which = function
            | JsonValue.Float f -> Ok f
            | x -> Error x
        _unto = fun (f: float) -> JsonValue.Float f
    }

let jsonBooleanPrism : Prism<_,_,_,_> =
    { 
        _which = function
            | JsonValue.Boolean b -> Ok b
            | x -> Error x
        _unto = fun (b: bool) -> JsonValue.Boolean b
    }

let jsonNullPrism : Prism<_,_,_,_> =
    { 
        _which = function
            | JsonValue.Null -> Ok ()
            | x -> Error x
        _unto = fun _ -> JsonValue.Null
    }

let jsonRecordMemberTraversal str : ITraversal<_,_,_,_> =
    idLens.ComposeWith(jsonRecordPrism).Each().Filtered(fun x -> fst x = str).ComposeWith(sndLens)

let rec private descendantSearch str json : JsonValue seq =
    match json with
    | JsonValue.Record r -> 
        Seq.concat [
            Seq.collect (fun (k,v) -> descendantSearch str v) r
            Seq.choose (fun (k,v) -> if k = str then Some v else None) r ]
    | JsonValue.Array a -> Seq.collect (descendantSearch str) a
    | _ -> Seq.empty

let rec private descendantUpdate str f (json: JsonValue) =
    match json with
    | JsonValue.Record r -> 
        r
        |> Array.map (fun (k, v) ->
            let v' = descendantUpdate str f v
            let v'' = if k = str then f v' else v'
            (k, v''))
        |> JsonValue.Record
    | JsonValue.Array a -> (JsonValue.Array << Array.map (descendantUpdate str f)) a
    | x -> x

let jsonDescendantTraversal str : Traversal<_,_,_,_> =
    {
        _fold = { _toSeq = fun (s: JsonValue) -> descendantSearch str s }
        _setter = { _over = (fun a2b (s: JsonValue) -> descendantUpdate str a2b s) }
    }

open System.Runtime.CompilerServices
[<Extension>]
type OpticsExtensionMethods_JsonValue =
    [<Extension>]
    static member inline Member(traversal : ITraversal<'a,'b,JsonValue,JsonValue>, memberName: string) : ITraversal<'a,'b,JsonValue,JsonValue> =
        traversal.ComposeWith(jsonRecordMemberTraversal memberName)
    [<Extension>]
    static member inline Descendants(traversal : ITraversal<'a,'b,JsonValue,JsonValue>, descendantName: string) : ITraversal<'a,'b,JsonValue,JsonValue> =
        traversal.ComposeWith(jsonDescendantTraversal descendantName)
    [<Extension>]
    static member inline IfRecord(prism : IPrism<'a,'b,JsonValue,JsonValue>) : IPrism<'a,'b,(string * JsonValue) array,(string * JsonValue) array> =
        prism.ComposeWith(jsonRecordPrism)
    [<Extension>]
    static member inline IfRecord(traversal : ITraversal<'a,'b,JsonValue,JsonValue>) : ITraversal<'a,'b,(string * JsonValue) array,(string * JsonValue) array> =
        traversal.ComposeWith(jsonRecordPrism)
    [<Extension>]
    static member inline IfArray(prism : IPrism<'a,'b,JsonValue,JsonValue>) : IPrism<'a,'b,JsonValue array,JsonValue array> =
        prism.ComposeWith(jsonArrayPrism)
    [<Extension>]
    static member inline IfArray(traversal : ITraversal<'a,'b,JsonValue,JsonValue>) : ITraversal<'a,'b,JsonValue array,JsonValue array> =
        traversal.ComposeWith(jsonArrayPrism)
    [<Extension>]
    static member inline IfString(prism : IPrism<'a,'b,JsonValue,JsonValue>) : IPrism<'a,'b,string,string> =
        prism.ComposeWith(jsonStringPrism)
    [<Extension>]
    static member inline IfString(traversal : ITraversal<'a,'b,JsonValue,JsonValue>) : ITraversal<'a,'b,string,string> =
        traversal.ComposeWith(jsonStringPrism)
    [<Extension>]
    static member inline IfNumber(prism : IPrism<'a,'b,JsonValue,JsonValue>) : IPrism<'a,'b,decimal,decimal> =
        prism.ComposeWith(jsonNumberPrism)
    [<Extension>]
    static member inline IfNumber(traversal : ITraversal<'a,'b,JsonValue,JsonValue>) : ITraversal<'a,'b,decimal,decimal> =
        traversal.ComposeWith(jsonNumberPrism)
    [<Extension>]
    static member inline IfFloat(prism : IPrism<'a,'b,JsonValue,JsonValue>) : IPrism<'a,'b,float,float> =
        prism.ComposeWith(jsonFloatPrism)
    [<Extension>]
    static member inline IfFloat(traversal : ITraversal<'a,'b,JsonValue,JsonValue>) : ITraversal<'a,'b,float,float> =
        traversal.ComposeWith(jsonFloatPrism)
    [<Extension>]
    static member inline IfBoolean(prism : IPrism<'a,'b,JsonValue,JsonValue>) : IPrism<'a,'b,bool,bool> =
        prism.ComposeWith(jsonBooleanPrism)
    [<Extension>]
    static member inline IfBoolean(traversal : ITraversal<'a,'b,JsonValue,JsonValue>) : ITraversal<'a,'b,bool,bool> =
        traversal.ComposeWith(jsonBooleanPrism)
    [<Extension>]
    static member inline IfNull(prism : IPrism<'a,'b,JsonValue,JsonValue>) : IPrism<'a,'b,unit,unit> =
        prism.ComposeWith(jsonNullPrism)
    [<Extension>]
    static member inline IfNull(traversal : ITraversal<'a,'b,JsonValue,JsonValue>) : ITraversal<'a,'b,unit,unit> =
        traversal.ComposeWith(jsonNullPrism)
        
