module Focal.Core

open FSharp.Core
open FSharp.Collections
open System.Collections.Generic

type IFold<'s,'a> =
    abstract member ToSeq : 's -> seq<'a>
    abstract member ComposeWith<'b> : IFold<'a,'b> -> IFold<'s,'b>

type Fold<'s,'a> =
    {
        _toSeq : 's -> seq<'a>
    }
    interface IFold<'s,'a> with
        member this.ToSeq s = this._toSeq s
        member this.ComposeWith<'b> (other : IFold<'a,'b>) = 
            { _toSeq = fun s -> this._toSeq s |> Seq.collect other.ToSeq }

type IGetter<'s, 'a> =
    inherit IFold<'s,'a>
    abstract member Get : 's -> 'a
    abstract member ComposeWith<'b> : IGetter<'a,'b> -> IGetter<'s,'b>

type Getter<'s,'a> =
    {
        _get : 's -> 'a
    }
    member this.CastAsFold () : IFold<'s,'a> = 
        { _toSeq = fun s -> Seq.singleton (this._get s) }
    interface IGetter<'s,'a> with
        member this.Get s = 
            this._get s
        member this.ComposeWith<'b> (other : IGetter<'a,'b>) =
            { _get = fun s -> s |> this._get |> other.Get }
    interface IFold<'s,'a> with
        member this.ToSeq s = 
            this.CastAsFold().ToSeq s
        member this.ComposeWith<'b> (other : IFold<'a,'b>) =
            this.CastAsFold().ComposeWith(other)

type ISetter<'s,'t,'a,'b> =
    abstract member Over : ('a -> 'b) -> 's -> 't
    abstract member ComposeWith<'c,'d> : ISetter<'a,'b,'c,'d> -> ISetter<'s,'t,'c,'d>

type ISetter'<'s,'a> = ISetter<'s,'s,'a,'a>

type Setter<'s,'t,'a,'b> =
    {
        _over : ('a -> 'b) -> 's -> 't
    }
    interface ISetter<'s,'t,'a,'b> with
        member this.Over a2b s = this._over a2b s
        member this.ComposeWith (other : ISetter<'a,'b,'c,'d>) =
            { _over = fun c2d s -> this._over (fun a -> other.Over c2d a) s }

type ITraversal<'s,'t,'a,'b> =
    inherit ISetter<'s,'t,'a,'b>
    inherit IFold<'s,'a>
    abstract member ComposeWith<'c,'d> : ITraversal<'a,'b,'c,'d> -> ITraversal<'s,'t,'c,'d>

type ITraversal'<'s,'a> = ITraversal<'s,'s,'a,'a>

type Traversal<'s,'t,'a,'b> =
    {
        _fold : IFold<'s,'a>
        _setter : ISetter<'s,'t,'a,'b>
    }
    interface ITraversal<'s,'t,'a,'b> with
        member this.ComposeWith (other : ITraversal<'a,'b,'c,'d>) : ITraversal<'s,'t,'c,'d> =
            { 
                _fold = this._fold.ComposeWith other 
                _setter = this._setter.ComposeWith other
            }
    interface ISetter<'s,'t,'a,'b> with
        member this.Over a2b s = this._setter.Over a2b s
        member this.ComposeWith (other : ISetter<'a,'b,'c,'d>) = this._setter.ComposeWith other
    interface IFold<'s,'a> with
        member this.ToSeq s = this._fold.ToSeq s
        member this.ComposeWith (other : IFold<'a,'c>) : IFold<'s,'c> = this._fold.ComposeWith other
    
type ILens<'s,'t,'a,'b> =
    inherit ITraversal<'s,'t,'a,'b>
    inherit ISetter<'s,'t,'a,'b>
    inherit IGetter<'s,'a>
    inherit IFold<'s,'a>
    abstract member ComposeWith<'c,'d> : ILens<'a,'b,'c,'d> -> ILens<'s,'t,'c,'d>

type ILens'<'s,'a> = ILens<'s,'s,'a,'a>

