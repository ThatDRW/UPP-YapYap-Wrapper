using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher;
using Nomnom.UnityProjectPatcher.AssetRipper;
using Nomnom.UnityProjectPatcher.Editor;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;

namespace ThatDRW.UnityProjectPatcher.Editor.Steps
{
    //[StructLayout(LayoutKind.Sequential, Size = 1)]
    public readonly struct SupressedCopyAssetRipperExportToProjectStep : IPatcherStep
    {
        private static readonly string[] _ignoreFiles = new string[2] { "UnitySourceGeneratedAssemblyMonoScriptTypes_v1.cs", "AssemblyInfo.cs" };

        public UniTask<StepResult> Run()
        {
            UPPatcherSettings settings = this.GetSettings();
            AssetRipperSettings assetRipperSettings = this.GetAssetRipperSettings();
            Directory.CreateDirectory(settings.ProjectGameAssetsFullPath);
            AssetCatalogue assetCatalogue = GuidRemapperStep.AssetRipperCatalogue ?? AssetScrubber.ScrubDiskFolder(assetRipperSettings.OutputExportAssetsFolderPath, assetRipperSettings.FoldersToExcludeFromRead);
            AssetCatalogue projectAssets = GuidRemapperStep.ProjectCatalogue ?? AssetScrubber.ScrubProject();
            string projectGameAssetsPath = settings.ProjectGameAssetsPath;
            AssetCatalogue.Entry[] array = GetAllowedEntries(assetCatalogue, projectAssets, assetRipperSettings).ToArray();
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < array.Length; i++)
            {
                AssetCatalogue.Entry entry = array[i];
                EditorUtility.DisplayProgressBar($"Copying assets [{i}/{array.Length}]", "Copying " + entry.RelativePathToRoot, (float)i / (float)array.Length);
                try
                {
                    string projectPathFromExportPath = AssetScrubber.GetProjectPathFromExportPath(projectGameAssetsPath, entry, settings, assetRipperSettings, ignoreExclude: false);
                    if (projectPathFromExportPath == null)
                    {
                        Debug.LogWarning(" - Could not find project path for \"" + entry.RelativePathToRoot + "\"");
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(projectPathFromExportPath));
                    string text = Path.Combine(assetCatalogue.RootAssetsPath, entry.RelativePathToRoot);
                    File.Copy(text, projectPathFromExportPath, overwrite: true);
                    string text2 = text + ".meta";
                    if (File.Exists(text2))
                    {
                        File.Copy(text2, projectPathFromExportPath + ".meta", overwrite: true);
                    }
                }
                catch
                {
                    Debug.LogError("Failed to copy \"" + entry.RelativePathToRoot + "\"");
                    EditorUtility.ClearProgressBar();
                    AssetDatabase.StopAssetEditing();
                    throw;
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            //return UniTask.FromResult(StepResult.RestartEditor);
            //Supressing restart for JettsScriptFixes.
            //CALL MANUAL RESTART AFTER JETTS FIXES!!!
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed)
        {
        }

        private static IEnumerable<AssetCatalogue.Entry> GetAllowedEntries(AssetCatalogue arAssets, AssetCatalogue projectAssets, AssetRipperSettings settings)
        {
            IReadOnlyList<string> foldersToCopy = settings.FoldersToCopy;
            IReadOnlyList<string> filesToExclude = settings.FilesToExcludeFromCopy;
            string[] filesToExcludePrefix = (from x in filesToExclude
                                             where x.EndsWith("*")
                                             select x.Substring(0, x.Length - 1)).ToArray();
            filesToExclude = filesToExclude.Except(filesToExcludePrefix).ToList();
            for (int i = 0; i < arAssets.Entries.Length; i++)
            {
                AssetCatalogue.Entry asset = arAssets.Entries[i];
                EditorUtility.DisplayProgressBar($"Getting allowed entries [{i}/{arAssets.Entries.Length}]", "Scrubbing " + asset.RelativePathToRoot, (float)i / (float)arAssets.Entries.Length);
                if (filesToExclude.Any((string x) => x == asset.RelativePathToRoot) || filesToExcludePrefix.Any((string x) => asset.RelativePathToRoot.StartsWith(x)))
                {
                    continue;
                }

                string fileName = Path.GetFileName(asset.RelativePathToRoot);
                if (_ignoreFiles.Any((string x) => fileName == x) || !foldersToCopy.Any((string x) => asset.RelativePathToRoot.StartsWith(x)))
                {
                    continue;
                }

                if (!(asset is AssetCatalogue.ScriptEntry))
                {
                    if (!asset.RelativePathToRoot.EndsWith(".asmdef"))
                    {
                        yield return asset;
                    }

                    continue;
                }

                AssetCatalogue.ScriptEntry otherEntry = asset as AssetCatalogue.ScriptEntry;
                if (!projectAssets.ContainsFullTypeName(otherEntry))
                {
                    yield return asset;
                }
            }

            EditorUtility.ClearProgressBar();
        }
    }
}

