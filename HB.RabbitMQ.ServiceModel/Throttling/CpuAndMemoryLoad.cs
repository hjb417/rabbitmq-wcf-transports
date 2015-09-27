namespace HB.RabbitMQ.ServiceModel.Throttling
{
    internal struct CpuAndMemoryLoad
    {
        private readonly float _cpu;
        private readonly float _mem;

        public CpuAndMemoryLoad(float cpu, float memory)
        {
            _cpu = cpu;
            _mem = memory;
        }

        public float Cpu { get { return _cpu; } }
        public float Memory { get { return _mem; } }
    }
}