type Lens<'s,'t,'a,'b> =
    {
        _getter : IGetter<'s,'a>
        _setter : ISetter<'s,'t,'a,'b>
    }
    interface ILens<'s,'t,'a,'b> with
        member this.ComposeWith (other : ILens<'a,'b,'c,'d>) : ILens<'s,'t,'c,'d> =
            { 
                _getter = this._getter.ComposeWith other 
                _setter = this._setter.ComposeWith other
            }
    interface ITraversal<'s,'t,'a,'b> with
        member this.ComposeWith (other : ITraversal<'a,'b,'c,'d>) : ITraversal<'s,'t,'c,'d> =
            { 
                _fold = this._getter.ComposeWith other 
                _setter = this._setter.ComposeWith other
            }
    interface ISetter<'s,'t,'a,'b> with
        member this.Over a2b s = this._setter.Over a2b s
        member this.ComposeWith (other : ISetter<'a,'b,'c,'d>) = this._setter.ComposeWith other
    interface IFold<'s,'a> with
        member this.ToSeq s = this._getter.ToSeq s
        member this.ComposeWith (other : IFold<'a,'c>) : IFold<'s,'c> = this._getter.ComposeWith other
    interface IGetter<'s,'a> with
        member this.Get s = this._getter.Get s
        member this.ComposeWith (other : IGetter<'a,'c>) : IGetter<'s,'c> = this._getter.ComposeWith other

type IPrism<'s,'t,'a,'b> =
    inherit ITraversal<'s,'t,'a,'b>
    inherit ISetter<'s,'t,'a,'b>
    inherit IFold<'s,'a>
    abstract member Which : 's -> Result<'a,'t>
    abstract member Unto : 'b -> 't
    abstract member ComposeWith<'c,'d> : IPrism<'a,'b,'c,'d> -> IPrism<'s,'t,'c,'d>

type IPrism'<'s,'a> = IPrism<'s,'s,'a,'a>

