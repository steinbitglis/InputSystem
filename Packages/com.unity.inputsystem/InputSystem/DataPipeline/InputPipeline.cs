using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine.Profiling;

namespace UnityEngine.InputSystem.DataPipeline
{
    // Applies transformations to 1 dimensional values in-place.
    // N->N conversion.
    [BurstCompile]
    internal struct InputPipeline : IJob
    {
        public InputDataset dataset;

        public EnumToFloatTypeConversion enumToFloatTypeConversion;
        public Vector2ToMagnitudeTypeConversion vector2ToMagnitudeTypeConversion;
        public Vector3ToMagnitudeTypeConversion vector3ToMagnitudeTypeConversion;
        public SingleComponentProcessor singleComponentProcessor;

        private static readonly ProfilerMarker s_PipelineMarker = new ProfilerMarker("InputPipeline");

        public InputPipeline(
            NativeArray<EnumToFloatTypeConversion.Operation> enumToFloatTypeConversionOperations,
            NativeArray<Vector2ToMagnitudeTypeConversion.Operation> vector2ToMagnitudeTypeConversionOperations,
            NativeArray<Vector3ToMagnitudeTypeConversion.Operation> vector3ToMagnitudeTypeConversionOperations,
            NativeArray<SingleComponentProcessor.Operation> singleComponentProcessorOperations,
            InputDataset setDataset
        )
        {
            dataset = setDataset;
            enumToFloatTypeConversion = new EnumToFloatTypeConversion(enumToFloatTypeConversionOperations, dataset);
            vector2ToMagnitudeTypeConversion =
                new Vector2ToMagnitudeTypeConversion(vector2ToMagnitudeTypeConversionOperations, dataset);
            vector3ToMagnitudeTypeConversion =
                new Vector3ToMagnitudeTypeConversion(vector3ToMagnitudeTypeConversionOperations, dataset);
            singleComponentProcessor = new SingleComponentProcessor(singleComponentProcessorOperations, dataset);
        }

        public void Execute()
        {
            enumToFloatTypeConversion.Execute();
            vector2ToMagnitudeTypeConversion.Execute();
            vector3ToMagnitudeTypeConversion.Execute();
            singleComponentProcessor.Execute();
        }

        [BurstDiscard]
        public void Run(IUserPipelineStep[] preProcessors, IUserPipelineStep[] postProcessors)
        {
            using (s_PipelineMarker.Auto())
            {
                if (preProcessors != null)
                    foreach (var userPipelineStep in preProcessors)
                        userPipelineStep.Execute(dataset);

                Execute();

                if (postProcessors != null)
                    foreach (var userPipelineStep in postProcessors)
                        userPipelineStep.Execute(dataset);
            }
        }
    }
}