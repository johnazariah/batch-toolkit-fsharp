namespace Batch.Toolkit

module CommonCommands =
    module Windows =
        let CopyJobPrepTaskFilesToJobTask = SimpleCommand "copy %AZ_BATCH_JOB_PREP_WORKING_DIR% ."
    
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Batch.Toolkit.Tests")>]
do ()