using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;

namespace countBox
{       
    class Program
    {
        static string SampleFile = "SampleLog.txt";

        static void Main(string[] args)
        {
            // group all control number occurences in EDIFACT for removal
            string UnhCtrlRmv = @".+?'UNH\+(\d+)\+.+?'UNT\+\d+?\+(\1)'UNZ\+\d+?\+(\1).*";   // marked 
            // group all 2nd column from flat XML file for removal
            string ColRmv = @"^(\w+)\s+(\w+)\s+.*";
            // group UNOA process time and all control number occurences in EDIFACT for removal
            // +140710:0425+1048'UNH
            //string TmUnhCtrlRmv = @".+?\+14071([01]:\d+\+\d+).*'UNH\+(\d+)\+.+?'DTM\+137:(\d+:)\d+'.+?'UNT\+\d+?\+(\2)'UNZ\+\d+?\+(\2).*";
            string TmUnhCtrlRmv = @".+?\+14071([01]:\d+\+)(\d+)\+*\d{0,1}'UNH\+(\d+)\+.+?'DTM\+137:(\d+:)\d+'.+?'UNT\+\d+?\+(\3)'UNZ\+\d+\+(\2).*";
            string ZzUnhCtrlRmv = @".+?:ZZ\+(\d+:\d+\+)(\d+)'UNH\+(\d+)\+.+?'UNT\+\d+\+(\3)'UNZ\+\d+\+(\2).*";
            //string Unh14CtrlRmv = @".*^UNB\+.*:14\+(\d+:\d+\+)(\d+).*^UNH\+(\d+)\+.*^UNT\+\d+\+(\3).*^UNZ\+\d+\+(\2).*";
            string Unh14CtrlRmv = @".*UNB\+.*:14\+(\d+:\d+)\+(\d+)'UNH\+(\d+)\+.*'DTM\+137:(\d+):.*'UNT\+\d+\+(\3)'UNZ\+\d+\+(\2)";
            //<ORDER_MESSAGE_150 partner="FOODSTUFFS" transaction="ORDERMESSAGE" version="1.50" timestamp="2014-07-17T01:35:10" document_mode="Live">
            string sugarCtrlRmv = ".*ORDER_MESSAGE.*timestamp=\"(.+?)\".*";
            //string tmVarianceCtrlRmv = @".*<DateTime>\r\n<Year>(\d+).*\r\n<>(\d+).*\r\n<Day>(\d+).*\r\n<Hour>(\d+).*\r\n<Minute>(\d+).*\r\n<Second>(\d+).*\r\n<SubSecond>(\d+).*";
            string tmVarianceCtrlRmv = @".*^\s*<DateTime>\s*^\s*<Year>(\d+)<.*\s*^\s*<Month>(\d+)<.*\s*^\s*<Day>(\d+)<.*\s*^\s*<Hour>(\d+)<.*\s*^\s*<Minute>(\d+)<.*\s*^\s*<Second>(\d+)<.*\s*^\s*<SubSecond>(\d+)<.*\s*";
            //string X12SingleLineCtrlRmv = @".+?\s*.*1407(\d+\*\d+)\*U\*.\d+\*(\d+)\*.+?\s*.*GS\*.+?1407(\d+\*\d+\*)(\d+)\*.*GE\*\d+\*(\4).*IEA\*\d+\*(\2).*";
            //string X12CtrlRmv = @".+?\s*.*1407(\d+\*\d+)\*U\*\d+\*(\d+)\*.+?\s*GS\*.+?1407(\d+\*\d+\*)(\d+)\*(.+?\s){1,}GE\*\d+\*(\4)\s*IEA\*\d+\*(\2).*";
            string ZzUnhCtrlPlusRmv = @".+?:ZZ\+(\d+:\d+\+)(\d+)\+{1,}.*'UNH\+(\d+)\+.+?'UNT\+\d+\+(\3)'UNZ\+\d+\+(\2).*";
            string Unh137CtrlRmv = @".+?'DTM\+137:(\d+):.*";
            //string pipeSepCtrlTmRmv = @".+?\|(\d+)\|(\d+:\d+:\d+)\|.+?.*";
            string pipeSepCtrlTmRmv = @".+?\|(\d+)\|(\d+:\d+:\d+)\|.+?\|(\1)\|(\2)\|.*";
            string X12CreateDateRmv = @".+?\t*\<create-date\>(\d+)/(\d+)/(\d+)\<.*";
            string gxsInstanceRmv = @".*\t*\<Day\>(\d+).*\s*\t*\<Hour\>(\d+).*\s*\t*\<Minute\>(\d+).*\s*\t*\<Second\>(\d+).*\s*\t*\<SubSecond\>(\d+).*";
            string gxsRcvTmRmv = @".*\t*\<ReceivedTime\>(.*T\d+:\d+:\d+)\<.*";
            string gxsMsgIdRmv = @".*\s*\t*\<MessageID\>(.*)\<.*";

            //rmvTrailingSp(true);
            getDiffernetFile(true);
            //copyToFolder();
            //renameFiles();
            //List<int> grps = new List<int> { 1,2,3,4,5 };
            //rmvControlVariance(gxsMsgIdRmv, grps);
            //rmvEdiFactControlVariance(sugarCtrlRmv);
            //getBoxNameProperty();
            //copySample();  
            //getFailedFiles(false);
            //Hashtable tokens = new Hashtable();
            //tokens.Add("140707:", "'UNH");
            //rmvTmMsgIdVariance(tokens);
            //rmvFlatFileCol(ColRmv, 2);
        }

