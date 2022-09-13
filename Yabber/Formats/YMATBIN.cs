using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.IO;

namespace Yabber
{
    static class YMATBIN
    {
        public static void Unpack(this MATBIN matbin, string sourceFile)
        {
            YBUtil.XmlSerialize<MATBIN>(matbin, sourceFile);
        }

        public static void Repack(string sourceFile)
        {
            string outPath;
            if (sourceFile.EndsWith(".matbin.xml"))
                outPath = sourceFile.Replace(".matbin.xml", ".matbin");
            else if (sourceFile.EndsWith(".matbin.dcx.xml"))
                outPath = sourceFile.Replace(".matbin.dcx.xml", ".matbin.dcx");
            else
                throw new InvalidOperationException("Invalid MATBIN xml filename.");


            YBUtil.XmlDeserialize<MATBIN>(sourceFile).Write(outPath);
        }
    }
}
