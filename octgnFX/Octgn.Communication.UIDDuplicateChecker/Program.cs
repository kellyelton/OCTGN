using Octgn.Communication.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Octgn.Communication.UIDDuplicateChecker
{
    class Program
    {
        static void Main(string[] args) {
            try {
                RunTest2();
            } catch (Exception ex) {
                Console.Error.WriteLine($"ERROR: {ex}");
            }

            Console.WriteLine();
            Console.WriteLine("Done");

            Console.ReadKey();
        }

        static void RunTest1() {
            var maxValue = Int32.MaxValue;

            var list =
                Enumerable.Range(0, maxValue)
                .AsParallel()
                .Select(Generate)
                .OrderBy(x => x)
            ;

            var hs = new HashSet<string>(list);
        }

        static void RunTest1_1() {
            var maxValue = Int32.MaxValue;

            var list =
                Enumerable.Range(0, maxValue)
                .AsParallel()
                .Select(Generate)
                .ToArray()
            ;

            for(var i = 0; i < list.Length; i++) {
                var current = list[i];

                if (i == list.Length - 1)
                    break;

                for (var i2 = i + 1; i2 < list.Length; i2++) {
                    var next = list[i2];

                    if (current == next) {
                        Console.Error.WriteLine($"Duplicate Found '{current}' at index {i} and {i2}");
                        break;
                    }
                }
            }
        }

        private const int MAX = Int32.MaxValue;

        private static string _numberPath = Path.Combine(Path.GetTempPath(), "NoDuplicates.numbers.txt");

        static void RunTest2() {
            var path = _numberPath;

            if (!File.Exists(path)) {
                Console.WriteLine($"Creating file at {path}");

                using (var stream = File.Create(path))
                using (var writer = new StreamWriter(stream, Encoding.ASCII)) {
                    for (var i = 0; i < MAX; i++) {
                        var uid = UID.Generate(i);

                        writer.WriteLine(uid);
                    }
                }
            }

            Console.WriteLine($"Finding duplicates...");
            using (var stream = File.OpenRead(path))
            using (var streamReader = new StreamReader(stream)) {
                for(var i = 0;i<MAX;i++) {
                    if (i == MAX - 1) break;

                    if ((i % 1000000) == 0) {
                        var percent = ((decimal)i / Int32.MaxValue) * 100;

                        Console.WriteLine($"{percent:00.00}%    {i} / {Int32.MaxValue}");
                    }

                    var left = ReadLine(stream, i);

                    MoveToLine(stream, i + 1);

                    var lineQueue = new ConcurrentQueue<byte[]>();

                    var readLinesTask = Task.Run(() => {
                        for (var rightIndex = i + 1; rightIndex < MAX; rightIndex++) {
                            var bytes = new byte[4100];

                            var cnt = stream.Read(bytes, 0, 4100);

                            for(var c = 0; c < 410; c++) {
                                var chunk = new byte[8];
                                Array.Copy(bytes, c * 10, chunk, 0, 8);

                                lineQueue.Enqueue(chunk);
                            }

                            stream.Position += 2;
                        }
                    });

                    var itemCount = MAX - (i + 1);
                    var half = itemCount / 2;
                    var midway = i + 1 + half;


                    Task.Run(() => {
                        for (var rightIndex = i + 1; rightIndex < midway; rightIndex++) {
                            while (lineQueue.Count == 0) {
                                Thread.Yield();
                            }

                            lineQueue.TryDequeue(out var chunk);

                            var right = Encoding.UTF8.GetString(chunk, 0, 8);

                            if (left == right) {
                                Console.Error.WriteLine($"Duplicate Found '{left}' at index {i} and {rightIndex}");
                                return;
                            }
                        }
                    });

                    for (var rightIndex = midway; rightIndex < MAX; rightIndex++) {
                        while (lineQueue.Count == 0) {
                            Thread.Yield();
                        }

                        lineQueue.TryDequeue(out var chunk);

                        var right = Encoding.UTF8.GetString(chunk, 0, 8);

                        if (left == right) {
                            Console.Error.WriteLine($"Duplicate Found '{left}' at index {i} and {rightIndex}");
                            return;
                        }
                    }
                }
            }
        }

        private static byte[] _readBuffer = new byte[8];
        private const int LineLength = 10;

        private static string ReadLine(Stream stream, int line) {
            stream.Position = line * LineLength;

            var cnt = stream.Read(_readBuffer, 0, 8);

            return Encoding.UTF8.GetString(_readBuffer, 0, cnt);
        }

        private static void MoveToLine(Stream stream, int line) {
            stream.Position = line * LineLength;
        }


        private static volatile int _count = 0;

        static string Generate(int from) {
            var current = Interlocked.Increment(ref _count);

            if((current % 10000000) == 0) {
                var percent = ((decimal)current / Int32.MaxValue) * 100;

                Console.WriteLine($"{percent:00.00}%    {current} / {Int32.MaxValue}");
            }

            return UID.Generate(from);
        }
    }
}
