namespace Batch.Toolkit.FSharp.Tests

open Batch.Toolkit.FSharp
open NUnit.Framework

open Newtonsoft.Json

open FSharp.Data
open FsUnit

module WindowsTaskCommandLineTests =
    [<Test>]
    let ``default command line should be correct`` () =
        let taskCommands = []
        taskCommands |> constructWindowsCommandLine |> should equal "cmd /c echo 'Task >' "

    [<Test>]
    let ``empty list of commands should result in valid script`` () =
        let task = {defaultTask with TaskCommands = []}

        task |> constructWindowsCommandScript |> should equal ""
    
    [<Test>]
    let ``list of only execution commands should not have :ERROR label`` () =
        let task = {defaultTask with TaskCommands = [TaskExecutionCommand "a"]}
        task |> constructWindowsCommandScript |> (fun s -> s.Contains(":ERROR")) |> should equal false

    [<Test>]
    let ``list of a single execution command should generate the right instructions`` () =
        let instructions = [TaskExecutionCommand "a.exe"] |> buildCommandInstructions |> List.ofSeq
        let expected = [
                "@echo off"
                "CALL a.exe"
                "IF NOT %ERRORLEVEL% == 0 GOTO EXIT"
                ":EXIT"
            ]
        instructions |> should equal expected

    [<Test>]
    let ``list of a single cleanup command should generate the right instructions`` () =
        let instructions = [TaskCleanupCommand "c.exe"] |> buildCommandInstructions |> List.ofSeq
        let expected = [
                "@echo off"
                "CALL c.exe"
                "IF NOT %ERRORLEVEL% == 0 GOTO EXIT"
                ":EXIT"
            ]
        instructions |> should equal expected

    [<Test>]
    let ``list of a multiple execution commands should generate the right instructions`` () =
        let instructions = [TaskExecutionCommand "a.exe"; TaskExecutionCommand "b.exe"] |> buildCommandInstructions |> List.ofSeq
        let expected = [
                "@echo off"
                "CALL a.exe"
                "IF NOT %ERRORLEVEL% == 0 GOTO EXIT"
                "CALL b.exe"
                "IF NOT %ERRORLEVEL% == 0 GOTO EXIT"
                ":EXIT"
            ]
        instructions |> should equal expected

    [<Test>]
    let ``list of a multiple cleanup commands should generate the right instructions`` () =
        let instructions = [TaskCleanupCommand "c.exe"; TaskCleanupCommand "d.exe"] |> buildCommandInstructions |> List.ofSeq
        let expected = [
                "@echo off"
                "CALL c.exe"
                "IF NOT %ERRORLEVEL% == 0 GOTO EXIT"
                "CALL d.exe"
                "IF NOT %ERRORLEVEL% == 0 GOTO EXIT"
                ":EXIT"
            ]
        instructions |> should equal expected

    [<Test>]
    let ``list of a one execution and one cleanup command should generate the right instructions`` () =
        let instructions = [TaskExecutionCommand "a.exe"; TaskCleanupCommand "c.exe"] |> buildCommandInstructions |> List.ofSeq
        let expected = [
                "@echo off"
                "CALL a.exe"
                "IF NOT %ERRORLEVEL% == 0 GOTO ERROR"
                "GOTO EXIT"
                ":ERROR"
                "CALL c.exe"
                ":EXIT"
            ]
        instructions |> should equal expected

    [<Test>]
    let ``execution commands should execute before cleanup commands regardless of specification order`` () =
        let instructions = [TaskCleanupCommand "c.exe"; TaskExecutionCommand "a.exe"] |> buildCommandInstructions |> List.ofSeq
        let expected = [
                "@echo off"
                "CALL a.exe"
                "IF NOT %ERRORLEVEL% == 0 GOTO ERROR"
                "GOTO EXIT"
                ":ERROR"
                "CALL c.exe"
                ":EXIT"
            ]
        instructions |> should equal expected

    [<Test>]
    let ``execution commands should execute in specification order`` () =
        let instructions = [TaskExecutionCommand "b.exe"; TaskCleanupCommand "c.exe"; TaskExecutionCommand "a.exe"] |> buildCommandInstructions |> List.ofSeq
        let expected = [
                "@echo off"
                "CALL b.exe"
                "IF NOT %ERRORLEVEL% == 0 GOTO ERROR"
                "CALL a.exe"
                "IF NOT %ERRORLEVEL% == 0 GOTO ERROR"
                "GOTO EXIT"
                ":ERROR"
                "CALL c.exe"
                ":EXIT"
            ]
        instructions |> should equal expected


    [<Test>]
    let ``execution and commands should execute in specification order within their kind `` () =
        let instructions = [TaskExecutionCommand "b.exe"; TaskCleanupCommand "c.exe"; TaskExecutionCommand "a.exe"; TaskCleanupCommand "d.exe"; ] |> buildCommandInstructions |> List.ofSeq
        let expected = [
                "@echo off"
                "CALL b.exe"
                "IF NOT %ERRORLEVEL% == 0 GOTO ERROR"
                "CALL a.exe"
                "IF NOT %ERRORLEVEL% == 0 GOTO ERROR"
                "GOTO EXIT"
                ":ERROR"
                "CALL c.exe"                
                "CALL d.exe"
                ":EXIT"
            ]
        instructions |> should equal expected
