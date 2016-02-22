namespace Batch.Toolkit

open System
open System.Collections.Generic

open Microsoft.Azure.Batch
open Microsoft.Azure.Batch.Common

type PoolName = PoolName of string

type PoolSpecification = {
    Scale : Scale
    CertificateReferences : CertificateReference list
    DisplayName : string option
    InterComputeNodeCommunicationEnabled : bool option
    MaxTasksPerComputeNode : int option
    Metadata : MetadataItem list
    OSFamily : OSFamily option
    ResizeTimeout : TimeSpan option
    StartTask : StartTask option
    TargetOSVersion : TargetOSVersion option
    TaskSchedulingPolicy : ComputeNodeFillType option
    //VirtualMachineSize : VirtualMachineSize option
    VirtualMachineSize : string option
}
with 
    member this.CloudPoolSpecification =
        let poolSpecification = new Microsoft.Azure.Batch.PoolSpecification ()
        poolSpecification.AutoScaleEnabled <- match this.Scale with | AutoScaleCluster _ -> Nullable(true) | _ -> Nullable(false) 
        poolSpecification.AutoScaleFormula <- match this.Scale with | AutoScaleCluster formula -> formula  | _ -> null
        poolSpecification.TargetDedicated  <- match this.Scale with | FixedClusterSize dedicated -> Nullable(dedicated) | _ -> Nullable()
        poolSpecification.CertificateReferences <- this.CertificateReferences |> List<CertificateReference>
        poolSpecification.DisplayName <- this.DisplayName |> getOrNull
        poolSpecification.InterComputeNodeCommunicationEnabled <- this.InterComputeNodeCommunicationEnabled |> getOrNullable
        poolSpecification.MaxTasksPerComputeNode <- this.MaxTasksPerComputeNode |> getOrNullable
        poolSpecification.Metadata <- this.Metadata |> List<MetadataItem>
        poolSpecification.OSFamily <- this.OSFamily |> Option.map (int >> string) |> getOrNull
        poolSpecification.ResizeTimeout <- this.ResizeTimeout |> getOrNullable
        poolSpecification.StartTask <- this.StartTask |> getOrNull
        poolSpecification.TargetOSVersion <- this.TargetOSVersion |> Option.map (fun (TargetOSVersion x) -> x) |> getOrNull
        poolSpecification.TaskSchedulingPolicy <- this.TaskSchedulingPolicy |> Option.map (TaskSchedulingPolicy) |> getOrNull
        poolSpecification.VirtualMachineSize <- this.VirtualMachineSize |> getOrElse "small"
        poolSpecification

and Scale = 
| FixedClusterSize of int
| AutoScaleCluster of string
and OSFamily = 
// https://msdn.microsoft.com/en-us/library/azure/dn820174.aspx
| Windows_Server_2008_R2_SP1 = 2
| Windows_Server_2012 = 3
| Windows_Server_2012_R2 = 4
//and VirtualMachineSize = 
//// https://azure.microsoft.com/en-us/documentation/articles/cloud-services-sizes-specs/
and TargetOSVersion = 
//https://azure.microsoft.com/en-us/documentation/articles/cloud-services-guestos-update-matrix/#releases
| TargetOSVersion of string
and TaskSchedulingPolicy = 
| Pack
| Spread

type Pool = 
    | NamedPool of NamedPool
    | AutoPool of AutoPool
and NamedPool = {
    NamedPoolName : PoolName
    NamedPoolSpecification : PoolSpecification
}            
and AutoPool = {
    AutoPoolPrefix : PoolName
    AutoPoolKeepAlive : Nullable<bool>
    AutoPoolLifetime : PoolLifetimeOption
    AutoPoolSpecification : PoolSpecification
}
