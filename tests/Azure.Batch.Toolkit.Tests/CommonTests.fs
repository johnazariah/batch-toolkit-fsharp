namespace Batch.Toolkit.Tests

open System
open Batch.Toolkit
open NUnit.Framework

open Newtonsoft.Json

open FSharp.Data
open FsUnit

module CommonTests =
    [<Test>]
    let ``getOrElse works properly`` () =    
        Some 3 |> getOrElse 0 |> should equal 3
        None   |> getOrElse 3 |> should equal 3

    [<Test>]
    let ``getOrNull works properly`` () =    
        Some "hello" |> getOrNull |> should equal "hello"
        None         |> getOrNull |> should equal null

    [<Test>]
    let ``getOrNullable works properly`` () =    
        Some 3 |> getOrNullable |> should equal 3
        None   |> getOrNullable |> should equal (Nullable())

