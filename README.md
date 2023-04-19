# FsOptics
Lenses/Prisms/Traversals/etc. with an emphasis on usability in the F# programming environment


```mermaid
classDiagram
    `IFold<'s,'a>` <|-- `IGetter<'s,'a>`
    `ISetter<'s,'t,'a,'b>` <|-- `ITraversal<'s,'t,'a,'b>`
    `IFold<'s,'a>` <|-- `ITraversal<'s,'t,'a,'b>`
    `ITraversal<'s,'t,'a,'b>` <|-- `ILens<'s,'t,'a,'b>`
    `IGetter<'s,'a>` <|-- `ILens<'s,'t,'a,'b>`
    `ITraversal<'s,'t,'a,'b>` <|-- `IPrism<'s,'t,'a,'b>`
    
    class `IFold<'s,'a>` {
        ToSeq: ('s) -> seq<'a>
        ComposeWith: (IFold<'a,'b>) -> IFold<'s,'b>
    }
    class `ISetter<'s,'t,'a,'b>` {
        Over: ('a -> 'b) -> 's -> 't
        ComposeWith: (ISetter<'a,'b,'c,'d>) -> ISetter<'s,'t,'c,'d>
    }
    class `IGetter<'s,'a>` {
        Get: ('s) -> 'a
        ComposeWith: (IGetter<'a,'b>) -> IGetter<'s,'b>
    }
    class `ITraversal<'s,'t,'a,'b>` {
        ComposeWith: (ITraversal<'a,'b,'c,'d>) -> ITraversal<'s,'t,'c,'d>
    }
    class `ILens<'s,'t,'a,'b>` {
        ComposeWith: (ILens<'a,'b,'c,'d>) -> ILens<'s,'t,'c,'d>
    }
    class `IPrism<'s,'t,'a,'b>` {
        Which: ('s) -> Result<'a,'t>
        Unto: ('b) -> 't
        ComposeWith: (IPrism<'a,'b,'c,'d>) -> IPrism<'s,'t,'c,'d>
    }
```

## Design Goals
The purpose of this project is to create an optics library that fits as well as possible into idiomatic F# code.  This is largely accomplished by a combination of an explicitly defined interface hierarchy for the main optics types, and complimented by extension methods to allow for composing optics using a Fluent-like interface.  This is a large departure from the [lens](https://hackage.haskell.org/package/lens) or [optics](https://github.com/well-typed/optics) libraries in Haskell, which rely on a much more powerful type system (and thus are capable of expressing far more that can be done within the confines of the dotnet type system).

That said, it is still possible to create very useful optics expressions, especially when paired with recursive type structures (as seen in FsOptics.Json) or code generation (as is done in FsGrpc). 