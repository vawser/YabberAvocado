using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.IO;

namespace Yabber
{
    static class YMTD
    {
        public static void Unpack(this MTD mtd, string sourceFile)
        {
            YBUtil.XmlSerialize<MTD>(mtd, sourceFile);
        }

        public static void Repack(string sourceFile)
        {
            string outPath;
            if (sourceFile.EndsWith(".mtd.xml"))
                outPath = sourceFile.Replace(".mtd.xml", ".mtd");
            else if (sourceFile.EndsWith(".mtd.dcx.xml"))
                outPath = sourceFile.Replace(".mtd.dcx.xml", ".mtd.dcx");
            else
                throw new InvalidOperationException("Invalid MTD xml filename.");

            YBUtil.Backup(outPath);
            YBUtil.XmlDeserialize<MTD>(sourceFile).Write(outPath);
        }
    }
}
