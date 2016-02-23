namespace Batch.Toolkit.Tests

open Batch.Toolkit
open NUnit.Framework

open Newtonsoft.Json

open FSharp.Data
open FsUnit

module JsonTests =    
    [<Test>]
    let ``batch configuration without service url specified should construct service url correctly`` () =
        let json = """{
      "BatchAccountName": "batch-account-name",
      "BatchAccountKey": "A_BIG_SECRET",
      "BatchAccountRegion": "southeastasia",
      "BatchServiceUri": null
    }"""

        let config = readJson<BatchConfiguration>(json)

        config.BatchAccountName   |> should equal "batch-account-name"
        config.BatchAccountKey    |> should equal "A_BIG_SECRET"
        config.BatchAccountRegion |> should equal "southeastasia"

        config |> getServiceUri |> should equal "https://batch-account-name.southeastasia.batch.azure.com"
   
    [<Test>]
    let ``batch configuration with service url specified should use it always`` () =
        let json = """{
      "BatchAccountName": "batch-account-name",
      "BatchAccountKey": "A_BIG_SECRET",
      "BatchAccountRegion": "southeastasia",
      "BatchServiceUri": "https://localhost:20201/goober"
    }"""

        let config = readJson<BatchConfiguration>(json)

        config.BatchAccountName   |> should equal "batch-account-name"
        config.BatchAccountKey    |> should equal "A_BIG_SECRET"
        config.BatchAccountRegion |> should equal "southeastasia"
        config.BatchServiceUri    |> should equal "https://localhost:20201/goober"

        config |> getServiceUri |> should equal "https://localhost:20201/goober"

    [<Test>]
    let ``should be able to read storage configuration file`` () =
        let json = """{
            "StorageAccountName": "storage-account-name",
            "StorageAccountKey" : "storage-account-key"
        }"""

        let config = readJson<StorageConfiguration>(json)
        config.StorageAccountName |> should equal "storage-account-name"
        config.StorageAccountKey  |> should equal "storage-account-key"

//    [<Test>]
//    let ``should be able to read task resources file`` () =
//        let json = """{
//            "Files": [
//                        "file 1",
//                        "file 2"
//                     ],
//        }"""
//
//        let config = readJson<LocalFiles>(json)
//        config.Files |> should equal ["file 1"; "file 2"]
