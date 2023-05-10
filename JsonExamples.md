# Focal.Json Example Usage

Manipulating semi-structured data is one place where optics libraries really shine.  The `JsonValue` type found in the `FSharp.Data` nuget package is a recursive discriminated union representing JSON data.  This makes it a perfect target for demonstrating the power of a few simple optics types.  

Before we do anything though, lets go ahead and pull down some data.  In this example, we will be pulling information about current US senators:

```fsharp
#r """nuget: FSharp.Data, version=5.0.2"""
#r "nuget: FsHttp"

open FsHttp
open FSharp.Data

let data = 
    http {
        GET "https://www.govtrack.us/api/v2/role?current=true&role_type=senator"
    }
    |> Request.send
    |> fun x -> x.content.ReadAsStringAsync()
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> JsonValue.Parse
```

Now we have a value `data` which contains a parsed `JsonValue` tree.  Great!  Now let's do something with it.  First a little setup...

```fsharp

#r """nuget: Focal.Json"""
open Focal.Core
open Focal.Json


let jsonLens = idLens<JsonValue,JsonValue> // create a specific id lens for JsonValue types to make auto-complete work a bit faster
```

Now that we have that taken care of, let's start investigating our data...  For instance, we could find out what fields are available within records under the `"objects"` setting:

```fsharp
> let objectFieldNames = 
-     data
-     |> jsonLens.Member("objects").IfArray().Each().IfRecord().Each().ComposeWith(fstLens).ToSeq 
-     |> Seq.distinct
-     |> List.ofSeq;;

val objectFieldNames: string list =
  ["caucus"; "congress_numbers"; "current"; "description"; "district";
   "enddate"; "extra"; "leadership_title"; "party"; "person"; "phone";
   "role_type"; "role_type_label"; "senator_class"; "senator_class_label";
   "senator_rank"; "senator_rank_label"; "startdate"; "state"; "title";
   "title_long"; "website"]
>
```

That went a bit fast, so let's slow that down at look at each point of optics composition:

```fsharp
let objectFieldNames = 
    data 
    // start with an identity lens..
    |> jsonLens // ILens<_,_,JsonValue,JsonValue>

        // ...drill down into the "objects" child
        .Member("objects") // ITraversal<_,_,JsonValue,JsonValue>
        
        // ...since "objects" is a JsonValue.Array, drill down into the array of JsonValue instances
        .IfArray() // ITraversal<_,_,JsonValue array,JsonValue array>
        
        // ...now drill down into each value in that array
        .Each() // ITraversal<_,_,JsonValue,JsonValue>
        
        // ...drill down only if this is a JsonValue.Record
        .IfRecord() // ITraversal<_,_,(string * JsonValue) array,(string * JsonValue) array>
        
        // ...drill down into each key/value pair of the JsonValue.Record type
        .Each() // ITraversal<_,_,(string * JsonValue),(string * JsonValue)>
        
        // ...drill down into the first object of the tuple
        .ComposeWith(fstLens) // ITraversal<_,_,string,string>
        
        // ...and get the sequence of the focused data
        .ToSeq // string seq
    |> Seq.distinct
    |> List.ofSeq
```
 
> If you're wondering why the first item is ILens and the rest are `ITraversal`, it's because the Member extension method returns an `ITraversal`.  And since `ILens` inherits from `ITraversal`, the composition of the two is an `ITraversal` because it is the *least general* type that can apply to both.

Wow, that was a lot..  Maybe we should just save that traversal off -- we might need it later

```fsharp
let objectsTraversal = jsonLens.Member("objects").IfArray().Each().IfRecord().Each()

let objectFieldNames = 
    data 
    |> objectsTraversal.ComposeWith(fstLens).ToSeq 
    |> Seq.distinct
    |> List.ofSeq
```

Okay, now where were we..  Ah, we were looking at available fields!  Now, what if we wanted to see all fields that were *under* the fields *under* objects (i.e. 2 levels down)?  It would look something like this:

```fsharp
>
- let objectFieldNamesLevel2 = 
-     data
-     |> jsonLens.Member("objects").IfArray().Each().IfRecord().Each().ComposeWith(sndLens).IfRecord().Each().ComposeWith(fstLens).ToSeq 
-     |> Seq.distinct
-     |> List.ofSeq;;
val objectFieldNamesLevel2: string list =
  ["address"; "contact_form"; "office"; "rss_url"; "bioguideid"; "birthday";
   "cspanid"; "fediverse_webfinger"; "firstname"; "gender"; "gender_label";
   "lastname"; "link"; "middlename"; "name"; "namemod"; "nickname"; "osid";
   "pvsid"; "sortname"; "twitterid"; "youtubeid"; "party_affiliations";
   "end-type"; "how"]

>
```

We could, of course, re-use the traversal we defined a moment ago:

```fsharp
let objectFieldNamesLevel2 = 
    data 
    |> objectsTraversal.ComposeWith(sndLens).IfRecord().Each().ComposeWith(fstLens).ToSeq 
    |> Seq.distinct
    |> List.ofSeq
```

Alright, enough investigating the structure of the data..  Let's get down to some manipulations..  Say we wanted to get just the names and addresses of senators:

