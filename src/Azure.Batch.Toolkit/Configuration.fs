namespace Batch.Toolkit

[<AutoOpen>]
module Configuration =    
    open System.IO
    open Microsoft.Azure.Batch.Auth
    open Microsoft.WindowsAzure.Storage
    open Microsoft.WindowsAzure.Storage.Auth

    type BatchConfiguration = {
        BatchAccountName : string
        BatchAccountKey  : string
        BatchAccountRegion : string
        BatchServiceUri : string
    }

    type StorageConfiguration = {
        StorageAccountName : string
        StorageAccountKey  : string
    }

    let internal getServiceUri c = 
        let buildUri c = sprintf "https://%s.%s.batch.azure.com" c.BatchAccountName c.BatchAccountRegion
        match c.BatchServiceUri with
        | null -> c |> buildUri
        | s when System.String.IsNullOrWhiteSpace(s) -> c |> buildUri
        | s -> s

    let GetBatchCredentials config =
        BatchSharedKeyCredentials (config |> getServiceUri, config.BatchAccountName, config.BatchAccountKey) 

    let internal toStorageCredentials c = 
        (c.StorageAccountName, c.StorageAccountKey)
        |> StorageCredentials

    let GetCloudStorageAccount config =
        CloudStorageAccount (config |> toStorageCredentials, true)
    
    [<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Batch.Toolkit.Tests")>]
    do ()