        /// <summary>
        /// retreive the retailer's name from input file. The mail box name is used to create .NET property in DB that will be used to invoke .NET translation 
        /// at the processing server.
        /// </summary>
        private static void getBoxNameProperty()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string InputDir = appSettings["SourceDir"];             
            string OutputDir = appSettings["TargetDir"];            

            FileStream filestream = new FileStream(InputDir + "\\" + SampleFile, FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);

            List<string> inFiles = new List<string>(Directory.GetFiles(InputDir));
            List<string> uNames = new List<string>();
            foreach (var aFile in inFiles)
            {
                //[LINFOXAKL][20140630][78811110][[pronto][0_9][stockAdjustments][v02].xml]
                string baseName = Path.GetFileName(aFile);
                if (baseName.IndexOf(SampleFile) != -1)
                    continue;       // skip log file

                int intNumber = 0;

                // get parts that has content after spliting
                string[] brackstGrps = baseName.Split(new char[] { '[', ']' }).Where(numStr => (string.IsNullOrEmpty(numStr) == false)).ToArray();

                // get file name part that has only digits like [20140630][78811110]
                string[] digitText = brackstGrps.Where(numStr => Int32.TryParse(numStr, out intNumber) == true).ToArray();

                if (digitText.Count() == 2)
                {
                    int startIdx = baseName.IndexOf(digitText[1]) + digitText[1].Length + 1;        // skip the digit string plus her closing bracket
                    int endIdx = baseName.ToUpper().IndexOf(".TXT");
                    int extractLength = endIdx - startIdx;
                    if (extractLength > 0)
                    {
                        string boxName = baseName.Substring(startIdx, extractLength);
                        if (!uNames.Contains(boxName))
                        {
                            uNames.Add(boxName);
                            Console.WriteLine("Unique box name is {0}", boxName);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// remmove all trailing spaces in input file before upload for translation
        /// </summary>
        private static void rmvTrailingSp(bool onlyLastChar)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string InputDir = appSettings["SourceDir"];
            string OutputDir = appSettings["TargetDir"];
            List<string> inFiles = new List<string>(Directory.GetFiles(InputDir));
            foreach (string fName in inFiles)
            {
                string outFName = OutputDir + "\\" + Path.GetFileName(fName);
                Console.WriteLine("Processing {0} ...", fName);
                if (onlyLastChar == false)
                    File.WriteAllText(outFName, (File.ReadAllText(fName).TrimEnd(null)));
                else
                {   // trim off only the last char
                    string bodyText = File.ReadAllText(fName);
                    int lastIdx = bodyText.Length;

                    if ((bodyText[lastIdx - 1] == '\n') && (bodyText[lastIdx - 2] == '\r'))        // only remove a new line if it's at the end of the message
                        File.WriteAllText(outFName, bodyText.Remove((lastIdx - 2), 2));
                }
            }
        }

        private static void rmvEdiFactControlVariance(string pattern)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string InputDir = appSettings["SourceDir"];
            List<string> inFiles = new List<string>(Directory.GetFiles(InputDir));

            FileStream filestream = new FileStream(InputDir + "\\" + SampleFile, FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);

            foreach (string fName in inFiles)
            {
                string baseName = Path.GetFileName(fName);
                if (baseName.IndexOf(SampleFile) != -1)
                    continue;       // skip log file

                string bodyStr = File.ReadAllText(fName);

                Match unhMatch = Regex.Match(bodyStr, pattern);
                char[] tempStr;
                if (unhMatch.Success)
                {
                    string secMatch = unhMatch.Groups[2].Value;
                    string thirdMatch = unhMatch.Groups[3].Value;

                    for (int posIdx = 1; posIdx < unhMatch.Groups.Count; posIdx++)
                    {
                        if ((posIdx > 1) &&
                            (unhMatch.Groups[posIdx].Value != secMatch) &&
                            (unhMatch.Groups[posIdx].Value != thirdMatch))
                        {   // only masked out UNOB (1st group), 2nd, 3rd and last 2 groups
                            continue;
                        }

                        int replaceCnt = unhMatch.Groups[posIdx].Length;
                        int replaceIdx = unhMatch.Groups[posIdx].Index;
                        int lastPos = replaceIdx + replaceCnt;

                        tempStr = bodyStr.ToCharArray();
                        for (int j = replaceIdx; j < lastPos; j++)
                            tempStr[j] = 'x';
                        bodyStr = new string(tempStr);
                    }

                    File.WriteAllText(fName, bodyStr);
                    Console.WriteLine("{0} control nunber removed.", fName);
                }
            }
        }

        
        /// <summary>
        ///  remove the UNH control number variance from files before diff for translation result
        /// </summary>
        /// <param name="tokenTable"></param>
        private static void rmvControlVariance(string pattern, List<int>targetGrps)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string InputDir = appSettings["SourceDir"];
            List<string> inFiles = new List<string>(Directory.GetFiles(InputDir));

            FileStream filestream = new FileStream(InputDir + "\\" + SampleFile, FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);

            foreach (string fName in inFiles)
            {
                string baseName = Path.GetFileName(fName);
                if ((baseName.IndexOf(SampleFile) != -1) ||
                    (baseName.Contains("GXS") == false))
                    continue;       // skip log file

                string bodyStr = File.ReadAllText(fName);
                Regex ItemRegex = new Regex(pattern, RegexOptions.Compiled);

                foreach (Match ItemMatch in ItemRegex.Matches(bodyStr))
                {
                    char[] tempStr;
                    for (int posIdx = 1; posIdx < ItemMatch.Groups.Count; posIdx++)
                    {
                        if (targetGrps.Contains(posIdx) == false)
                            continue;           // only replace groups that has target variance elements

                        int replaceCnt = ItemMatch.Groups[posIdx].Length;
                        int replaceIdx = ItemMatch.Groups[posIdx].Index;
                        int lastPos = replaceIdx + replaceCnt;

                        tempStr = bodyStr.ToCharArray();
                        for (int j = replaceIdx; j < lastPos; j++)
                            tempStr[j] = 'x';
                        bodyStr = new string(tempStr);
                    }
                }
                File.WriteAllText(fName, bodyStr);
                Console.WriteLine("{0} control nunber removed.", fName);

                //Match unhMatch = Regex.Match(bodyStr, pattern, RegexOptions.Multiline);
                //char[] tempStr;
                //if (unhMatch.Success)
                //{
                //    for (int posIdx = 1; posIdx < unhMatch.Groups.Count; posIdx++)
                //    {
                //        if (targetGrps.Contains(posIdx) == false)
                //            continue;           // only replace groups that has target variance elements

                //        int replaceCnt = unhMatch.Groups[posIdx].Length;
                //        int replaceIdx = unhMatch.Groups[posIdx].Index;
                //        int lastPos = replaceIdx + replaceCnt;

                //        tempStr = bodyStr.ToCharArray();
                //        for (int j = replaceIdx; j < lastPos; j++)
                //            tempStr[j] = 'x';
                //        bodyStr = new string(tempStr);
                //    }

                //    File.WriteAllText(fName, bodyStr);
                //    Console.WriteLine("{0} control nunber removed.", fName);
                //}
            }
        }

        /// <summary>
        /// remove varinace which is time based and message ID differences from translated file
        /// </summary>
        /// <param name="sToken"></param>
        /// <param name="eToken"></param>
        private static void rmvTmMsgIdVariance(Hashtable tokenTable)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string InputDir = appSettings["SourceDir"];
            List<string> inFiles = new List<string>(Directory.GetFiles(InputDir));
            foreach (DictionaryEntry de in tokenTable)
            {
                foreach (string fName in inFiles)
                {
                    string baseName = Path.GetFileName(fName);
                    if (baseName.IndexOf(SampleFile) != -1)
                        continue;       // skip log file

                    string bodyStr = File.ReadAllText(fName);
                    var aStringBuilder = new StringBuilder(bodyStr);

                    string sToken = de.Key.ToString();          // key stores the start token to be searched 
                    string eToken = de.Value.ToString();        // value stores the end token to be searched 

                    int startIdx = (bodyStr.IndexOf(sToken) + sToken.Length);
                    int rmvLen = bodyStr.IndexOf(eToken) - startIdx;
                    if (rmvLen > 0)
                    {
                        aStringBuilder.Remove(startIdx, rmvLen);
                    }
                    bodyStr = aStringBuilder.ToString();
                    File.WriteAllText(fName, bodyStr);
                }
            }
        }

