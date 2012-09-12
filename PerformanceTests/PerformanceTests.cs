//  GenericParsing
//  Copyright © 2010 Andrew Rissing
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights 
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//  of the Software, and to permit persons to whom the Software is furnished to do so, 
//  subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all 
//  copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
//  INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//  PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
//  FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
//  ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#region Using Directives

using System;
using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO;
using LumenWorks.Framework.IO.Csv;
using Old = VersionOneGenericParser;
using VB = Microsoft.VisualBasic.FileIO;

#endregion Using Directives

namespace GenericParsing.PerformanceTests
{
    /// <summary>
    /// This class performs test cases against the GenericParser to validate the
    /// output of its parsing and also performs performance testing.
    /// </summary>
    public class PerformanceTests
    {
        #region Performance Constants

        private const int ITERATIONS_OF_TESTS = 10;
        private const int NUMBER_OF_ROWS_IN_DATA = 10;
        private const int NUMBER_OF_COLUMNS_IN_DATA = 14;

        private const string CSV_DATA_FILE = "Csv.txt";
        private const string FW_DATA_FILE = "Fixedwidth.txt";
        private const string SCHEMA_FILE = "Schema.ini";
        private const string TEXT_DRIVER_CONN_STRING = @"Driver={{Microsoft Text Driver (*.txt; *.csv)}};Dbq={0};Extensions=txt;";

        #endregion Performance Constants

        #region Main

        /// <summary>
        ///   I don't like doing this, but hey its just for testing.
        /// </summary>
        private static int RowsOfData;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            long lngFileLength;
            double dblScaledValue;
            string strRowsOfData, strUnits;

            Console.Write(string.Format("Number of iterations of data [1 iteration = {0} rows]: ", PerformanceTests.NUMBER_OF_ROWS_IN_DATA));
            strRowsOfData = Console.ReadLine();

            if ((strRowsOfData.Length < 1) || !int.TryParse(strRowsOfData, out PerformanceTests.RowsOfData) || (PerformanceTests.RowsOfData < 1))
            {
                Console.WriteLine();
                Console.WriteLine("Please supply a valid number.");
                Console.WriteLine();
                Console.WriteLine("<- Press Enter to Proceed ->");
                Console.ReadLine();
                return;
            }

            /////////////////////////
            // Performance Testing //
            /////////////////////////

            // Make sure the user wants to perform it, since it will be a
            // long running process.
            Console.Write(Environment.NewLine);
            Console.WriteLine("The following will generate performance information about the");
            Console.WriteLine("GenericParser class.");
            Console.Write(Environment.NewLine);

            Console.WriteLine("Executions for each test: {0}", PerformanceTests.ITERATIONS_OF_TESTS);
            Console.WriteLine("Rows of CSV Data:         {0}", PerformanceTests.RowsOfData * PerformanceTests.NUMBER_OF_ROWS_IN_DATA);
            Console.WriteLine("Rows of FixedWidth Data:  {0}", PerformanceTests.RowsOfData * PerformanceTests.NUMBER_OF_ROWS_IN_DATA);
            Console.Write(Environment.NewLine);

            Console.WriteLine("<- Press Enter to Proceed ->");
            Console.ReadLine();

            ///////////////////////////////////////////
            // Start executing the performance tests //
            ///////////////////////////////////////////

            PerformanceTests._GenerateSchemaFile();
            PerformanceTests._GenerateCsvData();

            lngFileLength = new FileInfo(CSV_DATA_FILE).Length;

            PerformanceTests._ScaleValue((double)lngFileLength, out dblScaledValue, out strUnits);
            Console.WriteLine("Size of Csv data file: {0,5:#0.00} {1}", dblScaledValue, strUnits);
            Console.WriteLine();

