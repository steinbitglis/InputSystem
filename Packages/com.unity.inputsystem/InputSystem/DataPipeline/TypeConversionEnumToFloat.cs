using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

namespace UnityEngine.InputSystem.DataPipeline
{
    // Converts single integer enum component to single float component.
    // N->N conversion.
    [BurstCompile]
    internal struct TypeConversionEnumToFloat : IJob 
    {
        public struct Operation
        {
            public Slice1D src;
            public Slice1D dst;
        }

        public readonly NativeArray<Operation> operations;
        public InputDataset dataset;
        
        private static readonly ProfilerMarker s_OperationMarker = new ProfilerMarker("EnumToFloat");
        
        public TypeConversionEnumToFloat(NativeArray<Operation> setOperations, InputDataset setDataset)
        {
            operations = setOperations;
            dataset = setDataset;
        }

        public void Execute()
        {
            // TODO
        }
    }
}