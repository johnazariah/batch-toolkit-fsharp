namespace Batch.Toolkit

[<AutoOpen>]
module internal Common =
    open System
    open System.IO
    open Newtonsoft.Json

    let private id' = (fun _ c -> c)
    let getOrElse d mv   = mv |> Option.fold id' d
    let getOrNull mv     = mv |> Option.fold id' null
    let getOrNullable mv = mv |> Option.fold (fun _ c -> Nullable(c)) (Nullable())

    let readJson<'a> json = JsonConvert.DeserializeObject<'a>(json)

    let readConfig<'a> config = 
        let file = config |> FileInfo
        
        file.FullName
        |> File.ReadAllText
        |> readJson<'a>

    [<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Batch.Toolkit.Tests")>]
    do ()
