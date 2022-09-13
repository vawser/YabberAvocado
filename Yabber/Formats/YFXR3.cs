using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.IO;
/* using Newtonsoft.Json; */
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Yabber
{
    static class YFXR3
    {
        public static void Unpack(this FXR3 fxr, string sourceFile)
        {
            var xmlSer = new XmlSerializer(typeof(FXR3));
            var writer = new StreamWriter($"{sourceFile}.xml");
            var settings = new XmlWriterSettings() { Indent = true };

            using (var xw = XmlWriter.Create(writer, settings))
            {
                xmlSer.Serialize(xw, fxr);
            }
        }

        /* public static XDocument FXR3ToXml(FXR3 fxr) */
        /* { */
        /*     XDocument xDoc = new XDocument(); */

        /*     using (var xmlWriter = xDoc.CreateWriter()) */
        /*     { */
        /*         var thing = new XmlSerializer(typeof(FXR3)); */
        /*         thing.Serialize(xmlWriter, fxr); */
        /*     } */

        /*     return xDoc; */
        /* } */

        public static void Repack(string sourceFile)
        {
            string outPath;
            if (sourceFile.EndsWith(".fxr.xml"))
                outPath = sourceFile.Replace(".fxr.xml", ".fxr");
            else if (sourceFile.EndsWith(".fxr.dcx.xml"))
                outPath = sourceFile.Replace(".fxr.dcx.xml", ".fxr.dcx");
            else
                throw new InvalidOperationException("Invalid FXR3 xml filename.");

            YBUtil.Backup(outPath);

            var textReader = new StreamReader(sourceFile);
            var xmlSer = new XmlSerializer(typeof(FXR3));

            FXR3 fxr = (FXR3)xmlSer.Deserialize(textReader);

            fxr.Write(outPath);
        }

        /* public static FXR3 XmlToFXR3(XDocument xml) */
        /* { */
        /*     XmlSerializer test = new XmlSerializer(typeof(FXR3)); */
        /*     XmlReader xmlReader = xml.CreateReader(); */

        /*     return (FXR3)test.Deserialize(xmlReader); */
        /* } */
    }
}
