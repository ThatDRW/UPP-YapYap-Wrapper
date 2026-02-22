using UnityEngine;
using Nomnom.UnityProjectPatcher.Editor;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using ThatDRW.UnityProjectPatcher.Editor.Steps;


[UPPatcher("YAPYAPWrapper", true)]
public static class YAPYAPWrapper
{
    public static void GetSteps(StepPipeline stepPipeline)
    {
        stepPipeline.Steps.Clear();
        stepPipeline.InsertLast(new GenerateDefaultProjectStructureStep());
        // stepPipeline.InsertLast(new ImportTextMeshProStep());
        stepPipeline.InsertLast(new GenerateGitIgnoreStep());
        // stepPipeline.InsertLast(new GenerateReadmeStep());

        stepPipeline.InsertLast(new InstallUnityPackageFromUrlStep("https://github.com/MirrorNetworking/Mirror/releases/download/v96.0.1/Mirror-96.0.1.unitypackage", "Mirror v96.0.1"));
        stepPipeline.InsertLast(new PackagesInstallerStep()); // recompile
        stepPipeline.InsertLast(new CacheProjectCatalogueStep());
        stepPipeline.InsertLast(new AssetRipperStep());

        stepPipeline.InsertLast(new CopyGamePluginsStep()); // recompile // OccaSoftware excluded from DLLs to copy


        stepPipeline.InsertLast(new CopyExplicitScriptFolderStep()); // restarts
        stepPipeline.InsertLast(new EnableUnsafeCodeStep()); // recompiles
        stepPipeline.InsertLast(new CopyProjectSettingsStep(allowUnsafeCode: true)); // restart
        
        stepPipeline.InsertLast(new GuidRemapperStep());
        //stepPipeline.InsertLast(new CopyAssetRipperExportToProjectStep()); // restarts //  PATCHER BREAKING HERE  //

        // Incorportating JettsFixes
        stepPipeline.InsertLast(new SupressedCopyAssetRipperExportToProjectStep());
        //stepPipeline.InsertLast(new RestartEditorStep());


        stepPipeline.InsertLast(new FixProjectFileIdsStep());
        stepPipeline.InsertLast(new SortAssetTypesSteps());
        stepPipeline.InsertLast(new RestartEditorStep());

        // stepPipeline.SetInputSystem(InputSystemType.InputSystem_New);

        // - [0] GenerateDefaultProjectStructureStep
        // - [1] ImportTextMeshProStep
        // - [2] GenerateGitIgnoreStep
        // - [3] GenerateReadmeStep
        // - [4] PackagesInstallerStep
        // - [5] EnableNewInputSystemStep
        // - [6] CacheProjectCatalogueStep
        // - [7] AssetRipperStep
        // - [8] CopyGamePluginsStep
        // - [9] CopyExplicitScriptFolderStep
        // - [10] EnableUnsafeCodeStep
        // - [11] CopyProjectSettingsStep
        // - [12] GuidRemapperStep
        // - [13] CopyAssetRipperExportToProjectStep
        // - [14] FixInputSystemAssetsStep
        // - [15] FixProjectFileIdsStep
        // - [16] InjectURPAssetsStep
        // - [17] SortAssetTypesSteps
        // - [18] RestartEditorStep


    }
}
