using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ThatDRW.UnityProjectPatcher.Editor.Steps
{
    public readonly struct InstallGitPackageStep : IPatcherStep
    {
        private readonly string _packageGitUrl;
        private readonly string _displayName;

        public InstallGitPackageStep(string packageGitUrl, string displayName = null)
        {
            _packageGitUrl = packageGitUrl;
            _displayName = displayName ?? packageGitUrl;
        }

        public async UniTask<StepResult> Run()
        {
            Debug.Log($"[InstallGitPackageStep] Checking if '{_displayName}' is already installed...");

            // Strip revision suffix (#branch/tag/hash) for name matching
            string urlWithoutRevision = _packageGitUrl.Split('#')[0];

            ListRequest listRequest = Client.List(offlineMode: true, includeIndirectDependencies: false);
            while (!listRequest.IsCompleted)
                await UniTask.Delay(100);

            if (listRequest.Status == StatusCode.Success)
            {
                foreach (var package in listRequest.Result)
                {
                    if (package.packageId.Contains(urlWithoutRevision) ||
                        package.resolvedPath?.Contains(urlWithoutRevision) == true)
                    {
                        Debug.Log($"[InstallGitPackageStep] '{_displayName}' is already installed ({package.version}), skipping.");
                        return StepResult.Success;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[InstallGitPackageStep] Could not query installed packages: {listRequest.Error?.message}. Proceeding with install anyway.");
            }

            Debug.Log($"[InstallGitPackageStep] Installing package: {_displayName} ({_packageGitUrl})");

            AddRequest addRequest = Client.Add(_packageGitUrl);
            while (!addRequest.IsCompleted)
                await UniTask.Delay(100);

            if (addRequest.Status == StatusCode.Success)
            {
                Debug.Log($"[InstallGitPackageStep] Successfully installed: {addRequest.Result.displayName} ({addRequest.Result.version})");
                return StepResult.Success;
            }

            Debug.LogError($"[InstallGitPackageStep] Failed to install '{_displayName}': {addRequest.Error?.message}");
            return StepResult.Failure;
        }

        public void OnComplete(bool failed) { }
    }
}