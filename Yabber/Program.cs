using SoulsFormats;
using SoulsFormats.AC4;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Yabber
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Console.WriteLine(
                    $"{assembly.GetName().Name} {assembly.GetName().Version}\n\n" +
                    "Yabber has no GUI.\n" +
                    "Drag and drop a file onto the exe to unpack it,\n" +
                    "or an unpacked folder to repack it.\n\n" +
                    "DCX files will be transparently decompressed and recompressed;\n" +
                    "If you need to decompress or recompress an unsupported format,\n" +
                    "use Yabber.DCX instead.\n\n" +
                    "Press any key to exit."
                    );
                Console.ReadKey();
                return;
            }

            bool pause = false;

            foreach (string path in args)
            {
                try
                {
                    int maxProgress = Console.WindowWidth - 1;
                    int lastProgress = 0;
                    void report(float value)
                    {
                        int nextProgress = (int)Math.Ceiling(value * maxProgress);
                        if (nextProgress > lastProgress)
                        {
                            for (int i = lastProgress; i < nextProgress; i++)
                            {
                                if (i == 0)
                                    Console.Write('[');
                                else if (i == maxProgress - 1)
                                    Console.Write(']');
                                else
                                    Console.Write('=');
                            }
                            lastProgress = nextProgress;
                        }
                    }
                    IProgress<float> progress = new Progress<float>(report);

                    if (Directory.Exists(path))
                    {
                        pause |= ManageDir(path, progress);

                    }
                    else if (File.Exists(path))
                    {
                        pause |= ManageFile(path, progress);
                    }
                    else
                    {
                        Console.WriteLine($"File or directory not found: {path}");
                        pause = true;
                    }

                    if (lastProgress > 0)
                    {
                        progress.Report(1);
                        Console.WriteLine();
                    }
                }
                catch (DllNotFoundException ex) when (ex.Message.Contains("oo2core_6_win64.dll"))
                {
                    Console.WriteLine("In order to decompress .dcx files from Sekiro, you must copy oo2core_6_win64.dll from Sekiro into Yabber's lib folder.");
                    pause = true;
                }
                catch (UnauthorizedAccessException)
                {
                    using (Process current = Process.GetCurrentProcess())
                    {
                        var admin = new Process();
                        admin.StartInfo = current.StartInfo;
                        admin.StartInfo.FileName = current.MainModule.FileName;
                        admin.StartInfo.Arguments = Environment.CommandLine.Replace($"\"{Environment.GetCommandLineArgs()[0]}\"", "");
                        admin.StartInfo.Verb = "runas";
                        admin.Start();
                        return;
                    }
                }
                catch (FriendlyException ex)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Error: {ex.Message}");
                    pause = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Unhandled exception: {ex}");
                    pause = true;
                }

                Console.WriteLine();
            }

            if (pause)
            {
                Console.WriteLine("One or more errors were encountered and displayed above.\nPress any key to exit.");
                Console.ReadKey();
            }
        }

        private static bool ManageFile(string sourceFile, IProgress<float> progress)
        {
            string sourceDir = Path.GetDirectoryName(sourceFile);
            string filename = Path.GetFileName(sourceFile);
            string targetDir = $"{sourceDir}\\{filename.Replace('.', '-')}";
            if (File.Exists(targetDir))
                targetDir += "-ybr";

            string inputFile = sourceFile;
            bool isDCX = DCX.Is(sourceFile);
            DCX.Type compression = DCX.Type.Unknown;

            if (isDCX)
            {
                Console.WriteLine($"Decompressing DCX: {filename}...");

                if (sourceFile.EndsWith(".dcx"))
                    inputFile = $"{sourceDir}\\{Path.GetFileNameWithoutExtension(sourceFile)}";
                else
                    inputFile = $"{sourceDir}\\{sourceFile}.undcx";

                byte[] bytes = DCX.Decompress(sourceFile, out DCX.Type compr);
                compression = compr;
                File.WriteAllBytes(inputFile, bytes);
            }

            if (BND3.Is(inputFile))
            {
                Console.WriteLine($"Unpacking BND3: {filename}...");
                using (var bnd = new BND3Reader(inputFile))
                {
                    if (isDCX) bnd.Compression = compression;
                    bnd.Unpack(filename, targetDir, progress);
                }
            }
            else if (BND4.Is(inputFile))
            {
                Console.WriteLine($"Unpacking BND4: {filename}...");
                using (var bnd = new BND4Reader(inputFile))
                {
                    if (isDCX) bnd.Compression = compression;
                    bnd.Unpack(filename, targetDir, progress);
                }
            }
            else if (BXF3.IsBHD(inputFile))
            {
                string bdtExtension = Path.GetExtension(filename).Replace("bhd", "bdt");
                string bdtFilename = $"{Path.GetFileNameWithoutExtension(filename)}{bdtExtension}";
                string bdtPath = $"{sourceDir}\\{bdtFilename}";
                if (File.Exists(bdtPath))
                {
                    Console.WriteLine($"Unpacking BXF3: {filename}...");
                    using (var bxf = new BXF3Reader(inputFile, bdtPath))
                    {
                        bxf.Unpack(filename, bdtFilename, targetDir, progress);
                        YBUtil.Backup(bdtFilename);
                    }
                }
                else
                {
                    Console.WriteLine($"BDT not found for BHD: {filename}");
                    return true;
                }
            }
            else if (BXF4.IsBHD(inputFile))
            {
                string bdtExtension = Path.GetExtension(filename).Replace("bhd", "bdt");
                string bdtFilename = $"{Path.GetFileNameWithoutExtension(filename)}{bdtExtension}";
                string bdtPath = $"{sourceDir}\\{bdtFilename}";
                if (File.Exists(bdtPath))
                {
                    Console.WriteLine($"Unpacking BXF4: {filename}...");
                    using (var bxf = new BXF4Reader(inputFile, bdtPath))
                    {
                        bxf.Unpack(filename, bdtFilename, targetDir, progress);
                        YBUtil.Backup(bdtFilename);
                    }
                }
                else
                {
                    Console.WriteLine($"BDT not found for BHD: {filename}");
                    return true;
                }
            }
            else if (FFXDLSE.Is(inputFile))
            {
                Console.WriteLine($"Unpacking FFX: {filename}...");
                var ffx = FFXDLSE.Read(inputFile);
                if (isDCX) ffx.Compression = compression;
                ffx.Unpack(inputFile);
            }
            else if (inputFile.EndsWith(".ffx.xml") || inputFile.EndsWith(".ffx.dcx.xml"))
            {
                Console.WriteLine($"Repacking FFX: {filename}...");
                YFFX.Repack(inputFile);
            }
            else if (inputFile.EndsWith(".fmg"))
            {
                Console.WriteLine($"Unpacking FMG: {filename}...");
                FMG fmg = FMG.Read(inputFile);
                if (isDCX) fmg.Compression = compression;
                fmg.Unpack(inputFile);
            }
            else if (inputFile.EndsWith(".fmg.xml") || inputFile.EndsWith(".fmg.dcx.xml"))
            {
                Console.WriteLine($"Repacking FMG: {filename}...");
                YFMG.Repack(inputFile);
            }
            else if (GPARAM.Is(inputFile))
            {
                Console.WriteLine($"Unpacking GPARAM: {filename}...");
                GPARAM gparam = GPARAM.Read(inputFile);
                if (isDCX) gparam.Compression = compression;
                gparam.Unpack(inputFile);
            }
            else if (inputFile.EndsWith(".gparam.xml") || inputFile.EndsWith(".gparam.dcx.xml")
                    || inputFile.EndsWith(".fltparam.xml") || inputFile.EndsWith(".fltparam.dcx.xml"))
            {
                Console.WriteLine($"Repacking GPARAM: {filename}...");
                YGPARAM.Repack(inputFile);
            }
            else if (FXR3.Is(inputFile))
            {
                Console.WriteLine($"Unpacking FXR3: {filename}...");
                FXR3 fxr = FXR3.Read(inputFile);
                if (isDCX) fxr.Compression = compression;
                fxr.Unpack(inputFile);
            }
            else if (inputFile.EndsWith(".fxr.xml") || inputFile.EndsWith(".fxr.dcx.xml"))
            {
                Console.WriteLine($"Repacking FXR3: {filename}...");
                YFXR3.Repack(inputFile);
            }
            else if (inputFile.EndsWith(".btab"))
            {
                Console.WriteLine($"Unpacking BTAB: {filename}...");
                BTAB btab = BTAB.Read(inputFile);
                if (isDCX) btab.Compression = compression;
                btab.Unpack(inputFile);
            }
            else if (inputFile.EndsWith(".btab.json") || inputFile.EndsWith(".btab.dcx.json"))
            {
                Console.WriteLine($"Repacking BTAB: {filename}...");
                YBTAB.Repack(inputFile);
            }
            else if (inputFile.EndsWith(".matbin"))
            {
                Console.WriteLine($"Unpacking MATBIN: {filename}...");
                MATBIN matbin = MATBIN.Read(inputFile);
                if (isDCX) matbin.Compression = compression;
                matbin.Unpack(inputFile);
            }
            else if (inputFile.EndsWith(".matbin.xml"))
            {
                Console.WriteLine($"Repacking MATBIN: {filename}...");
                YMATBIN.Repack(inputFile);
            }
            else if (inputFile.EndsWith(".mtd"))
            {
                Console.WriteLine($"Unpacking MTD: {filename}...");
                MTD mtd = MTD.Read(inputFile);
                if (isDCX) mtd.Compression = compression;
                mtd.Unpack(inputFile);
            }
            else if (inputFile.EndsWith(".mtd.xml"))
            {
                Console.WriteLine($"Repacking MTD: {filename}...");
                YMTD.Repack(inputFile);
            }
            else if (inputFile.EndsWith(".msb"))
            {

                Console.WriteLine($"Unpacking MSB: {filename}...");

                if (File.Exists($"{sourceDir}\\_er"))
                {
                    var msb = MSBE.Read(inputFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(inputFile);
                }
                else if (File.Exists($"{sourceDir}\\_sekiro"))
                {
                    var msb = MSBS.Read(inputFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(inputFile);
                }
                else if (File.Exists($"{sourceDir}\\_bb"))
                {
                    var msb = MSBB.Read(inputFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(inputFile);
                }
                else if (File.Exists($"{sourceDir}\\_des"))
                {
                    var msb = MSBD.Read(inputFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(inputFile);
                }
                else if (File.Exists($"{sourceDir}\\_ds3"))
                {
                    var msb = MSB3.Read(inputFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(inputFile);
                }
                else if (File.Exists($"{sourceDir}\\_ds2"))
                {
                    var msb = MSB2.Read(inputFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(inputFile);
                }
                else if (File.Exists($"{sourceDir}\\_ds1"))
                {
                    var msb = MSB1.Read(inputFile);
                    if (isDCX) msb.Compression = compression;
                    msb.Unpack(inputFile);
                }
                else
                {
                    Console.WriteLine($"Create a file with name corresponding to the game.");
                    Console.WriteLine($"Valid names: _er, _sekiro, _bb, _des, _ds3, _ds2, _ds1");
                    return true;
                }
            }
            else if (inputFile.EndsWith(".msb.json") || inputFile.EndsWith(".msb.dcx.json"))
            {
                Console.WriteLine($"Repacking MSB: {filename}...");

                if (File.Exists($"{sourceDir}\\_er"))
                {
                    YMSBE.Repack(inputFile);
                }
                else if (File.Exists($"{sourceDir}\\_sekiro"))
                {
                    YMSBS.Repack(inputFile);
                }
                else if (File.Exists($"{sourceDir}\\_bb"))
                {
                    YMSBB.Repack(inputFile);
                }
                else if (File.Exists($"{sourceDir}\\_des"))
                {
                    YMSBD.Repack(inputFile);
                }
                else if (File.Exists($"{sourceDir}\\_ds3"))
                {
                    YMSB3.Repack(inputFile);
                }
                else if (File.Exists($"{sourceDir}\\_ds2"))
                {
                    YMSB2.Repack(inputFile);
                }
                else if (File.Exists($"{sourceDir}\\_ds1"))
                {
                    YMSB1.Repack(inputFile);
                }
                else
                {
                    Console.WriteLine($"Create a file with name corresponding to the game.");
                    Console.WriteLine($"Valid names: _er, _sekiro, _bb, _des, _ds3, _ds2, _ds1");
                    return true;
                }
            }
            else if (inputFile.EndsWith(".btl"))
            {
                Console.WriteLine($"Unpacking BTL: {filename}...");
                BTL btl = BTL.Read(inputFile);
                if (isDCX) btl.Compression = compression;
                btl.Unpack(inputFile);
            }
            else if (inputFile.EndsWith(".btl.json") || inputFile.EndsWith(".btl.dcx.json"))
            {
                Console.WriteLine($"Repacking BTL: {filename}...");
                YBTL.Repack(inputFile);
            }
            else if (inputFile.EndsWith(".luagnl"))
            {
                Console.WriteLine($"Unpacking LUAGNL: {filename}...");
                LUAGNL gnl = LUAGNL.Read(inputFile);
                if (isDCX) gnl.Compression = compression;
                gnl.Unpack(inputFile);
            }
            else if (inputFile.EndsWith(".luagnl.xml"))
            {
                Console.WriteLine($"Repacking LUAGNL: {filename}...");
                YLUAGNL.Repack(inputFile);
            }
            else if (LUAINFO.Is(inputFile))
            {
                Console.WriteLine($"Unpacking LUAINFO: {filename}...");
                LUAINFO info = LUAINFO.Read(inputFile);
                if (isDCX) info.Compression = compression;
                info.Unpack(inputFile);
            }
            else if (inputFile.EndsWith(".luainfo.xml"))
            {
                Console.WriteLine($"Repacking LUAINFO: {filename}...");
                YLUAINFO.Repack(inputFile);
            }
            else if (TPF.Is(inputFile))
            {
                Console.WriteLine($"Unpacking TPF: {filename}...");
                TPF tpf = TPF.Read(inputFile);
                if (isDCX) tpf.Compression = compression;
                tpf.Unpack(filename, targetDir, progress);
            }
            else if (Zero3.Is(inputFile))
            {
                Console.WriteLine($"Unpacking 000: {filename}...");
                Zero3 z3 = Zero3.Read(inputFile);
                z3.Unpack(targetDir);
            }
            else
            {
                Console.WriteLine($"File format not recognized: {filename}");
                return true;
            }

            if (isDCX) File.Delete(inputFile);
            YBUtil.Backup(sourceFile);

            return false;
        }

        private static bool ManageDir(string sourceDir, IProgress<float> progress)
        {
            string sourceName = new DirectoryInfo(sourceDir).Name;
            string targetDir = new DirectoryInfo(sourceDir).Parent.FullName;

            if (File.Exists($"{sourceDir}\\_yabber-bnd3.xml"))
            {
                Console.WriteLine($"Repacking BND3: {sourceName}...");
                YBND3.Repack(sourceDir, targetDir);
                YBUtil.Backup(sourceDir);
            }
            else if (File.Exists($"{sourceDir}\\_yabber-bnd4.xml"))
            {
                Console.WriteLine($"Repacking BND4: {sourceName}...");
                YBND4.Repack(sourceDir, targetDir);
                YBUtil.Backup(sourceDir);
            }
            else if (File.Exists($"{sourceDir}\\_yabber-bxf3.xml"))
            {
                Console.WriteLine($"Repacking BXF3: {sourceName}...");
                YBXF3.Repack(sourceDir, targetDir);
                YBUtil.Backup(sourceDir);
            }
            else if (File.Exists($"{sourceDir}\\_yabber-bxf4.xml"))
            {
                Console.WriteLine($"Repacking BXF4: {sourceName}...");
                YBXF4.Repack(sourceDir, targetDir);
                YBUtil.Backup(sourceDir);
            }
            else if (File.Exists($"{sourceDir}\\_yabber-tpf.xml"))
            {
                Console.WriteLine($"Repacking TPF: {sourceName}...");
                YTPF.Repack(sourceDir, targetDir);
                YBUtil.Backup(sourceDir);
            }
            else
            {
                foreach (string sourceFile in Directory.EnumerateFiles(sourceDir))
                {
                    ManageFile(sourceFile, progress);
                }
            }

            return false;
        }
    }
}
