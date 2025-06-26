namespace Refulgence.Sm5;

[Flags]
public enum SyncFlags : byte
{
    ThreadsInGroup                  = 0x1,
    ThreadGroupSharedMemory         = 0x2,
    UnorderedAccessViewMemoryGroup  = 0x4,
    UnorderedAccessViewMemoryGlobal = 0x8,
}
