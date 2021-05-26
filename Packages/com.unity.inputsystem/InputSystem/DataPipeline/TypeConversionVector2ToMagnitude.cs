using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

namespace UnityEngine.InputSystem.DataPipeline
{
    // Converts 2 dimensional vector to single float magnitude.
    // N->N conversion.
    [BurstCompile]
    internal struct TypeConversionVector2ToMagnitude : IJob
    {
        public struct Operation
        {
            public Slice2D src;
            public Slice1D dst;
        }

        public readonly NativeArray<Operation> operations;
        public InputDataset dataset;

        private static readonly ProfilerMarker s_OperationMarker = new ProfilerMarker("Vector2ToMagnitude");

        public TypeConversionVector2ToMagnitude(NativeArray<Operation> setOperations, InputDataset setDataset)
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
                    var (x, y) = dataset.GetValues(op.src);
                    var r = dataset.GetValues(op.dst);

                    for (var i = 0; i < length; ++i)
                        r[i] = new Vector2(x[i], y[i]).magnitude;
                }
            }
        }
    }
}