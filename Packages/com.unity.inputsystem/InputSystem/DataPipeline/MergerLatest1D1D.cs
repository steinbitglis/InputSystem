using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using static Unity.Burst.CompilerServices.Aliasing;

namespace UnityEngine.InputSystem.DataPipeline
{
    // Merges two 1D slices into one 1D slice in order of timestamps.
    // x(N)+y(M)->z(N+M) conversion.
    [BurstCompile]
    public struct MergerLatest1D1D : IJob
    {
        public struct Operation
        {
            public Slice1D src1, src2;
            public Slice1D dst;
        }

        public readonly NativeArray<Operation> operations;
        public InputDataset dataset;

        private static readonly ProfilerMarker s_OperationMarker =
            new ProfilerMarker("MergerLatest1D1D");

        public MergerLatest1D1D(NativeArray<Operation> setOperations, InputDataset setDataset)
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
                    var l1 = dataset.lengths[op.src1.lengthIndex];
                    var l2 = dataset.lengths[op.src2.lengthIndex];
                    dataset.lengths[op.dst.lengthIndex] = l1 + l2;

                    var t1 = dataset.GetTimestamps(op.src1); 
                    var t2 = dataset.GetTimestamps(op.src2); 
                    var t3 = dataset.GetTimestamps(op.dst); 

                    var v1 = dataset.GetValues(op.src1);
                    var v2 = dataset.GetValues(op.src2);
                    var v3 = dataset.GetValues(op.dst);

                    // TODO mark that all of them don't alias
                    // ExpectNotAliased(in t1, in t2);
                    // ExpectNotAliased(in t1, in t3);
                    // ExpectNotAliased(in t2, in t2);
                    //
                    // ExpectNotAliased(in v1, in v2);
                    // ExpectNotAliased(in v1, in v3);
                    // ExpectNotAliased(in v2, in v3);

                    var i1 = 0;
                    var i2 = 0;

                    for (var i3 = 0; i3 < l1 + l2; ++i3)
                    {
                        if (i1 < l1 && (i2 >= l2 || t1[i1] <= t2[i2]))
                        {
                            t3[i3] = t1[i1];
                            v3[i3] = v1[i1];
                            ++i1;
                        }
                        else
                        {
                            t3[i3] = t2[i2];
                            v3[i3] = v2[i2];
                            ++i2;
                        }
                    }
                }
            }
        }
    }
}