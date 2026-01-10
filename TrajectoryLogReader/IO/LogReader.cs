using System.Text;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.IO
{
    public static class LogReader
    {
        /// <summary>
        /// Reads a binary (*.bin) trajectory log file (Varian) asynchronously.
        /// </summary>
        /// <param name="filePath">The log file path</param>
        /// <param name="mode">If set to HeaderAndMetaData, the reader does not read log data, only the header and metadata./></param>
        /// <returns></returns>
        public static async Task<TrajectoryLog> ReadBinaryAsync(string filePath,
            LogReaderReadMode mode = LogReaderReadMode.All)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            var log = await ReadBinaryAsync(fs, mode);
            log.FilePath = filePath;
            return log;
        }

        /// <summary>
        /// Reads a binary (*.bin) trajectory log file (Varian).
        /// </summary>
        /// <param name="filePath">The log file path</param>
        /// <param name="mode">If set to HeaderAndMetaData, the reader does not read log data, only the header and metadata./></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static TrajectoryLog ReadBinary(string filePath, LogReaderReadMode mode = LogReaderReadMode.All)
        {
            using var fs = File.OpenRead(filePath);
            var log = ReadBinary(fs, mode);
            log.FilePath = filePath;
            return log;
        }

        /// <summary>
        /// Reads a binary (*.bin) trajectory log file (Varian) from a file stream.
        /// </summary>
        /// <param name="stream">The file stream</param>
        /// <param name="mode">If set to HeaderAndMetaData, the reader does not read log data, only the header and metadata./></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static TrajectoryLog ReadBinary(Stream stream, LogReaderReadMode mode = LogReaderReadMode.All)
        {
            return ParseFromStream(stream, mode);
        }

        /// <summary>
        /// Reads a binary (*.bin) trajectory log file (Varian) from a file stream asynchronously.
        /// </summary>
        /// <param name="stream">The file stream</param>
        /// <param name="mode">If set to HeaderAndMetaData, the reader does not read log data, only the header and metadata./></param>
        /// <returns></returns>
        public static async Task<TrajectoryLog> ReadBinaryAsync(Stream stream,
            LogReaderReadMode mode = LogReaderReadMode.All)
        {
            // For async reading, we buffer the entire stream into memory asynchronously,
            // then run the synchronous parser on the memory stream.
            // This avoids duplicating complex parsing logic while getting true async IO.
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            return ParseFromStream(ms, mode);
        }

        private static TrajectoryLog ParseFromStream(Stream stream, LogReaderReadMode mode)
        {
            var utf8 = Encoding.UTF8;
            var log = new TrajectoryLog() { FilePath = null };
            var header = new Header();

            // We keep the stream open as it is managed by the caller (or the async wrapper)
            using (var br = new BinaryReader(stream, Encoding.UTF8, true))
            {
                // first 16 bytes should be 'VOSTL'
                var sigBytes = br.ReadBytes(16);
                if (sigBytes.Length < 16)
                    throw new EndOfStreamException("File too short");

                var sig = utf8.GetString(sigBytes);

                if (!sig.StartsWith("VOSTL"))
                    throw new Exception($"Invalid start of file {sig}. Expected VOSTL");

                header.Version = double.Parse(utf8.GetString(br.ReadBytes(16)));
                br.ReadInt32(); //header.HeaderSize - always 1024
                header.SamplingIntervalInMS = br.ReadInt32();
                header.NumAxesSampled = br.ReadInt32();

                header.AxesSampled = new Axis[header.NumAxesSampled];
                for (int i = 0; i < header.NumAxesSampled; i++)
                    header.AxesSampled[i] = (Axis)br.ReadInt32();

                header.SamplesPerAxis = new int[header.NumAxesSampled];
                for (int i = 0; i < header.NumAxesSampled; i++)
                    header.SamplesPerAxis[i] = br.ReadInt32();

                header.AxisScale = (AxisScale)br.ReadInt32();
                header.NumberOfSubBeams = br.ReadInt32();
                header.IsTruncated = br.ReadInt32() == 1;
                header.NumberOfSnapshots = br.ReadInt32();
                header.MlcModel = (MLCModel)br.ReadInt32();

                // Only one MLC model supported at the moment.
                log.MlcModel = header.MlcModel == MLCModel.NDS120 ? new Millenium120MLC() : null;

                log.Header = header;
                log.MetaData = ReadMetaData(br.ReadBytes(745), utf8);

                // Skip the data if mode is HeaderAndMetaData
                if (mode == LogReaderReadMode.HeaderAndMetaData)
                    return log;

                // Reserved bytes.
                // TrueBeam spec says offset 1024 - (64 + num_axis * 8) but it actually 
                // includes metadata size (thanks pylinac source).
                // We just skip to 1024.
                br.ReadBytes((1024 - (64 + header.NumAxesSampled * 8)) - 745);

                for (int i = 0; i < header.NumberOfSubBeams; i++)
                {
                    var subBeam = new SubBeam(log);
                    subBeam.ControlPoint = br.ReadInt32();
                    subBeam.MU = br.ReadSingle();
                    subBeam.RadTime = br.ReadSingle();
                    subBeam.SequenceNumber = br.ReadInt32();
                    subBeam.Name = utf8.GetString(br.ReadBytes(512)).Trim().Trim(new[] { '\t', '\0' });
                    br.ReadBytes(32);
                    log.SubBeams.Add(subBeam);
                }

                log.AxisData = new AxisData[header.NumAxesSampled];
                for (int i = 0; i < header.NumAxesSampled; i++)
                {
                    var samplesForAxis = header.GetNumberOfSamples(i) * 2;
                    log.AxisData[i] = new AxisData(header.NumberOfSnapshots, samplesForAxis);
                }

                for (int i = 0; i < header.NumberOfSnapshots; i++)
                {
                    for (int j = 0; j < header.NumAxesSampled; j++)
                    {
                        var axisData = log.AxisData[j];
                        var samplesForAxis = axisData.SamplesPerSnapshot;
                        var offset = i * samplesForAxis;

                        for (int k = 0; k < samplesForAxis; k++)
                        {
                            axisData.Data[offset + k] = br.ReadSingle();
                        }
                    }
                }
            }

            return log;
        }

        private static MetaData ReadMetaData(byte[] bytes, Encoding strEncoding)
        {
            var metaData = new MetaData();
            var metaDataStr = strEncoding.GetString(bytes);
            var lines = metaDataStr.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var lineSplit = line.Split(new[] { ':' }, 2);
                var type = lineSplit[0];
                var val = lineSplit[1];

                if (type == "Patient ID")
                    metaData.PatientId = val.Trim().Trim(new[] { '\t', '\0' });
                else if (type == "Plan Name")
                    metaData.PlanName = val.Trim().Trim(new[] { '\t', '\0' });
                else if (type == "Plan UID")
                    metaData.PlanUID = val.Trim().Trim(new[] { '\t', '\0' });
                else if (type == "Original MU")
                    metaData.MUPlanned = double.Parse(val.Trim());
                else if (type == "Remaining MU")
                    metaData.MURemaining = double.Parse(val.Trim());
                else if (type == "Energy")
                    metaData.Energy = val.Trim().Trim(new[] { '\t', '\0' });
                else if (type == "BeamName")
                    metaData.BeamName = val.Trim().Trim(new[] { '\t', '\0' });
            }

            return metaData;
        }
    }
}