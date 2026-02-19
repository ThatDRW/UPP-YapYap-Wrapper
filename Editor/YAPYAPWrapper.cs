using UnityEngine;
using Nomnom.UnityProjectPatcher.Editor;
using Nomnom.UnityProjectPatcher.Editor.Steps;



[UPPatcher("YAPYAPWrapper", true)]
public static class YAPYAPWrapper
{
    public static void GetSteps(StepPipeline stepPipeline)
    {
        stepPipeline.SetInputSystem(InputSystemType.InputSystem_New);
    }
}
