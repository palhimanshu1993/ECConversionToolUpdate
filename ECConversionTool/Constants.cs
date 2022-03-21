using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECConversionTool
{
    internal class Constants
    {
        public const string Path = @"D:\Test_EC\Hawk64\User";
        public const string SliceModeTag = "SliceMode";
        public const string XmlExtension = ".xml";
        public const string GzExtension = ".gz";
        public const string ImportFilesPattern = "*.xml.gz|*.examcard";
        public const string OutputFolder = "\\ConvertedExamcards";

        public const string ExamCardsFileName = "ExamCards.xml";

    }

    /// <summary>
    /// 
    /// </summary>
    public enum SliceMode
    {
        Thor7300 = 64,
        Hawk7500 = 128
    }
}
