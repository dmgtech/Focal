# Focal
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
The purpose of this project is to create an optics library that fits as well as possible into idiomatic F# code.  This is largely accomplished by a combination of an explicitly defined interface hierarchy for the main optics types, and complimented by extension methods to allow for composing optics using a Fluent-like interface.  The instances of said interfaces are implemented as records of matching fuctions.  This is a large departure from the [lens](https://hackage.haskell.org/package/lens) or [optics](https://github.com/well-typed/optics) libraries in Haskell.  In exchange for a reduced level of expressiveness, we are able to make heavy use of the auto-complete features available for F# in VSCode and Visual Studio.

## Examples

[JSON Examples](JsonExamples.md)

