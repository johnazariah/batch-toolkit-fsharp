namespace Batch.Toolkit

open Microsoft.Azure.Batch

module JobOperations =
    let SubmitJobToPoolAsync batchConfiguration storageConfiguration pool job =
        let cloudStorageAccount = GetCloudStorageAccount storageConfiguration
        let stagingContainerName = ContainerName "uploaded-batch-files"
        let fileUploader = UploadFilesToContainer cloudStorageAccount stagingContainerName
        let batchCredentials = GetBatchCredentials batchConfiguration
        async {
            let! batchClient = BatchClient.OpenAsync (batchCredentials) |> Async.AwaitTask
            let poolInformation = EnsurePool batchClient pool
            do! submitJob batchClient poolInformation fileUploader job |> Async.AwaitTask
        } |> Async.StartAsTask

module WorkloadOperations =
    let SubmitWorkloadToPoolAsync batchConfiguration storageConfiguration pool workload = 
        let workloadIdentifier = System.DateTime.UtcNow.ToString("yyyyMMddHHmmssffff") |> int64        
        let workloadName = sprintf "workload-%d" workloadIdentifier 
        
        workload
        |> getJobForWorkload workloadName
        |> JobOperations.SubmitJobToPoolAsync batchConfiguration storageConfiguration pool

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Batch.Toolkit.Tests")>]
do ()