            PerformanceTests._TestRunner(new Action(PerformanceTests._TextDriverCsv), FieldType.Delimited, lngFileLength, "MS Text Driver");
            PerformanceTests._TestRunner(new Action(PerformanceTests._TextFieldParserCsv), FieldType.Delimited, lngFileLength, "MS Text Field Parser");
            PerformanceTests._TestRunner(new Action(PerformanceTests._CsvReader), FieldType.Delimited, lngFileLength, "CsvReader 3.7");
            PerformanceTests._TestRunner(new Action(PerformanceTests._GenericParser10Csv), FieldType.Delimited, lngFileLength, "GenericParser 1.0");
            PerformanceTests._TestRunner(new Action(PerformanceTests._GenericParser11Csv), FieldType.Delimited, lngFileLength, "GenericParser 1.1");

            Console.Write(Environment.NewLine);
            Console.WriteLine("============================================");
            Console.Write(Environment.NewLine);

            PerformanceTests._GenerateFixedWidthData();

            lngFileLength = new FileInfo(PerformanceTests.FW_DATA_FILE).Length;

            PerformanceTests._ScaleValue((double)lngFileLength, out dblScaledValue, out strUnits);
            Console.WriteLine("Size of FixedWidth data file: {0,5:#0.00} {1}", dblScaledValue, strUnits);
            Console.WriteLine();

            PerformanceTests._TestRunner(new Action(PerformanceTests._TextDriverFixedWidth), FieldType.FixedWidth, lngFileLength, "MS Text Driver");
            PerformanceTests._TestRunner(new Action(PerformanceTests._TextFieldParserFixedWidth), FieldType.FixedWidth, lngFileLength, "MS Text Field Parser");
            PerformanceTests._TestRunner(new Action(PerformanceTests._GenericParser10FixedWidth), FieldType.FixedWidth, lngFileLength, "GenericParser 1.0");
            PerformanceTests._TestRunner(new Action(PerformanceTests._GenericParser11FixedWidth), FieldType.FixedWidth, lngFileLength, "GenericParser 1.1");

            Console.Write(Environment.NewLine);
            Console.WriteLine("Completed Performance testing.");
            Console.Write(Environment.NewLine);
            Console.WriteLine("<- Press Enter to Exit ->");
            Console.ReadLine();

            // Cleanup the files we created.
            File.Delete(PerformanceTests.CSV_DATA_FILE);
            File.Delete(PerformanceTests.FW_DATA_FILE);
            File.Delete(PerformanceTests.SCHEMA_FILE);
        }

        #endregion Main

        #region Data Generation

