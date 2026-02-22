using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ThatDRW.UnityProjectPatcher.Editor.Steps
{
    public readonly struct PatchYapYapScriptsStep : IPatcherStep
    {
        public async UniTask<StepResult> Run()
        {
            Debug.Log("[PatchYapYapScriptsStep] Beginning script patching...");

            string[] guids = AssetDatabase.FindAssets("t:Script", new[] { "Assets" });
            bool modifiedDatabase = false;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!path.EndsWith(".cs") || !path.Contains("YAPYAP")) continue;

                // Delete job reflection artifacts and fog rotator
                if (path.Contains("__JobReflectionRegistrationOutput__") || path.EndsWith("FogDensityMaskRotator.cs"))
                {
                    File.Delete(path);
                    Debug.Log($"[PatchYapYapScriptsStep] Deleted artifact: {path}");
                    modifiedDatabase = true;
                    continue;
                }

                // Replace RecognizedPhrase with a safe stub
                if (path.EndsWith("RecognizedPhrase.cs"))
                {
                    File.WriteAllText(path, @"public class RecognizedPhrase {
    public const string ConfidenceKey = ""confidence"";
    public const string TextKey = ""text"";
    public string Text = """";
    public float Confidence;
    public RecognizedPhrase() { }
}");
                    Debug.Log($"[PatchYapYapScriptsStep] Replaced RecognizedPhrase stub: {path}");
                    modifiedDatabase = true;
                    continue;
                }

                string content = File.ReadAllText(path);
                bool modifiedFile = false;

                // MenuController: strip OccaSoftware usings and AutoExposureOverride references
                if (path.EndsWith("MenuController.cs"))
                {
                    if (content.Contains("using OccaSoftware"))
                    {
                        content = Regex.Replace(content, @"using OccaSoftware[^;]*;", "");
                        modifiedFile = true;
                    }
                    if (content.Contains("AutoExposureOverride"))
                    {
                        content = Regex.Replace(content, @".*AutoExposureOverride.*", "// omitted AutoExposureOverride");
                        modifiedFile = true;
                    }
                }

                // FmodResonanceAudio: fix velocity out parameter type
                if (path.EndsWith("FmodResonanceAudio.cs"))
                {
                    if (content.Contains("out UnityEngine.Vector3 vel"))
                    {
                        content = content.Replace("out UnityEngine.Vector3 vel", "out FMOD.VECTOR vel");
                        modifiedFile = true;
                    }
                    else if (content.Contains("out var vel"))
                    {
                        content = content.Replace("out var vel", "out FMOD.VECTOR vel");
                        modifiedFile = true;
                    }
                }

                // Global Stats fix: ensure struct Stats is public
                if (content.Contains("struct Stats"))
                {
                    string patched = Regex.Replace(content,
                        @"(?:public\s+|private\s+|protected\s+|internal\s+)*(readonly\s+)?(?:partial\s+)?struct\s+Stats\b",
                        "public $1struct Stats");
                    if (patched != content) { content = patched; modifiedFile = true; }
                }

                // RemoteStatistics: ensure class is public
                if (path.EndsWith("RemoteStatistics.cs"))
                {
                    string patched = Regex.Replace(content,
                        @"(?:public\s+|private\s+|protected\s+|internal\s+)*(class\s+RemoteStatistics\b)",
                        "public $1");
                    if (patched != content) { content = patched; modifiedFile = true; }
                }

                // YAPYAP enums: ensure State and OrbState are public
                if (content.Contains("enum State"))
                {
                    string patched = Regex.Replace(content,
                        @"(?:public\s+|private\s+|protected\s+|internal\s+)*enum\s+State\b",
                        "public enum State");
                    if (patched != content) { content = patched; modifiedFile = true; }
                }
                if (content.Contains("enum OrbState"))
                {
                    string patched = Regex.Replace(content,
                        @"(?:public\s+|private\s+|protected\s+|internal\s+)*enum\s+OrbState\b",
                        "public enum OrbState");
                    if (patched != content) { content = patched; modifiedFile = true; }
                }

                // SyncVar visibility fixes
                if (content.Contains("public override void SerializeSyncVars"))
                {
                    content = content.Replace("public override void SerializeSyncVars", "protected override void SerializeSyncVars");
                    modifiedFile = true;
                }
                if (content.Contains("public override void DeserializeSyncVars"))
                {
                    content = content.Replace("public override void DeserializeSyncVars", "protected override void DeserializeSyncVars");
                    modifiedFile = true;
                }

                if (modifiedFile)
                {
                    File.WriteAllText(path, content);
                    Debug.Log($"[PatchYapYapScriptsStep] Patched: {path}");
                    modifiedDatabase = true;
                }
            }

            if (modifiedDatabase)
            {
                AssetDatabase.Refresh();
                await UniTask.Delay(500); // Allow refresh to settle before next step
            }

            Debug.Log("[PatchYapYapScriptsStep] Script patching complete.");
            return StepResult.Success;
        }

        public void OnComplete(bool failed) { }
    }
}