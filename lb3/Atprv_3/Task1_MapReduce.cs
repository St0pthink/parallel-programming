using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace lb3.Task1
{
    public readonly struct MapReduceProgress
    {
        public long TotalBytes { get; }
        public long ProcessedBytes { get; }
        public string CurrentFile { get; }
        public MapReduceProgress(long total, long done, string currentFile)
        { TotalBytes = total; ProcessedBytes = done; CurrentFile = currentFile; }
    }

    internal sealed class FileSegment
    {
        public string Path { get; }
        public long Start { get; }
        public long Length { get; }
        public FileSegment(string path, long start, long length)
        { Path = path; Start = start; Length = length; }
    }

    public sealed class TextMapReduceProcessor
    {
        private static readonly Regex RxWord = new(@"[\p{L}\p{Nd}_]+", RegexOptions.Compiled);

        public IDictionary<string, long> ProcessFiles(
            IEnumerable<string> filePaths,
            int degreeOfParallelism,
            IProgress<MapReduceProgress> progress,
            CancellationToken ct)
        {
            var files = filePaths.Where(File.Exists).ToArray();
            long totalBytes = files.Sum(f => new FileInfo(f).Length);
            if (totalBytes == 0) return new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            int chunkSize = 1 << 20;

            long targetChunks = Math.Max(4L * degreeOfParallelism, 32);
            if (totalBytes / chunkSize < targetChunks)
            {
                long suggested = totalBytes / targetChunks;
                chunkSize = (int)Math.Max(64 * 1024, Math.Min(int.MaxValue, suggested));
            }
            var plan = BuildSegmentsUniform(files, chunkSize);

            var counts = new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            long processedSum = 0;
            object progressLock = new();

            var po = new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism, CancellationToken = ct };

            Parallel.ForEach(plan, po, seg =>
            {
                ct.ThrowIfCancellationRequested();

                ProcessSegment(seg, counts, bytesAdvanced =>
                {
                    lock (progressLock)
                    {
                        processedSum += bytesAdvanced;
                        progress?.Report(new MapReduceProgress(totalBytes, processedSum, Path.GetFileName(seg.Path)));
                    }
                }, ct);
            });

            return counts;
        }

        private static List<FileSegment> BuildSegmentsUniform(string[] files, int chunkSizeBytes)
        {
            var segments = new List<FileSegment>(capacity: files.Length * 8);

            foreach (var path in files)
            {
                long len = new FileInfo(path).Length;
                if (len <= 0) continue;

                for (long pos = 0; pos < len; pos += chunkSizeBytes)
                {
                    long segLen = Math.Min(chunkSizeBytes, len - pos);
                    segments.Add(new FileSegment(path, pos, segLen));
                }
            }
            return segments;
        }

        private static void ProcessSegment(
    FileSegment seg,
    ConcurrentDictionary<string, long> counts,
    Action<long> report,
    CancellationToken ct)
        {
            const int ByteBufSize = 64 * 1024;

            using var fs = new FileStream(seg.Path, FileMode.Open, FileAccess.Read, FileShare.Read, ByteBufSize, useAsync: false);

            // кодировка BOM - UTF8/16/32 иначе UTF-8 без BOM
            var encoding = DetectEncoding(fs) ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            //выравнивание начало сегмента 
            AlignStartToCharBoundary(fs, encoding, seg.Start, out long alignedStart);
            bool skipHeadInsideWord = PrevCharIsWord(fs, encoding, alignedStart);

            fs.Position = alignedStart;

            var decoder = encoding.GetDecoder();
            byte[] byteBuf = ArrayPool<byte>.Shared.Rent(ByteBufSize);
            char[] charBuf = ArrayPool<char>.Shared.Rent(encoding.GetMaxCharCount(ByteBufSize));

            try
            {
                long endExclusive = seg.Start + seg.Length; // границу сегмента не трогаем
                long bytesPos = alignedStart;

                string carry = null;

                while (bytesPos < endExclusive)
                {
                    ct.ThrowIfCancellationRequested();

                    int toRead = (int)Math.Min(ByteBufSize, endExclusive - bytesPos);
                    int read = fs.Read(byteBuf, 0, toRead);
                    if (read <= 0) break;

                    bytesPos += read;
                    report(read);

                    // корректная декодировка рваных символов
                    decoder.Convert(byteBuf, 0, read, charBuf, 0, charBuf.Length,
                                    flush: false, out int _, out int charsUsed, out bool _);

                    var span = new ReadOnlySpan<char>(charBuf, 0, charsUsed);

                    
                    if (skipHeadInsideWord)
                    {
                        int i = 0;
                        while (i < span.Length && IsWordChar(span[i])) i++;
                        if (i < span.Length && !IsWordChar(span[i]))
                        {
                            
                            skipHeadInsideWord = false;
                            span = span.Slice(i + 1);
                            decoder.Reset();
                        }
                        else
                        {
                            continue;
                        }
                    }

                    int idx = 0;

                    if (carry != null && !span.IsEmpty)
                    {
                        if (IsWordChar(span[0]))
                        {
                            int j = 0;
                            while (j < span.Length && IsWordChar(span[j])) j++;
                            carry += span.Slice(0, j).ToString().ToLowerInvariant();

                            if (j < span.Length) 
                            {
                                counts.AddOrUpdate(carry, 1, (_, cur) => cur + 1);
                                carry = null;
                                idx = j + 1;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            counts.AddOrUpdate(carry, 1, (_, cur) => cur + 1);
                            carry = null;
                            idx = 1;
                        }
                    }

                    // основной парсер по span
                    while (idx < span.Length)
                    {
                        while (idx < span.Length && !IsWordChar(span[idx])) idx++;
                        if (idx >= span.Length) break;

                        int start = idx;
                        while (idx < span.Length && IsWordChar(span[idx])) idx++;

                        if (idx == span.Length)
                        {
                            carry = span.Slice(start).ToString().ToLowerInvariant();
                            break;
                        }
                        else
                        {
                            string word = span.Slice(start, idx - start).ToString().ToLowerInvariant();
                            counts.AddOrUpdate(word, 1, (_, cur) => cur + 1);
                        }
                    }
                }

                if (carry != null)
                {
                    int safetyReads = 0;
                    while (safetyReads++ < 4 && fs.Position < fs.Length)
                    {
                        ct.ThrowIfCancellationRequested();
                        int read = fs.Read(byteBuf, 0, ByteBufSize);
                        if (read <= 0) break;

                        decoder.Convert(byteBuf, 0, read, charBuf, 0, charBuf.Length,
                                        flush: false, out int _, out int charsUsed, out bool _);

                        var span = new ReadOnlySpan<char>(charBuf, 0, charsUsed);
                        int i = 0;
                        while (i < span.Length && IsWordChar(span[i]))
                        {
                            carry += char.ToLowerInvariant(span[i]);
                            i++;
                        }
                        if (i < span.Length && !IsWordChar(span[i]))
                        {
                            counts.AddOrUpdate(carry, 1, (_, cur) => cur + 1);
                            carry = null;
                            break;
                        }
                    }
                    if (carry != null) 
                        counts.AddOrUpdate(carry, 1, (_, cur) => cur + 1);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteBuf);
                ArrayPool<char>.Shared.Return(charBuf);
            }
        }

        private static bool IsWordChar(char ch) => char.IsLetterOrDigit(ch) || ch == '_';

        private static int IndexOfDelimiter(ReadOnlySpan<char> span)
        {
            for (int i = 0; i < span.Length; i++)
                if (!IsWordChar(span[i])) return i;
            return -1;
        }

        // выравнивание начала сегмента
        private static void AlignStartToCharBoundary(FileStream fs, Encoding enc, long start, out long alignedStart)
        {
            alignedStart = start;
            if (start <= 0) return;

            switch (enc.CodePage)
            {
                case 65001: // UTF-8
                    {
                        long saved = fs.Position;
                        fs.Position = start;
                        int b = fs.ReadByte();
                        while (b >= 0 && (b & 0xC0) == 0x80)
                        {
                            alignedStart++;
                            b = fs.ReadByte();
                        }
                        fs.Position = saved;
                        break;
                    }
                case 1200: // UTF-16 LE
                case 1201: // UTF-16 BE
                    if ((alignedStart & 1) != 0) alignedStart++;
                    break;
                case 12000: // UTF-32 LE
                case 12001: // UTF-32 BE
                    long mod = alignedStart % 4;
                    if (mod != 0) alignedStart += (4 - mod);
                    break;
                default:
                    // 1-байтовые кодировки
                    break;
            }
        }

        // Проверяем символ перед start
        private static bool PrevCharIsWord(FileStream fs, Encoding enc, long start)
        {
            if (start <= 0) return false;
            const int LookBehind = 8;
            long saved = fs.Position;
            long from = Math.Max(0, start - LookBehind);
            int bytesToRead = (int)(start - from);

            byte[] tmp = ArrayPool<byte>.Shared.Rent(bytesToRead);
            char[] chars = ArrayPool<char>.Shared.Rent(enc.GetMaxCharCount(bytesToRead));
            try
            {
                fs.Position = from;
                int read = fs.Read(tmp, 0, bytesToRead);
                var dec = enc.GetDecoder();
                int nChars = dec.GetChars(tmp, 0, read, chars, 0, flush: true);

                for (int i = nChars - 1; i >= 0; i--)
                {
                    char ch = chars[i];
                    if (char.IsControl(ch)) continue;
                    return IsWordChar(ch);
                }
                return false;
            }
            finally
            {
                fs.Position = saved;
                ArrayPool<byte>.Shared.Return(tmp);
                ArrayPool<char>.Shared.Return(chars);
            }
        }

        private static Encoding DetectEncoding(FileStream fs)
        {
            long pos = fs.Position;
            fs.Seek(0, SeekOrigin.Begin);
            Span<byte> bom = stackalloc byte[4];
            int read = fs.Read(bom);
            fs.Seek(pos, SeekOrigin.Begin);

            if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) return new UTF8Encoding(true);
            if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE) return Encoding.Unicode;
            if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF) return Encoding.BigEndianUnicode;
            if (read >= 4 && bom[0] == 0xFF && bom[1] == 0xFE && bom[2] == 0x00 && bom[3] == 0x00) return Encoding.UTF32;
            if (read >= 4 && bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xFE && bom[3] == 0xFF) return new UTF32Encoding(true, true);
            return null; // нет BOM — предполагаем UTF-8 без BOM
        }
    }
}