        /// <summary>
        ///   Generates a file full of CSV data to be used in the testing of the performance of
        ///   the <see cref="GenericParser"/>.
        /// </summary>
        private static void _GenerateCsvData()
        {
            using (StreamWriter sw = new StreamWriter(PerformanceTests.CSV_DATA_FILE, false))
            {
                string strData = string.Format(@"10001,MEREP,8,Mere Paillarde,43 rue St. Laurent,Montreal,Quebec,H1J 1C3,Canada,3,""May 13, 1991"",""June 10, 1991"",""May 23, 1991"",79.45{0}10011,WELLI,6,Wellington Importadora,""Rua do Mercado, 12"",Resende,SP,08737-363,Brazil,3,""May 29, 1991"",""June 26, 1991"",""June 3, 1991"",31.54{0}10012,LINOD,6,LINO-Delicateses,Ave. 5 de Mayo Porlamar,I. de Margarita,Nueva Esparta,4980,Venezuela,1,""May 30, 1991"",""June 27, 1991"",""June 3, 1991"",102.59{0}10014,GROSR,4,GROSELLA-Restaurante,5a Ave. Los Palos Grandes,Caracas,DF,1081,Venezuela,3,""June 3, 1991"",""July 1, 1991"",""June 12, 1991"",17.67{0}10018,RATTC,4,""Rattlesnake """"Canyon"""" Grocery"",2817 Milton Dr.,Albuquerque,NM,87110,USA,1,""June 10, 1991"",""July 8, 1991"",""July 5, 1991"",65.46{0}10024,RATTC,4,""Rattlesnake """"Canyon"""" Grocery"",2817 Milton Dr.,Albuquerque,NM,87110,USA,1,""June 19, 1991"",""July 17, 1991"",""June 21, 1991"",5.19{0}10025,RATTC,4,""Rattlesnake """"Canyon"""" Grocery"",2817 Milton Dr.,Albuquerque,NM,87110,USA,3,""June 21, 1991"",""July 19, 1991"",""June 25, 1991"",3.32{0}10030,LILAS,1,LILA-Supermercado,""87 Polk St.{0}Suite 5"",Barquisimeto,Lara,3508,Venezuela,3,""July 1, 1991"",""July 29, 1991"",""July 8, 1991"",26.49{0}10035,HILAA,1,HILARION-Abastos,Carrera 22 con Ave. Carlos Soublette #8-35,San Cristobal,Tachira,5022,Venezuela,3,""July 9, 1991"",""August 6, 1991"",""July 17, 1991"",192.55{0}10038,OLDWO,1,Old World Delicatessen,2743 Bering St.,Anchorage,AK,99508,USA,3,""July 12, 1991"",""August 9, 1991"",""July 15, 1991"",37.37{0}",
                    Environment.NewLine);

                // Populate the memory stream with data.
                for (int i = 0; i < PerformanceTests.RowsOfData; ++i)
                    sw.Write(strData);
            }
        }
        /// <summary>
        ///   Generates a file full of FixedWidth data to be used in the testing of the performance of
        ///   the <see cref="GenericParser"/>.
        /// </summary>
        private static void _GenerateFixedWidthData()
        {
            using (StreamWriter sw = new StreamWriter(PerformanceTests.FW_DATA_FILE, false))
            {
                string strData = string.Format(@"10001MEREP8Mere Paillarde              43 rue St. Laurent                        Montreal       Quebec       H1J 1C3  Canada   3May 13, 1991 June 10, 1991 May 23, 1991 79.45 {0}10011WELLI6Wellington Importadora      Rua do Mercado, 12                        Resende        SP           08737-363Brazil   3May 29, 1991 June 26, 1991 June 3, 1991 31.54 {0}10012LINOD6LINO-Delicateses            Ave. 5 de Mayo Porlamar                   I. de MargaritaNueva Esparta4980     Venezuela1May 30, 1991 June 27, 1991 June 3, 1991 102.59{0}10014GROSR4GROSELLA-Restaurante        5a Ave. Los Palos Grandes                 Caracas        DF           1081     Venezuela3June 3, 1991 July 1, 1991  June 12, 199117.67 {0}10018RATTC4Rattlesnake ""Canyon"" Grocery2817 Milton Dr.                           Albuquerque    NM           87110    USA      1June 10, 1991July 8, 1991  July 5, 1991 65.46 {0}10024RATTC4Rattlesnake ""Canyon"" Grocery2817 Milton Dr.                           Albuquerque    NM           87110    USA      1June 19, 1991July 17, 1991 June 21, 19915.19  {0}10025RATTC4Rattlesnake ""Canyon"" Grocery2817 Milton Dr.                           Albuquerque    NM           87110    USA      3June 21, 1991July 19, 1991 June 25, 19913.32  {0}10030LILAS1LILA-Supermercado           87 Polk St. Suite 5                       Barquisimeto   Lara         3508     Venezuela3July 1, 1991 July 29, 1991 July 8, 1991 26.49 {0}10035HILAA1HILARION-Abastos            Carrera 22 con Ave. Carlos Soublette #8-35San Cristobal  Tachira      5022     Venezuela3July 9, 1991 August 6, 1991July 17, 1991192.55{0}10038OLDWO1Old World Delicatessen      2743 Bering St.                           Anchorage      AK           99508    USA      3July 12, 1991August 9, 1991July 15, 199137.37 {0}",
                    Environment.NewLine);

                // Populate the memory stream with data.
                for (int i = 0; i < PerformanceTests.RowsOfData; ++i)
                    sw.Write(strData);
            }
        }
        /// <summary>
        /// Generates the Schema.ini file for the Microsoft Text Parser.
        /// </summary>
        private static void _GenerateSchemaFile()
        {
            using (StreamWriter sw = new StreamWriter(PerformanceTests.SCHEMA_FILE, false))
            {
                sw.WriteLine("[{0}]", PerformanceTests.CSV_DATA_FILE);
                sw.WriteLine("ColNameHeader=False");
                sw.WriteLine("Col1=Column1 Text");
                sw.WriteLine("Col2=Column2 Text");
                sw.WriteLine("Col3=Column3 Text");
                sw.WriteLine("Col4=Column4 Text");
                sw.WriteLine("Col5=Column5 Text");
                sw.WriteLine("Col6=Column6 Text");
                sw.WriteLine("Col7=Column7 Text");
                sw.WriteLine("Col8=Column8 Text");
                sw.WriteLine("Col9=Column9 Text");
                sw.WriteLine("Col10=Column10 Text");
                sw.WriteLine("Col11=Column11 Text");
                sw.WriteLine("Col12=Column12 Text");
                sw.WriteLine("Col13=Column13 Text");
                sw.WriteLine("Col14=Column14 Text");
                sw.Write(Environment.NewLine);
                sw.WriteLine("[{0}]", PerformanceTests.FW_DATA_FILE);
                sw.WriteLine("ColNameHeader=False");
                sw.WriteLine("Format=FixedLength");
                sw.WriteLine("Col1=Column1 Text Width 5");
                sw.WriteLine("Col2=Column2 Text Width 5");
                sw.WriteLine("Col3=Column3 Text Width 1");
                sw.WriteLine("Col4=Column4 Text Width 28");
                sw.WriteLine("Col5=Column5 Text Width 42");
                sw.WriteLine("Col6=Column6 Text Width 15");
                sw.WriteLine("Col7=Column7 Text Width 13");
                sw.WriteLine("Col8=Column8 Text Width 9");
                sw.WriteLine("Col9=Column9 Text Width 9");
                sw.WriteLine("Col10=Column10 Text Width 1");
                sw.WriteLine("Col11=Column11 Text Width 13");
                sw.WriteLine("Col12=Column12 Text Width 14");
                sw.WriteLine("Col13=Column13 Text Width 13");
                sw.WriteLine("Col14=Column14 Text Width 6");
                sw.Write(Environment.NewLine);
            }
        }

