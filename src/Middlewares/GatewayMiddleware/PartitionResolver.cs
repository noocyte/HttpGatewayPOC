using System;
using System.Data.HashFunction;

namespace Gateway.Admin.Middlewares.GatewayMiddleware
{
    public static class PartitionResolver
    {
        private const uint DefaultSeed = 54325U;
        private const int DefaultPartitionCount = 128;

        public static long Resolve(string input)
        {
            return Resolve(input, DefaultPartitionCount);
        }

        public static long Resolve(string input, int partitions)
        {
            var hasher = new MurmurHash3(32, DefaultSeed);
            var hashRaw = hasher.ComputeHash(input);
            var fullHash = BitConverter.ToUInt32(hashRaw, 0);

            var hash = fullHash % partitions;

            return hash;
        }
    }
}