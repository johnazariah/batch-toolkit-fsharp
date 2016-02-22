namespace Batch.Toolkit

[<AutoOpen>]
module Storage =
    open System
    open System.Collections.Generic
    open System.IO
    open System.Linq
    open System.Threading.Tasks
    open Microsoft.Azure.Batch
    open Microsoft.WindowsAzure.Storage
    open Microsoft.WindowsAzure.Storage.Blob

    type ContainerName = | ContainerName of string

    let internal toResourceFile stagingContainer fileName = 
        let constructSasUri validForHours (container : CloudBlobContainer) =
            let expiryTime = validForHours |> float |>  DateTime.UtcNow.AddHours |> DateTimeOffset |>  Nullable
            let sasPolicy = new SharedAccessBlobPolicy ()
            sasPolicy.Permissions <- SharedAccessBlobPermissions.Read
            sasPolicy.SharedAccessExpiryTime <- expiryTime            
            let sasString = container.GetSharedAccessSignature sasPolicy
            Uri(container.Uri, sasString)

        let constructBlobSource blobName (containerSasUri : Uri)  =
            let parts = containerSasUri.AbsoluteUri.Split ('?')
            if (parts.Length = 1) then
                sprintf "%s/%s" parts.[0] blobName
            else
                sprintf "%s/%s?%s" parts.[0] blobName parts.[1]

        let blobSource = 
            stagingContainer 
            |> constructSasUri 2 
            |> constructBlobSource fileName
        (blobSource, fileName) |> ResourceFile
    
    let internal getClientAndContainer (cloudStorageAccount : CloudStorageAccount) containerName =
        let toCanonicalName (ContainerName name) = 
            name.ToLowerInvariant ()
        let blobClient = cloudStorageAccount.CreateCloudBlobClient ()
        let stagingContainer = containerName |> toCanonicalName |> blobClient.GetContainerReference
        (blobClient, stagingContainer)

    let ListFilesInContainer cloudStorageAccount containerName = 
        let (_, container) = getClientAndContainer cloudStorageAccount containerName 
        container.ListBlobs() |> Seq.map (fun b -> b.Uri.AbsoluteUri |> Path.GetFileName)

    let DeleteContainerAsync cloudStorageAccount containerName =
        let (_, container) = getClientAndContainer cloudStorageAccount containerName
        container.DeleteIfExistsAsync()

    let DeleteContainer cloudStorageAccount containerName =
        let (_, container) = getClientAndContainer cloudStorageAccount containerName
        container.DeleteIfExists()

    let UploadFilesToContainer (cloudStorageAccount : CloudStorageAccount) containerName (LocalFiles files) =        
        let (_, stagingContainer) = getClientAndContainer cloudStorageAccount containerName

        async {
            let! _ = 
                stagingContainer.CreateIfNotExistsAsync (BlobContainerPublicAccessType.Off, null, null) 
                |> Async.AwaitTask 
            
            do! files
                |> Seq.filter (fun fi -> fi.Exists)
                |> Seq.map (fun fi -> (fi, fi.Name |> stagingContainer.GetBlockBlobReference))
                |> Seq.map (fun (fi, br) -> br.UploadFromFileAsync(fi.FullName, FileMode.Open))
                |> Threading.Tasks.Task.WhenAll
                |> Async.AwaitTask
        }
        |> Async.RunSynchronously
        |> ignore

        files 
        |> Seq.map (fun fi -> fi.Name |> toResourceFile stagingContainer)

    [<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Batch.Toolkit.Tests")>]
    do ()