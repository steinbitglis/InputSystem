using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine.InputSystem.DataPipeline.Demux;
using UnityEngine.InputSystem.DataPipeline.Merger;
using UnityEngine.InputSystem.DataPipeline.Processor;
using UnityEngine.InputSystem.DataPipeline.SlidingWindow;
using UnityEngine.InputSystem.DataPipeline.TypeConversion;
using UnityEngine.Profiling;

namespace UnityEngine.InputSystem.DataPipeline
{
    [BurstCompile]
    internal struct InputPipeline : IJob
    {
        public Dataset dataset;
        public DatasetPlanner datasetPlanner;
        
        public DynamicDemuxer dynamicDemuxer;

        public NativeArray<float> enumsToFloatsLut;
        public NativeArray<EnumToFloat> enumsToFloats;

        public NativeArray<Vec2ToMagnitude> vec2sToMagnitudes;

        public NativeArray<Processor1D> process1Ds;

        public NativeArray<Accumulate1D> accumulate1Ds;

        public NativeArray<Latest1D> latest1Ds;

        private static readonly ProfilerMarker s_MarkerDynamicDemuxer = new ProfilerMarker("DynamicDemuxer");
        private static readonly ProfilerMarker s_MarkerEnumToFloat = new ProfilerMarker("EnumToFloat");
        private static readonly ProfilerMarker s_MarkerVec2ToMagnitude = new ProfilerMarker("Vec2ToMagnitude");
        private static readonly ProfilerMarker s_MarkerProcessor1D = new ProfilerMarker("Processor1D");
        private static readonly ProfilerMarker s_MarkerAccumulate1D = new ProfilerMarker("Accumulate1D");
        private static readonly ProfilerMarker s_MarkerLatest1D = new ProfilerMarker("Latest1D");

        public unsafe void Execute()
        {
            dynamicDemuxer.Execute();

            foreach (var op in enumsToFloats)
                using (s_MarkerEnumToFloat.Auto())
                    op.Execute(dataset, (float*)enumsToFloatsLut.GetUnsafePtr());

            foreach (var op in vec2sToMagnitudes)
                using (s_MarkerVec2ToMagnitude.Auto())
                    op.Execute(dataset);

            foreach (var op in process1Ds)
                using (s_MarkerProcessor1D.Auto())
                    op.Execute(dataset);

            foreach (var op in accumulate1Ds)
                using (s_MarkerAccumulate1D.Auto())
                    op.Execute(dataset);

            foreach (var op in latest1Ds)
                using (s_MarkerLatest1D.Auto())
                    op.Execute(dataset);
        }

        public void Dispose()
        {
            enumsToFloats.Dispose();
            vec2sToMagnitudes.Dispose();

            process1Ds.Dispose();

            accumulate1Ds.Dispose();

            latest1Ds.Dispose();
        }
    }
}