```fsharp
>
- let justNamesAndAddresses = 
-     data
-     |> jsonLens.Member("objects").IfArray().Each().ToSeq
-     |> List.ofSeq
-     |> List.map (fun x ->
-         let name = x |> jsonLens.Member("person").Member("name").IfString().HeadOrDefault("N/A")
-         let address = x |> jsonLens.Member("extra").Member("address").HeadOrDefault(JsonValue.Null)
-         JsonValue.Record [|
-             ("name", JsonValue.String name) 
-             ("address", address)|] )    
- ;;
val justNamesAndAddresses: JsonValue list =
  [{
  "name": "Sen. Maria Cantwell [D-WA]",
  "address": "511 Hart Senate Office Building Washington DC 20510"
};
   {
  "name": "Sen. Thomas Carper [D-DE]",
  "address": "513 Hart Senate Office Building Washington DC 20510"
};
   {
  "name": "Sen. Dianne Feinstein [D-CA]",
  "address": "331 Hart Senate Office Building Washington DC 20510"
};
   {
  "name": "Sen. Debbie Stabenow [D-MI]",
  "address": "731 Hart Senate Office Building Washington DC 20510"
};
...
```

Neat I guess?  Maybe it would be more interesting if we modified those objects in-place:

```fsharp
>
- let justNamesAndAddresses = 
-     data
-     |> jsonLens.Member("objects").IfArray().Each().Over(fun x ->
-         let name = x |> jsonLens.Member("person").Member("name").IfString().HeadOrDefault("N/A")
-         let address = x |> jsonLens.Member("extra").Member("address").HeadOrDefault(JsonValue.Null)
-         JsonValue.Record [|
-             ("name", JsonValue.String name)
-             ("address", address)|] )    ;;
val justNamesAndAddresses: JsonValue =
  {
  "meta": {
    "limit": 100,
    "offset": 0,
    "total_count": 100
  },
  "objects": [
    {
      "name": "Sen. Maria Cantwell [D-WA]",
      "address": "511 Hart Senate Office Building Washington DC 20510"
    },
    {
      "name": "Sen. Thomas Carper [D-DE]",
      "address": "513 Hart Senate Office Building Washington DC 20510"
    },
    {
      "name": "Sen. Dianne Feinstein [D-CA]",
      "address": "331 Hart Senate Office Building Washington DC 20510"
    },
    {
      ...
```

What just happened here??  The `Over` takes a function and then surgically applies it in-place at the current focus of your optic (in this instance, each `JsonValue`  instance under the `"objects"` node).  `name` and `address` are pulled using optics over the focused node.

Let's do a couple more and call it a day..  Maybe we want to find out how many senators are of each gender and party:
```fsharp
>
- let groupedByPartyAndGender = 
-     data
-     |> jsonLens.Member("objects").IfArray().Each().ToSeq
-     |> Seq.groupBy (fun x ->
-         jsonLens.Member("party").IfString().TryHead() x,
-         jsonLens.Member("person").Member("gender").IfString().TryHead() x)
-     |> Seq.map (sndLens.Over(Seq.length))
-     |> List.ofSeq;;
val groupedByPartyAndGender: ((string option * string option) * int) list =
  [((Some "Democrat", Some "female"), 15);
   ((Some "Democrat", Some "male"), 33);
   ((Some "Republican", Some "female"), 9);
   ((Some "Independent", Some "male"), 2);
   ((Some "Republican", Some "male"), 40);
   ((Some "Independent", Some "female"), 1)]
```

Or maybe we want to sort the senators by age:

```fsharp
let sortOldestToYoungest = 
    data
    |> jsonLens.Member("objects").IfArray()
        .Over(Array.sortBy (jsonLens.Member("person").Member("birthday").IfString().HeadOrDefault("")))
```

And for reference, the finished script:
```fsharp
#r """nuget: FSharp.Data, version=5.0.2"""
#r "nuget: FsHttp"

open FsHttp
open FSharp.Data

let data = 
    http {
        GET "https://www.govtrack.us/api/v2/role?current=true&role_type=senator"
    }
    |> Request.send
    |> fun x -> x.content.ReadAsStringAsync()
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> JsonValue.Parse

#r """nuget: Focal.Json"""
open Focal.Core
open Focal.Json

let jsonLens = idLens<JsonValue,JsonValue>

let objectsTraversal = jsonLens.Member("objects").IfArray().Each().IfRecord().Each()

let objectFieldNames = 
    data 
    |> objectsTraversal.ComposeWith(fstLens).ToSeq 
    |> Seq.distinct
    |> List.ofSeq

let objectFieldNamesLevel2 = 
    data 
    |> jsonLens.Member("objects").IfArray().Each().IfRecord().Each().ComposeWith(sndLens).IfRecord().Each().ComposeWith(fstLens).ToSeq 
    |> Seq.distinct
    |> List.ofSeq

let justNamesAndAddresses = 
    data
    |> jsonLens.Member("objects").IfArray().Each().Over(fun x ->         
        let name = x |> jsonLens.Member("person").Member("name").IfString().HeadOrDefault("N/A")
        let address = x |> jsonLens.Member("extra").Member("address").HeadOrDefault(JsonValue.Null)
        JsonValue.Record [|
            ("name", JsonValue.String name) 
            ("address", address)|] )    

let sortOldestToYoungest = 
    data
    |> jsonLens.Member("objects").IfArray()
        .Over(Array.sortBy (jsonLens.Member("person").Member("birthday").IfString().HeadOrDefault("")))
    |> fun x -> System.IO.File.WriteAllText("c:\\temp\\data.json", string x)

let groupedByPartyAndGender = 
    data
    |> jsonLens.Member("objects").IfArray().Each().ToSeq
    |> Seq.groupBy (fun x -> 
        jsonLens.Member("party").IfString().TryHead() x,
        jsonLens.Member("person").Member("gender").IfString().TryHead() x)
    |> Seq.map (sndLens.Over(Seq.length))
    |> List.ofSeq
```