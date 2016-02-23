namespace Batch.Toolkit

open System
open System.Collections.Generic
open System.IO
open System.Text
open Microsoft.Azure.Batch
open Microsoft.Azure.Batch.FileStaging

type Command = 
| SimpleCommand of string
| ParametrizedCommand of ParametrizedCommand
and ParametrizedCommand = { Command : string; Parameters : string list} 

type CommandWithErrorHandler = {
    Try : Command;
    OnError : Command option
}
with
    static member Zero = {
        Try = SimpleCommand String.Empty
        OnError = None
    }

type CommandSet = { 
    MainCommands : CommandWithErrorHandler list
    FinallyCommands : Command list
}
with
    static member Zero = {
        MainCommands = []
        FinallyCommands = []
    }
    static member (+) (a : CommandSet, b : CommandSet) = { 
        MainCommands = a.MainCommands @ b.MainCommands
        FinallyCommands = a.FinallyCommands @ b.FinallyCommands
    }
    static member FromCommand (cmd : Command) = {
        MainCommands = [ {Try = cmd; OnError = None}]; 
        FinallyCommands = []
    }

type LocalFiles = 
| LocalFiles of FileInfo list
with
    static member Zero = LocalFiles []
    static member (+) (a : LocalFiles, b : LocalFiles) =
        let (LocalFiles afs) = a
        let (LocalFiles bfs) = b
        LocalFiles (afs @ bfs)

type UploadedFiles = 
| UploadedFiles of ResourceFile list
with
    static member Zero = UploadedFiles []
    static member (+) (a : UploadedFiles, b : UploadedFiles) =
        let (UploadedFiles afs) = a
        let (UploadedFiles bfs) = b
        UploadedFiles (afs @ bfs)

type WorkloadUnitTemplate = {
    WorkloadUnitCommandSet : CommandSet
    WorkloadUnitLocalFiles : LocalFiles
    WorkloadUnitRunElevated : bool
}
with
    static member Zero = {
        WorkloadUnitCommandSet = CommandSet.Zero
        WorkloadUnitLocalFiles = LocalFiles.Zero
        WorkloadUnitRunElevated = false
    }
    static member (+) (a : WorkloadUnitTemplate, b : WorkloadUnitTemplate) = {
        WorkloadUnitCommandSet = a.WorkloadUnitCommandSet + b.WorkloadUnitCommandSet
        WorkloadUnitLocalFiles = a.WorkloadUnitLocalFiles + b.WorkloadUnitLocalFiles
        WorkloadUnitRunElevated = a.WorkloadUnitRunElevated || b.WorkloadUnitRunElevated
    }

type WorkloadArguments = 
| WorkloadArguments of Map<string, string list>
with
    static member Zero = Map.empty |> WorkloadArguments
    static member (+) (a : WorkloadArguments, b : WorkloadArguments) = 
        let (WorkloadArguments a_args) = a
        let (WorkloadArguments b_args) = b
        let merge (d : Map<_,_>) (KeyValue (k, vs)) =
            match (d.TryFind k) with
            | Some evs -> evs @ vs |> Map.add k <| d 
            | None     -> vs |> Map.add k <| d
        a_args |> Seq.fold merge b_args |> WorkloadArguments

type WorkloadSpecification = {
    WorkloadUnitTemplates : WorkloadUnitTemplate list
    WorkloadCommonLocalFiles : LocalFiles
    WorkloadArguments : WorkloadArguments
}
with
    static member Zero = {
        WorkloadUnitTemplates = []
        WorkloadCommonLocalFiles = LocalFiles.Zero
        WorkloadArguments = [] |> Map.ofSeq |> WorkloadArguments
    }
    static member (+) (a : WorkloadSpecification, b : WorkloadSpecification) = {
        WorkloadUnitTemplates = a.WorkloadUnitTemplates @ b.WorkloadUnitTemplates
        WorkloadCommonLocalFiles = a.WorkloadCommonLocalFiles + b.WorkloadCommonLocalFiles
        WorkloadArguments = a.WorkloadArguments + b.WorkloadArguments
    }

type TaskName = 
| TaskName of string
with 
    static member Zero = String.Empty |> TaskName

type TaskArguments = 
| TaskArguments of Map<string, string>
with
    static member Zero = Map.empty |> TaskArguments
    static member (+) (a : TaskArguments, b : TaskArguments) = 
        let (TaskArguments a_args) = a
        let (TaskArguments b_args) = b
        let addOrReplace (d : Map<_,_>) (KeyValue (k, v)) = 
            v |> Map.add k <| d             
        a_args |> Seq.fold addOrReplace b_args |> TaskArguments

type TaskSpecification = {
    TaskAffinityInformation : AffinityInformation option
    TaskConstraints : TaskConstraints option
    TaskCustomBehaviors : BatchClientBehavior list
    TaskDisplayName : string option
    TaskEnvironmentSettings : EnvironmentSetting list
    TaskFilesToStage : IFileStagingProvider list
    TaskId : TaskName
    //TaskMultiInstanceSettings : MultiInstanceSettings option
    TaskResourceFiles : ResourceFile seq
    TaskRunElevated : bool

    TaskCommandSet : CommandSet
    TaskArguments : TaskArguments
    TaskLocalFiles : LocalFiles
}
with
    static member Zero =  {
        TaskAffinityInformation = None
        TaskConstraints = None
        TaskCustomBehaviors = []
        TaskDisplayName = None
        TaskEnvironmentSettings = []
        TaskFilesToStage = []
        TaskId = TaskName.Zero
        //TaskMultiInstanceSettings = None
        TaskResourceFiles = []
        TaskRunElevated = false

        TaskCommandSet = CommandSet.Zero
        TaskArguments = TaskArguments.Zero
        TaskLocalFiles = LocalFiles.Zero
    }

type JobName = 
| JobName of string
with
    static member Zero = String.Empty |> JobName 

type JobPriority = 
| JobPriority of Nullable<int>
with
    static member Zero = Nullable<int>() |> JobPriority

type JobSpecification = {
    JobCommonEnvironmentSettings : EnvironmentSetting list
    JobConstraints : JobConstraints option
    JobCustomBehaviors : BatchClientBehavior list
    JobDisplayName : string option
    JobId : JobName
    JobManagerTask : TaskSpecification option
    JobPreparationTask : TaskSpecification option
    JobReleaseTask : TaskSpecification option
    JobMetadata : MetadataItem list
    JobPoolInformation : PoolInformation option
    JobPriority : JobPriority option

    JobTasks : TaskSpecification list
    JobSharedLocalFiles : LocalFiles
}
with
    static member Zero = {
        JobCommonEnvironmentSettings = []
        JobConstraints = None
        JobCustomBehaviors = []
        JobDisplayName = None
        JobId = JobName.Zero
        JobManagerTask = None
        JobPreparationTask = None
        JobReleaseTask = None
        JobMetadata = []
        JobPoolInformation = None
        JobPriority = None

        JobTasks = []
        JobSharedLocalFiles = LocalFiles.Zero
    }

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Batch.Toolkit.Tests")>]
do ()