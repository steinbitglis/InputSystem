using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.InputSystem.DataPipeline
{
    // Converts single integer enum component to single float component.
    // N->N conversion.
    [BurstCompile]
    internal struct EnumToFloatTypeConversion : IJob 
    {
        public struct Operation
        {
            public Slice1D src;
            public Slice1D dst;
        }

        public readonly NativeArray<Operation> operations;
        public InputDataset dataset;

        public void Execute()
        {
            // TODO
        }
    }
}