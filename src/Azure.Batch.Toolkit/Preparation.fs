namespace Batch.Toolkit
open System
open System.Collections.Generic
open System.IO
open System.Text
open Microsoft.Azure.Batch
open Microsoft.Azure.Batch.FileStaging

[<AutoOpen>]
module internal Preparation = 
    type PreparedTaskSpecification = {
        PreparedTaskAffinityInformation : AffinityInformation option
        PreparedTaskCommandLine : string
        PreparedTaskConstraints : TaskConstraints option
        PreparedTaskCustomBehaviors : BatchClientBehavior list
        PreparedTaskDisplayName : string option
        PreparedTaskEnvironmentSettings : EnvironmentSetting list
        PreparedTaskFilesToStage : IFileStagingProvider list
        PreparedTaskId : TaskName
        //PreparedTaskMultiInstanceSettings : MultiInstanceSettings option
        PreparedTaskResourceFiles : ResourceFile seq
        PreparedTaskRunElevated : bool
    }
    with
        static member Zero = {
            PreparedTaskAffinityInformation = None
            PreparedTaskCommandLine = String.Empty
            PreparedTaskConstraints = None
            PreparedTaskCustomBehaviors = []
            PreparedTaskDisplayName = None
            PreparedTaskEnvironmentSettings = []
            PreparedTaskFilesToStage = []
            PreparedTaskId = TaskName.Zero
            //PreparedTaskMultiInstanceSettings = None
            PreparedTaskResourceFiles = []
            PreparedTaskRunElevated = false
        }
        
        static member FromTask (task) = {
            PreparedTaskSpecification.Zero with
                PreparedTaskAffinityInformation = task.TaskAffinityInformation
                PreparedTaskConstraints = task.TaskConstraints
                PreparedTaskCustomBehaviors = task.TaskCustomBehaviors
                PreparedTaskDisplayName = task.TaskDisplayName
                PreparedTaskEnvironmentSettings = task.TaskEnvironmentSettings
                PreparedTaskFilesToStage = task.TaskFilesToStage
                PreparedTaskId = task.TaskId
                //PreparedTaskMultiInstanceSettings = None
                PreparedTaskRunElevated = task.TaskRunElevated
        }

    let toPreparedTaskSpecification taskCommandLine taskResourceFiles task = {
        PreparedTaskSpecification.FromTask(task) with
            PreparedTaskCommandLine = taskCommandLine
            PreparedTaskResourceFiles = taskResourceFiles
        }

    type internal PreparedJobSpecification = {
        PreparedJobCommonEnvironmentSettings : EnvironmentSetting list
        PreparedJobConstraints : JobConstraints option
        PreparedJobCustomBehaviors : BatchClientBehavior list
        PreparedJobDisplayName : string option
        PreparedJobId : JobName
        PreparedJobManagerTask : PreparedTaskSpecification option
        PreparedJobPreparationTask : PreparedTaskSpecification option
        PreparedJobReleaseTask : PreparedTaskSpecification option
        PreparedJobMetadata : MetadataItem list
        PreparedJobPoolInformation : PoolInformation option
        PreparedJobPriority : JobPriority option
    }
    with
        static member Zero = {
            PreparedJobCommonEnvironmentSettings = []
            PreparedJobConstraints = None
            PreparedJobCustomBehaviors = []
            PreparedJobDisplayName = None
            PreparedJobId = JobName.Zero
            PreparedJobManagerTask = None
            PreparedJobPreparationTask = None
            PreparedJobReleaseTask = None
            PreparedJobMetadata = []
            PreparedJobPoolInformation = None
            PreparedJobPriority = None
        }

    let toPreparedJobSpecification jobManagerTaskPreparer jobReleaseTaskPreparer jobPreparationTaskPreparer job = {
        PreparedJobSpecification.Zero with
            PreparedJobId                        = job.JobId
            PreparedJobConstraints               = job.JobConstraints              
            PreparedJobCommonEnvironmentSettings = job.JobCommonEnvironmentSettings
            PreparedJobCustomBehaviors           = job.JobCustomBehaviors          
            PreparedJobDisplayName               = job.JobDisplayName              
            PreparedJobMetadata                  = job.JobMetadata                 
            PreparedJobPriority                  = job.JobPriority                 
            PreparedJobManagerTask               = job.JobManagerTask     |> jobManagerTaskPreparer
            PreparedJobReleaseTask               = job.JobReleaseTask     |> jobReleaseTaskPreparer
            PreparedJobPreparationTask           = job.JobPreparationTask |> jobPreparationTaskPreparer
        }
    
    let toCloudTask task = 
        let (TaskName taskName) = task.PreparedTaskId
        let result = new CloudTask(taskName, task.PreparedTaskCommandLine)

        result.ResourceFiles <- task.PreparedTaskResourceFiles |> List<ResourceFile>
        result.RunElevated <- task.PreparedTaskRunElevated |> Nullable
        result.AffinityInformation <- task.PreparedTaskAffinityInformation |> getOrNull
        result.Constraints <- task.PreparedTaskConstraints |> getOrNull
        result.DisplayName <- task.PreparedTaskDisplayName |> getOrNull
        result.EnvironmentSettings <- task.PreparedTaskEnvironmentSettings |> List<EnvironmentSetting>
        result.CustomBehaviors <- task.PreparedTaskCustomBehaviors |> List<BatchClientBehavior>        
        result

    let toJobPreparationTask task = 
        let (TaskName taskName) = task.PreparedTaskId
        let result = new JobPreparationTask ()

        result.Id <- taskName
        result.CommandLine <- task.PreparedTaskCommandLine
        result.ResourceFiles <- task.PreparedTaskResourceFiles |> List<ResourceFile>
        result.RunElevated <- task.PreparedTaskRunElevated |> Nullable
        result.Constraints <- task.PreparedTaskConstraints |> getOrNull
        result.EnvironmentSettings <- task.PreparedTaskEnvironmentSettings |> List<EnvironmentSetting>
        result

    let toJobManagerTask task = 
        let (TaskName taskName) = task.PreparedTaskId
        let result = new JobManagerTask ()

        result.Id <- taskName
        result.CommandLine <- task.PreparedTaskCommandLine
        result.ResourceFiles <- task.PreparedTaskResourceFiles |> List<ResourceFile>
        result.RunElevated <- task.PreparedTaskRunElevated |> Nullable
        result.Constraints <- task.PreparedTaskConstraints |> getOrNull
        result.DisplayName <- task.PreparedTaskDisplayName |> getOrNull
        result.EnvironmentSettings <- task.PreparedTaskEnvironmentSettings |> List<EnvironmentSetting>
        result

    let toJobReleaseTask task = 
        let (TaskName taskName) = task.PreparedTaskId
        let result = new JobReleaseTask ()

        result.Id <- taskName
        result.CommandLine <- task.PreparedTaskCommandLine
        result.ResourceFiles <- task.PreparedTaskResourceFiles |> List<ResourceFile>
        result.RunElevated <- task.PreparedTaskRunElevated |> Nullable
        result.EnvironmentSettings <- task.PreparedTaskEnvironmentSettings |> List<EnvironmentSetting>
        result

    let toPoolStartTask task =
        let result = new StartTask ()

        result.CommandLine <- task.PreparedTaskCommandLine
        result.ResourceFiles <- task.PreparedTaskResourceFiles |> List<ResourceFile>
        result.RunElevated <- task.PreparedTaskRunElevated |> Nullable
        result.EnvironmentSettings <- task.PreparedTaskEnvironmentSettings |> List<EnvironmentSetting>
        result.MaxTaskRetryCount <- Nullable(3)
        result

    let getJobForWorkload workload = 
        let getWorkloadUnitTasks =
            let getTaskForWorkloadUnit args workloadUnit = 
                let taskName = sprintf "workload-unit-%s" (Guid.NewGuid().ToString("D")) |> TaskName
                { TaskSpecification.Zero with
                    TaskId = taskName
                    TaskCommandSet = workloadUnit.WorkloadUnitCommandSet
                    TaskLocalFiles = workloadUnit.WorkloadUnitLocalFiles
                    TaskRunElevated = workloadUnit.WorkloadUnitRunElevated
                    TaskArguments = TaskArguments args
                }

            let getCrossJoinOfArguments (m : Map<'a, 'b list>) : Map<'a, 'b> seq = 
                let addKeyAndValue (key, value) (dict : Map<'a, 'b>) = dict.Add (key, value)
                let joinValue dicts key value = dicts |> Seq.map (addKeyAndValue (key, value))
                let joinValuesForKey dicts key values = values |> Seq.collect (joinValue dicts key)
                let emptySeqOfDicts = ([ Map.empty ] |> Seq.ofList)
                m |> Map.fold joinValuesForKey emptySeqOfDicts

            let getCrossJoinOfWorkloadArguments (WorkloadArguments wa) = getCrossJoinOfArguments wa            
            let applyArgument workload args = workload.WorkloadUnitTemplates |> List.map (getTaskForWorkloadUnit args)

            workload.WorkloadArguments 
            |> getCrossJoinOfWorkloadArguments
            |> Seq.collect (applyArgument workload)
            |> List.ofSeq

        let jobName = sprintf "workload-%s" (Guid.NewGuid().ToString("D")) |> JobName   
        { JobSpecification.Zero with
            JobId = jobName
            JobTasks = getWorkloadUnitTasks
            JobSharedLocalFiles = workload.WorkloadCommonLocalFiles
        }

    let bindParametersToCommand args pc =
        let interpolateParameter (s : string) (paramName, paramValue) = 
            let placeHolder = sprintf "%%%s%%" paramName
            let value = sprintf "%A" paramValue
            s.Replace(placeHolder, value)

        let lookupParameter (args : Map<string, _>) p =
            (p, args.[p])

        let replaceParameterInString p s = 
            Success p <!> lookupParameter args <!> interpolateParameter s

        pc.Parameters |> foldrM replaceParameterInString pc.Command

    let bindArgument (args : Map<string, _>) = function
    | ParametrizedCommand pc -> pc |> bindParametersToCommand args 
    | SimpleCommand sc -> Success sc 

    let FinallyLabel = ":_FINALLY_"
    let ExitLabel = ":_EXIT_" 

    let bindCommandSet args commandSet = 
        let appendTo (buf : StringBuilder) item = buf.AppendLine item
        
        let generateCall args c = bindArgument args c <!> sprintf "CALL %s"

        let generateTryCatch args (idx, tc) stringBuilder =
            let successLabel = sprintf "_SUCCESS_%d" idx
            let errorLabel = sprintf "_ERROR_%d" idx

            succeed {
                let! stringBuilder = tc.Try |> generateCall args                                            <!> appendTo stringBuilder
                do sprintf "IF NOT %%ERRORLEVEL%% == 0 GOTO %s" errorLabel                                   |> appendTo stringBuilder |> ignore
                do sprintf "GOTO %s" successLabel                                                            |> appendTo stringBuilder |> ignore
                do sprintf ":%s" errorLabel                                                                  |> appendTo stringBuilder |> ignore
                let! stringBuilder = tc.OnError |> Option.map (generateCall args) |> getOrElse (Success "") <!> appendTo stringBuilder
                do sprintf ":%s" successLabel                                                                |> appendTo stringBuilder |> ignore
                return stringBuilder
            }

        let generateFinally args f stringBuilder = f |> generateCall args <!> appendTo stringBuilder
        
        succeed {
            let! stringBuilder = 
                commandSet.MainCommands 
                |> List.mapi (fun i c -> (i, c)) 
                |> foldrM (generateTryCatch args) (new StringBuilder())
            
            do FinallyLabel |> sprintf "%s" |> appendTo stringBuilder |> ignore
            
            let! stringBuilder = 
                commandSet.FinallyCommands 
                |> foldrM (generateFinally args) (stringBuilder)
            
            do ExitLabel    |> sprintf "%s" |> appendTo stringBuilder |> ignore
            
            return stringBuilder.ToString ()
        }

    let prepareTaskForSubmission fileUploader task =
        let ensureCommandLine task =
            let (TaskName taskName) = task.TaskId
            let (TaskArguments args) = task.TaskArguments

            let writeTextToFile fileName (text : string) = 
                use writer = new StreamWriter(fileName, false, System.Text.Encoding.Unicode)
                text |> writer.Write 
                writer.Flush ()
                writer.Close ()

            succeed {
                let commandScriptFile = sprintf "%s.cmd" taskName |> FileInfo
                let commandLine = sprintf "cmd /c %s" commandScriptFile.Name
                let! commandScript = bindCommandSet args task.TaskCommandSet 
                do writeTextToFile commandScriptFile.FullName commandScript
                return (commandLine, Some commandScriptFile)
            }

        let ensureUploadedFiles fileUploader (files, script) =
            script |> Option.map (fun s -> LocalFiles [s] + files) |> getOrElse files |> fileUploader 

        succeed {
            let! (taskCommandLine, commandScriptFile) = ensureCommandLine task
            let uploadedTaskFiles = ensureUploadedFiles fileUploader (task.TaskLocalFiles, commandScriptFile)
            return task |> toPreparedTaskSpecification taskCommandLine uploadedTaskFiles
        } |> getOrThrow
            
    let internal prepareJobForSubmission fileUploader job =
        let ensureJobPreparationTaskWithFiles files = function
        | Some pt -> 
            { pt with TaskLocalFiles = pt.TaskLocalFiles + files }
        | None -> 
            { TaskSpecification.Zero with TaskId = "default-job-prep" |> TaskName; TaskLocalFiles = files }
        
        let prepareTask' = prepareTaskForSubmission fileUploader
        let jobManagerTaskPreparer = prepareTask' |> Option.map 
        let jobReleaseTaskPreparer = prepareTask' |> Option.map 
        let jobPreparationTaskPreparer j = j |> ensureJobPreparationTaskWithFiles job.JobSharedLocalFiles  |> prepareTask' |> Some

        job |> toPreparedJobSpecification jobManagerTaskPreparer jobReleaseTaskPreparer jobPreparationTaskPreparer

    let submitTask (batchClient : BatchClient) (jobId : string) (task : CloudTask) = 
        batchClient.JobOperations.AddTaskAsync (jobId, task, null, null)

    let toCloudJob (batchClient : BatchClient) fileUploader poolInformation job =
        let preparedJob = job |> prepareJobForSubmission fileUploader
        let cloudJob = batchClient.JobOperations.CreateJob ()

        let (JobName id) = preparedJob.PreparedJobId
        cloudJob.Id                        <- id
        cloudJob.Constraints               <- preparedJob.PreparedJobConstraints |> getOrNull
        cloudJob.CommonEnvironmentSettings <- preparedJob.PreparedJobCommonEnvironmentSettings
        cloudJob.CustomBehaviors           <- preparedJob.PreparedJobCustomBehaviors |> List<BatchClientBehavior>
        cloudJob.DisplayName               <- preparedJob.PreparedJobDisplayName |> getOrNull
        cloudJob.Metadata                  <- preparedJob.PreparedJobMetadata |> List<MetadataItem>
        cloudJob.Priority                  <- preparedJob.PreparedJobPriority |> Option.map (fun (JobPriority j) -> j) |> getOrElse (Nullable(0))
        cloudJob.JobManagerTask            <- preparedJob.PreparedJobManagerTask     |> Option.map (toJobManagerTask)     |> getOrNull
        cloudJob.JobReleaseTask            <- preparedJob.PreparedJobReleaseTask     |> Option.map (toJobReleaseTask)     |> getOrNull
        cloudJob.JobPreparationTask        <- preparedJob.PreparedJobPreparationTask |> Option.map (toJobPreparationTask) |> getOrNull
        cloudJob.PoolInformation           <- poolInformation
        cloudJob

    let submitJob batchClient poolInformation fileUploader job = 
        let insertCopyCommand hasSharedFiles task = 
            if hasSharedFiles then
                let copyFilesCommandSet = CommandSet.FromCommand (CommonCommands.Windows.CopyJobPrepTaskFilesToJobTask) 
                { task with TaskCommandSet =  task.TaskCommandSet + copyFilesCommandSet }
            else
                task

        let (JobName jobId) = job.JobId

        let cloudJob = job |> toCloudJob batchClient fileUploader poolInformation

        let prepareAndSubmitTask = 
            insertCopyCommand (job.JobSharedLocalFiles = LocalFiles.Zero) 
            >> prepareTaskForSubmission fileUploader 
            >> toCloudTask 
            >> submitTask batchClient jobId

        async {
            do! cloudJob.CommitAsync () |> Async.AwaitTask            
            do! job.JobTasks
                |> List.map prepareAndSubmitTask
                |> Threading.Tasks.Task.WhenAll
                |> Async.AwaitTask
        } |> Async.StartAsTask