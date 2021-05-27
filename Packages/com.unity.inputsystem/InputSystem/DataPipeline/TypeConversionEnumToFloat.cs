using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

namespace UnityEngine.InputSystem.DataPipeline
{
    // Converts single integer enum component to single float component.
    // Applies bit mask first, then looks into enum LUT with offset.
    // N->N conversion.
    [BurstCompile]
    internal struct TypeConversionEnumToFloat : IJob
    {
        public struct Operation
        {
            public SliceEnum src;
            public Slice1D dst;

            public int mask;
            public int offsetInLUT;
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
            foreach (var op in operations)
            {
                using (s_OperationMarker.Auto())
                {
                    var length = dataset.SetLengthAsNToNMapping(op.src, op.dst);
                    var src = dataset.GetValues(op.src);
                    var dst = dataset.GetValues(op.dst);

                    // TODO No SIMD here yet :( do we need AVX-512?
                    for (var i = 0; i < length; ++i)
                        dst[i] = dataset.enumLUT[(src[i] & op.mask) + op.offsetInLUT];
                }
            }
        }
    }
}