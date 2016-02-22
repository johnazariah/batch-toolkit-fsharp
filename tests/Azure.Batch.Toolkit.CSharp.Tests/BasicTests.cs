using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.Batch;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Batch.Toolkit.CSharp.Tests
{
    

    [TestFixture]
    public class BasicTests
    {
        [Test]
        public void ShouldBeAbleToCreateJobModelTypes()
        {
            var created = from simpleCommand in Command.NewSimpleCommand("echo 'Hello, World!'").Lift("simple-command")
                from parameterizedCommand in
                    Command.NewParametrizedCommand(
                        new ParametrizedCommand(
                            "echo 'Hello, %user%'",
                            new[]
                            {
                                "user"
                            }.ToFSharpList())).Lift()
                from tryWithCatch in
                    new CommandWithErrorHandler(parameterizedCommand, FSharpOption<Command>.Some(simpleCommand)).Lift(
                        "try-with-catch")
                from tryWithoutCatch in
                    new CommandWithErrorHandler(simpleCommand, FSharpOption<Command>.None).Lift("try-without-catch")
                from commandSet0 in CommandSet.Zero.Lift()
                from commandSet in new CommandSet(
                    new[]
                    {
                        tryWithCatch
                    }.ToFSharpList(),
                    new[]
                    {
                        simpleCommand
                    }.ToFSharpList()).Lift()
                from localFiles0 in LocalFiles.Zero.Lift()
                from localFiles in LocalFiles.NewLocalFiles(
                    new[]
                    {
                        new FileInfo("temp.txt")
                    }.ToFSharpList()).Lift()
                from uploadedFiles0 in UploadedFiles.Zero.Lift()
                from uploadedFiles in UploadedFiles.NewUploadedFiles(
                    new[]
                    {
                        new ResourceFile("blobSource", "blobPath")
                    }.ToFSharpList()).Lift("uploaded-files")
                from workloadUnitTemplate0 in WorkloadUnitTemplate.Zero.Lift()
                from workloadUnitTemplate in new WorkloadUnitTemplate(commandSet, localFiles, false).Lift()
                from workloadArguments0 in WorkloadArguments.Zero.Lift()
                from workloadArguments in
                    WorkloadArguments.NewWorkloadArguments(
                        new Dictionary<string, FSharpList<string>>
                        {
                            {
                                "users", new[]
                                {
                                    "john",
                                    "pradeep"
                                }.ToFSharpList()
                            }
                        }.ToFSharpMap()).Lift()
                from workloadSpecification in new WorkloadSpecification(
                    new[]
                    {
                        workloadUnitTemplate
                    }.ToFSharpList(),
                    LocalFiles.Zero,
                    workloadArguments).Lift()
                from taskName in TaskName.NewTaskName("simple-task").Lift()
                from taskArguments0 in TaskArguments.Zero.Lift()
                from taskArguments in TaskArguments.NewTaskArguments(
                    new Dictionary<string, string>
                    {
                        {"name", "john"}
                    }.ToFSharpMap()).Lift()
                from defaultTaskSpecification in TaskSpecification.Zero.Lift()
                from jobName in JobName.NewJobName("simple-job").Lift()
                from nullJobPriority in JobPriority.NewJobPriority(null).Lift()
                from jobPriority in JobPriority.NewJobPriority(10).Lift()
                from defaultJobSpecification in JobSpecification.Zero.Lift()
                select "done";

            Assert.AreEqual("done", created.Value);
        }

        [Test]
        public void ShouldBuildList()
        {
            var input = new List<int>(
                new[]
                {
                    1,
                    2,
                    3,
                    4,
                    5
                });
            var output = input.ToFSharpList();
            Assert.AreEqual(5, output.Length);
        }

        //[Test]
        public async void SimpleWorkflowTest()
        {
            if (!File.Exists("batch-config.json") || !File.Exists("storage-config.json")) return;

            var batchConfig =
                JsonConvert.DeserializeObject<Configuration.BatchConfiguration>(File.ReadAllText("batch-config.json"));
            var storageConfig =
                JsonConvert.DeserializeObject<Configuration.StorageConfiguration>(
                    File.ReadAllText("storage-config.json"));

            var simpleCommand = Command.NewSimpleCommand("echo 'Hello World'");

            var simpleWorkload =
                new WorkloadSpecification(
                    new FSharpList<WorkloadUnitTemplate>(
                        new WorkloadUnitTemplate(
                            new CommandSet(
                                new FSharpList<CommandWithErrorHandler>(
                                    new CommandWithErrorHandler(simpleCommand, FSharpOption<Command>.None),
                                    FSharpList<CommandWithErrorHandler>.Empty),
                                FSharpList<Command>.Empty),
                            LocalFiles.Zero,
                            false),
                        FSharpList<WorkloadUnitTemplate>.Empty),
                    LocalFiles.Zero,
                    WorkloadArguments.Zero);

            await
                WorkloadOperations.SubmitWorkloadToPoolAsync(
                    batchConfig,
                    storageConfig,
                    Pool.NewNamedPool(
                        new NamedPool(PoolName.NewPoolName("john"), PoolOperations.GetDefaultPoolSpecification)),
                    simpleWorkload);
        }
    }
}