        private static void getDiffernetFile(bool goCopy)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string InputDir = appSettings["SourceDir"];     // all files that have been uploaded for translation
            string OutputDir = appSettings["TargetDir"];    // all files that output from translation

            FileStream filestream = new FileStream(InputDir + "\\" + SampleFile, FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);

            List<string> inFiles = new List<string>(Directory.GetFiles(InputDir));
            List<string> outFiles = new List<string>(Directory.GetFiles(OutputDir));

            string srcSeqPath = InputDir + "\\seqDiffFiles";
            if (goCopy && !Directory.Exists(srcSeqPath))
                Directory.CreateDirectory(srcSeqPath);

            string targetSeqPath = OutputDir + "\\seqDiffFiles";
            if (goCopy && !Directory.Exists(targetSeqPath))
                Directory.CreateDirectory(targetSeqPath);

            foreach (var aFile in inFiles)
            {
                string baseName = Path.GetFileName(aFile);
                if (baseName.IndexOf(SampleFile) != -1)
                    continue;       // skip log file
                string partnerName = OutputDir + @"\" + baseName;

                if (outFiles.Contains(partnerName))
                {
                    if (FileCompare(aFile, partnerName) == false)
                    {
                        Console.WriteLine("{0} different", aFile);
                        if (goCopy)
                        {
                            System.IO.File.Copy(aFile, srcSeqPath + @"\" + baseName, true);
                            System.IO.File.Copy(partnerName, targetSeqPath + @"\" + baseName, true);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// compare input directory against output directory where translation result files are stored.
        /// only extract the date & messageID part of those files as unique file name before diff the file content
        /// [SUGAR_AUSTRALIA][20140618][78370576][[flatfile][na][InventoryReport][v02].xml].1
        /// sample file name has [20140618][78370576] as the unique part name.
        /// If file from input dir cannot be found in output dir then it implies translation has failed.
        /// </summary>
        private static void getFailedFiles(bool goCopy)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string InputDir = appSettings["SourceDir"];     // all files that have been uploaded for translation
            string OutputDir = appSettings["TargetDir"];    // all files that output from translation

            FileStream filestream = new FileStream(InputDir + "\\" + SampleFile, FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);

            List<string> inFiles = new List<string>(Directory.GetFiles(InputDir));
            List<string> inList = getUniquNames(inFiles);

            List<string> outFiles = new List<string>(Directory.GetFiles(OutputDir));
            List<string> outList = getUniquNames(outFiles);
            string failedPath = InputDir + "\\failedFiles";

            if (goCopy && !Directory.Exists(failedPath))
                Directory.CreateDirectory(failedPath);

            foreach (var aFile in inList)
            {
                if (!outList.Contains(aFile))
                {
                    string fullName = InputDir + @"\" + aFile;
                    Console.WriteLine("{0}", fullName);
                    if (goCopy)
                    {
                        string fullPathFile = inFiles.Where(failed => (failed.Contains(aFile) == true)).First();
                        string baseName = Path.GetFileName(fullPathFile);
                        string destFile = System.IO.Path.Combine(failedPath, baseName);
                        System.IO.File.Copy(fullPathFile, destFile, true);
                    }
                }
            }
        }

        /// <summary>
        /// take out the unique file name part from an input file.
        /// note that 1 input file can be translated into multiple output files tagged by a sequence number
        /// therefore this unique part in a file name helps to confirm if a file has been translated and saved
        /// into output dir
        /// </summary>
        /// <param name="inputList"></param>
        /// <returns></returns>
        private static List<string> getUniquNames(List<string>inputList)
        {
            List<string> resultList = new List<string>();

            // [AUSTRALIANBAKELS_INT][20140702][78941036][[emea][na][poa][v01].xml]

            foreach (string fName in inputList)
            {
                string baseName = Path.GetFileName(fName);

                if (baseName.IndexOf(SampleFile) != -1)
                    continue;                   // skip log file

                // filter out null string after spliting
                if (baseName[0] != '[')
                    baseName = "[" + baseName;      // compensate output file name that does not start with '['

                string FileNrRmvPat = @"\[.+?\.xml\](\.*\d+)\.txt$";
                Match fileNrMatch = Regex.Match(baseName, FileNrRmvPat);
                if (fileNrMatch.Success == false)
                {
                    if (!resultList.Contains(baseName.ToLower()))
                        resultList.Add(baseName.ToLower());
                }
                else if (fileNrMatch.Groups.Count == 2)
                {
                    int cutCnt = fileNrMatch.Groups[1].Length;
                    int cutIdx = fileNrMatch.Groups[1].Index;
                    string newBase = baseName.Remove(cutIdx, cutCnt);
                    if (!resultList.Contains(newBase.ToLower()))
                        resultList.Add(newBase.ToLower());
                }
            }
            return (resultList);
        }

        private static void renameFiles()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string InputDir = appSettings["SourceDir"];

            FileStream filestream = new FileStream(InputDir + "\\SampleLog.txt", FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);

            string mixMatchDir = InputDir + "\\bracAdded";
            Directory.CreateDirectory(mixMatchDir);
            List<string> allFilesToCopy = new List<string>();

            foreach (string file in Directory.EnumerateFiles(
                    InputDir, "*.*", SearchOption.AllDirectories))
            {
                if ((file.IndexOf("SampleLog") != -1) || (allFilesToCopy.Contains(file)))
                    continue;       // skip log file
                allFilesToCopy.Add(file);
                Console.WriteLine("Raw file to be translated {0}", file);
            }

            int totalCnt = 0;
            foreach (var item in allFilesToCopy)
            {
                string baseName = Path.GetFileName(item);

                File.Copy(item, mixMatchDir + "\\[" + baseName, true);
                ++totalCnt;
            }

            Console.WriteLine();
            Console.WriteLine("Total count of copied files {0}", totalCnt);
            Console.WriteLine();
        }

        /// <summary>
        /// copy a pre-defined number of files from each each mailbox folder into a designated folder 
        /// </summary>
        private static void copyToFolder()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string InputDir = appSettings["SourceDir"];
            string MIXBOXES = "MixBoxes";

            List<string> dirs = new List<string>(Directory.EnumerateDirectories(InputDir));
            Dictionary<string, int> dirsCnt = new Dictionary<string, int>();

            foreach (var dir in dirs)
            {
                if (dir.Contains(MIXBOXES) == false)
                {
                    dirsCnt.Add(dir.Substring(InputDir.Length + 1), 0);        // initialize the count of deposited files for each mail folder
                }
            }

            int maxToCopy = 0;
            Int32.TryParse(appSettings["CopyCnt"], out maxToCopy);

            FileStream filestream = new FileStream(InputDir + "\\SampleLog.txt", FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);

            string mixMatchDir = InputDir + "\\" + MIXBOXES;

            if (Directory.Exists(mixMatchDir))
                Directory.Delete(mixMatchDir, true);

            Directory.CreateDirectory(mixMatchDir);
            List<string> allFilesToCopy = new List<string>();

            foreach (string file in Directory.EnumerateFiles(
                    InputDir, "*.*", SearchOption.AllDirectories))
            {
                if ((file.IndexOf("SampleLog") != -1) || (allFilesToCopy.Contains(file)))
                    continue;       // skip log file

                // chop of the input directory and get the immediate directory name (mail box name) of where the file is deposited
                string dirName = file.Substring(InputDir.Length+1).Split(new[] { '\\' }).Where(a => a.ToUpper().Contains(@".TXT") == false).First();
                
                // make sure copy only a fixed number of files from each mail box folder
                if (dirsCnt.ContainsKey(dirName) && (dirsCnt[dirName] < maxToCopy))
                {
                    allFilesToCopy.Add(file);
                    ++dirsCnt[dirName];
                    Console.WriteLine("Added {0}", file);
                }
            }

            int totalCnt = 0;
            foreach (var item in allFilesToCopy)
            {
                string baseName = Path.GetFileName(item);

                File.Copy(item, mixMatchDir + "\\" + baseName, true);
                ++totalCnt;                
            }

            Console.WriteLine();
            Console.WriteLine("Total count of copied files {0}", totalCnt);
            Console.WriteLine();
        }

        // remove a colum from a flat file
        private static void rmvFlatFileCol(string ColRmv, int ColIdx)
        {
            //"(\w+)\b(\w+)\b.*"; 
            var appSettings = ConfigurationManager.AppSettings;
            string InputDir = appSettings["SourceDir"];

            FileStream filestream = new FileStream(InputDir + "\\SampleLog.txt", FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);

            List<string> inFiles = new List<string>(Directory.GetFiles(InputDir));
            string line;
            foreach (var aFile in inFiles)
            {
                string baseName = Path.GetFileName(aFile);
                if (baseName.IndexOf(SampleFile) != -1)
                    continue;       // skip log file

                System.IO.StreamReader file =
                    new System.IO.StreamReader(@aFile);

                string newFileBody = "";
                while ((line = file.ReadLine()) != null)
                {
                    Match colMatch = Regex.Match(line, ColRmv);
                    if (colMatch.Success && (colMatch.Groups.Count > ColIdx))
                    {
                        string colBody = colMatch.Groups[ColIdx].Value;
                        int colIndex = colMatch.Groups[ColIdx].Index;
                        StringBuilder strBuild = new StringBuilder(line);
                        newFileBody += strBuild.Remove(colIndex, colBody.Length).ToString() + "\n";                         
                    }
                }

                file.Close();
                File.WriteAllText(aFile, newFileBody);
                Console.WriteLine("{0} has column removed", aFile);
            }
        }

        /// <summary>
        /// seperrately copy input files from a batch into various directories based on their mail box name
        /// the same file will be copied again into a mixBox directory used for trabslation.
        /// </summary>
        private static void copySample()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string InputDir = appSettings["BoxDir"];

            FileStream filestream = new FileStream(InputDir + "\\SampleLog.txt", FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);

            HashSet<string> boxNames = new HashSet<string>();
            int maxToCopy = 0;
            Int32.TryParse(appSettings["CopyCnt"], out maxToCopy);

            List<string> inFiles = new List<string>(Directory.GetFiles(InputDir));
            foreach (string fName in inFiles)
            {
                string baseName = Path.GetFileName(fName);
                if (baseName.IndexOf(SampleFile) != -1)
                    continue;       // skip log file

                string unqBox = baseName.Split(new char[] { '[', ']' })[1];
                if (boxNames.Add(unqBox))
                    Console.WriteLine("Unique Box name {0}", unqBox);
            }

            string mixMatchDir = InputDir + "\\MixBoxes";
            Directory.CreateDirectory(mixMatchDir);

            string newDir = "";
            int totalCnt = 0;
            foreach (String hashVal in boxNames)
            {
                newDir = InputDir + "\\" + hashVal;
                Directory.CreateDirectory(newDir);
                Console.WriteLine("New Dir created {0}", newDir);
                string patStr = "[" + hashVal + "]*";
                List<string> boxFiles = new List<string>(Directory.GetFiles(InputDir, @patStr));

                int fileCnt = 0;
                foreach (string boxFname in boxFiles)
                {
                    if (fileCnt >= maxToCopy)
                        break;
                    string baseName = Path.GetFileName(boxFname);

                    File.Copy(boxFname, newDir + "\\" + baseName, true);
                    File.Copy(boxFname, mixMatchDir + "\\" + baseName, true);
                    ++fileCnt;
                }
                Console.WriteLine("Copied {0} files to {1}", fileCnt, newDir);
                Console.WriteLine();
                totalCnt += fileCnt;
            }
            Console.WriteLine("Total count of files {0}", totalCnt);
            Console.WriteLine();
        }

        // This method accepts two strings the represent two files to 
        // compare. A return value of 0 indicates that the contents of the files
        // are the same. A return value of any other value indicates that the 
        // files are not the same.
        private static bool FileCompare(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            fs1 = new FileStream(file1, FileMode.Open);
            fs2 = new FileStream(file2, FileMode.Open);

            // Check the file sizes. If they are not the same, the files 
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            return ((file1byte - file2byte) == 0);
        }
    }
}