using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

namespace UnityEngine.InputSystem.DataPipeline
{
    // Processes two component value
    // N->N conversion.
    [BurstCompile]
    internal struct Processor2D : IJob
    {
        public struct Operation
        {
            public Slice2D slice;

            // [minRange, maxRange] for clamping magnitude 
            public float minMagnitude, maxMagnitude;

            // if 1.0f value is normalized to 0.0f where 0.0f point is defined by minRange, 1.0f is maxRange
            public float normalize;

            // result value scale and offset factor
            public Vector2 scale;
            public Vector2 offset;
        }

        public readonly NativeArray<Operation> operations;
        public InputDataset dataset;

        private static readonly ProfilerMarker s_OperationMarker =
            new ProfilerMarker("Processor2D");

        public Processor2D(NativeArray<Operation> setOperations, InputDataset setDataset)
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
                    var (vx, vy) = dataset.GetValues(op.slice);

                    // TODO does it even make sense?
                    for (var i = 0; i < length; ++i)
                    {
                        var x = vx[i];
                        var y = vy[i];

                        var mag1 = Mathf.Sqrt(x * x + y * y);
                        var mag2 = Mathf.Clamp(mag1, op.minMagnitude, op.maxMagnitude);
                        // branchless normalize of magnitude 
                        var mag3 = Mathf.LerpUnclamped(mag2,
                            (mag2 - op.minMagnitude) / (op.maxMagnitude - op.minMagnitude), op.normalize);

                        var factor = mag3 / mag1; // normalize factor

                        vx[i] = x * factor * op.scale.x + op.offset.x;
                        vy[i] = y * factor * op.scale.y + op.offset.y;
                    }
                }
            }
        }
    }
}