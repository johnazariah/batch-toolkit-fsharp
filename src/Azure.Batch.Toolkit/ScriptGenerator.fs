namespace Batch.Toolkit

module internal ScriptGenerator =
    let private interpolateParameter (s : string) (paramName, paramValue) = 
        let placeHolder = sprintf "%%%s%%" paramName
        let value = sprintf "%A" paramValue
        s.Replace(placeHolder, value)

    let private replaceParameterInString (args : Map<string, _>) p s = 
        Success p <!> (fun p -> (p, args.[p])) <!> interpolateParameter s

    let private bindParametersToCommand args pc =
        pc.Parameters |> foldrM (replaceParameterInString args) pc.Command

    let private bindArgument (args : Map<string, _>) = function
        | ParametrizedCommand pc -> pc |> bindParametersToCommand args 
        | SimpleCommand sc -> Success sc 

    let private emitCall args c = bindArgument(args) c <!> sprintf "CALL %s"
        
    let private generateTryCatch appendLine args (idx, tc) buffer =
        let successLabel = sprintf "_SUCCESS_%d" idx
        let errorLabel = sprintf "_ERROR_%d" idx

        succeed {
            let! buffer = 
                tc.Try 
                |> emitCall args                                       <!> appendLine buffer
            do sprintf "IF NOT %%ERRORLEVEL%% == 0 GOTO %s" errorLabel  |> appendLine buffer |> ignore
            do sprintf "GOTO %s" successLabel                           |> appendLine buffer |> ignore
            do sprintf ":%s" errorLabel                                 |> appendLine buffer |> ignore
                                                                           
            let! buffer =                                              
                tc.OnError                                             
                |> Option.map (emitCall args)   
                |> getOrElse (Success "")                              <!> appendLine buffer               
            do sprintf ":%s" successLabel                               |> appendLine buffer |> ignore
                
            return buffer
        }

    let private generateFinally appendLine args f buffer = 
        f |> emitCall args <!> appendLine buffer

    let FinallyLabel = ":_FINALLY_"
    let ExitLabel = ":_EXIT_" 

    let generateScript args commandSet (buffer : 'buffer, appendLine : 'buffer -> string -> 'buffer) =
        succeed {
            let! buffer = 
                commandSet.MainCommands 
                |> List.mapi (fun i c -> (i, c)) 
                |> foldrM (generateTryCatch appendLine args) buffer
            
            do FinallyLabel |> appendLine buffer |> ignore
            
            let! buffer = 
                commandSet.FinallyCommands 
                |> foldrM (generateFinally appendLine args) buffer
            
            do ExitLabel    |> appendLine buffer |> ignore
            
            return buffer.ToString ()
        }

    [<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Batch.Toolkit.Tests")>]
    do ()