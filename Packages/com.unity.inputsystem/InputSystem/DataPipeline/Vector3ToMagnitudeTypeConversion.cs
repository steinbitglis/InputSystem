using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

namespace UnityEngine.InputSystem.DataPipeline
{
    [BurstCompile]
    internal struct Vector3ToMagnitudeTypeConversion : IJob
    {
        public struct Operation
        {
            public Slice3D src;
            public Slice1D dst;
        }

        public readonly NativeArray<Operation> operations;
        public InputDataset dataset;

        private static readonly ProfilerMarker s_OperationMarker = new ProfilerMarker("Vector3ToMagnitudeTypeConversion");

        public Vector3ToMagnitudeTypeConversion(NativeArray<Operation> setOperations, InputDataset setDataset)
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
                    var (x, y, z) = dataset.GetValues(op.src);
                    var r = dataset.GetValues(op.dst);

                    for (var i = 0; i < length; ++i)
                        r[i] = new Vector3(x[i], y[i], z[i]).magnitude;
                }
            }
        }
    }
}