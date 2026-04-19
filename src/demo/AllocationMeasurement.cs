
internal static class AllocationMeasurement
{
    internal static Measurement Start()
    {
        var m = new Measurement();
        m.before = GC.GetAllocatedBytesForCurrentThread();
        return m;
    }

    public struct Measurement
    {
        internal long before;

        public readonly long Stop()
        {
            var after = GC.GetAllocatedBytesForCurrentThread();
            return after - before;
        }
    }
}