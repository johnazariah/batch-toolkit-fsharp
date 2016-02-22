namespace Batch.Toolkit
open System
open System.Collections.Generic
open System.IO
open System.Text
open Microsoft.Azure.Batch
open Microsoft.Azure.Batch.FileStaging

module DSL =
    let ``:cmd`` = SimpleCommand

    let ``:params`` = None
    let ``:cmd_with_params`` command _ parameters = ParametrizedCommand { Command = command; Parameters = parameters }

    let ``:do`` c = {Try = c; OnError = None }
    
    let ``:with`` = None
    let ``:try`` c ``:with`` h = { Try = c; OnError = Some h }

    let ``:main`` = None
    let ``:admin`` = None
    let ``:finally`` = None
    let ``:files`` = None
    let ``:unit`` 
        ``:admin`` runAsAdmin 
        ``:main`` mainBlock
        ``:finally`` finallyBlock
        ``:files`` localFiles = 
        {
            WorkloadUnitCommandSet = { MainCommands = mainBlock; FinallyCommands = finallyBlock } 
            WorkloadUnitLocalFiles = localFiles |> LocalFiles
            WorkloadUnitRunElevated = runAsAdmin
        }
    
    let ``:over`` = None
    let ``:range`` parameterName ``:is`` parameterValues = 
        (parameterName, parameterValues)

    let ``:units`` = None
    let ``:arguments`` = None
    
    let ``:workload`` 
            ``:units`` workloadUnitTemplates 
            ``:files`` commonFiles 
            ``:arguments`` arguments =
        {
            WorkloadUnitTemplates = workloadUnitTemplates
            WorkloadCommonLocalFiles = commonFiles |> LocalFiles
            WorkloadArguments = arguments |> Map.ofSeq |> WorkloadArguments
        }

    let ``:exit`` = SimpleCommand "goto :EXIT"

    let CopyOutputToAzure = ``:cmd`` "echo 'Copying output to Azure'"
    let SayHelloWorld = ``:cmd`` "echo 'Hello, world!'"
    let SayGoodbye = ``:cmd`` "echo 'Goodbye'"
    let ReadItemFromQueue = ``:cmd_with_params`` "echo 'Reading from queue'" ``:params`` ["%queue_name%"]
    
    let workload =
        ``:workload``
            ``:units`` 
                [
                    (``:unit`` 
                        ``:admin`` true 
                        ``:main`` 
                            [
                                ``:do`` SayHelloWorld
                                ``:try`` 
                                    (``:cmd_with_params`` "echo 'Hello %user%'" ``:params`` ["%user%"]) 
                                    ``:with`` ``:exit``
                            ] 
                        ``:finally``
                            [
                                CopyOutputToAzure
                                SayGoodbye
                            ]
                        ``:files`` []) 
                ]
            ``:files`` 
                []
            ``:arguments`` 
                [
                    ``:range`` "%user%" ``:over`` ["John"; "Ivan"; "Pablo"]
                ]