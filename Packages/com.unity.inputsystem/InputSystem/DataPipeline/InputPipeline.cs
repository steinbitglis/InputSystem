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

        public Processor1D processor1D;
        public Processor2D processor2D;
        
        public Accumulate1D accumulate1D;

        public MergerLatest1D1D mergerLatest1D1D;

        private static readonly ProfilerMarker s_PipelineMarker = new ProfilerMarker("InputPipeline");

        public InputPipeline(
            NativeArray<TypeConversionEnumToFloat.Operation> typeConversionEnumToFloatOperations,
            NativeArray<TypeConversionVector2ToMagnitude.Operation> typeConversionVector2ToMagnitudeOperations,
            NativeArray<TypeConversionVector3ToMagnitude.Operation> typeConversionVector3ToMagnitudeOperations,
            NativeArray<Processor1D.Operation> processor1DOperations,
            NativeArray<Processor2D.Operation> processor2DOperations,
            NativeArray<Accumulate1D.Operation> accumulate1DOperations,
            NativeArray<MergerLatest1D1D.Operation> mergerLatest1D1DOperations,
            InputDataset setDataset
        )
        {
            dataset = setDataset;
            
            typeConversionEnumToFloat = new TypeConversionEnumToFloat(typeConversionEnumToFloatOperations, dataset);
            typeConversionVector2ToMagnitude =
                new TypeConversionVector2ToMagnitude(typeConversionVector2ToMagnitudeOperations, dataset);
            typeConversionVector3ToMagnitude =
                new TypeConversionVector3ToMagnitude(typeConversionVector3ToMagnitudeOperations, dataset);
            
            processor1D = new Processor1D(processor1DOperations, dataset);
            processor2D = new Processor2D(processor2DOperations, dataset);
            
            accumulate1D = new Accumulate1D(accumulate1DOperations, dataset);

            mergerLatest1D1D = new MergerLatest1D1D(mergerLatest1D1DOperations, dataset);
        }

        public void Execute()
        {
            typeConversionEnumToFloat.Execute();
            typeConversionVector2ToMagnitude.Execute();
            typeConversionVector3ToMagnitude.Execute();
            processor1D.Execute();
            processor2D.Execute();
            accumulate1D.Execute();
            mergerLatest1D1D.Execute();
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