        #endregion Data Generation

        #region Test Runner

        /// <summary>
        ///   Executes the provided <see cref="Action"/> several times to average out the time spent in execution.
        /// </summary>
        /// <param name="action">The action representing the test.</param>
        /// <param name="fieldType">The field type to test (Delimited or FixedWidth).</param>
        /// <param name="lngFileLength">The length of the file being parsed in bytes.</param>
        /// <param name="strTestName">The name of the test.</param>
        private static void _TestRunner(Action action, FieldType fieldType, long lngFileLength, string strTestName)
        {
            Stopwatch sw;
            double dblAvgDuration;

            // Clear out the trash so as to minimize the likelyhood of the garbage being taken out during the tests.
            GC.Collect();
            GC.WaitForFullGCComplete();
            GC.WaitForPendingFinalizers();

            sw = Stopwatch.StartNew();

            for (int intTestIteration = 0; intTestIteration < PerformanceTests.ITERATIONS_OF_TESTS; ++intTestIteration)
            {
                action();
            }

            sw.Stop();

            dblAvgDuration = ((double)sw.ElapsedMilliseconds) / ((double)PerformanceTests.ITERATIONS_OF_TESTS);

            Console.WriteLine(string.Format("{0,-10} {1,21} - Avg. Time {2,10:#0.0} (ms) [{3,14}]",
                fieldType.ToString(),
                strTestName,
                dblAvgDuration,
                PerformanceTests._CalculateThroughput(lngFileLength, dblAvgDuration)));
        }
        /// <summary>
        ///   Gets the scaled value for the provided value.
        /// </summary>
        /// <param name="lngValue">The unscaled value in bytes.</param>
        /// <param name="dblScaledValue">The scaled value in the units provided by <paramref name="strUnits"/>.</param>
        /// <param name="strUnits">The units the scaled value is now in.</param>
        private static void _ScaleValue(double dblValue, out double dblScaledValue, out string strUnits)
        {
            if (dblValue < 1024L)
            {
                dblScaledValue = dblValue;
                strUnits = " b";
            }
            else if (dblValue < (1024D * 1024D))
            {
                dblScaledValue = dblValue / 1024D;
                strUnits = "kb";
            }
            else if (dblValue < (1024D * 1024D * 1024D))
            {
                dblScaledValue = dblValue / (1024D * 1024D);
                strUnits = "mb";
            }
            else if (dblValue < (1024L * 1024L * 1024L * 1024L))
            {
                dblScaledValue = dblValue / (1024D * 1024D * 1024D);
                strUnits = "gb";
            }
            else  /* if (dblValue < (1024D * 1024D * 1024D * 1024D * 1024D)) */
            {
                dblScaledValue = dblValue / (1024D * 1024D * 1024D * 1024D);
                strUnits = "tb";
            }
        }
        /// <summary>
        ///   Calculates the throughput by the length of the file and the average duration to parse it.
        /// </summary>
        /// <param name="lngFileLength">The length of the file in bytes.</param>
        /// <param name="dblAvgDuration">The average duration to parse the file.</param>
        /// <returns>The string represntation of the throughput.</returns>
        private static string _CalculateThroughput(long lngFileLength, double dblAvgDuration)
        {
            string strUnits;
            double dblUnscaledThroughput, dblScaledThroughput;

            // Multiply the length by 1000 change the units to seconds.
            dblUnscaledThroughput = (((double)lngFileLength) * 1000d) / dblAvgDuration;

            // Perform the scaling of the filelength.
            PerformanceTests._ScaleValue(dblUnscaledThroughput, out dblScaledThroughput, out strUnits);

            return string.Format("{0,9:#0.000} {1}/s", dblScaledThroughput, strUnits);
        }

