using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ThatDRW.UnityProjectPatcher.Editor.Steps
{
    public readonly struct InstallUnityPackageFromUrlStep : IPatcherStep
    {
        private readonly string _packageUrl;
        private readonly string _displayName;

        public InstallUnityPackageFromUrlStep(string packageUrl, string displayName = null)
        {
            _packageUrl = packageUrl;
            _displayName = displayName ?? packageUrl;
        }

        public async UniTask<StepResult> Run()
        {
            Debug.Log($"[InstallUnityPackageFromUrlStep] Downloading '{_displayName}' from {_packageUrl}...");

            string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.unitypackage");

            // Download the package
            using UnityWebRequest request = new UnityWebRequest(_packageUrl, UnityWebRequest.kHttpVerbGET);
            request.downloadHandler = new DownloadHandlerFile(tempPath);
            request.SetRequestHeader("User-Agent", "UnityPlayer");

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
                await UniTask.Delay(100);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[InstallUnityPackageFromUrlStep] Failed to download '{_displayName}': {request.error}");
                return StepResult.Failure;
            }

            Debug.Log($"[InstallUnityPackageFromUrlStep] Download complete, importing '{_displayName}'...");

            // Import the package interactively (false = no dialog)
            AssetDatabase.ImportPackage(tempPath, false);
            AssetDatabase.Refresh();

            Debug.Log($"[InstallUnityPackageFromUrlStep] Successfully imported '{_displayName}'.");

            // Clean up temp file
            try { File.Delete(tempPath); }
            catch (Exception e) { Debug.LogWarning($"[InstallUnityPackageFromUrlStep] Could not delete temp file '{tempPath}': {e.Message}"); }

            return StepResult.Success;
        }

        public void OnComplete(bool failed)
        {
            // Nothing to clean up here — temp file is handled in Run()
        }
    }
}