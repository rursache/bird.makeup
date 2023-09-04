namespace BirdsiteLive.Twitter.Tools;
using System;

public class TwitterSnowflakeGenerator
{
    private const long Twepoch = 1288834974657L;
    private const int MachineIdBits = 5;
    private const int DatacenterIdBits = 5;
    private const int SequenceBits = 12;

    private const long MaxMachineId = (1 << MachineIdBits) - 1;
    private const long MaxDatacenterId = (1 << DatacenterIdBits) - 1;
    private const long MaxSequence = (1 << SequenceBits) - 1;

    private readonly long machineId;
    private readonly long datacenterId;
    private long lastTimestamp = -1L;
    private long sequence = 0L;

    public TwitterSnowflakeGenerator(long machineId, long datacenterId)
    {
        if (machineId < 0 || machineId > MaxMachineId)
        {
            throw new ArgumentException($"Machine ID must be between 0 and {MaxMachineId}");
        }

        if (datacenterId < 0 || datacenterId > MaxDatacenterId)
        {
            throw new ArgumentException($"Datacenter ID must be between 0 and {MaxDatacenterId}");
        }

        this.machineId = machineId;
        this.datacenterId = datacenterId;
    }

    public long NextId()
    {
        long timestamp = CurrentTimestamp();

        if (timestamp < lastTimestamp)
        {
            throw new InvalidOperationException("Clock moved backwards. Refusing to generate ID.");
        }

        if (timestamp == lastTimestamp)
        {
            sequence = (sequence + 1) & MaxSequence;
            if (sequence == 0)
            {
                timestamp = WaitNextMillis(lastTimestamp);
            }
        }
        else
        {
            sequence = 0;
        }

        lastTimestamp = timestamp;

        long id = ((timestamp - Twepoch) << (MachineIdBits + DatacenterIdBits + SequenceBits)) |
                  (datacenterId << (MachineIdBits + SequenceBits)) |
                  (machineId << SequenceBits) |
                  sequence;

        return id;
    }

    private static long CurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private static long WaitNextMillis(long lastTimestamp)
    {
        long timestamp = CurrentTimestamp();
        while (timestamp <= lastTimestamp)
        {
            timestamp = CurrentTimestamp();
        }
        return timestamp;
    }
}