type Prism<'s,'t,'a,'b> =
    {
        _which : 's -> Result<'a,'t>
        _unto : 'b -> 't
    }
    member this._over (a2b : 'a -> 'b) (s : 's)  =
        match this._which s with
        | Error t -> t
        | Ok a -> this._unto (a2b a)
    member this._toSeq s = 
            match this._which s with
            | Error t -> Seq.empty
            | Ok a -> Seq.singleton a
    interface IPrism<'s,'t,'a,'b> with
        member this.Which s = this._which s
        member this.Unto b = this._unto b
        member this.ComposeWith (other : IPrism<'a,'b,'c,'d>) = 
            {
                _unto = fun d -> other.Unto d |> this._unto
                _which = fun s ->
                    match this._which s with
                    | Error t -> Error t
                    | Ok a -> 
                        match other.Which a with
                        | Error b -> Error (this._unto b)
                        | Ok c -> Ok c
            }
    interface ITraversal<'s,'t,'a,'b> with
        member this.ComposeWith (other : ITraversal<'a,'b,'c,'d>) =
            { 
                _setter = ({ _over = this._over } :> ISetter<'s,'t,'a,'b>).ComposeWith other
                _fold = ({ _toSeq = this._toSeq } :> IFold<'s,'a>).ComposeWith other
            }
    interface ISetter<'s,'t,'a,'b> with
        member this.Over a2b s = this._over a2b s
        member this.ComposeWith (other : ISetter<'a,'b,'c,'d>) = 
            ({ _over = this._over } :> ISetter<'s,'t,'a,'b>).ComposeWith other
    interface IFold<'s,'a> with
        member this.ToSeq s = this._toSeq s
        member this.ComposeWith (other : IFold<'a,'c>) : IFold<'s,'c> = 
            ({ _toSeq = this._toSeq } :> IFold<'s,'a>).ComposeWith other

module OptionPrism =
    let ifSome<'a> : IPrism<Option<'a>,Option<'a>,'a,'a> =
        {
            _unto = fun a -> Some a
            _which = fun s ->
                match s with
                | Some a -> Ok a
                | _ -> Error s
        } : IPrism<Option<'a>,Option<'a>,'a,'a>
        
module ResultPrism =
    let ifOk<'a,'b> : IPrism<Result<'a,'b>,Result<'a,'b>,'a,'a> =
        {
            _unto = fun a -> Ok a
            _which = fun s ->
                match s with
                | Ok a -> Ok a
                | _ -> Error s
        } : IPrism<Result<'a,'b>,Result<'a,'b>,'a,'a>
    let ifError<'a,'b> : IPrism<Result<'a,'b>,Result<'a,'b>,'b,'b> =
        {
            _unto = fun b -> Error b
            _which = fun s ->
                match s with
                | Error b -> Ok b
                | _ -> Error s
        } : IPrism<Result<'a,'b>,Result<'a,'b>,'b,'b>


let idSetter<'s,'t> : ISetter<'s,'t,'s,'t> =
    { _over = fun s2t s -> s2t s }

let idLens<'s,'t> : ILens<'s,'t,'s,'t> =
    {
        _getter = { _get = fun s -> s }
        _setter = idSetter<'s,'t>
    }

let fstSetter<'sfst,'ssnd,'t> : ISetter<('sfst*'ssnd),('t*'ssnd),_,_> =
    { _over = fun s2t (pFst: 'sfst, pSnd: 'ssnd) -> (s2t pFst, pSnd) }

let fstLens<'sfst,'ssnd,'t> : ILens<('sfst*'ssnd),('t*'ssnd),'sfst,'t> = 
    {
        _getter = { _get = fun (pFst: 'sfst, _) -> pFst }
        _setter = fstSetter
    }

let sndSetter<'sfst,'ssnd,'t> : ISetter<('sfst*'ssnd),('sfst*'t),_,_> =
    { _over = fun s2t (pFst: 'sfst, pSnd: 'ssnd) -> (pFst, s2t pSnd) }

let sndLens<'sfst,'ssnd,'t> : ILens<('sfst*'ssnd),('sfst*'t),'ssnd,'t> = 
    {
        _getter = { _get = fun (_, pSnd: 'ssnd) -> pSnd }
        _setter = sndSetter
    }

let filteredPrism predicate : IPrism<_,_,_,_> =
        {
            _unto = fun s -> s
            _which = fun s ->
                match predicate s with
                | true -> Ok s
                | _ -> Error s
        } : IPrism<_,_,_,_>

let memberTraversal key : ITraversal<_,_,_,_> =
        {
            _fold = { _toSeq = fun s -> 
                            match Map.tryFind key s with
                            | Some v -> Seq.singleton v
                            | None -> Seq.empty } :> IFold<_,_>
            _setter = { _over = fun a2b s -> 
                            match Map.tryFind key s with
                            | Some v -> Map.add key (a2b v) s
                            | None -> s } :> ISetter<_,_,_,_>
        } : ITraversal<_,_,_,_>

module EachTraversal =
    let eachOfArray<'a,'b> : ITraversal<'a array,'b array,'a,'b> =
        {
            _fold = { _toSeq = fun s -> Array.toSeq s } :> IFold<'a array,'a>
            _setter = { _over = fun a2b s -> Array.map a2b s } :> ISetter<'a array,'b array,'a,'b>
        }
    let eachOfList<'a,'b> : ITraversal<'a list,'b list,'a,'b> =
        {
            _fold = { _toSeq = fun s -> List.toSeq s } :> IFold<'a list,'a>
            _setter = { _over = fun a2b s -> List.map a2b s } :> ISetter<'a list,'b list,'a,'b>
        }
    let eachOfSet<'a,'b when 'a:comparison and 'b:comparison> : ITraversal<Set<'a>,Set<'b>,'a,'b> =
        {
            _fold = { _toSeq = fun s -> Set.toSeq s } :> IFold<Set<'a>,'a>
            _setter = { _over = fun a2b s -> Set.map a2b s } :> ISetter<Set<'a>,Set<'b>,'a,'b>
        }
    let eachOfSeq<'a,'b> : ITraversal<'a seq,'b seq,'a,'b> =
        {
            _fold = { _toSeq = fun s -> s } :> IFold<'a seq,'a>
            _setter = { _over = fun a2b s -> Seq.map a2b s } :> ISetter<'a seq,'b seq,'a,'b>
        }
    let eachOfMap<'aKey,'aVal,'bKey,'bVal when 'aKey:comparison and 'bKey:comparison> : ITraversal<Map<'aKey,'aVal>,Map<'bKey,'bVal>,'aKey*'aVal,'bKey*'bVal> =
        {
            _fold = { _toSeq = fun s -> Map.toSeq s } :> IFold<Map<'aKey,'aVal>,'aKey*'aVal>
            _setter = { _over = fun a2b s -> s |> Map.toSeq |> Seq.map a2b |> Map.ofSeq } :> ISetter<Map<'aKey,'aVal>,Map<'bKey,'bVal>,'aKey*'aVal,'bKey*'bVal>
        }
    let eachOfMapValue<'aKey,'aVal,'bVal when 'aKey:comparison> : ITraversal<Map<'aKey,'aVal>,Map<'aKey,'bVal>,'aVal,'bVal> =
        {
            _fold = { _toSeq = fun s -> Map.values s } :> IFold<Map<'aKey,'aVal>,'aVal>
            _setter = { _over = fun a2b s -> Map.map (fun _ v -> a2b v) s } :> ISetter<Map<'aKey,'aVal>,Map<'aKey,'bVal>,'aVal,'bVal>
        }



open System.Runtime.CompilerServices
[<Extension>]
type OpticsExtensions =
    [<Extension>]
    static member inline Set(setter: ISetter<'s,'t,'a,'b>, x : 'b) = fun s -> setter.Over (fun _ -> x) s

    [<Extension>]
    static member inline Filtered(traversal: ITraversal<'s,'t,'a,'a>, predicate: 'a -> bool) : ITraversal<'s,'t,'a,'a> =
        traversal.ComposeWith(filteredPrism predicate)

    [<Extension>]
    static member inline Each(traversal: ITraversal<'s,'t,array<'a>,array<'b>>) : ITraversal<'s,'t,'a,'b> =
        traversal.ComposeWith(EachTraversal.eachOfArray)
        
    [<Extension>]
    static member inline Each(traversal: ITraversal<'s,'t,list<'a>,list<'b>>) : ITraversal<'s,'t,'a,'b> =
        traversal.ComposeWith(EachTraversal.eachOfList)

    [<Extension>]
    static member inline Each(traversal: ITraversal<'s,'t,Set<'a>,Set<'b>>) : ITraversal<'s,'t,'a,'b> =
        traversal.ComposeWith(EachTraversal.eachOfSet)
    
    [<Extension>]
    static member inline Each(traversal: ITraversal<'s,'t,Map<'aKey,'aVal>,Map<'bKey,'bVal>>) : ITraversal<'s,'t,'aKey*'aVal,'bKey*'bVal> =
        traversal.ComposeWith(EachTraversal.eachOfMap)
    
    [<Extension>]
    static member inline Each(traversal: ITraversal<'s,'t,seq<'a>,seq<'b>>) : ITraversal<'s,'t,'a,'b> =
        traversal.ComposeWith(EachTraversal.eachOfSeq)
    
    [<Extension>]
    static member inline EachValue(traversal: ITraversal<'s,'t,Map<'aKey,'aVal>,Map<'aKey,'bVal>>) : ITraversal<'s,'t,'aVal,'bVal> =
        traversal.ComposeWith(EachTraversal.eachOfMapValue)

    [<Extension>]
    static member inline Member(traversal: ITraversal<'s,'t,Map<'a,'b>,Map<'a,'b>>, key: 'a) : ITraversal<'s,'t,'b,'b> =
        traversal.ComposeWith(memberTraversal key)

    [<Extension>]
    static member inline IfSome(prism: IPrism<'s,'t,Option<'a>,Option<'a>>) : IPrism<'s,'t,'a,'a> =
        prism.ComposeWith(OptionPrism.ifSome)
        
    [<Extension>]
    static member inline IfSome(traversal: ITraversal<'s,'t,Option<'a>,Option<'a>>) : ITraversal<'s,'t,'a,'a> =
        traversal.ComposeWith(OptionPrism.ifSome)

    [<Extension>]
    static member inline IfOk(prism: IPrism<'s,'t,Result<'a,_>,Result<'a,_>>) : IPrism<'s,'t,'a,'a> =
        prism.ComposeWith(ResultPrism.ifOk)
        
    [<Extension>]
    static member inline IfOk(prism: ITraversal<'s,'t,Result<'a,_>,Result<'a,_>>) : ITraversal<'s,'t,'a,'a> =
        prism.ComposeWith(ResultPrism.ifOk)

    [<Extension>]
    static member inline IfError(prism: IPrism<'s,'t,Result<_,'b>,Result<_,'b>>) : IPrism<'s,'t,'b,'b> =
        prism.ComposeWith(ResultPrism.ifError)
        
    [<Extension>]
    static member inline IfError(prism: ITraversal<'s,'t,Result<_,'b>,Result<_,'b>>) : ITraversal<'s,'t,'b,'b> =
        prism.ComposeWith(ResultPrism.ifError)
        
    [<Extension>]
    static member inline Exists(fold: IFold<'s,'a>, predicate: 'a -> bool) =
        fun x -> x |> fold.ToSeq |> Seq.exists predicate

    [<Extension>]
    static member inline Exists(fold: IFold<'s,'a>) =
        fun x -> x |> fold.ToSeq |> Seq.exists(fun _ -> true)

    [<Extension>]
    static member inline TryHead(fold : IFold<_,_>, x) =
        Seq.tryHead << fold.ToSeq

    [<Extension>]
    static member inline HeadOrDefault(fold : IFold<_,_>, x) =
        Option.defaultValue x << Seq.tryHead << fold.ToSeq