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

        public TypeConversionEnumToFloat typeConversionEnumToFloat;
        public TypeConversionVector2ToMagnitude typeConversionVector2ToMagnitude;
        public TypeConversionVector3ToMagnitude typeConversionVector3ToMagnitude;
        public ProcessorSingleComponent processorSingleComponent;

        private static readonly ProfilerMarker s_PipelineMarker = new ProfilerMarker("InputPipeline");

        public InputPipeline(
            NativeArray<TypeConversionEnumToFloat.Operation> enumToFloatTypeConversionOperations,
            NativeArray<TypeConversionVector2ToMagnitude.Operation> vector2ToMagnitudeTypeConversionOperations,
            NativeArray<TypeConversionVector3ToMagnitude.Operation> vector3ToMagnitudeTypeConversionOperations,
            NativeArray<ProcessorSingleComponent.Operation> singleComponentProcessorOperations,
            InputDataset setDataset
        )
        {
            dataset = setDataset;
            typeConversionEnumToFloat = new TypeConversionEnumToFloat(enumToFloatTypeConversionOperations, dataset);
            typeConversionVector2ToMagnitude =
                new TypeConversionVector2ToMagnitude(vector2ToMagnitudeTypeConversionOperations, dataset);
            typeConversionVector3ToMagnitude =
                new TypeConversionVector3ToMagnitude(vector3ToMagnitudeTypeConversionOperations, dataset);
            processorSingleComponent = new ProcessorSingleComponent(singleComponentProcessorOperations, dataset);
        }

        public void Execute()
        {
            typeConversionEnumToFloat.Execute();
            typeConversionVector2ToMagnitude.Execute();
            typeConversionVector3ToMagnitude.Execute();
            processorSingleComponent.Execute();
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