using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Refulgence.Text;

namespace Refulgence.Interop;

[SupportedOSPlatform("windows")]
public static class D3DCompiler
{
    private static readonly
        ConcurrentDictionary<nint, ConcurrentDictionary<nint, (ISourceFile SourceFile, GCHandle? GCHandle)>> Includes = [];

    private static readonly unsafe ID3DInclude_Open  IncludeOpenDelegate  = IncludeOpen;
    private static readonly        ID3DInclude_Close IncludeCloseDelegate = IncludeClose;

    private static readonly nint[] D3DIncludeVtbl =
    [
        Marshal.GetFunctionPointerForDelegate(IncludeOpenDelegate), Marshal.GetFunctionPointerForDelegate(IncludeCloseDelegate),
    ];

    private static unsafe int IncludeOpen(nint self, uint includeType, byte* pFileName, nint pParentData,
        out nint ppData, out uint pBytes)
    {
        try {
            var parentSource = Includes[self][pParentData].SourceFile;
            var includedSource = parentSource.Include(
                (IncludeType)includeType,
                Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(pFileName))
            );
            (ppData, pBytes) = PinSourceFile(self, includedSource);

            return 0;
        } catch (Exception e) {
            ppData = 0;
            pBytes = 0;

            return Marshal.GetHRForException(e);
        }
    }

    private static int IncludeClose(nint self, nint pData)
    {
        try {
            if (Includes.TryGetValue(
                    self,
                    out var includes
                ) &&
                includes.TryRemove(pData, out var entry)) {
                entry.GCHandle?.Free();
                (entry.SourceFile as IDisposable)?.Dispose();
            }

            return 0;
        } catch (Exception e) {
            return Marshal.GetHRForException(e);
        }
    }

    private static (nint Address, uint Length) PinSourceFile(nint d3dInclude, ISourceFile file)
    {
        var contents = file.Contents.ToArray();
        var gcHandle = GCHandle.Alloc(contents, GCHandleType.Pinned);
        try {
            var address = gcHandle.AddrOfPinnedObject();
            if (!Includes[d3dInclude].TryAdd(address, (file, gcHandle))) {
                throw new InvalidOperationException();
            }

            return (address, (uint)contents.Length);
        } catch {
            gcHandle.Free();
            throw;
        }
    }

    private static (byte[] Bytes, long[] Offsets) MarshalDefines(IReadOnlyDictionary<string, string>? defines)
    {
        if (defines is null) {
            return ([], []);
        }

        var utf8 = Encoding.UTF8;
        using var definesStream = new MemoryStream();
        var offsets = new List<long>(defines.Count * 2);
        foreach (var (name, definition) in defines) {
            offsets.Add(definesStream.Length);
            definesStream.Write(utf8.GetBytes(name));
            definesStream.WriteByte(0);
            offsets.Add(definesStream.Length);
            definesStream.Write(utf8.GetBytes(definition));
            definesStream.WriteByte(0);
        }

        return (definesStream.ToArray(), offsets.ToArray());
    }

    public static (byte[] ShaderBlob, string ErrorMessages) Compile(ISourceFile source, string target,
        string? sourceName = null, IReadOnlyDictionary<string, string>? defines = null, string entrypoint = "main",
        D3DCompileFlags flags = 0, D3DEffectFlags effectFlags = 0, D3DSecondaryDataFlags secondaryDataFlags = 0)
        => Compile(source, target, [], sourceName, defines, entrypoint, flags, effectFlags, secondaryDataFlags);

    public static (byte[] ShaderBlob, string ErrorMessages) Compile(ReadOnlySpan<byte> source, string target,
        string? sourceName = null, IReadOnlyDictionary<string, string>? defines = null, string entrypoint = "main",
        D3DCompileFlags flags = 0, D3DEffectFlags effectFlags = 0, D3DSecondaryDataFlags secondaryDataFlags = 0)
        => Compile(source, target, [], sourceName, defines, entrypoint, flags, effectFlags, secondaryDataFlags);

    public static unsafe (byte[] ShaderBlob, string ErrorMessages) Compile(ISourceFile source, string target,
        ReadOnlySpan<byte> secondaryData, string? sourceName = null,
        IReadOnlyDictionary<string, string>? defines = null, string entrypoint = "main", D3DCompileFlags flags = 0,
        D3DEffectFlags effectFlags = 0, D3DSecondaryDataFlags secondaryDataFlags = 0)
    {
        var (defineBytes, defineOffsets) = MarshalDefines(defines);
        var code = new BlobWrapper();
        var errorMsgs = new BlobWrapper();
        string errorMessages;
        nint includesKey = 0;
        try {
            fixed (byte* pSecondaryData = secondaryData) {
                fixed (byte* pDefineBytes = defineBytes) {
                    var definePtrs = new byte*[defineOffsets.Length + 2];
                    for (var i = 0; i < defineOffsets.Length; i++) {
                        definePtrs[i] = pDefineBytes + defineOffsets[i];
                    }

                    fixed (byte** pDefines = definePtrs) {
                        fixed (nint* pIncludeVtbl = D3DIncludeVtbl) {
                            var d3dInclude = stackalloc nint*[1];
                            d3dInclude[0] = pIncludeVtbl;
                            includesKey = new(d3dInclude);
                            var includes = new ConcurrentDictionary<nint, (ISourceFile, GCHandle?)>();
                            includes.TryAdd(0, (source, null));
                            if (!Includes.TryAdd(includesKey, includes)) {
                                throw new InvalidOperationException();
                            }

                            var (pSrcData, srcDataLength) = PinSourceFile(includesKey, source);
                            var hr = D3DCompile2(
                                (byte*)pSrcData.ToPointer(), srcDataLength, sourceName, pDefines,
                                d3dInclude,                  entrypoint,    target,     (uint)flags,
                                (uint)effectFlags,
                                secondaryData.IsEmpty ? 0 : (uint)secondaryDataFlags,
                                secondaryData.IsEmpty ? null : pSecondaryData, (nuint)secondaryData.Length,
                                out code.Blob,                                 out errorMsgs.Blob
                            );
                            errorMessages = Encoding.UTF8.GetString(errorMsgs.AsSpan());
                            if (hr < 0) {
                                throw new D3DCompilerException(errorMessages, Marshal.GetExceptionForHR(hr));
                            }
                        }
                    }
                }
            }

            return (code.ToByteArray(), errorMessages);
        } finally {
            if (0 != includesKey && Includes.TryRemove(
                    includesKey,
                    out var includes
                )) {
                foreach (var (_, gcHandle) in includes.Values) {
                    gcHandle?.Free();
                }
            }

            errorMsgs.Dispose();
            code.Dispose();
        }
    }

    public static unsafe (byte[] ShaderBlob, string ErrorMessages) Compile(ReadOnlySpan<byte> source, string target,
        ReadOnlySpan<byte> secondaryData, string? sourceName = null,
        IReadOnlyDictionary<string, string>? defines = null, string entrypoint = "main", D3DCompileFlags flags = 0,
        D3DEffectFlags effectFlags = 0, D3DSecondaryDataFlags secondaryDataFlags = 0)
    {
        var (defineBytes, defineOffsets) = MarshalDefines(defines);
        var code = new BlobWrapper();
        var errorMsgs = new BlobWrapper();
        string errorMessages;
        try {
            fixed (byte* pSecondaryData = secondaryData) {
                fixed (byte* pDefineBytes = defineBytes) {
                    var definePtrs = new byte*[defineOffsets.Length + 2];
                    for (var i = 0; i < defineOffsets.Length; i++) {
                        definePtrs[i] = pDefineBytes + defineOffsets[i];
                    }

                    fixed (byte** pDefines = definePtrs) {
                        int hr;
                        fixed (byte* pSrcData = source) {
                            hr = D3DCompile2(
                                pSrcData, (uint)source.Length, sourceName, pDefines,
                                null,     entrypoint,          target,     (uint)flags,
                                (uint)effectFlags,
                                secondaryData.IsEmpty ? 0 : (uint)secondaryDataFlags,
                                secondaryData.IsEmpty ? null : pSecondaryData, (nuint)secondaryData.Length,
                                out code.Blob,                                 out errorMsgs.Blob
                            );
                        }

                        errorMessages = Encoding.UTF8.GetString(errorMsgs.AsSpan());
                        if (hr < 0) {
                            throw new D3DCompilerException(errorMessages, Marshal.GetExceptionForHR(hr));
                        }
                    }
                }
            }

            return (code.ToByteArray(), errorMessages);
        } finally {
            errorMsgs.Dispose();
            code.Dispose();
        }
    }

    public static unsafe Utf8Bytes Disassemble(ReadOnlySpan<byte> blob, D3DDisassembleFlags flags = 0,
        string comments = "")
    {
        var disassembly = new BlobWrapper();
        try {
            fixed (byte* pSrcData = blob) {
                var hr = D3DDisassemble(pSrcData, (uint)blob.Length, (uint)flags, comments, out disassembly.Blob);
                Marshal.ThrowExceptionForHR(hr);
            }

            return new(disassembly.ToByteArray());
        } finally {
            disassembly.Dispose();
        }
    }

    [PreserveSig]
    [DllImport("D3DCompiler_47.dll")]
    private static extern unsafe int D3DCompile2(
        [In] byte* pSrcData,
        [In] nuint srcDataSize,
        [MarshalAs(UnmanagedType.LPStr)] string? pSourceName,
        [In] byte** pDefines,
        [In] void* pInclude,
        [MarshalAs(UnmanagedType.LPStr)] string? pEntrypoint,
        [MarshalAs(UnmanagedType.LPStr)] string pTarget,
        uint flags1,
        uint flags2,
        uint secondaryDataFlags,
        [In] byte* pSecondaryData,
        [In] nuint secondaryDataSize,
        out ID3DBlob? ppCode,
        out ID3DBlob? ppErrorMsgs);

    [PreserveSig]
    [DllImport("D3DCompiler_47.dll")]
    private static extern unsafe int D3DDisassemble(
        [In] byte* pSrcData,
        [In] nuint srcDataSize,
        uint flags,
        [MarshalAs(UnmanagedType.LPStr)] string szComments,
        out ID3DBlob? ppDisassembly);

    private unsafe delegate int ID3DInclude_Open(nint self, uint includeType, byte* pFileName, nint pParentData,
        out nint ppData, out uint pBytes);

    private delegate int ID3DInclude_Close(nint self, nint pData);

    [Guid("8BA5FB08-5195-40e2-AC58-0D989C3A0102")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ID3DBlob
    {
        [PreserveSig]
        public unsafe void* GetBufferPointer();

        [PreserveSig]
        public UIntPtr GetBufferSize();
    }

    private ref struct BlobWrapper
    {
        public ID3DBlob? Blob;

        public readonly unsafe ReadOnlySpan<byte> AsSpan()
            => Blob is null
                ? []
                : new ReadOnlySpan<byte>(Blob.GetBufferPointer(), (int)Blob.GetBufferSize());

        public readonly byte[] ToByteArray()
            => AsSpan().ToArray();

        public readonly void Dispose()
        {
            if (Blob is not null) {
                Marshal.FinalReleaseComObject(Blob);
            }
        }
    }
}
