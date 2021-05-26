using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

namespace UnityEngine.InputSystem.DataPipeline
{
    // Processes single component value
    // N->N conversion.
    [BurstCompile]
    internal struct SingleComponentProcessor : IJob
    {
        public struct Operation
        {
            public Slice1D slice;

            // [minRange, maxRange] for clamp or compare 
            public float minRange, maxRange;

            // if 1.0f uses compare results instead of clamped value
            public float compare;

            // if compare == 1.0f and value is in [minRange, maxRange] then value is replaced by compareResultIfInRange
            // if compare == 1.0f and value is not in [minRange, maxRange] then value is replaced by compareResultIfOutOfRange
            public float compareResultIfInRange, compareResultIfOutOfRange;

            // if 1.0f value is normalized to 0.0f where 0.0f point is defined by minRange, 1.0f is maxRange
            public float normalize;

            // result value scale and offset factor
            public float scale;
            public float offset;

            // if 1.0f value is converted to absolute value before processing and converted back to signed afterwards
            public float processAsAbs;
        }

        public readonly NativeArray<Operation> operations;
        public InputDataset dataset;

        private static readonly ProfilerMarker s_OperationMarker =
            new ProfilerMarker("SingleComponentProcessor");

        public SingleComponentProcessor(NativeArray<Operation> setOperations, InputDataset setDataset)
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

                    for (var i = 0; i < length; ++i)
                    {
                        var v0 = v[i];

                        // branchless conditional abs
                        var v1 = Mathf.LerpUnclamped(v0, (v0 < 0.0f ? -v0 : v0), op.processAsAbs);

                        // branchless conditional clamp|compare
                        var clamped = Mathf.Clamp(v1, op.minRange, op.maxRange);
                        var banded = (v1 <= op.maxRange)
                            ? (v1 >= op.minRange ? op.compareResultIfInRange : op.compareResultIfOutOfRange)
                            : op.compareResultIfOutOfRange;
                        var v2 = Mathf.LerpUnclamped(clamped, banded, op.compare);

                        // branchless conditional normalize
                        var normalized = (v2 - op.minRange) / (op.maxRange - op.minRange);
                        var v3 = Mathf.LerpUnclamped(v2, normalized, op.normalize);

                        // branchless conditional sign restore
                        var v4 = Mathf.LerpUnclamped(v3, (v0 < 0.0f ? -v3 : v3), op.processAsAbs);

                        // FMA
                        var v5 = v4 * op.scale + op.offset;

                        v[i] = v5;
                    }
                }
            }
        }
    }
}