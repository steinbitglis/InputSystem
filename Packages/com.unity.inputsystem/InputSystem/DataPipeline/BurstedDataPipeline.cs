using Unity.Burst;
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
}