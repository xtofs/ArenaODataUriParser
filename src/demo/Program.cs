using System.Buffers;
using System.IO;
using ODataUriArenaParser.Binding;
using ODataUriArenaParser.Syntax;

using var arena = MemoryPool<byte>.Shared.Rent(65536);
ReadOnlySpan<byte> input = "ame eq 'foo'"u8;

// Changed per request: warm up JIT and one-time runtime paths before collecting allocation numbers.
_ = ArenaParser.Parse(arena, input);

var m0 = AllocationMeasurement.Start();

var m1 = AllocationMeasurement.Start();
var syntax = ArenaParser.Parse(arena, input);
var parsingAllocatedBytes = m1.Stop();

var m2 = AllocationMeasurement.Start();
// Measure serializer cost without Console.Out side effects.
syntax.Write(TextWriter.Null);
var writingAllocatedBytes = m2.Stop();

var m3 = AllocationMeasurement.Start();
var semantic = syntax.Bind();
var bindingAllocatedBytes = m3.Stop();

// dispose the arena after all operations are done 
arena.Dispose();

var totalAllocatedBytes = m0.Stop();

// Changed per request: all Console.WriteLine calls now happen after every Stop call.
Console.WriteLine();
syntax.Write(Console.Out);
Console.WriteLine();
Console.WriteLine("measuring parsing allocations and syntax graph materialization");
Console.WriteLine($"parsing allocated bytes: {parsingAllocatedBytes}");
Console.WriteLine();

Console.WriteLine("measuring serialization allocations");
Console.WriteLine($"writing allocated bytes: {writingAllocatedBytes}");
Console.WriteLine();

Console.WriteLine("measuring binding allocations and semantic graph materialization");
Console.WriteLine(semantic);
Console.WriteLine($"binding allocated bytes: {bindingAllocatedBytes}");
Console.WriteLine();

Console.WriteLine($"total allocated bytes: {totalAllocatedBytes}");