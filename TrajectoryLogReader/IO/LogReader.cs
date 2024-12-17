using System.Text;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.IO
{
    public static class LogReader
    {
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
        /// <param name="fs">The file stream</param>
        /// <param name="mode">If set to HeaderAndMetaData, the reader does not read log data, only the header and metadata./></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static TrajectoryLog ReadBinary(FileStream fs, LogReaderReadMode mode = LogReaderReadMode.All)
        {
            var utf8 = Encoding.UTF8;

            var log = new TrajectoryLog()
            {
                FilePath = null
            };

            var header = new Header();

            using (fs)
            {
                using (var br = new BinaryReader(fs, Encoding.UTF8, false))
                {
                    // first 16 bytes should be 'VOSTL'
                    var sig = utf8.GetString(br.ReadBytes(16));
                    Console.WriteLine(sig);
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
                    br.ReadBytes((1024 - (64 + header.NumAxesSampled * 8)) - 745);

                    for (int i = 0; i < header.NumberOfSubBeams; i++)
                    {
                        var subBeam = new SubBeam();
                        subBeam.ControlPoint = br.ReadInt32();
                        subBeam.MU = br.ReadSingle();
                        subBeam.RadTime = br.ReadSingle();
                        subBeam.SequenceNumber = br.ReadInt32();
                        subBeam.Name = utf8.GetString(br.ReadBytes(512));
                        br.ReadBytes(32);
                        log.SubBeams.Add(subBeam);
                    }

                    log.AxisData = new AxisData[header.NumAxesSampled];
                    for (int i = 0; i < header.NumAxesSampled; i++)
                    {
                        log.AxisData[i] = new AxisData(header.NumberOfSnapshots);
                    }

                    for (int i = 0; i < header.NumberOfSnapshots; i++)
                    {
                        for (int j = 0; j < header.NumAxesSampled; j++)
                        {
                            // number of samples * 2 because we have expected and actual for all axes
                            var samplesForAxis = header.GetNumberOfSamples(j) * 2;
                            var snapShotData = new float[samplesForAxis];
                            for (int k = 0; k < samplesForAxis; k++)
                            {
                                snapShotData[k] = br.ReadSingle();
                            }

                            log.AxisData[j].RawData[i] = snapShotData;
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