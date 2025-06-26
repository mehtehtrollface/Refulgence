using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Refulgence.IO;

public sealed class SubStreamOrchestrator(long baseOffset = 0L)
{
    private readonly List<Stream>             _subStreams      = [];
    private readonly List<DelayedPointerImpl> _delayedPointers = [];

    private int FindExistingSubStream(Stream subStream, string message, string paramName)
    {
        var index = _subStreams.IndexOf(subStream);
        if (index < 0) {
            throw new ArgumentException(message, paramName);
        }

        return index;
    }

    private void ValidateNewSubStreams(scoped ReadOnlySpan<Stream> subStreams, string paramName)
    {
        foreach (var subStream in subStreams) {
            if (!subStream.CanSeek) {
                throw new ArgumentException("Only seekable streams can be added", paramName);
            }

            if (_subStreams.Contains(subStream)) {
                throw new ArgumentException("This sub-stream has already been added", paramName);
            }
        }
    }

    public void AddSubStreams(params ReadOnlySpan<Stream> newSubStreams)
    {
        if (newSubStreams.Length == 0) {
            return;
        }

        ValidateNewSubStreams(newSubStreams, nameof(newSubStreams));
        _subStreams.AddRange(newSubStreams);
    }

    public void InsertSubStreamsBefore(Stream existingSubStream, params ReadOnlySpan<Stream> newSubStreams)
    {
        if (newSubStreams.Length == 0) {
            return;
        }

        ValidateNewSubStreams(newSubStreams, nameof(newSubStreams));
        var index = FindExistingSubStream(existingSubStream, "Unrecognized sub-stream", nameof(existingSubStream));
        _subStreams.InsertRange(index, newSubStreams);
        for (var i = 0; i < _delayedPointers.Count; ++i) {
            var pointer = _delayedPointers[i];
            if (pointer.PointerStream >= index) {
                if (pointer.PointeeStream >= index) {
                    _delayedPointers[i] = pointer with
                    {
                        PointerStream = pointer.PointerStream + newSubStreams.Length,
                        PointeeStream = pointer.PointeeStream + newSubStreams.Length,
                    };
                } else {
                    _delayedPointers[i] = pointer with
                    {
                        PointerStream = pointer.PointerStream + newSubStreams.Length,
                    };
                }
            } else if (pointer.PointeeStream >= index) {
                _delayedPointers[i] = pointer with
                {
                    PointeeStream = pointer.PointeeStream + newSubStreams.Length,
                };
            }
        }
    }

    public unsafe DelayedPointer WriteDelayedPointer<T>(Stream pointerStream, Stream pointeeStream, long pointeePosition)
        where T : unmanaged, IBinaryInteger<T>
    {
        var pointerStreamIdx = FindExistingSubStream(pointerStream, "Pointer stream is not a sub-stream", nameof(pointerStream));
        var pointeeStreamIdx = FindExistingSubStream(pointeeStream, "Pointee stream is not a sub-stream", nameof(pointeeStream));

        var pointerPosition = pointerStream.Position;
        pointerStream.Write(default(T));
        var pointerIdx = _delayedPointers.Count;
        _delayedPointers.Add(new(pointerStreamIdx, pointerPosition, sizeof(T), pointeeStreamIdx, pointeePosition));

        return new(this, pointerIdx);
    }

    public void WriteAllTo(Stream destination)
    {
        var subStreamOffsets = new long[_subStreams.Count];
        subStreamOffsets[0] = baseOffset;
        for (var i = 1; i < _subStreams.Count; ++i) {
            subStreamOffsets[i] = subStreamOffsets[i - 1] + _subStreams[i - 1].Length;
        }

        var savedPositions = new long[_subStreams.Count];
        for (var i = 0; i < _subStreams.Count; ++i) {
            savedPositions[i] = _subStreams[i].Position;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(long));
        try {
            foreach (var pointer in _delayedPointers) {
                BinaryPrimitives.WriteInt64LittleEndian(buffer, subStreamOffsets[pointer.PointeeStream] + pointer.PointeePosition);
                var pointerStream = _subStreams[pointer.PointerStream];
                pointerStream.Position = pointer.PointerPosition;
                pointerStream.Write(buffer, 0, pointer.PointerSize);
            }

            foreach (var stream in _subStreams) {
                if (stream is MemoryStream memoryStream) {
                    memoryStream.WriteTo(destination);
                } else {
                    stream.Position = 0L;
                    stream.CopyTo(destination);
                }
            }
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
            for (var i = 0; i < _subStreams.Count; ++i) {
                _subStreams[i].Position = savedPositions[i];
            }
        }
    }

    public readonly struct DelayedPointer
    {
        private readonly SubStreamOrchestrator _orchestrator;
        private readonly int                   _pointerIdx;

        public long PointeePosition
        {
            get => _orchestrator._delayedPointers[_pointerIdx].PointeePosition;
            set => _orchestrator._delayedPointers[_pointerIdx] = _orchestrator._delayedPointers[_pointerIdx] with
            {
                PointeePosition = value,
            };
        }

        internal DelayedPointer(SubStreamOrchestrator orchestrator, int pointerIdx)
        {
            _orchestrator = orchestrator;
            _pointerIdx = pointerIdx;
        }
    }

    private readonly record struct DelayedPointerImpl(
        int PointerStream,
        long PointerPosition,
        int PointerSize,
        int PointeeStream,
        long PointeePosition);
}
