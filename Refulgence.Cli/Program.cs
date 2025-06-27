using System.Diagnostics.CodeAnalysis;
using Refulgence.Cli.Programs;

var argIndex = 0;

return GetArg("verb").ToLowerInvariant() switch
{
    "dump" => Dump.Run(GetArg("input file name")),
    "roundtrip" => RoundTripTest.Run(
        GetArg("input file name"), TryGetArg(out var outputFileName, "output file name") ? outputFileName : string.Empty
    ),
    "shcd.make" or "mkshcd" => ShaderCodeMake.Run(GetArg("input file name"), GetArg("output file name")),
    "shcd.extract" or "unshcd" => ShaderCodeExtract.Run(GetArg("input file name"), GetArg("output file name")),
    "shpk.extract" or "unshpk" => ShaderPackageExtract.Run(GetArg("input file name"), GetArgRest(), false),
    "shpk.extract.shcd" or "unshpk.shcd" => ShaderPackageExtract.Run(GetArg("input file name"), GetArgRest(), true),
    "shpk.update" or "shpkupdate" => ShaderPackageUpdate.Run(GetArg("input file name"), GetArg("output file name"), GetArgRest()),
    "xivcrc32" or "xivcrc" => XivCrc32.Run(GetArgRest()),
    var verb => UnrecognizedVerb(verb),
};

bool TryGetArg([NotNullWhen(true)] out string? arg, string description)
{
    if (args.Length > argIndex) {
        arg = args[argIndex++];
        return true;
    }

    Console.Error.WriteLine($"Missing argument, expected {description}");
    arg = null;
    return false;
}

string GetArg(string description)
{
    if (!TryGetArg(out var arg, description)) {
        Environment.Exit(2);
    }

    return arg;
}

ReadOnlySpan<string> GetArgRest()
    => args.AsSpan(argIndex);

static int UnrecognizedVerb(string verb)
{
    Console.WriteLine($"Unrecognized verb {verb}");
    return 2;
}

