namespace Batch.Toolkit.FSharp.Tests

open Microsoft.Azure.Batch
open Batch.Toolkit.FSharp
open NUnit.Framework

open Newtonsoft.Json

open FSharp.Data
open FsUnit

module JobPreparationTests =
    [<Test>]
    let ``composing shared resources on job with no job prep task should work`` () =
        let resourceA = ("a", "a") |> ResourceFile
        let resourceB = ("b", "b") |> ResourceFile

        let resources = [ resourceA; resourceB ]

        let jobWithSharedResources = {defaultJobSpecification with JobSharedResources = resources}        
        jobWithSharedResources.JobPreparationTask |> Option.isSome |> should equal false

        let jobPrepTask = 
            jobWithSharedResources 
            |> composeJobPreparationTaskWithSharedResources
                
        jobPrepTask |> Option.map (fun _ -> true) |> should equal (Some true)

        jobPrepTask |> Option.map (fun j -> j.TaskCommands |> countOf windowsCopyJobPrepFilesCommand) |> should equal (Some 1)

        jobPrepTask |> Option.map (fun j -> j.TaskResources |> countOf resourceA) |> should equal (Some 1)
        jobPrepTask |> Option.map (fun j -> j.TaskResources |> countOf resourceB) |> should equal (Some 1)

    [<Test>]
    let ``composing shared resources on job with a job prep task should work`` () =
        let resourceA = ("a", "a") |> ResourceFile
        let resourceB = ("b", "b") |> ResourceFile

        let existingJobPrepTask = {defaultTask with TaskResources = [resourceA]; TaskCommands = [ TaskExecutionCommand "existing_command"] }

        let jobWithSharedResources = {defaultJobSpecification with JobSharedResources = [resourceB]; JobPreparationTask = Some existingJobPrepTask}        
        jobWithSharedResources.JobPreparationTask |> Option.isSome |> should equal true

        let jobPrepTask = jobWithSharedResources |> composeJobPreparationTaskWithSharedResources        
        jobPrepTask |> Option.isSome |> should equal true

        jobPrepTask |> Option.map (fun j -> j.TaskCommands |> countOf windowsCopyJobPrepFilesCommand) |> should equal (Some 1)
        jobPrepTask |> Option.map (fun j -> j.TaskCommands |> countOf (TaskExecutionCommand "existing_command")) |> should equal (Some 1)

        jobPrepTask |> Option.map (fun j -> j.TaskResources |> countOf resourceA) |> should equal (Some 1)
        jobPrepTask |> Option.map (fun j -> j.TaskResources |> countOf resourceB) |> should equal (Some 1)


    [<Test>]
    let ``composing shared resources should not duplicate commands or resources`` () =
        let resourceA = ("a", "a") |> ResourceFile
        let resourceB = ("b", "b") |> ResourceFile

        let existingJobPrepTask = {defaultTask with TaskResources = [resourceA; resourceB]; TaskCommands = [ TaskExecutionCommand "existing_command"] }

        let jobWithSharedResources = {defaultJobSpecification with JobSharedResources = [resourceA]; JobPreparationTask = Some existingJobPrepTask}        
        jobWithSharedResources.JobPreparationTask |> Option.isSome |> should equal true

        let jobPrepTask = jobWithSharedResources |> composeJobPreparationTaskWithSharedResources        
        jobPrepTask |> Option.isSome |> should equal true

        jobPrepTask |> Option.map (fun j -> j.TaskCommands |> countOf windowsCopyJobPrepFilesCommand) |> should equal (Some 1)
        jobPrepTask |> Option.map (fun j -> j.TaskCommands |> countOf (TaskExecutionCommand "existing_command")) |> should equal (Some 1)

        jobPrepTask |> Option.map (fun j -> j.TaskResources |> countOf resourceA) |> should equal (Some 1)
        jobPrepTask |> Option.map (fun j -> j.TaskResources |> countOf resourceB) |> should equal (Some 1)
