namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Azure.Batch.Toolkit")>]
[<assembly: AssemblyProductAttribute("Azure.Batch.Toolkit")>]
[<assembly: AssemblyDescriptionAttribute("A toolkit to simplify interacting with Azure Batch Services")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
