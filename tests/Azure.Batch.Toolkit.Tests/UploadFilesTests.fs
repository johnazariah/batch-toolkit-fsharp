namespace Batch.Toolkit.Tests

open Microsoft.Azure.Batch
open Batch.Toolkit
open NUnit.Framework

open Newtonsoft.Json

open FSharp.Data
open FsUnit

open System.IO

module UploadFilesTests =
    // never check-in storage-config.json!
    let storageConfigFile = "storage-config.json"
    let storageContainerName = ContainerName "batch-toolkit-fsharp-test"
    let cloudStorageAccount = 
        storageConfigFile
        |> readConfig<StorageConfiguration> 
        |> GetCloudStorageAccount

    let test cloudStorageAccount storageContainerName f =
        try
            f() 
        finally
            DeleteContainer cloudStorageAccount storageContainerName |> ignore
            System.Threading.Thread.Sleep (5000)
         

    [<Test>]
    let ``upload named files to specified storage config should work`` () =
        if File.Exists("storage-config.json") then
            let files = ["storage-config.json"] |> List.map (FileInfo) 
            let expectedCount = (files |> Seq.length)
            test 
                cloudStorageAccount storageContainerName 
                (fun () ->
                    let result = UploadFilesToContainer cloudStorageAccount storageContainerName (LocalFiles files)
                    (result |> Seq.length) |> should equal expectedCount

                    let containerFiles = ListFilesInContainer cloudStorageAccount storageContainerName
                    (containerFiles |> Seq.length) |> should equal expectedCount
                )
        else
            ()