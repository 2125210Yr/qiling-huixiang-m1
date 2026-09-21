using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Resonance.EditorTools
{
    /// <summary>Only runs in an independently copied, hash-manifested original-expedition project.</summary>
    public static class OriginalExpeditionBuild
    {
        public static void PreserveInjectedPerformanceResources(string project)
        {
            project = Path.GetFullPath(project);
            CheckPhysical(project);
            var manifestPath = Path.Combine(project, "package-input-manifest.json");
            CheckPhysical(manifestPath);
            var manifest = JsonUtility.FromJson<InputManifest>(File.ReadAllText(manifestPath));
            if (manifest == null || !SamePath(manifest.destination, project) || SamePath(manifest.sourceProject, project))
                throw new InvalidOperationException("Generated artifacts may only be preserved in a declared independent project.");
            var destination = Path.Combine(project, "BuildAudit", "ExcludedPerformanceTestAssets");
            var existingParent = destination;
            while (!Directory.Exists(existingParent) && !File.Exists(existingParent)) existingParent = Path.GetDirectoryName(existingParent);
            CheckPhysical(existingParent);
            if (!Directory.Exists(existingParent)) throw new InvalidOperationException("Artifact destination parent is not a directory.");
            var moves = new List<KeyValuePair<string, string>>();
            foreach (var name in PerformanceArtifacts)
            {
                var source = Path.Combine(project, "Assets", "Resources", name);
                if (Directory.Exists(source)) throw new InvalidOperationException("Expected a generated file, not a directory: " + source);
                if (!File.Exists(source)) continue;
                CheckPhysical(source);
                var target = Path.Combine(destination, name);
                if (File.Exists(target) || Directory.Exists(target))
                    throw new InvalidOperationException("Preserved artifact already exists; refusing overwrite: " + target);
                moves.Add(new KeyValuePair<string, string>(source, target));
            }
            if (moves.Count == 0) return;
            Directory.CreateDirectory(destination);
            CheckPhysical(destination);
            // All four explicit targets were checked before the first move. Failure preserves
            // partial output for diagnosis; neither this guard nor the caller deletes files.
            foreach (var move in moves)
            {
                CheckPhysical(move.Key);
                CheckPhysical(destination);
                File.Move(move.Key, move.Value);
            }
        }

        [Serializable] public sealed class InputFile { public string path, sha256; }
        [Serializable] public sealed class InputManifest
        { public string sourceProject, destination, sourceCommit; public InputFile[] files; }
        [Serializable] public sealed class PackedRow { public string container, path, guid; public ulong bytes; }
        [Serializable] public sealed class OutputRow { public string path, role, sha256; public ulong bytes; }
        [Serializable] public sealed class Audit
        {
            public string result, error, project, sourceCommit, inputManifestSha256, contentHash, exe;
            public string[] dependencies;
            public List<PackedRow> packed = new List<PackedRow>();
            public List<OutputRow> outputs = new List<OutputRow>();
            public List<OutputRow> excludedBuildArtifacts = new List<OutputRow>();
        }

        static Audit activeAudit;
        static readonly string[] PerformanceArtifacts = {
            "PerformanceTestRunInfo.json", "PerformanceTestRunInfo.json.meta",
            "PerformanceTestRunSettings.json", "PerformanceTestRunSettings.json.meta"
        };

        internal static void BeforeAuditedPlayerBuild()
        {
            if (activeAudit == null) return;
            var project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var manifest = JsonUtility.FromJson<InputManifest>(File.ReadAllText(Path.Combine(project, "package-input-manifest.json")));
            if (!SamePath(activeAudit.project, project) || !SamePath(manifest.destination, project) || SamePath(manifest.sourceProject, project))
                throw new InvalidOperationException("Performance artifact exclusion requires the active isolated build.");
            foreach (var name in PerformanceArtifacts)
                if (manifest.files.Any(f => f.path == "Assets/Resources/" + name))
                    throw new InvalidOperationException("Performance artifact unexpectedly belongs to declared inputs: " + name);
            PreserveInjectedPerformanceResources(project);
            foreach (var name in PerformanceArtifacts)
            {
                var full = Path.Combine(project, "BuildAudit", "ExcludedPerformanceTestAssets", name);
                if (File.Exists(full)) activeAudit.excludedBuildArtifacts.Add(new OutputRow {
                    path = "BuildAudit/ExcludedPerformanceTestAssets/" + name,
                    role = "preserved-generated-performance-test-artifact-not-player-input",
                    sha256 = Hash(full), bytes = checked((ulong)new FileInfo(full).Length)
                });
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        public static void BuildAndExit()
        {
            var project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var audit = new Audit { result = "FAIL", project = project, contentHash = Resonance.Battle.ExpeditionContent.ContentHash };
            var auditDirectory = Path.Combine(project, "BuildAudit");
            try
            {
                var manifestPath = Path.Combine(project, "package-input-manifest.json");
                if (!File.Exists(manifestPath)) throw new InvalidOperationException("Independent package input manifest required.");
                var manifest = JsonUtility.FromJson<InputManifest>(File.ReadAllText(manifestPath));
                if (manifest == null || !SamePath(manifest.destination, project) || SamePath(manifest.sourceProject, project))
                    throw new InvalidOperationException("Build must run in the declared independent destination project.");
                CheckPhysical(project);
                audit.sourceCommit = manifest.sourceCommit;
                audit.inputManifestSha256 = Hash(manifestPath);
                var allowed = ValidateInputs(project, manifest);
                Directory.CreateDirectory(auditDirectory);
                audit.dependencies = AssetDatabase.GetDependencies(new[] { "Assets/Scenes/Boot.unity" }, true);
                foreach (var dependency in audit.dependencies)
                    if (dependency.StartsWith("Assets/", StringComparison.Ordinal) && !allowed.Contains(dependency))
                        throw new InvalidOperationException("Scene dependency outside package inputs: " + dependency);
                var output = Path.Combine(project, "Builds", "OriginalExpeditionWin64");
                if (Directory.Exists(output)) throw new InvalidOperationException("Use a fresh project/output; existing Player output is preserved.");
                Directory.CreateDirectory(output);
                var exe = Path.Combine(output, "OriginalExpedition.exe");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                    throw new InvalidOperationException("Cannot select Windows x64 build target.");
                PlayerSettings.productName = "契灵回响";
                PlayerSettings.defaultIsNativeResolution = false;
                PlayerSettings.defaultScreenWidth = 540;
                PlayerSettings.defaultScreenHeight = 960;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.resizableWindow = false;
                PlayerSettings.allowFullscreenSwitch = false;
                PlayerSettings.runInBackground = true;
                PlayerSettings.visibleInBackground = true;
                PlayerSettings.SplashScreen.show = false;
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                BuildReport report;
                activeAudit = audit;
                try
                {
                    report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                    {
                        scenes = new[] { "Assets/Scenes/Boot.unity" }, locationPathName = exe,
                        target = BuildTarget.StandaloneWindows64,
                        options = BuildOptions.CompressWithLz4 | BuildOptions.DetailedBuildReport
                    });
                }
                finally { activeAudit = null; }
                audit.exe = exe;
                foreach (var container in report.packedAssets)
                    foreach (var asset in container.contents)
                    {
                        var source = (asset.sourceAssetPath ?? "").Replace('\\', '/');
                        audit.packed.Add(new PackedRow { container = container.shortPath, path = source,
                            guid = asset.sourceAssetGUID.ToString(), bytes = asset.packedSize });
                        if (source.StartsWith("Assets/", StringComparison.Ordinal) && !allowed.Contains(source))
                            throw new InvalidOperationException("Packed asset outside the verified input manifest: " + source);
                    }
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Unity BuildPlayer result: " + report.summary.result);
                if (!audit.packed.Any(p => p.path == "Assets/Resources/Fonts/NotoSansSC.otf"))
                    throw new InvalidOperationException("Portable font is missing from packed-asset evidence.");
                var outputRoles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var file in report.GetFiles()) outputRoles[Path.GetFullPath(file.path)] = file.role;
                foreach (var notice in manifest.files.Where(f => f.path.StartsWith("ThirdPartyNotices/", StringComparison.Ordinal)))
                {
                    var target = Path.Combine(output, notice.path);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(Path.Combine(project, notice.path), target, false);
                    outputRoles[Path.GetFullPath(target)] = "third-party-notice";
                }
                var copiedManifest = Path.Combine(output, "package-input-manifest.json");
                File.Copy(manifestPath, copiedManifest, false);
                outputRoles[copiedManifest] = "verified-input-manifest";
                foreach (var full in Directory.GetFiles(output, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
                {
                    CheckPhysical(full);
                    if (!full.StartsWith(output + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Unexpected build output location: " + full);
                    var relative = full.Substring(output.Length + 1).Replace('\\', '/');
                    if (relative.IndexOf("StreamingAssets/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        relative.EndsWith("inochi2d.dll", StringComparison.OrdinalIgnoreCase) ||
                        relative.EndsWith("inmath.dll", StringComparison.OrdinalIgnoreCase) ||
                        relative.EndsWith("i2d.dll", StringComparison.OrdinalIgnoreCase) ||
                        relative.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Excluded native/model data in Player output: " + relative);
                    string role;
                    outputRoles.TryGetValue(full, out role);
                    audit.outputs.Add(new OutputRow { path = relative, role = role ?? "additional-player-output",
                        bytes = checked((ulong)new FileInfo(full).Length), sha256 = Hash(full) });
                }
                audit.result = "PASS";
                Debug.Log("ORIGINAL_BUILD_AUDIT_PASS exe=" + exe);
            }
            catch (Exception error)
            {
                audit.error = error.ToString();
                Debug.LogError(error);
            }
            // A missing source-project manifest must not make this tool write into the shared project.
            if (File.Exists(Path.Combine(project, "package-input-manifest.json")))
            {
                Directory.CreateDirectory(auditDirectory);
                File.WriteAllText(Path.Combine(auditDirectory, "build-audit.json"), JsonUtility.ToJson(audit, true));
            }
            EditorApplication.Exit(audit.result == "PASS" ? 0 : 1);
        }

        static HashSet<string> ValidateInputs(string project, InputManifest manifest)
        {
            if (manifest.files == null || manifest.files.Length == 0) throw new InvalidOperationException("Empty package manifest.");
            var allowed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in manifest.files)
            {
                if (file == null || string.IsNullOrEmpty(file.path) || Path.IsPathRooted(file.path))
                    throw new InvalidOperationException("Invalid package input path.");
                var full = Path.GetFullPath(Path.Combine(project, file.path));
                if (!full.StartsWith(project + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Package input escapes copied project.");
                CheckPhysical(full);
                if (!File.Exists(full) || !string.Equals(Hash(full), file.sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Copied input changed: " + file.path);
                if (!allowed.Add(file.path.Replace('\\', '/'))) throw new InvalidOperationException("Duplicate package input.");
            }
            foreach (var full in Directory.GetFiles(Path.Combine(project, "Assets"), "*", SearchOption.AllDirectories))
            {
                CheckPhysical(full);
                var relative = full.Substring(project.Length + 1).Replace('\\', '/');
                if (relative.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                if (!allowed.Contains(relative)) throw new InvalidOperationException("Unlisted Assets file: " + relative);
                if (relative.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    relative != "Assets/Resources/Fonts/NotoSansSC.otf")
                    throw new InvalidOperationException("Unexpected Resources data: " + relative);
                if (relative.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || relative.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Project native/model asset is excluded: " + relative);
            }
            return allowed;
        }

        static void CheckPhysical(string path)
        {
            for (var cursor = Path.GetFullPath(path); !string.IsNullOrEmpty(cursor); cursor = Path.GetDirectoryName(cursor))
                if ((File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Linked path is not an independent package input: " + cursor);
        }
        static bool SamePath(string a, string b) => !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) &&
            string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
        static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }

    // The installed performance package injects Resources in its order-0 callback even for
    // non-test builds. Preserve only those generated files outside Assets before collection.
    public sealed class OriginalExpeditionPerformanceArtifactFilter : IPreprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;
        public void OnPreprocessBuild(BuildReport report) => OriginalExpeditionBuild.BeforeAuditedPlayerBuild();
    }
}
