using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.InputSystem.DataPipeline
{
    [BurstCompile]
    internal struct BurstedDataPipeline : IJob
    {
        public Dataset dataset;
        public DataPipeline dataPipeline;
        
        public void Execute()
        {
            dataPipeline.Execute(dataset);
        }
    }
    
    [BurstCompile(CompileSynchronously = true)]
    internal struct TestJob : IJob
    {
        public UnsafeResizableNativeArray array;
        public void Execute()
        {
            array.Realloc(1024 * 2);
            array.Realloc(1024 * 4);
            array.Realloc(1024 * 8);
            array.Realloc(1024 * 16);
            array.Realloc(1024 * 32);
            array.Realloc(1024 * 64);
            array.Realloc(1024 * 128);
        }
    }
}