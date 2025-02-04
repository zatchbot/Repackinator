﻿using Resurgent.UtilityBelt.Library.Utilities.ImageInput;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Resurgent.UtilityBelt.Library.Utilities
{
    public static class XisoUtility
    {
        private struct IndexInfo
        {
            public ulong Value { get; set; }

            public bool Compressed { get; set; }
        }

        private struct TreeNodeInfo
        {
            public uint DirectorySize { get; set; }
            public long DirectoryPos { get; set; }
            public uint Offset { get; set; }
            public string Path { get; set; }
        };

        public struct FileInfo
        {
            public bool IsFile { get; set; }
            public string Path { get; set; }
            public string Filename { get; set; }
            public long Size { get; set; }
            public int StartSector { get; set; }
            public int EndSector { get; set; }
            public string InSlices { get; set; }
        };

        public static HashSet<uint> GetDataSectorsFromXiso(IImageInput input, Action<float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0);
            }

            var dataSectors = new HashSet<uint>();

            var position = 20U;
            var headerSector = (uint)input.SectorOffset + 0x20U;
            dataSectors.Add(headerSector);
            dataSectors.Add(headerSector + 1);
            position += headerSector << 11;

            var rootSector = input.ReadUint32(position);
            var rootSize = input.ReadUint32(position + 4);
            var rootOffset = (long)rootSector << 11;

            var treeNodes = new List<TreeNodeInfo>
            {
                new TreeNodeInfo
                {
                    DirectorySize = rootSize,
                    DirectoryPos = rootOffset,
                    Offset = 0,
                    Path = string.Empty
                }
            };

            var totalNodes = 1;
            var processedNodes = 0;

            while (treeNodes.Count > 0)
            {
                var currentTreeNode = treeNodes[0];
                treeNodes.RemoveAt(0);
                processedNodes++;

                var currentPosition = (input.SectorOffset << 11) + currentTreeNode.DirectoryPos + currentTreeNode.Offset * 4;

                for (var i = currentPosition >> 11; i < (currentPosition >> 11) + ((currentTreeNode.DirectorySize - (currentTreeNode.Offset * 4) + 2047) >> 11); i++)
                {
                    dataSectors.Add((uint)i);
                }

                if ((currentTreeNode.Offset * 4) >= currentTreeNode.DirectorySize)
                {
                    continue;
                }

                var left = input.ReadUint16(currentPosition);
                var right = input.ReadUint16(currentPosition + 2);
                var sector = (long)input.ReadUint32(currentPosition + 4);
                var size = input.ReadUint32(currentPosition + 8);
                var attribute = input.ReadByte(currentPosition + 12);

                var nameLength = input.ReadByte(currentPosition + 13);
                var filenameBytes = input.ReadBytes(currentPosition + 14, nameLength);
                var filename = Encoding.ASCII.GetString(filenameBytes);
                //System.Diagnostics.Debug.WriteLine(filename);

                if (left == 0xFFFF)
                {
                    continue;
                }

                if (left != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryPos = currentTreeNode.DirectoryPos,
                        Offset = left,
                        Path = currentTreeNode.Path
                    });
                    totalNodes++;
                }

                if ((attribute & 0x10) != 0)
                {
                    if (size > 0)
                    {
                        treeNodes.Add(new TreeNodeInfo
                        {
                            DirectorySize = size,
                            DirectoryPos = sector << 11,
                            Offset = 0,
                            Path = Path.Combine(currentTreeNode.Path, filename)
                        });
                        totalNodes++;
                    }
                }
                else
                {
                    if (size > 0)
                    {
                        for (var i = (input.SectorOffset + sector); i < (input.SectorOffset + sector) + ((size + 2047) >> 11); i++)
                        {
                            dataSectors.Add((uint)i);
                        }
                    }
                }

                if (right != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryPos = currentTreeNode.DirectoryPos,
                        Offset = right,
                        Path = currentTreeNode.Path
                    });
                    totalNodes++;
                }

                if (progress != null)
                {
                    progress(processedNodes / (float)totalNodes);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            return dataSectors;
        }

        public static void GetFileInfoFromXiso(IImageInput input, Action<FileInfo> info, Action<float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0);
            }

            var position = 20U;
            var headerSector = (uint)input.SectorOffset + 0x20U;
            position += headerSector << 11;

            var rootSector = input.ReadUint32(position);
            var rootSize = input.ReadUint32(position + 4);
            var rootOffset = (long)rootSector << 11;

            var treeNodes = new List<TreeNodeInfo>
            {
                new TreeNodeInfo
                {
                    DirectorySize = rootSize,
                    DirectoryPos = rootOffset,
                    Offset = 0,
                    Path = string.Empty
                }
            };

            var totalNodes = 1;
            var processedNodes = 0;

            while (treeNodes.Count > 0)
            {
                var currentTreeNode = treeNodes[0];
                treeNodes.RemoveAt(0);
                processedNodes++;

                var currentPosition = (input.SectorOffset << 11) + currentTreeNode.DirectoryPos + currentTreeNode.Offset * 4;

                if ((currentTreeNode.Offset * 4) >= currentTreeNode.DirectorySize)
                {
                    continue;
                }

                var left = input.ReadUint16(currentPosition);
                var right = input.ReadUint16(currentPosition + 2);
                var sector = (long)input.ReadUint32(currentPosition + 4);
                var size = input.ReadUint32(currentPosition + 8);
                var attribute = input.ReadByte(currentPosition + 12);

                var nameLength = input.ReadByte(currentPosition + 13);
                var filenameBytes = input.ReadBytes(currentPosition + 14, nameLength);
                var filename = Encoding.ASCII.GetString(filenameBytes);

                if (left == 0xFFFF)
                {
                    continue;
                }

                if (left != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryPos = currentTreeNode.DirectoryPos,
                        Offset = left,
                        Path = currentTreeNode.Path
                    });
                    totalNodes++;
                }

                if ((attribute & 0x10) != 0)
                {
                    if (size > 0)
                    {
                        treeNodes.Add(new TreeNodeInfo
                        {
                            DirectorySize = size,
                            DirectoryPos = sector << 11,
                            Offset = 0,
                            Path = Path.Combine(currentTreeNode.Path, filename)
                        });
                        totalNodes++;
                        info(new FileInfo
                        {
                            IsFile = false,
                            Path = Path.Combine(currentTreeNode.Path, filename),
                            Filename = filename,
                            Size = size,
                            StartSector = (int)(input.SectorOffset + sector),
                            EndSector = (int)((input.SectorOffset + sector) + ((size + 2047) >> 11) - 1),
                            InSlices = "N/A"
                        });
                    }
                }
                else
                {
                    if (size > 0)
                    {
                        var startSector = (int)(input.SectorOffset + sector);
                        var endSector = (int)((input.SectorOffset + sector) + ((size + 2047) >> 11) - 1);
                        var stringBuilder = new StringBuilder();
                        var slices = new HashSet<int>();
                        if (size > 0)
                        {
                            slices.Add(input.SectorInSlice(startSector));
                            slices.Add(input.SectorInSlice(endSector));
                            for (var i = 0; i < slices.Count; i++)
                            {
                                if (i > 0)
                                {
                                    stringBuilder.Append("-");
                                }
                                stringBuilder.Append(slices.ElementAt(i).ToString());
                            }
                        }
                        else
                        {
                            stringBuilder.Append("N/A");
                        }
                        info(new FileInfo
                        {
                            IsFile = true,
                            Path = currentTreeNode.Path,
                            Filename = filename,
                            Size = size,
                            StartSector = size > 0 ? startSector : -1,
                            EndSector = size > 0 ? endSector : -1,
                            InSlices = stringBuilder.ToString()
                        });
                    } 
                    else 
                    {
                        info(new FileInfo
                        {
                            IsFile = true,
                            Path = currentTreeNode.Path,
                            Filename = filename,
                            Size = size,
                            StartSector = -1,
                            EndSector = -1,
                            InSlices = "N/A"
                        });
                    }
                }

                if (right != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryPos = currentTreeNode.DirectoryPos,
                        Offset = right,
                        Path = currentTreeNode.Path
                    });
                    totalNodes++;
                }

                if (progress != null)
                {
                    progress(processedNodes / (float)totalNodes);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        public static HashSet<uint> GetSecuritySectorsFromXiso(IImageInput input, HashSet<uint> datasecs, Action<float>? progress, CancellationToken cancellationToken)
        {
            var securitySectors = new HashSet<uint>();            
            if (input.TotalSectors != Constants.RedumpSectors && input.TotalSectors != Constants.IsoSectors)
            {
                return securitySectors;
            }

            if (progress != null)
            {
                progress(0);
            }

            var flag = false;
            var start = 0U;

            const int endSector = 0x345B60;
            for (var sectorIndex = 0; sectorIndex <= endSector; sectorIndex++)
            {
                var currentSector = (uint)(input.SectorOffset + sectorIndex);

                byte[] sectorBuffer = input.ReadSectors(currentSector, 1);

                var isEmptySector = true;
                for (var i = 0; i < sectorBuffer.Length; i++)
                {
                    if (sectorBuffer[i] != 0)
                    {
                        isEmptySector = false;
                        break;
                    }
                }

                var isDataSector = datasecs.Contains(currentSector);
                if (isEmptySector == true && flag == false && !isDataSector)
                {
                    start = currentSector;
                    flag = true;
                }
                else if (isEmptySector == false && flag == true)
                {
                    var end = currentSector - 1;
                    flag = false;
                    if (end - start == 0xFFF)
                    {
                        for (var i = start; i <= end; i++)
                        {
                            securitySectors.Add(i);
                        }
                    }
                }

                if (progress != null)
                {
                    progress(sectorIndex / (float)endSector);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            return securitySectors;
        }

        public static string GetChecksumFromXiso(IImageInput input, Action<float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0);
            }

            using var hash = SHA256.Create();
            for (var i = 0; i < input.TotalSectors; i++)
            {
                var buffer = input.ReadSectors(i, 1);
                hash.TransformBlock(buffer, 0, buffer.Length, null, 0);
                if (progress != null)
                {
                    progress(i / (float)input.TotalSectors);
                }                
            }
            hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var sha256Hash = hash.Hash;
            if (sha256Hash == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            return BitConverter.ToString(sha256Hash).Replace("-", string.Empty);
        }

        public static bool TryGetDefaultXbeFromXiso(IImageInput input, ref byte[] xbeData)
        {
            var position = 20U;
            var headerSector = (uint)input.SectorOffset + 0x20U;
            position += headerSector << 11;

            var rootSector = input.ReadUint32(position);
            var rootSize = input.ReadUint32(position + 4);
            var rootOffset = (long)rootSector << 11;

            var treeNodes = new List<TreeNodeInfo>
            {
                new TreeNodeInfo
                {
                    DirectorySize = rootSize,
                    DirectoryPos = rootOffset,
                    Offset = 0,
                    Path = string.Empty
                }
            };

            while (treeNodes.Count > 0)
            {
                var currentTreeNode = treeNodes[0];
                treeNodes.RemoveAt(0);

                var currentPosition = (input.SectorOffset << 11) + currentTreeNode.DirectoryPos + currentTreeNode.Offset * 4;

                if ((currentTreeNode.Offset * 4) >= currentTreeNode.DirectorySize)
                {
                    continue;
                }

                var left = input.ReadUint16(currentPosition);
                var right = input.ReadUint16(currentPosition + 2);
                var sector = input.ReadUint32(currentPosition + 4);
                var size = input.ReadUint32(currentPosition + 8);
                var attribute = input.ReadByte(currentPosition + 12);

                var nameLength = input.ReadByte(currentPosition + 13);
                var filenameBytes = input.ReadBytes(currentPosition + 14, nameLength);
                var filename = Encoding.ASCII.GetString(filenameBytes);

                if ((attribute & 0x10) == 0 && filename.Equals("default.xbe", StringComparison.CurrentCultureIgnoreCase))
                {
                    var result = new byte[size];
                    var processed = 0U;
                    while (processed < size)
                    {
                        var buffer = input.ReadSectors(sector + input.SectorOffset, 1);
                        var bytesToCopy = Math.Min(size - processed, 2048);
                        Array.Copy(buffer, 0, result, processed, bytesToCopy);
                        sector++;
                        processed += bytesToCopy;
                    }
                    xbeData = result;
                    return true;
                }

                if (left == 0xFFFF)
                {
                    continue;
                }

                if (left != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryPos = currentTreeNode.DirectoryPos,
                        Offset = left,
                        Path = currentTreeNode.Path
                    });
                }

                if (right != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryPos = currentTreeNode.DirectoryPos,
                        Offset = right,
                        Path = currentTreeNode.Path
                    });
                }
            }

            return false;
        }

        //https://github.com/Qubits01/xbox_shrinker

        public static void CompareXISO(IImageInput input1, IImageInput input2, Action<string> log, Action<float>? progress)
        {
            if (input1.SectorOffset > 0)
            {
                log("First contains a video partition, compare will ignore those sectors.");
            }

            if (input2.SectorOffset > 0)
            {
                log("Second contains a video partition, compare will ignore those sectors.");
            }

            if (input1.TotalSectors - input1.SectorOffset != input2.TotalSectors - input2.SectorOffset)
            {
                log("Expected sector counts do not match, assuming image could be trimmed.");
            }

            var flag = false;
            var startRange = 0L;
            var endRange = 0L;
            for (var i = 0; i < input1.TotalSectors - input1.SectorOffset; i++)
            {
                var buffer1 = new byte[2048];
                var buffer2 = new byte[2048];

                if (i < input1.TotalSectors)
                {
                    buffer1 = input1.ReadSectors(i + input1.SectorOffset, 1);
                }

                if (i < input2.TotalSectors) 
                { 
                    buffer2 = input2.ReadSectors(i + input2.SectorOffset, 1);
                }

                var same = true;
                for (var j = 0; j < 2048; j++)
                {
                    if (buffer1[j] != buffer2[j])
                    {
                        same = false;
                        break;
                    }
                }

                endRange = i;
                if (!same)
                {
                    if (!flag)
                    {
                        startRange = i;
                        flag = true;
                    }
                }
                else if (flag)
                {
                    log($"Game partition sectors in range {startRange}-{endRange} (Redump range {startRange + (Constants.VideoSectors - input1.SectorOffset)}-{endRange + (Constants.VideoSectors - input1.SectorOffset)}) are different.");
                    flag = false;
                }

                if (progress != null)
                {
                    progress(i / (float)(input1.TotalSectors - input1.SectorOffset));
                }
            }

            if (flag)
            {
                log($"Game partition sectors in range {startRange}-{endRange} (Redump range {startRange + (Constants.VideoSectors - input1.SectorOffset)}-{endRange + (Constants.VideoSectors - input1.SectorOffset)}) are different.");
            }

            log("");

            log("Getting data sectors hash for first...");
            var dataSectors1 = GetDataSectorsFromXiso(input1, progress, default);

            log("Calculating data sector hashes for first...");
            using var dataSectorsHash1 = SHA1.Create();
            for (var i = 0; i < dataSectors1.Count; i++)
            {
                var dataSector1 = dataSectors1.ElementAt(i);
                var buffer = input1.ReadSectors(dataSector1 + input1.SectorOffset, 1);
                dataSectorsHash1.TransformBlock(buffer, 0, buffer.Length, null, 0);
                if (progress != null)
                {
                    progress(i / (float)dataSectors1.Count);
                }
            }
            var dataChecksum1 = dataSectorsHash1.Hash;
            if (dataChecksum1 == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            var dataSectorsHash1Result = BitConverter.ToString(dataChecksum1).Replace("-", string.Empty);

            log("Getting data sectors hash for second...");
            var dataSectors2 = GetDataSectorsFromXiso(input2, progress, default);

            log("Calculating data sector hash for second...");
            using var dataSectorsHash2 = SHA1.Create();
            for (var i = 0; i < dataSectors2.Count; i++)
            {
                var dataSector2 = dataSectors1.ElementAt(i);
                var buffer = input1.ReadSectors(dataSector2 + input2.SectorOffset, 1);
                dataSectorsHash2.TransformBlock(buffer, 0, buffer.Length, null, 0);
                if (progress != null)
                {
                    progress(i / (float)dataSectors2.Count);
                }
            }
            var dataChecksum2 = dataSectorsHash2.Hash;
            if (dataChecksum2 == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            var dataSectorsHash2Result = BitConverter.ToString(dataChecksum2).Replace("-", string.Empty);

            if (dataSectorsHash1Result == dataSectorsHash2Result)
            {
                log("Data sectors match.");
            }
            else
            {
                log("Data sectors do not match.");
            }


            log("");

            log("Getting security sectors hash for first...");
            var securitySectors1 = GetSecuritySectorsFromXiso(input1, dataSectors1, progress, default);

            log("Calculating security sector hashes for first...");
            using var securitySectorsHash1 = SHA1.Create();
            for (var i = 0; i < securitySectors1.Count; i++)
            {
                var securitySector1 = securitySectors1.ElementAt(i);
                var buffer = input1.ReadSectors(securitySector1 + input1.SectorOffset, 1);
                securitySectorsHash1.TransformBlock(buffer, 0, buffer.Length, null, 0);
                if (progress != null)
                {
                    progress(i / (float)securitySectors1.Count);
                }
            }
            var secutityChecksum1 = securitySectorsHash1.Hash;
            if (secutityChecksum1 == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            var securitySectorsHash1Result = BitConverter.ToString(secutityChecksum1).Replace("-", string.Empty);

            log("Getting security sectors hash for second...");
            var securitySectors2 = GetSecuritySectorsFromXiso(input2, dataSectors2, progress, default);

            log("Calculating security sector hash for second...");
            using var securitySectorsHash2 = SHA1.Create();
            for (var i = 0; i < securitySectors2.Count; i++)
            {
                var securitySector2 = securitySectors2.ElementAt(i);
                var buffer = input1.ReadSectors(securitySector2 + input2.SectorOffset, 1);
                securitySectorsHash2.TransformBlock(buffer, 0, buffer.Length, null, 0);
                if (progress != null)
                {
                    progress(i / (float)securitySectors2.Count);
                }
            }
            securitySectorsHash2.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var secutityChecksum2 = securitySectorsHash2.Hash;
            if (secutityChecksum2 == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            var securitySectorsHash2Result = BitConverter.ToString(secutityChecksum2).Replace("-", string.Empty);

            if (securitySectorsHash1Result == securitySectorsHash2Result)
            {
                log("Security sectors match.");
            }
            else
            {
                log("Security sectors do not match.");
            }
        }

        public static bool Split(IImageInput input, string outputPath, string name, string extension, bool scrub, bool trimmedScrub, Action<int, float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0, 0);
            }

            Action<float> progress1 = (percent) => {
                if (progress != null)
                {
                    progress(0, percent);
                }
            };

            Action<float> progress2 = (percent) => {
                if (progress != null)
                {
                    progress(1, percent);
                }
            };

            var endSector = input.TotalSectors;
            var dataSectors = new HashSet<uint>();
            if (scrub)
            {
                dataSectors = GetDataSectorsFromXiso(input, progress1, cancellationToken);

                if (trimmedScrub)
                {
                    endSector = Math.Min(dataSectors.Max() + 1, input.TotalSectors);
                }

                var securitySectors = GetSecuritySectorsFromXiso(input, dataSectors, progress2, cancellationToken);
                for (var i = 0; i < securitySectors.Count; i++)
                {
                    dataSectors.Add(securitySectors.ElementAt(i));
                }
            }

            var sectorSplit = (uint)(endSector - input.SectorOffset) / 2;

            using var partStream1 = new FileStream(Path.Combine(outputPath, $"{name}.1{extension}"), FileMode.Create, FileAccess.Write);
            using var partWriter1 = new BinaryWriter(partStream1);

            using var partStream2 = new FileStream(Path.Combine(outputPath, $"{name}.2{extension}"), FileMode.Create, FileAccess.Write);
            using var partWriter2 = new BinaryWriter(partStream2);

            var emptySector = new byte[2048];

            for (var i = (uint)input.SectorOffset; i < endSector; i++)
            {
                var currentWriter = i - input.SectorOffset >= sectorSplit ? partWriter2 : partWriter1;
               
                var writeSector = true;
                if (scrub)
                {
                    writeSector = dataSectors.Contains(i);
                }
                if (writeSector == true)
                {
                    var sectorBuffer = input.ReadSectors(i, 1);
                    currentWriter.Write(sectorBuffer);
                }
                else
                {
                    currentWriter.Write(emptySector);
                }

                if (progress != null)
                {
                    progress(2, i / (float)(endSector - input.SectorOffset));
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            return true;
        }

        public static bool CreateCCI(IImageInput input, string outputPath, string name, string extension, bool scrub, bool trimmedScrub, Action<int, float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0, 0);
            }

            Action<float> progress1 = (percent) => {
                if (progress != null)
                {
                    progress(0, percent);
                }
            };

            Action<float> progress2 = (percent) => {
                if (progress != null)
                {
                    progress(1, percent);
                }
            };

            var endSector = input.TotalSectors;
            var dataSectors = new HashSet<uint>();
            if (scrub)
            {
                dataSectors = GetDataSectorsFromXiso(input, progress1, cancellationToken);

                if (trimmedScrub)
                {
                    endSector = Math.Min(dataSectors.Max() + 1, input.TotalSectors);
                }

                var securitySectors = GetSecuritySectorsFromXiso(input, dataSectors, progress2, cancellationToken);
                for (var i = 0; i < securitySectors.Count; i++)
                {
                    dataSectors.Add(securitySectors.ElementAt(i));
                }
            }

            var sectorOffset = input.TotalSectors == Constants.RedumpSectors ? Constants.VideoSectors : 0U;

            var splitMargin = 0xFF000000L;
            var emptySector = new byte[2048];
            var compressedData = new byte[2048];
            var sectorsWritten = (uint)sectorOffset;
            var iteration = 0;

            while (sectorsWritten < endSector)
            {
                var indexInfos = new List<IndexInfo>();

                var outputFile = Path.Combine(outputPath, iteration > 0 ? $"{name}.{iteration + 1}{extension}" : $"{name}{extension}");
                var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                var outputWriter = new BinaryWriter(outputStream);

                uint header = 0x4D494343U;
                outputWriter.Write(header);

                uint headerSize = 32;
                outputWriter.Write(headerSize);

                ulong uncompressedSize = (ulong)0;
                outputWriter.Write(uncompressedSize);

                ulong indexOffset = (ulong)0;
                outputWriter.Write(indexOffset);

                uint blockSize = 2048;
                outputWriter.Write(blockSize);

                byte version = 1;
                outputWriter.Write(version);

                byte indexAlignment = 2;
                outputWriter.Write(indexAlignment);

                ushort unused = 0;
                outputWriter.Write(unused);

                var splitting = false;
                var sectorCount = 0U;
                while (sectorsWritten < endSector)
                {
                    var writeSector = true;
                    if (scrub)
                    {
                        writeSector = dataSectors.Contains(sectorsWritten);
                    }

                    var sectorToWrite = writeSector == true ? input.ReadSectors(sectorsWritten, 1) :  emptySector;              

                    var compressedSize = K4os.Compression.LZ4.LZ4Codec.Encode(sectorToWrite, compressedData, K4os.Compression.LZ4.LZ4Level.L12_MAX);
                    if (compressedSize > 0 && compressedSize < (2048 - (4 + (1 << indexAlignment))))
                    {
                        var multiple = (1 << indexAlignment);
                        var padding = ((compressedSize + 1 + multiple - 1) / multiple * multiple) - (compressedSize + 1);
                        outputWriter.Write((byte)padding);
                        outputWriter.Write(compressedData, 0, compressedSize);
                        if (padding != 0)
                        {
                            outputWriter.Write(new byte[padding]);
                        }
                        indexInfos.Add(new IndexInfo { Value = (ushort)(compressedSize + 1 + padding), Compressed = true });
                    }
                    else
                    {
                        outputWriter.Write(sectorToWrite);
                        indexInfos.Add(new IndexInfo { Value = 2048, Compressed = false });
                    }

                    uncompressedSize += 2048;
                    sectorsWritten++;
                    sectorCount++;

                    if (outputStream.Position > splitMargin)
                    {
                        splitting = true;
                        break;
                    }

                    if (progress != null)
                    {
                        progress(2, sectorsWritten / (float)(endSector - sectorOffset));
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    outputStream.Dispose();
                    outputWriter.Dispose();
                    return true;
                }

                indexOffset = (ulong)outputStream.Position;

                var position = (ulong)headerSize;
                for (var i = 0; i < indexInfos.Count; i++)
                {
                    var index = (uint)(position >> indexAlignment) | (indexInfos[i].Compressed ? 0x80000000U : 0U);
                    outputWriter.Write(index);
                    position += indexInfos[i].Value;
                }
                var indexEnd = (uint)(position >> indexAlignment);
                outputWriter.Write(indexEnd);

                outputStream.Position = 8;
                outputWriter.Write(uncompressedSize);
                outputWriter.Write(indexOffset);

                outputStream.Dispose();
                outputWriter.Dispose();

                if (splitting)
                {
                    File.Move(outputFile, Path.Combine(outputPath, $"{name}.{iteration + 1}{extension}"));
                }

                iteration++;
            }

            return true;
        }

        public static bool ConvertCCItoISO(string inputFile, string outputFile)
        {
            using var inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            using var inputReader = new BinaryReader(inputStream);

            var header = inputReader.ReadUInt32();
            if (header != 0x4D494343)
            {
                return false;
            }

            uint headerSize = inputReader.ReadUInt32();
            if (headerSize != 32)
            {
                return false;
            }

            ulong uncompressedSize = inputReader.ReadUInt64();

            ulong indexOffset = inputReader.ReadUInt64();

            uint blockSize = inputReader.ReadUInt32();
            if (blockSize != 2048)
            {
                return false;
            }

            byte version = inputReader.ReadByte();
            if (version != 1)
            {
                return false;
            }

            byte indexAlignment = inputReader.ReadByte();
            if (indexAlignment != 2)
            {
                return false;
            }

            using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            using var outputWriter = new BinaryWriter(outputStream);

            var entries = (int)(uncompressedSize / (ulong)blockSize);

            inputStream.Position = (long)indexOffset;

            var indexInfos = new List<IndexInfo>();
            for (var i = 0; i <= entries; i++)
            {
                var index = inputReader.ReadUInt32();
                indexInfos.Add(new IndexInfo
                {
                    Value = (index & 0x7FFFFFFF) << indexAlignment,
                    Compressed = (index & 0x80000000) > 0
                });
            }

            var decodeBuffer = new byte[2048];
            for (var i = 0; i < entries; i++)
            {
                inputStream.Position = (long)indexInfos[i].Value;

                var size = (int)(indexInfos[i + 1].Value - indexInfos[i].Value);
                if (size < 2048 || indexInfos[i].Compressed)
                { 
                    var padding = inputReader.ReadByte();
                    var buffer = inputReader.ReadBytes(size);
                    var compressedSize = K4os.Compression.LZ4.LZ4Codec.Decode(buffer, 0, size - (padding + 1), decodeBuffer, 0, 2048);
                    if (compressedSize < 0)
                    {
                        return false;
                    }
                    outputWriter.Write(decodeBuffer);
                }
                else
                {
                    var buffer = inputReader.ReadBytes(2048);
                    outputWriter.Write(buffer);
                }
            }

            return true;
        }
    }
}

