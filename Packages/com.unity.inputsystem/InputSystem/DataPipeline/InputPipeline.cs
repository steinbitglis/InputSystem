using Unity.Burst;
using Unity.Jobs;

namespace UnityEngine.InputSystem.DataPipeline
{
    // Applies transformations to 1 dimensional values in-place.
    // N->N conversion.
    [BurstCompile]
    internal struct DataPipeline : IJob
    {
        public InputDataset dataset;

        public EnumToFloatTypeConversion enumToFloatTypeConversion;
        public Vector2ToMagnitudeTypeConversion vector2ToMagnitudeTypeConversion;
        public Vector3ToMagnitudeTypeConversion vector3ToMagnitudeTypeConversion;
        public SingleComponentProcessor singleComponentProcessor;

        public void Execute()
        {
            enumToFloatTypeConversion.dataset = dataset;
            vector2ToMagnitudeTypeConversion.dataset = dataset;
            vector3ToMagnitudeTypeConversion.dataset = dataset;
            singleComponentProcessor.dataset = dataset;

            enumToFloatTypeConversion.Execute();
            vector2ToMagnitudeTypeConversion.Execute();
            vector3ToMagnitudeTypeConversion.Execute();
            singleComponentProcessor.Execute();
        }

        [BurstDiscard]
        public void Run(InputDataset dataset, IUserPipelineStep[] preProcessors, IUserPipelineStep[] postProcessors)
        {
            foreach (var userPipelineStep in preProcessors)
                userPipelineStep.Execute(dataset);

            Execute();

            foreach (var userPipelineStep in postProcessors)
                userPipelineStep.Execute(dataset);
        }
    }


}