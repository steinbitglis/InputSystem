using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

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

        public void Execute()
        {
            foreach (var op in operations)
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