using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

namespace UnityEngine.InputSystem.DataPipeline
{
    [BurstCompile]
    public struct Accumulate1D : IJob
    {
        public struct Operation
        {
            public Slice1D slice;
        }

        public readonly NativeArray<Operation> operations;
        public InputDataset dataset;
        
        private static readonly ProfilerMarker s_OperationMarker = new ProfilerMarker("Accumulate1D");
        
        public Accumulate1D(NativeArray<Operation> setOperations, InputDataset setDataset)
        {
            operations = setOperations;
            dataset = setDataset;
        }

        public void Execute()
        {
            foreach (var op in operations)
            {
                using (s_OperationMarker.Auto())
                {
                    var length = dataset.lengths[op.slice.lengthIndex];
                    var v = dataset.GetValues(op.slice);

                    // TODO prefix sum on SIMD 
                    var value = 0.0f;
                    for (var i = 0; i < length; ++i)
                    {
                        value += v[i];
                        v[i] = value;
                    }
                }
            }
        }
    }
}