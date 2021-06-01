using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.DataPipeline
{
    /*
    // helper class to calculate and manage memory allocation for the dataset
    [BurstCompile]
    public unsafe struct DatasetPlanner
    {
        public enum StepsDependencyType
        {
            // Every sample maps to every sample
            NToN,

            // N and M map to N+M
            NAndMToNPlusM
        }
        
        // TODO can also of this be based on all kernels we have?
        
        public struct IngressStepFunctions
        {
            public IStepFunctionWithActuationNotion test;
            public int srcOpaqueValueStride;
        }

        public struct StepsFunctionDependency
        {
            public StepsDependencyType type;
            public int src1, src2;
            public int dst;
            public int dstOpaqueValueStride;
        }

        // public struct DatasetAllocationPlan
        // {
        //     var timestampsCount = 0;
        //     var valuesCount = 0;
        //     var opaqueValuesBytes = 0;
        // }

        public NativeArray<IngressStepFunctions> ingress;
        public NativeArray<StepsFunctionDependency> dependencies;

        [BurstCompile]
        public Dataset Plan(Dataset dataset)
        {
            var timestampsCount = 0;
            var valuesCount = 0;
            var opaqueValuesBytes = 0;

            foreach (var ing in ingress)
            {
                var l = dataset.lengths[ing.src];
                timestampsCount += l;
                if (ing.srcOpaqueValueStride > 0)
                    opaqueValuesBytes += ing.srcOpaqueValueStride * (l + 1); // padding
                else
                    valuesCount += l;
            }

            foreach (var dep in dependencies)
            {
                switch (dep.type)
                {
                    case StepsDependencyType.NToN:
                    {
                        var l = dataset.lengths[dep.src1];
                        dataset.lengths[dep.dst] = l;
                        timestampsCount += l;
                        if (dep.dstOpaqueValueStride > 0)
                            opaqueValuesBytes += dep.dstOpaqueValueStride * (l + 1);
                        else
                            valuesCount += l;
                        break;
                    }
                    case StepsDependencyType.NAndMToNPlusM:
                    {
                        var l1 = dataset.lengths[dep.src1];
                        var l2 = dataset.lengths[dep.src2];
                        var l = l1 + l2;
                        dataset.lengths[dep.dst] = l;
                        timestampsCount += l;
                        if (dep.dstOpaqueValueStride > 0)
                            opaqueValuesBytes += dep.dstOpaqueValueStride * (l + 1);
                        else
                            valuesCount += l;
                        break;
                    }
                }
            }

            // TODO move allocation to dataset
            if (timestampsCount > dataset.timestampsAllocCount)
            {
                UnsafeUtility.Free(dataset.timestamps, Allocator.Persistent);
                
                dataset.timestamps = (ulong*)UnsafeUtility.Malloc(timestampsCount * sizeof(ulong), 16, Allocator.Persistent);
                dataset.timestampsAllocCount = timestampsCount;
            }
            
            if (valuesCount > dataset.valuesAllocCount)
            {
                UnsafeUtility.Free(dataset.values, Allocator.Persistent);
                
                dataset.values = (float*)UnsafeUtility.Malloc(valuesCount * sizeof(float), 16, Allocator.Persistent);
                dataset.valuesAllocCount = valuesCount;
            }

            if (opaqueValuesBytes > dataset.valuesOpaqueAllocSize)
            {
                UnsafeUtility.Free(dataset.valuesOpaque, Allocator.Persistent);
                
                dataset.valuesOpaque = UnsafeUtility.Malloc(opaqueValuesBytes, 16, Allocator.Persistent);
                dataset.valuesOpaqueAllocSize = opaqueValuesBytes;
            }
            return dataset;
        }
    }
    */
}