using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEngine;

namespace Resonance.EditorTests
{
    // These disk fixtures verify artifact preservation, not a successful Unity Player build.
    public sealed class OriginalBuildArtifactGuardTests
    {
        static readonly string[] InjectedNames =
        {
            "PerformanceTestRunInfo.json",
            "PerformanceTestRunInfo.json.meta",
            "PerformanceTestRunSettings.json",
            "PerformanceTestRunSettings.json.meta"
        };

        string _project;
        string _resources;
        string _excluded;

        [Serializable]
        sealed class FixtureManifest
        {
            public string sourceProject;
            public string destination;
            public string sourceCommit = "fixture-not-a-release-commit";
        }

        [SetUp]
        public void SetUp()
        {
            var fixture = Path.Combine(Path.GetTempPath(), "original-build-artifact-guard-" + Guid.NewGuid().ToString("N"));
            _project = Path.Combine(fixture, "independent-project");
            _resources = Path.Combine(_project, "Assets", "Resources");
            _excluded = Path.Combine(_project, "BuildAudit", "ExcludedPerformanceTestAssets");
            var sourceProject = Path.Combine(fixture, "source-project");
            Directory.CreateDirectory(_resources);
            Directory.CreateDirectory(sourceProject);
            File.WriteAllText(Path.Combine(_project, "package-input-manifest.json"), JsonUtility.ToJson(
                new FixtureManifest { sourceProject = sourceProject, destination = _project }));
            // Retain the unique fixture directory as evidence; no recursive cleanup.
            TestContext.WriteLine("Preserved build artifact fixture: " + fixture);
        }

        [Test]
        public void PreserveInjectedResources_MovesOnlyExactArtifactsAndPreservesEveryByte()
        {
            var expected = WriteAllInjectedFiles();
            var untouched = new Dictionary<string, byte[]>
            {
                { "Fonts/NotoSansSC.otf", new byte[] { 0, 255, 12, 13, 128 } },
                { "PerformanceTestRunInfo.json.backup", new byte[] { 80, 81 } },
                { "Nested/PerformanceTestRunSettings.json", new byte[] { 90, 91 } },
                { "UnrelatedSettings.json", new byte[] { 123, 125 } }
            };
            foreach (var file in untouched) WriteFile(Path.Combine(_resources, file.Key), file.Value);
            var manifestBefore = File.ReadAllBytes(Path.Combine(_project, "package-input-manifest.json"));

            PreserveInjectedResources();

            CollectionAssert.AreEquivalent(InjectedNames, Directory.GetFiles(_excluded).Select(Path.GetFileName).ToArray());
            foreach (var file in expected)
            {
                Assert.That(File.Exists(Path.Combine(_resources, file.Key)), Is.False, file.Key + " still resides in Resources");
                CollectionAssert.AreEqual(file.Value, File.ReadAllBytes(Path.Combine(_excluded, file.Key)), file.Key);
            }
            CollectionAssert.AreEquivalent(untouched.Keys, ReadFiles(_resources).Keys);
            foreach (var file in untouched)
                CollectionAssert.AreEqual(file.Value, File.ReadAllBytes(Path.Combine(_resources, file.Key)), file.Key);
            CollectionAssert.AreEqual(manifestBefore, File.ReadAllBytes(Path.Combine(_project, "package-input-manifest.json")));
        }

        [Test]
        public void PreserveInjectedResources_WhenArtifactsAreAbsent_DoesNotChangeProject()
        {
            WriteFile(Path.Combine(_resources, "Fonts", "NotoSansSC.otf"), new byte[] { 10, 20, 30 });
            var before = ReadFiles(_project);
            var directoriesBefore = Directory.GetDirectories(_project, "*", SearchOption.AllDirectories);

            PreserveInjectedResources();

            AssertFilesUnchanged(before);
            CollectionAssert.AreEquivalent(directoriesBefore, Directory.GetDirectories(_project, "*", SearchOption.AllDirectories));
            Assert.That(Directory.Exists(_excluded), Is.False);
        }

        [TestCase("PerformanceTestRunInfo.json")]
        [TestCase("PerformanceTestRunInfo.json.meta")]
        [TestCase("PerformanceTestRunSettings.json")]
        [TestCase("PerformanceTestRunSettings.json.meta")]
        public void PreserveInjectedResources_AnyOccupiedTargetRejectsBeforeMovingAnything(string occupiedName)
        {
            WriteAllInjectedFiles();
            WriteFile(Path.Combine(_excluded, occupiedName), new byte[] { 111, 99, 99, 117, 112, 105, 101, 100 });
            var before = ReadFiles(_project);
            var directoriesBefore = Directory.GetDirectories(_project, "*", SearchOption.AllDirectories);

            Assert.Throws<InvalidOperationException>(() => PreserveInjectedResources());

            AssertFilesUnchanged(before);
            CollectionAssert.AreEquivalent(directoriesBefore, Directory.GetDirectories(_project, "*", SearchOption.AllDirectories));
        }

        [Test]
        public void PreserveInjectedResources_AbsentCompanionFilesAreNotCreated()
        {
            var original = new byte[] { 239, 187, 191, 123, 125, 13, 10 };
            WriteFile(Path.Combine(_resources, InjectedNames[0]), original);

            PreserveInjectedResources();

            Assert.That(Directory.GetFiles(_resources, "*", SearchOption.AllDirectories), Is.Empty);
            CollectionAssert.AreEquivalent(new[] { InjectedNames[0] }, Directory.GetFiles(_excluded).Select(Path.GetFileName).ToArray());
            CollectionAssert.AreEqual(original, File.ReadAllBytes(Path.Combine(_excluded, InjectedNames[0])));
        }

        Dictionary<string, byte[]> WriteAllInjectedFiles()
        {
            var files = new Dictionary<string, byte[]>();
            for (var i = 0; i < InjectedNames.Length; i++)
            {
                var bytes = new byte[] { 239, 187, 191, 0, (byte)i, 255, 13, 10 };
                WriteFile(Path.Combine(_resources, InjectedNames[i]), bytes);
                files.Add(InjectedNames[i], bytes);
            }
            return files;
        }

        void AssertFilesUnchanged(Dictionary<string, byte[]> expected)
        {
            var actual = ReadFiles(_project);
            CollectionAssert.AreEquivalent(expected.Keys, actual.Keys);
            foreach (var file in expected) CollectionAssert.AreEqual(file.Value, actual[file.Key], file.Key);
        }

        static Dictionary<string, byte[]> ReadFiles(string root)
        {
            return Directory.GetFiles(root, "*", SearchOption.AllDirectories).ToDictionary(
                path => path.Substring(root.Length + 1).Replace('\\', '/'), File.ReadAllBytes);
        }

        static void WriteFile(string path, byte[] bytes)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
        }

        void PreserveInjectedResources()
        {
            // Discover the build entry without adding an assembly dependency to the UI test asmdef.
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("Resonance.EditorTools.OriginalExpeditionBuild", false))
                .FirstOrDefault(candidate => candidate != null);
            Assert.That(type, Is.Not.Null, "OriginalExpeditionBuild editor entry point must be loaded.");
            var method = type.GetMethod("PreserveInjectedPerformanceResources", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(string) }, null);
            Assert.That(method, Is.Not.Null, "The build artifact preservation guard must exist.");
            try { method.Invoke(null, new object[] { _project }); }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
    }
}
