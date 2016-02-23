[![Issue Stats](http://issuestats.com/github/johnazariah/batch-toolkit-fsharp/badge/issue)](http://issuestats.com/github/johnazariah/batch-toolkit-fsharp)
[![Issue Stats](http://issuestats.com/github/johnazariah/batch-toolkit-fsharp/badge/pr)](http://issuestats.com/github/johnazariah/batch-toolkit-fsharp)

# batch-toolkit-fsharp

[Azure Batch Services](https://azure.microsoft.com/en-us/services/batch/) provides the ability to run batch computing processes at cloud scale.

The services are exposed through a [REST api](https://msdn.microsoft.com/en-us/library/azure/dn820158.aspx) and a comprehensive [SDK](https://msdn.microsoft.com/en-us/library/azure/mt348682.aspx).
Currently the SDK is targeted to the .NET platform and is somewhat C#-centric. Other languages and platforms may be supported in the future. 
The SDK provides a comprehensive re-interpretation of the REST API, affording a simpler interaction with the service, and the documentation allows users to develop applications in idiomatic C#.

This library is built over the SDK, and abstracts out common patterns and usage scenarios to allow use of idiomatic F# to interact with Azure Batch.

It is not my goal to obviate the SDK with this project, but rather to provide a code model centred around composition so that more complex batch-processing can be done in a simpler fashion.

## Build Status

Mono | .NET
---- | ----
[![Mono CI Build Status](https://img.shields.io/travis/johnazariah/batch-toolkit/develop.svg)](https://travis-ci.org/johnazariah/batch-toolkit) | [![.NET Build Status](https://img.shields.io/appveyor/ci/johnazariah/batch-toolkit/develop.svg)](https://ci.appveyor.com/project/johnazariah/batch-toolkit)

## Maintainer(s)

- [@johnazariah](https://github.com/johnazariah)

The default maintainer account for projects under "fsprojects" is [@fsprojectsgit](https://github.com/fsprojectsgit) - F# Community Project Incubation Space (repo management)
