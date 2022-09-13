using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.IO;
using Newtonsoft.Json;

namespace Yabber
{
    static class YBTAB
    {
        public static void Unpack(this BTAB btab, string sourceFile)
        {
            File.WriteAllText($"{sourceFile}.json", YBUtil.JsonSerialize(btab));
        }

        public static void Repack(string sourceFile)
        {
            string outPath;
            if (sourceFile.EndsWith(".btab.json"))
                outPath = sourceFile.Replace(".btab.json", ".btab");
            else if (sourceFile.EndsWith(".btab.dcx.json"))
                outPath = sourceFile.Replace(".btab.dcx.json", ".btab.dcx");
            else
                throw new InvalidOperationException("Invalid BTAB json filename.");

            YBUtil.Backup(outPath);
            YBUtil.JsonDeserialize<BTAB>(File.ReadAllText(sourceFile)).Write(outPath);
        }
    }
}
