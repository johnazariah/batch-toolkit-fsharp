namespace Batch.Toolkit

open System
open System.Collections.Generic

open Microsoft.Azure.Batch

[<AutoOpen>]
module PoolOperations =
    let GetDefaultPoolSpecification = { 
        Scale = FixedClusterSize 2
        CertificateReferences = []
        DisplayName = None
        InterComputeNodeCommunicationEnabled = None
        MaxTasksPerComputeNode = None
        Metadata = []
        OSFamily = OSFamily.Windows_Server_2012_R2 |> Some
        ResizeTimeout = None
        StartTask = None
        TargetOSVersion = None
        TaskSchedulingPolicy = None
        //VirtualMachineSize = VirtualMachineSize.Small |> Some
        VirtualMachineSize = Some "small"
    }

    let internal toPoolInformation = function
    | NamedPool this -> 
        let result = new PoolInformation ()
        let (PoolName id) = this.NamedPoolName
        result.PoolId <- id
        result
    | AutoPool this ->
        let (PoolName id) = this.AutoPoolPrefix
        let autoPoolSpecification = new AutoPoolSpecification ()
        autoPoolSpecification.AutoPoolIdPrefix <- id
        autoPoolSpecification.KeepAlive <- this.AutoPoolKeepAlive
        autoPoolSpecification.PoolLifetimeOption <- this.AutoPoolLifetime
        autoPoolSpecification.PoolSpecification <- this.AutoPoolSpecification.CloudPoolSpecification
        
        let result = new PoolInformation ()
        result.AutoPoolSpecification <- autoPoolSpecification
        result

    let internal toCloudPool (batchClient : BatchClient) namedPool  =
        let (PoolName name) = namedPool.NamedPoolName
        let poolSpecification = namedPool.NamedPoolSpecification.CloudPoolSpecification
        let result = 
            batchClient.PoolOperations.CreatePool(
                name, 
                poolSpecification.OSFamily, 
                poolSpecification.VirtualMachineSize, 
                poolSpecification.MaxTasksPerComputeNode)

        result.AutoScaleEnabled <- poolSpecification.AutoScaleEnabled
        result.AutoScaleFormula <- poolSpecification.AutoScaleFormula
        result.CertificateReferences <- poolSpecification.CertificateReferences
        result.DisplayName <- poolSpecification.DisplayName
        result.InterComputeNodeCommunicationEnabled <- poolSpecification.InterComputeNodeCommunicationEnabled
        result.MaxTasksPerComputeNode <- poolSpecification.MaxTasksPerComputeNode
        result.Metadata <- poolSpecification.Metadata
        result.OSFamily <- poolSpecification.OSFamily
        result.ResizeTimeout <- poolSpecification.ResizeTimeout
        result.StartTask <- poolSpecification.StartTask
        result.TargetDedicated <- poolSpecification.TargetDedicated
        result.TargetOSVersion <- poolSpecification.TargetOSVersion
        result.TaskSchedulingPolicy <- poolSpecification.TaskSchedulingPolicy
        result.VirtualMachineSize <- poolSpecification.VirtualMachineSize
        result

    let internal tryGetPoolWithName (batchClient : BatchClient) poolName = 
        try 
            batchClient.PoolOperations.GetPoolAsync(poolName) 
            |> (fun task -> task.Result)
            |> Some
        with
        | _ -> None

    let internal createNamedPoolAsync (batchClient : BatchClient) pool = 
        let cloudPool = pool |> toCloudPool batchClient

        async { do! cloudPool.CommitAsync() |> Async.AwaitTask } 
        |> Async.StartAsTask

    let EnsurePool (batchClient : BatchClient) = function
    | AutoPool pool -> pool |> AutoPool |> toPoolInformation
    | NamedPool pool ->
        let (PoolName poolName) = pool.NamedPoolName
        
        match tryGetPoolWithName batchClient poolName with 
        | Some _ -> ()
        | None -> createNamedPoolAsync batchClient pool |> (fun task -> task.Result)

        pool |> NamedPool |> toPoolInformation
    
    [<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Batch.Toolkit.Tests")>]
    do ()