        #endregion Test Runner

        #region Microsoft's TextDriver

        /// <summary>
        /// Uses the Microsoft Text Driver to parse the CSV file, to give a baseline against an
        /// established class library for parsing.
        /// </summary>
        private static void _TextDriverCsv()
        {
            string s;

            using (OdbcConnection connection = new OdbcConnection(string.Format(PerformanceTests.TEXT_DRIVER_CONN_STRING, Directory.GetCurrentDirectory())))
            {
                using (OdbcCommand command = new OdbcCommand("SELECT * FROM " + PerformanceTests.CSV_DATA_FILE, connection))
                {
                    connection.Open();

                    using (OdbcDataReader reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; ++i)
                                s = reader[i] as string;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Uses the Microsoft Text Driver to parse the FixedWidth file, to give a baseline
        /// against an established class library for parsing.
        /// </summary>
        private static void _TextDriverFixedWidth()
        {
            string s;

            using (OdbcConnection connection = new OdbcConnection(string.Format(PerformanceTests.TEXT_DRIVER_CONN_STRING, Directory.GetCurrentDirectory())))
            {
                using (OdbcCommand command = new OdbcCommand("SELECT * FROM " + FW_DATA_FILE, connection))
                {
                    connection.Open();

                    using (OdbcDataReader reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; ++i)
                                s = reader[i] as string;
                        }
                    }
                }
            }
        }

        #endregion Microsoft's TextDriver

        #region TextFieldParser

        /// <summary>
        /// Uses the Microsoft Text Field Parser to parse the CSV file, to give a baseline against an
        /// established class library for parsing.
        /// </summary>
        private static void _TextFieldParserCsv()
        {
            string[] fields;
            string s;

            using (VB.TextFieldParser tfp = new VB.TextFieldParser(PerformanceTests.CSV_DATA_FILE))
            {
                tfp.SetDelimiters(",");
                tfp.CommentTokens = new string[] { "#" };
                tfp.HasFieldsEnclosedInQuotes = true;

                while (!tfp.EndOfData)
                {
                    fields = tfp.ReadFields();

                    for (int i = 0; i < fields.Length; ++i)
                        s = fields[i] as string;
                }
            }
        }
        /// <summary>
        /// Uses the Microsoft Text Field Parser to parse the FixedWidth file, to give a baseline
        /// against an established class library for parsing.
        /// </summary>
        private static void _TextFieldParserFixedWidth()
        {
            string[] fields;
            string s;

            using (VB.TextFieldParser tfp = new VB.TextFieldParser(PerformanceTests.FW_DATA_FILE))
            {
                tfp.TextFieldType = VB.FieldType.FixedWidth;
                tfp.SetFieldWidths(new int[PerformanceTests.NUMBER_OF_COLUMNS_IN_DATA] { 5, 5, 1, 28, 42, 15, 13, 9, 9, 1, 13, 14, 13, 6 });
                tfp.CommentTokens = new string[] { "#" };
                tfp.HasFieldsEnclosedInQuotes = true;

                while (!tfp.EndOfData)
                {
                    fields = tfp.ReadFields();

                    for (int i = 0; i < fields.Length; ++i)
                        s = fields[i] as string;
                }
            }
        }

        #endregion TextFieldParser

        #region CsvReader 3.7

        /// <summary>
        ///   Uses the <see cref="CsvReader"/> to read the Csv file.
        /// </summary>
        private static void _CsvReader()
        {
            string s;

            using (StreamReader sr = new StreamReader(PerformanceTests.CSV_DATA_FILE))
            {
                using (CsvReader csvReader = new CsvReader(sr, false, ',', '"', '"', '#', ValueTrimmingOptions.None, 4096))
                {
                    while (csvReader.ReadNextRecord())
                    {
                        for (int i = 0; i < csvReader.FieldCount; ++i)
                            s = csvReader[i];
                    }
                }
            }
        }

        #endregion CsvReader 3.7

        #region GenericParser 1.0

        /// <summary>
        ///   Uses the <see cref="GenericParser"/> to read the Csv file.
        /// </summary>
        private static void _GenericParser10Csv()
        {
            string s;

            using (Old.GenericParser gp = new Old.GenericParser(PerformanceTests.CSV_DATA_FILE))
            {
                gp.MaxBufferSize = 4096;

                while (gp.Read())
                {
                    for (int i = 0; i < gp.ColumnCount; ++i)
                        s = gp[i];
                }
            }
        }
        /// <summary>
        ///   Uses the <see cref="GenericParser"/> to read the FixedWidth file.
        /// </summary>
        private static void _GenericParser10FixedWidth()
        {
            string s;

            using (Old.GenericParser gp = new Old.GenericParser(PerformanceTests.FW_DATA_FILE))
            {
                gp.MaxBufferSize = 4096;
                gp.ColumnWidths = new int[PerformanceTests.NUMBER_OF_COLUMNS_IN_DATA] { 5, 5, 1, 28, 42, 15, 13, 9, 9, 1, 13, 14, 13, 6 };
                gp.TrimResults = true;

                while (gp.Read())
                {
                    for (int i = 0; i < gp.ColumnCount; ++i)
                        s = gp[i];
                }
            }
        }

        #endregion GenericParser 1.0

        #region GenericParser 1.1

        /// <summary>
        ///   Uses the <see cref="GenericParser"/> to read the Csv file.
        /// </summary>
        private static void _GenericParser11Csv()
        {
            string s;

            using (GenericParser gp = new GenericParser(PerformanceTests.CSV_DATA_FILE))
            {
                gp.MaxBufferSize = 4096;

                while (gp.Read())
                {
                    for (int i = 0; i < gp.ColumnCount; ++i)
                        s = gp[i];
                }
            }
        }
        /// <summary>
        ///   Uses the <see cref="GenericParser"/> to read the FixedWidth file.
        /// </summary>
        private static void _GenericParser11FixedWidth()
        {
            string s;

            using (GenericParser gp = new GenericParser(PerformanceTests.FW_DATA_FILE))
            {
                gp.MaxBufferSize = 4096;
                gp.ColumnWidths = new int[PerformanceTests.NUMBER_OF_COLUMNS_IN_DATA] { 5, 5, 1, 28, 42, 15, 13, 9, 9, 1, 13, 14, 13, 6 };
                gp.TrimResults = true;

                while (gp.Read())
                {
                    for (int i = 0; i < gp.ColumnCount; ++i)
                        s = gp[i];
                }
            }
        }

        #endregion GenericParser 1.1
    }
}