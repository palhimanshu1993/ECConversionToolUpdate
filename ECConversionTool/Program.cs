
using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace ECConversionTool
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Debugger.Launch();

                Console.Write("Please enter the source folder location : ");
                //Get the source folder location
                var sourcePath = Console.ReadLine();

                if (Validate(sourcePath))
                {
                    //Create an output folder                    
                    var targetPath = $"{sourcePath}{Constants.OutputFolder}";

                    if (!Directory.Exists(targetPath))
                    {
                        //Create the target directory where converted exam cards will be saved
                        Directory.CreateDirectory(targetPath);
                    }

                    //Copy the exam cards to new ConvertedExamcards folder using File.Copy() method
                    if (Directory.Exists(sourcePath))
                    {
                        string[] filesAtSource = Directory.GetFiles(sourcePath);

                        // Copy the files and overwrite destination files if they already exist.
                        foreach (string file in filesAtSource)
                        {
                            // Use static Path methods to extract only the file name from the path.
                            var fileName = Path.GetFileName(file);
                            var destFile = Path.Combine(targetPath, fileName);
                            File.Copy(file, destFile, true);
                        }
                    }

                    //Get the files with .xml.gz and .examcard extensions.
                    var fileList = GetExamCardFiles(targetPath, Constants.ImportFilesPattern, SearchOption.TopDirectoryOnly);

                    if (fileList != null && fileList.Count > 0)
                    {
                        //Iterate over the list of files
                        foreach (var file in fileList)
                        {
                            FileInfo fileInfoObject = new FileInfo(file.ToString());
                            ReadExamCardFiles(fileInfoObject);
                            CompressEditedExamCardFiles(fileInfoObject);
                            DeleteRedundantFile(fileInfoObject);
                        }
                        Console.WriteLine("\nConversion completed successfully.");
                        Console.WriteLine("\nConverted examcards kept at {0}", targetPath);
                    }
                    Console.WriteLine("\nPress any key to continue...");

                }
                Console.ReadKey();
            }
            catch (ArgumentNullException argNull)
            {
                Console.WriteLine("\nArgumentNull Exception occurred in ExamCard Converiosn tool");
                Console.WriteLine(argNull.Message);
                Console.WriteLine(argNull.StackTrace);
            }
            catch (DirectoryNotFoundException dirEx)
            {
                Console.WriteLine("\nDirectoryNotFoundException occurred in ExamCard Converiosn tool");
                Console.WriteLine(dirEx.Message);
                Console.WriteLine(dirEx.StackTrace);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nUnhandled exception occurred in ExamCard Converiosn tool");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// This method return the collection of the files matching with the provided search pattern.
        /// </summary>
        /// <param name="sourceFolderPath">Location of folder</param>
        /// <param name="searchPattern">Search pattern for the exam card zipped files</param>
        /// <param name="searchOption">Specify wheather to search the current directory or to subdirectories also.</param>
        /// <returns>List of files</returns>
        public static ICollection GetExamCardFiles(string sourceFolderPath, string searchPattern, SearchOption searchOption)
        {

            if (string.IsNullOrEmpty(searchPattern))
                return null;

            List<string> filesList = new List<string>();
            string[] arrExt = searchPattern.Split('|');

            for (int i = 0; i < arrExt.Length; i++)
            {
                filesList.AddRange(Directory.GetFiles(sourceFolderPath, arrExt[i], searchOption));
            }
            return filesList;
        }


        /// <summary>
        /// This method will read the zipped exam card files
        /// </summary>
        /// <param name="fileInfo"></param>
        public static void ReadExamCardFiles(FileInfo fileInfo)
        {
            // It will be used for loading the exam card into XmlDocument format
            XmlDocument doc = new XmlDocument
            {
                XmlResolver = null
            };

            // For storing slice mode nodes info
            XmlNodeList sliceModeNodeList = null;

            if (fileInfo.Exists)
            {
                using (FileStream inFile = fileInfo.OpenRead())
                {
                    using (GZipStream compStream = new GZipStream(inFile, CompressionMode.Decompress))
                    {
                        using (StreamReader sr = new StreamReader(compStream))
                        {
                            doc.Load(sr);
                            sliceModeNodeList = doc.GetElementsByTagName(Constants.SliceModeTag);

                            foreach (XmlNode sliceMode in sliceModeNodeList)
                            {
                                int.TryParse(sliceMode.InnerText, out int sliceModeValue);

                                //Change the slice mode value as per Thor7300 scaner model
                                if (sliceModeValue != (int)SliceMode.Thor7300)
                                {
                                    sliceMode.InnerText = SliceMode.Thor7300.ToString("d");
                                }

                            }
                        }
                    }
                }
                doc.Save(fileInfo.FullName);
            }
        }

        /// <summary>
        /// This method will clean up the folder and delete the intermediate files generated at runtime
        /// </summary>
        /// <param name="fileInfo">This provides properties and instance methods for various operations on files</param>
        public static void DeleteRedundantFile(FileInfo fileInfo)
        {
            var fileList = fileInfo.Directory.GetFiles();

            foreach (var file in fileList)
            {
                FileInfo singleFileInfo = new FileInfo(file.FullName);
                if (singleFileInfo.Name != Constants.ExamCardsFileName)
                {
                    if (singleFileInfo.Extension == Constants.XmlExtension)
                    {
                        File.Delete(singleFileInfo.FullName);
                    }
                }
            }

        }


        /// <summary>
        /// Compress the file 
        /// </summary>
        /// <param name="fileToCompress">This provides properties and instance methods for various operations on files</param>
        public static void CompressEditedExamCardFiles(FileInfo fileToCompress)
        {
            string fileNameWithChangedExtension = string.Empty;
            if (fileToCompress.Extension != Constants.GzExtension)
            {
                fileNameWithChangedExtension = Path.ChangeExtension(fileToCompress.FullName, Constants.XmlExtension.TrimStart('.'));
            }
            else
            {
                fileNameWithChangedExtension = Path.ChangeExtension(fileToCompress.FullName, null);
            }

            File.Move(fileToCompress.FullName, fileNameWithChangedExtension);
            FileInfo fi = new FileInfo(fileNameWithChangedExtension);
            using (FileStream originalFileStream = fi.OpenRead())
            {
                using (FileStream compressedFileStream = File.Create(fi.FullName + Constants.GzExtension))
                {
                    using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                    {
                        originalFileStream.CopyTo(compressionStream);
                    }
                }
            }

        }

        /// <summary>
        /// This method will validate the user input
        /// </summary>
        /// <param name="args">source folder path</param>
        /// <returns>True is source folder is valid else False</returns>
        private static bool Validate(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                Console.WriteLine("Entered source folder location is not valid");
                Console.WriteLine($"Example: {Environment.NewLine} :");
                Console.WriteLine(@"Please enter the source folder location : D:\Demo\ExamCardFileName.xml.gz");
                return false;
            }

            //get files at source
            var getfiles = Directory.GetFiles(args);

            // check if location has any file or not          
            if (getfiles.Length <= 0)
            {
                Console.WriteLine("Examcards not found at source folder: {0}", args);
                return false;
            }

            return true;
        }
        
    }
}
