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
using System.IO;
using System.Reflection;
using System.Xml;
using GenericParsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

#endregion Using Directives

namespace GenericParsing.UnitTests
{
    /// <summary>
    ///   The following class contains all of the unit tests for the GenericParser.
    /// </summary>
    public class UnitTests
    {
        #region Testing Constants

        private const string TEST_DATA_FOLDER = "TestData";
        private const string TEST_INPUT_FILE = "txt";
        private const string TEST_CONFIG_FILE = "xml";
        private const string TEST_EXPECTED_OUTPUT_FILE = "out";

        #endregion Testing Constants

        #region TestClasses

        [TestClass]
        public class Properties
        {
            [TestMethod]
            [ExpectedException(typeof(ArgumentOutOfRangeException))]
            public void EmptyArrayColumnWidths()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                    gp.ColumnWidths = new int[0];
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentOutOfRangeException))]
            public void InvalidArrayColumnWidths()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                    gp.ColumnWidths = new int[3] { 1, -2, 4 };
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentOutOfRangeException))]
            public void InvalidMaxBufferSize()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                    gp.MaxBufferSize = -1024;
            }

            [TestMethod]
            public void OutOfBoundsValues()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.MaxRows = -64;
                    gp.SkipStartingDataRows = -2346;
                    gp.ExpectedColumnCount = -12312;

                    Assert.AreEqual(0, gp.MaxRows);
                    Assert.AreEqual(0, gp.SkipStartingDataRows);
                    Assert.AreEqual(0, gp.ExpectedColumnCount);
                }
            }

            [TestMethod]
            public void FirstRowSetsExpectedColumnCountToTrue()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.TextFieldType = FieldType.FixedWidth;
                    gp.ColumnWidths = new int[] { 1, 2, 3 };

                    Assert.IsFalse(gp.FirstRowSetsExpectedColumnCount);
                    Assert.AreEqual(gp.TextFieldType, FieldType.FixedWidth);
                    Assert.IsNotNull(gp.ColumnWidths);

                    gp.FirstRowSetsExpectedColumnCount = true;

                    Assert.IsTrue(gp.FirstRowSetsExpectedColumnCount);
                    Assert.AreEqual(gp.TextFieldType, FieldType.Delimited);
                    Assert.IsNull(gp.ColumnWidths);
                }
            }

            [TestMethod]
            public void SettingExpectedColumnCount()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.TextFieldType = FieldType.FixedWidth;
                    gp.ColumnWidths = new int[] { 1, 2, 4 };

                    Assert.AreEqual(gp.TextFieldType, FieldType.FixedWidth);
                    Assert.IsNotNull(gp.ColumnWidths);
                    Assert.AreEqual(3, gp.ColumnWidths.Length);

                    gp.ExpectedColumnCount = 4;

                    Assert.AreEqual(gp.TextFieldType, FieldType.Delimited);
                    Assert.IsNull(gp.ColumnWidths);
                    Assert.AreEqual(4, gp.ExpectedColumnCount);
                }
            }

            [TestMethod]
            public void FixedWidthToTrue()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.TextFieldType = FieldType.Delimited;
                    gp.FirstRowSetsExpectedColumnCount = true;
                    gp.ColumnDelimiter = ',';

                    Assert.AreEqual(gp.TextFieldType, FieldType.Delimited);
                    Assert.IsTrue(gp.FirstRowSetsExpectedColumnCount);
                    Assert.IsNotNull(gp.ColumnDelimiter);

                    gp.TextFieldType = FieldType.FixedWidth;

                    Assert.AreEqual(gp.TextFieldType, FieldType.FixedWidth);
                    Assert.IsFalse(gp.FirstRowSetsExpectedColumnCount);
                    Assert.IsNull(gp.ColumnDelimiter);
                }
            }

            [TestMethod]
            public void ClearingColumnWidths()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.TextFieldType = FieldType.FixedWidth;
                    gp.ColumnWidths = new int[] { 1, 2, 3 };

                    Assert.AreEqual(gp.TextFieldType, FieldType.FixedWidth);
                    Assert.IsNotNull(gp.ColumnWidths);
                    Assert.AreEqual(3, gp.ColumnWidths.Length);

                    gp.ColumnWidths = null;

                    Assert.AreEqual(gp.TextFieldType, FieldType.Delimited);
                    Assert.IsNull(gp.ColumnWidths);
                }
            }

            [TestMethod]
            public void NotUpdatingDuringParse()
            {
                Action<GenericParserAdapter>[] testActions;

                testActions = new Action<GenericParserAdapter>[]
                {
                    gp => gp.ColumnWidths = new int[] { 1, 2 },
                    gp => gp.MaxBufferSize = 1025,
                    gp => gp.MaxRows = 1000,
                    gp => gp.SkipStartingDataRows = 10,
                    gp => gp.SkipEndingDataRows = 10,
                    gp => gp.ExpectedColumnCount = 5,
                    gp => gp.FirstRowHasHeader = true,
                    gp => gp.TrimResults = true,
                    gp => gp.StripControlChars = true,
                    gp => gp.SkipEmptyRows = true,
                    gp => gp.TextFieldType = FieldType.FixedWidth,
                    gp => gp.IncludeFileLineNumber = true,
                    gp => gp.FirstRowSetsExpectedColumnCount = true,
                    gp => gp.ColumnDelimiter = '|',
                    gp => gp.TextQualifier = '\'',
                    gp => gp.EscapeCharacter = '\\',
                    gp => gp.CommentCharacter = '@',
                    gp => gp.SetDataSource((string)null),
                    gp => gp.SetDataSource((string)null, (Encoding)null),
                    gp => gp.SetDataSource((TextReader)null),
                    gp => gp.Load((XmlReader)null),
                    gp => gp.Load((TextReader)null),
                    gp => gp.Load((Stream)null),
                    gp => gp.Load((string)null),
                    gp => gp.Load((XmlDocument)null)
                };

                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    using (StringReader sr = new StringReader("a\r\na\r\n"))
                    {
                        gp.SetDataSource(sr);

                        // Read the first row to get us into the parsing state.
                        Assert.IsTrue(gp.Read());

                        // Iterate through each delegate and make sure it throws an exception.
                        foreach (Action<GenericParserAdapter> action in testActions)
                        {
                            try
                            {
                                // Executing this action should cause an InvalidOperationException.
                                action(gp);
                            }
                            catch (InvalidOperationException)
                            {
                                continue;
                            }

                            Assert.Fail("Performing action should have thrown an InvalidOperationException.");
                        }
                    }
                }
            }

            [TestMethod]
            public void ColumnCountAndLargestColumnCountConstant()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    using (StringReader sr = new StringReader(string.Format("1,2,3{0}4,5,6{0}7,8,9{0}", Environment.NewLine)))
                    {
                        gp.SetDataSource(sr);

                        while (gp.Read())
                        {
                            Assert.AreEqual(3, gp.ColumnCount);
                            Assert.AreEqual(3, gp.LargestColumnCount);
                        }
                    }
                }
            }

            [TestMethod]
            public void ColumnCountAndLargestColumnCountChanging()
            {
                const int NUMBER_OF_ROWS = 100;

                StringBuilder sb = new StringBuilder();

                for (int intRow = 0; intRow < NUMBER_OF_ROWS; ++intRow)
                {
                    for (int intColumn = 0; intColumn <= intRow; ++intColumn)
                    {
                        if (intColumn > 0)
                            sb.Append(",");

                        sb.Append(intColumn.ToString());
                    }

                    sb.AppendLine();
                }

                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    using (StringReader sr = new StringReader(sb.ToString()))
                    {
                        gp.SetDataSource(sr);

                        for (int intRow = 1; intRow <= NUMBER_OF_ROWS; ++intRow)
                        {
                            Assert.IsTrue(gp.Read());
                            Assert.AreEqual(intRow, gp.ColumnCount);
                            Assert.AreEqual(intRow, gp.LargestColumnCount);
                        }
                    }
                }
            }

            [TestMethod]
            public void ProtectedArrayProperties()
            {
                int[] intArray;

                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    intArray = new int[] { 1, 2, 3, 4, 5 };
                    gp.ColumnWidths = intArray;

                    Assert.AreNotSame(intArray, gp.ColumnWidths);
                    Assert.AreEqual(intArray.Length, gp.ColumnWidths.Length);

                    for (int i = 0; i < intArray.Length; ++i)
                        Assert.AreEqual(intArray[i], gp.ColumnWidths[i]);
                }
            }
        }

        [TestClass]
        public class Miscellaneous
        {
            [TestMethod]
            [ExpectedException(typeof(InvalidOperationException))]
            public void NoDataSource()
            {
                using (GenericParserAdapter parser = new GenericParserAdapter())
                    parser.Read();
            }

            [TestMethod]
            [ExpectedException(typeof(ObjectDisposedException))]
            public void SettingDataSourceTwice()
            {
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    using (StringReader sr1 = new StringReader("abc"),
                                        sr2 = new StringReader("def"))
                    {
                        parser.SetDataSource(sr1);
                        parser.SetDataSource(sr2);

                        // sr1 should be dispose, so accessing it should throw an exception.
                        sr1.ReadToEnd();
                    }
                }
            }

            [TestMethod]
            [ExpectedException(typeof(ParsingException))]
            public void BufferTooSmall()
            {
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    using (StringReader srInput = new StringReader("1,222222,3,4,5,6,7"))
                    {
                        parser.MaxBufferSize = 5;
                        parser.SetDataSource(srInput);
                        parser.Read();
                    }
                }
            }

            [TestMethod]
            [ExpectedException(typeof(InvalidOperationException))]
            public void DelimitedWithoutSetup()
            {
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    using (StringReader srInput = new StringReader("Data that will never be read."))
                    {
                        parser.TextFieldType = FieldType.Delimited;
                        parser.ColumnDelimiter = null;

                        parser.Read();
                    }
                }
            }

            [TestMethod]
            [ExpectedException(typeof(InvalidOperationException))]
            public void FixedWidthWithoutSetup()
            {
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    using (StringReader srInput = new StringReader("Data that will never be read."))
                    {
                        parser.TextFieldType = FieldType.FixedWidth;
                        parser.SetDataSource(srInput);

                        parser.Read();
                    }
                }
            }

            [TestMethod]
            [ExpectedException(typeof(ParsingException))]
            public void TooManyColumnsInFixedWidth()
            {
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    using (StringReader srInput = new StringReader("1 2 3 4 5"))
                    {
                        parser.TextFieldType = FieldType.FixedWidth;
                        parser.ColumnWidths = new int[4] { 2, 2, 2, 2 };
                        parser.SetDataSource(srInput);

                        parser.Read();
                    }
                }
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void NullFileName()
            {
                using (GenericParserAdapter parser = new GenericParserAdapter())
                    parser.SetDataSource((string)null);
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentException))]
            public void NonExistentFile()
            {
                using (GenericParserAdapter parser = new GenericParserAdapter())
                    parser.SetDataSource(@"C:\1234\5678\910ajb.txt");
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void NullTextReader()
            {
                using (GenericParserAdapter parser = new GenericParserAdapter())
                    parser.SetDataSource((TextReader)null);
            }

            [TestMethod]
            public void LoadingSavingDelimited()
            {
                using (GenericParserAdapter parserA = new GenericParserAdapter(),
                                            parserB = new GenericParserAdapter())
                {
                    parserA.ColumnDelimiter = '%';
                    parserA.CommentCharacter = '*';
                    parserA.EscapeCharacter = null;
                    parserA.StripControlChars = true;
                    parserA.SkipEmptyRows = false;
                    parserA.FirstRowSetsExpectedColumnCount = true;
                    parserA.ExpectedColumnCount = 4;
                    parserA.FirstRowHasHeader = false;
                    parserA.TextFieldType = FieldType.Delimited;
                    parserA.IncludeFileLineNumber = true;
                    parserA.MaxBufferSize = 76;
                    parserA.MaxRows = 65423;
                    parserA.SkipStartingDataRows = 13;
                    parserA.SkipEndingDataRows = 123;
                    parserA.TextQualifier = '[';
                    parserA.TrimResults = true;

                    parserB.Load(parserA.Save());

                    Assert.AreEqual<char?>(parserA.ColumnDelimiter, parserB.ColumnDelimiter);
                    Assert.AreEqual<char?>(parserA.CommentCharacter, parserB.CommentCharacter);
                    Assert.AreEqual<char?>(parserA.EscapeCharacter, parserB.EscapeCharacter);
                    Assert.AreEqual(parserA.StripControlChars, parserB.StripControlChars);
                    Assert.AreEqual(parserA.SkipEmptyRows, parserB.SkipEmptyRows);
                    Assert.AreEqual(parserA.FirstRowSetsExpectedColumnCount, parserB.FirstRowSetsExpectedColumnCount);
                    Assert.AreEqual(parserA.ExpectedColumnCount, parserB.ExpectedColumnCount);
                    Assert.AreEqual(parserA.FirstRowHasHeader, parserB.FirstRowHasHeader);
                    Assert.AreEqual(parserA.TextFieldType, parserB.TextFieldType);
                    Assert.AreEqual(parserA.IncludeFileLineNumber, parserB.IncludeFileLineNumber);
                    Assert.AreEqual(parserA.MaxBufferSize, parserB.MaxBufferSize);
                    Assert.AreEqual(parserA.MaxRows, parserB.MaxRows);
                    Assert.AreEqual(parserA.SkipStartingDataRows, parserB.SkipStartingDataRows);
                    Assert.AreEqual(parserA.SkipEndingDataRows, parserB.SkipEndingDataRows);
                    Assert.AreEqual(parserA.TextQualifier, parserB.TextQualifier);
                    Assert.AreEqual(parserA.TrimResults, parserB.TrimResults);
                }
            }

            [TestMethod]
            public void LoadingSavingFixedWidth()
            {
                using (GenericParserAdapter parserA = new GenericParserAdapter(),
                                            parserB = new GenericParserAdapter())
                {
                    parserA.ColumnWidths = new int[4] { 1, 2, 5, 7 };
                    parserA.CommentCharacter = null;
                    parserA.EscapeCharacter = '?';
                    parserA.StripControlChars = false;
                    parserA.SkipEmptyRows = false;
                    parserA.FirstRowSetsExpectedColumnCount = false;
                    parserA.ExpectedColumnCount = 4;
                    parserA.FirstRowHasHeader = false;
                    parserA.TextFieldType = FieldType.FixedWidth;
                    parserA.IncludeFileLineNumber = true;
                    parserA.MaxBufferSize = 76;
                    parserA.MaxRows = 65423;
                    parserA.SkipStartingDataRows = 13;
                    parserA.SkipEndingDataRows = 45;
                    parserA.TextQualifier = '[';
                    parserA.TrimResults = true;

                    parserB.Load(parserA.Save());

                    Assert.AreEqual(parserA.ColumnWidths.Length, parserB.ColumnWidths.Length);

                    for (int i = 0; i < parserA.ColumnWidths.Length; ++i)
                        Assert.AreEqual(parserA.ColumnWidths[i], parserB.ColumnWidths[i]);

                    Assert.AreEqual(parserA.CommentCharacter, parserB.CommentCharacter);
                    Assert.AreEqual(parserA.EscapeCharacter, parserB.EscapeCharacter);
                    Assert.AreEqual(parserA.StripControlChars, parserB.StripControlChars);
                    Assert.AreEqual(parserA.SkipEmptyRows, parserB.SkipEmptyRows);
                    Assert.AreEqual(parserA.FirstRowSetsExpectedColumnCount, parserB.FirstRowSetsExpectedColumnCount);
                    Assert.AreEqual(parserA.ExpectedColumnCount, parserB.ExpectedColumnCount);
                    Assert.AreEqual(parserA.FirstRowHasHeader, parserB.FirstRowHasHeader);
                    Assert.AreEqual(parserA.TextFieldType, parserB.TextFieldType);
                    Assert.AreEqual(parserA.IncludeFileLineNumber, parserB.IncludeFileLineNumber);
                    Assert.AreEqual(parserA.MaxBufferSize, parserB.MaxBufferSize);
                    Assert.AreEqual(parserA.MaxRows, parserB.MaxRows);
                    Assert.AreEqual(parserA.SkipStartingDataRows, parserB.SkipStartingDataRows);
                    Assert.AreEqual(parserA.SkipEndingDataRows, parserB.SkipEndingDataRows);
                    Assert.AreEqual(parserA.TextQualifier, parserB.TextQualifier);
                    Assert.AreEqual(parserA.TrimResults, parserB.TrimResults);
                }
            }

            [TestMethod]
            public void Reusability()
            {
                string strExpectedResult;

                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    strExpectedResult = UnitTests._PrepParserForTest(parser, "ComplexDataDelimiter");

                    using (DataSet dsResult = parser.GetDataSet())
                    {
                        using (StringWriter swProducedResult = new StringWriter())
                        {
                            dsResult.WriteXml(swProducedResult);

                            Assert.AreEqual(strExpectedResult, swProducedResult.ToString());

                            parser.Close();
                            Assert.IsTrue(parser.State == ParserState.Finished);
                        }
                    }

                    /*********************************************************************************/

                    strExpectedResult = UnitTests._PrepParserForTest(parser, "ComplexDataFixed");

                    using (DataSet dsResult = parser.GetDataSet())
                    {
                        using (StringWriter swProducedResult = new StringWriter())
                        {
                            dsResult.WriteXml(swProducedResult);

                            Assert.AreEqual(strExpectedResult, swProducedResult.ToString());

                            parser.Close();
                            Assert.IsTrue(parser.State == ParserState.Finished);
                        }
                    }
                }
            }

            [TestMethod]
            public void Disposing()
            {
                UnitTests._ValidateResults("SimpleDelimiter");
            }

            [TestMethod]
            public void BoundsCheckOnColumns()
            {
                string strExpectedResult;

                // Check this with a header.
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    strExpectedResult = UnitTests._PrepParserForTest(parser, "ReadingInHeader");

                    // Make sure we are able to read one row of data.
                    Assert.IsTrue(parser.Read());

                    // Checking using integers to index the columns.
                    Assert.AreEqual(null, parser.GetColumnName(-3));
                    Assert.AreEqual(null, parser.GetColumnName(30));
                    Assert.AreEqual("a", parser.GetColumnName(0));

                    // Checking using integers to index the columns.
                    Assert.AreEqual(0, parser.GetColumnIndex("a"));
                    Assert.AreEqual(-1, parser.GetColumnIndex("foobar"));
                }

                // Check this without a header.
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    strExpectedResult = UnitTests._PrepParserForTest(parser, "SimpleDelimiter");

                    // Make sure we are able to read one row of data.
                    Assert.IsTrue(parser.Read());

                    // Checking using integers to index the columns.
                    Assert.AreEqual(null, parser.GetColumnName(-3));
                    Assert.AreEqual(null, parser.GetColumnName(30));
                    Assert.AreEqual(null, parser.GetColumnName(0));

                    // Checking using integers to index the columns.
                    Assert.AreEqual(-1, parser.GetColumnIndex("a"));
                    Assert.AreEqual(-1, parser.GetColumnIndex("foobar"));
                }
            }

            [TestMethod]
            public void BoundsCheckOnData()
            {
                string strExpectedResult;

                // Check this with a header.
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    strExpectedResult = UnitTests._PrepParserForTest(parser, "ReadingInHeader");

                    // Make sure we are able to read one row of data.
                    Assert.IsTrue(parser.Read());

                    // Checking using integers to index the columns.
                    Assert.AreEqual(null, parser[-1]);
                    Assert.AreEqual(null, parser[10]);
                    Assert.AreEqual("3", parser[2]);

                    // Checking using integers to index the columns.
                    Assert.AreEqual(null, parser["foobar"]);
                    Assert.AreEqual("1", parser["a"]);
                }

                // Check this without a header.
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    strExpectedResult = UnitTests._PrepParserForTest(parser, "SimpleDelimiter");

                    // Make sure we are able to read one row of data.
                    Assert.IsTrue(parser.Read());

                    // Checking using integers to index the columns.
                    Assert.AreEqual(null, parser[-1]);
                    Assert.AreEqual(null, parser[10]);
                    Assert.AreEqual("c", parser[2]);

                    // Checking using integers to index the columns.
                    Assert.AreEqual(null, parser["foobar"]);
                    Assert.AreEqual(null, parser["a"]);
                }
            }

            [TestMethod]
            public void NotStrippingControlCharactersInEscapedText()
            {
                const string INPUT = "[	\\\"	],\"[	\\\"	]\"";

                using (StringReader sr = new StringReader(INPUT))
                using (GenericParser gp = new GenericParser(sr))
                {
                    gp.EscapeCharacter = '\\';

                    Assert.IsTrue(gp.Read());
                    Assert.AreEqual<int>(2, gp.ColumnCount);
                    Assert.AreEqual<string>("[	\"	]", gp[0]);
                    Assert.AreEqual<string>("[	\"	]", gp[1]);
                }
            }

            [TestMethod]
            public void HandlingShortInput()
            {
                const string INPUT = "abcd";

                using (StringReader sr = new StringReader(INPUT))
                using (GenericParser gp = new GenericParser(sr))
                {
                    Assert.IsTrue(gp.Read());
                    Assert.AreEqual<int>(1, gp.ColumnCount);
                    Assert.AreEqual<string>(INPUT, gp[0]);
                }
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void SetDataSourceNullEncoding()
            {
                const string TEST_FILE = "Test.txt";

                try
                {
                    File.WriteAllText(TEST_FILE, "Blah blah blah");

                    // Check this with a header.
                    using (GenericParserAdapter parser = new GenericParserAdapter())
                    {
                        parser.SetDataSource(TEST_FILE, null);
                    }
                }
                finally
                {
                    File.Delete(TEST_FILE);
                }
            }

            [TestMethod]
            [ExpectedException(typeof(ObjectDisposedException))]
            public void SpecializedConstructors()
            {
                using (StringReader sr = new StringReader(string.Format("a,b,c,d{0}1,2,3,4{0}", Environment.NewLine)))
                {
                    using (GenericParserAdapter parser = new GenericParserAdapter(sr))
                    {
                        using (DataTable dtResult = parser.GetDataTable())
                        {
                            Assert.AreEqual(1, dtResult.Select("[Column1] = 'a' AND [Column2] = 'b' AND [Column3] = 'c' AND [Column4] = 'd'").Length);
                            Assert.AreEqual(1, dtResult.Select("[Column1] = '1' AND [Column2] = '2' AND [Column3] = '3' AND [Column4] = '4'").Length);
                        }
                    }

                    // This should throw an ObjectDisposedException.
                    sr.Peek();
                }
            }

            [TestMethod]
            public void DisposeEvent()
            {
                bool blnDisposeCalled = false;

                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.Disposed += (sender, e) =>
                    {
                        blnDisposeCalled = true;
                    };

                    Assert.IsFalse(blnDisposeCalled);
                }

                Assert.IsTrue(blnDisposeCalled);
            }

            [TestMethod]
            public void DisposeEventException()
            {
                bool blnDisposeCalled = false;

                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.Disposed += (sender, e) =>
                    {
                        blnDisposeCalled = true;
                        throw new ArgumentException("Testing whether or not the exception is caught in the event.");
                    };

                    Assert.IsFalse(blnDisposeCalled);
                }

                Assert.IsTrue(blnDisposeCalled);
            }
        }

        [TestClass]
        public class ProperParsing
        {
            [TestMethod]
            public void SimpleDelimiter()
            {
                UnitTests._ValidateResults("SimpleDelimiter");
            }

            [TestMethod]
            public void SimpleFixedWidth()
            {
                UnitTests._ValidateResults("SimpleFixedWidth");
            }

            [TestMethod]
            public void SkippingComments()
            {
                UnitTests._ValidateResults("SkippingComments");
            }

            [TestMethod]
            public void SkippingEmptyRowsWithDelimiter()
            {
                UnitTests._ValidateResults("SkippingEmptyRowsWithDelimiter");
            }

            [TestMethod]
            public void SkippingEmptyRowsWithFixedWidth()
            {
                UnitTests._ValidateResults("SkippingEmptyRowsWithFixedWidth");
            }

            [TestMethod]
            public void NotSkippingEmptyRowsDelimiter()
            {
                UnitTests._ValidateResults("NotSkippingEmptyRowsDelimiter");
            }

            [TestMethod]
            public void NotSkippingEmptyRowsDelimiterWithoutFileRowNumber()
            {
                UnitTests._ValidateResults("NotSkippingEmptyRowsDelimiterWithoutFileRowNumber");
            }

            [TestMethod]
            public void NotSkippingEmptyRowsDelimiterWithHeaderWithoutFileRowNumber()
            {
                UnitTests._ValidateResults("NotSkippingEmptyRowsDelimiterWithHeaderWithoutFileRowNumber");
            }

            [TestMethod]
            public void NotSkippingEmptyRowsDelimiterWithHeaderWithFileRowNumber()
            {
                UnitTests._ValidateResults("NotSkippingEmptyRowsDelimiterWithHeaderWithFileRowNumber");
            }

            [TestMethod]
            public void NotSkippingEmptyRowsFixedWidth()
            {
                UnitTests._ValidateResults("NotSkippingEmptyRowsFixedWidth");
            }

            [TestMethod]
            public void BasicSkipRows()
            {
                UnitTests._ValidateResults("BasicSkipRows");
            }

            [TestMethod]
            public void BasicMaxRows()
            {
                UnitTests._ValidateResults("BasicMaxRows");
            }

            [TestMethod]
            public void DifferentColumnDelimiter()
            {
                UnitTests._ValidateResults("DifferentColumnDelimiter");
            }

            [TestMethod]
            public void BasicTextQualifiers()
            {
                UnitTests._ValidateResults("BasicTextQualifiers");
            }

            [TestMethod]
            public void TextQualifiersWithDelimiters()
            {
                UnitTests._ValidateResults("TextQualifiersWithDelimiters");
            }

            [TestMethod]
            public void TextQualifierNotClosedAtEnd()
            {
                UnitTests._ValidateResults("TextQualifierNotClosedAtEnd");
            }

            [TestMethod]
            public void TextQualifierBeginningAndEnd()
            {
                UnitTests._ValidateResults("TextQualifierBeginningAndEnd");
            }

            [TestMethod]
            public void TextQualifierWithoutEscape()
            {
                UnitTests._ValidateResults("TextQualifierWithoutEscape");
            }

            [TestMethod]
            public void EscapeWithoutTextQualifier()
            {
                UnitTests._ValidateResults("EscapeWithoutTextQualifier");
            }

            [TestMethod]
            public void BasicEscapedCharacters()
            {
                UnitTests._ValidateResults("BasicEscapedCharacters");
            }
            
            [TestMethod]
            public void EscapedCharacterAtEndOfRowDelimiter()
            {
                UnitTests._ValidateResults("EscapedCharacterAtEndOfRowDelimiter");
            }

            [TestMethod]
            public void EscapedCharacterAtEndOfRowFixedWidth()
            {
                UnitTests._ValidateResults("EscapedCharacterAtEndOfRowFixedWidth");
            }

            [TestMethod]
            public void EscapedCharacterAtEndOfFile()
            {
                UnitTests._ValidateResults("EscapedCharacterAtEndOfFile");
            }

            [TestMethod]
            public void ReadingInHeader()
            {
                UnitTests._ValidateResults("ReadingInHeader");
            }

            [TestMethod]
            public void HeaderRowWithoutData()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.FirstRowHasHeader = true;

                    Assert.IsTrue(gp.FirstRowHasHeader);

                    using (StringReader sr = new StringReader("a,b,c,d"))
                    {
                        gp.SetDataSource(sr);

                        using (DataTable dt = gp.GetDataTable())
                        {
                            Assert.IsNotNull(dt);
                            Assert.AreEqual(4, dt.Columns.Count);
                            Assert.AreEqual("a", dt.Columns[0].ColumnName);
                            Assert.AreEqual("b", dt.Columns[1].ColumnName);
                            Assert.AreEqual("c", dt.Columns[2].ColumnName);
                            Assert.AreEqual("d", dt.Columns[3].ColumnName);
                            Assert.AreEqual(0, dt.Rows.Count);
                        }
                    }
                }
            }

            [TestMethod]
            public void HeaderRowWithoutDataAndIncludeFileLineNumber()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.FirstRowHasHeader = true;
                    gp.IncludeFileLineNumber = true;

                    Assert.IsTrue(gp.FirstRowHasHeader);

                    using (StringReader sr = new StringReader("a,b,c,d"))
                    {
                        gp.SetDataSource(sr);

                        using (DataTable dt = gp.GetDataTable())
                        {
                            Assert.IsNotNull(dt);
                            Assert.AreEqual(5, dt.Columns.Count);
                            Assert.AreEqual("FileLineNumber", dt.Columns[0].ColumnName);
                            Assert.AreEqual("a", dt.Columns[1].ColumnName);
                            Assert.AreEqual("b", dt.Columns[2].ColumnName);
                            Assert.AreEqual("c", dt.Columns[3].ColumnName);
                            Assert.AreEqual("d", dt.Columns[4].ColumnName);
                            Assert.AreEqual(0, dt.Rows.Count);
                        }
                    }
                }
            }

            [TestMethod]
            public void EmptyDataSource()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    using (StringReader sr = new StringReader(string.Empty))
                    {
                        gp.SetDataSource(sr);

                        using (DataTable dt = gp.GetDataTable())
                        {
                            Assert.IsNotNull(dt);
                            Assert.AreEqual(0, dt.Columns.Count);
                            Assert.AreEqual(0, dt.Rows.Count);
                        }
                    }
                }
            }

            [TestMethod]
            public void HeaderRowWithoutDataWithExpectedColumnCount()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.FirstRowHasHeader = true;
                    gp.FirstRowSetsExpectedColumnCount = true;

                    Assert.IsTrue(gp.FirstRowHasHeader);
                    Assert.IsTrue(gp.FirstRowSetsExpectedColumnCount);
                    Assert.AreEqual(0, gp.ExpectedColumnCount);

                    using (StringReader sr = new StringReader("a,b,c,d"))
                    {
                        gp.SetDataSource(sr);

                        using (DataTable dt = gp.GetDataTable())
                        {
                            Assert.IsNotNull(dt);
                            Assert.AreEqual(4, dt.Columns.Count);
                            Assert.AreEqual(0, dt.Rows.Count);
                            Assert.AreEqual(4, gp.ExpectedColumnCount);
                        }
                    }
                }
            }

            [TestMethod]
            public void ReadingInHeaderAfterComments()
            {
                UnitTests._ValidateResults("ReadingInHeaderAfterComments");
            }

            [TestMethod]
            public void MaxRowsIgnoresCommentsHeader()
            {
                UnitTests._ValidateResults("MaxRowsIgnoresCommentsHeader");
            }

            [TestMethod]
            public void HandlingDuplicateColumnNames()
            {
                UnitTests._ValidateResults("HandlingDuplicateColumnNames");
            }

            [TestMethod]
            public void RowWithoutColumnDelimiter()
            {
                UnitTests._ValidateResults("RowWithoutColumnDelimiter");
            }

            [TestMethod]
            public void TrimmingResults()
            {
                UnitTests._ValidateResults("TrimmingResults");
            }

            [TestMethod]
            public void ReadLastRowWithoutRowDelimiter()
            {
                UnitTests._ValidateResults("ReadLastRowWithoutRowDelimiter");
            }

            [TestMethod]
            public void ReadLastRowWithRowDelimiter()
            {
                UnitTests._ValidateResults("ReadLastRowWithRowDelimiter");
            }

            [TestMethod]
            public void IncludeLineNumber()
            {
                UnitTests._ValidateResults("IncludeLineNumber");
            }

            [TestMethod]
            public void UnicodeSupported()
            {
                UnitTests._ValidateResults("UnicodeSupported");
            }

            [TestMethod]
            public void EncodingTest()
            {
                const string TEST_FILE = "Test.txt";

                try
                {
                    File.WriteAllText(TEST_FILE, "между,tölvuiðnaðarins,ũΩ₤,používat", Encoding.Unicode);

                    using (GenericParserAdapter gp = new GenericParserAdapter())
                    {
                        gp.SetDataSource(TEST_FILE, Encoding.Unicode);

                        using (DataTable dt = gp.GetDataTable())
                        {
                            Assert.IsNotNull(dt);
                            Assert.AreEqual(4, dt.Columns.Count);
                            Assert.AreEqual(1, dt.Rows.Count);
                        }
                    }
                }
                finally
                {
                    File.Delete(TEST_FILE);
                }
            }

            [TestMethod]
            public void SkippingAllRows()
            {
                UnitTests._ValidateResults("SkippingAllRows");
            }

            [TestMethod]
            public void LargeDataParsing()
            {
                const int NUMBER_OF_COLUMNS_IN_BASE_DATA = 6;
                const int NUMBER_OF_ITERATIONS_OF_BASE_DATA = 1;
                const int NUMBER_OF_ROWS_IN_BASE_DATA = 8;

                int intCurrentDataRowIndex;
                string strData;

                using (MemoryStream msData = new MemoryStream(1572864))
                {
                    using (StreamWriter sw = new StreamWriter(msData))
                    {
                        using (GenericParserAdapter parser = new GenericParserAdapter())
                        {
                            parser.FirstRowHasHeader = true;

                            // Add the header to it.
                            sw.Write(string.Format("Column1,Column2,Column3,Column4,Column5,Column6{0}", Environment.NewLine));

                            strData = string.Format("a,b,c,d,e,f{0}1,2,3,4,5,6{0}g,h,i,j,k,l{0}7,8,9,10,11,12{0}m,n,o,p,q,r{0}13,14,15,16,17,18{0}t,u,v,w,x,y{0}19,20,21,22,23,24{0}",
                              Environment.NewLine);

                            // Populate the memory stream with data.
                            for (int i = 0; i < NUMBER_OF_ITERATIONS_OF_BASE_DATA; ++i)
                                sw.Write(strData);

                            sw.Flush();
                            msData.Position = 0;

                            using (StreamReader sr = new StreamReader(msData))
                            {
                                parser.SetDataSource(sr);

                                while (parser.Read())
                                {
                                    Assert.AreEqual(NUMBER_OF_COLUMNS_IN_BASE_DATA, parser.ColumnCount);

                                    Assert.AreEqual("Column1", parser.GetColumnName(0));
                                    Assert.AreEqual("Column2", parser.GetColumnName(1));
                                    Assert.AreEqual("Column3", parser.GetColumnName(2));
                                    Assert.AreEqual("Column4", parser.GetColumnName(3));
                                    Assert.AreEqual("Column5", parser.GetColumnName(4));
                                    Assert.AreEqual("Column6", parser.GetColumnName(5));

                                    intCurrentDataRowIndex = (parser.DataRowNumber - 1) % NUMBER_OF_ROWS_IN_BASE_DATA;

                                    switch (intCurrentDataRowIndex)
                                    {
                                        case 0:
                                            Assert.AreEqual("a", parser[0]);
                                            Assert.AreEqual("b", parser[1]);
                                            Assert.AreEqual("c", parser[2]);
                                            Assert.AreEqual("d", parser[3]);
                                            Assert.AreEqual("e", parser[4]);
                                            Assert.AreEqual("f", parser[5]);
                                            break;

                                        case 1:
                                            Assert.AreEqual("1", parser[0]);
                                            Assert.AreEqual("2", parser[1]);
                                            Assert.AreEqual("3", parser[2]);
                                            Assert.AreEqual("4", parser[3]);
                                            Assert.AreEqual("5", parser[4]);
                                            Assert.AreEqual("6", parser[5]);
                                            break;

                                        case 2:
                                            Assert.AreEqual("g", parser[0]);
                                            Assert.AreEqual("h", parser[1]);
                                            Assert.AreEqual("i", parser[2]);
                                            Assert.AreEqual("j", parser[3]);
                                            Assert.AreEqual("k", parser[4]);
                                            Assert.AreEqual("l", parser[5]);
                                            break;

                                        case 3:
                                            Assert.AreEqual("7", parser[0]);
                                            Assert.AreEqual("8", parser[1]);
                                            Assert.AreEqual("9", parser[2]);
                                            Assert.AreEqual("10", parser[3]);
                                            Assert.AreEqual("11", parser[4]);
                                            Assert.AreEqual("12", parser[5]);
                                            break;

                                        case 4:
                                            Assert.AreEqual("m", parser[0]);
                                            Assert.AreEqual("n", parser[1]);
                                            Assert.AreEqual("o", parser[2]);
                                            Assert.AreEqual("p", parser[3]);
                                            Assert.AreEqual("q", parser[4]);
                                            Assert.AreEqual("r", parser[5]);
                                            break;

                                        case 5:
                                            Assert.AreEqual("13", parser[0]);
                                            Assert.AreEqual("14", parser[1]);
                                            Assert.AreEqual("15", parser[2]);
                                            Assert.AreEqual("16", parser[3]);
                                            Assert.AreEqual("17", parser[4]);
                                            Assert.AreEqual("18", parser[5]);
                                            break;

                                        case 6:
                                            Assert.AreEqual("t", parser[0]);
                                            Assert.AreEqual("u", parser[1]);
                                            Assert.AreEqual("v", parser[2]);
                                            Assert.AreEqual("w", parser[3]);
                                            Assert.AreEqual("x", parser[4]);
                                            Assert.AreEqual("y", parser[5]);
                                            break;

                                        case 7:
                                            Assert.AreEqual("19", parser[0]);
                                            Assert.AreEqual("20", parser[1]);
                                            Assert.AreEqual("21", parser[2]);
                                            Assert.AreEqual("22", parser[3]);
                                            Assert.AreEqual("23", parser[4]);
                                            Assert.AreEqual("24", parser[5]);
                                            break;

                                        default:

                                            Assert.Fail(string.Format("Invalid state reached when parsing (DataRowNumber: {0}, CurrentDataRowIndex: {1})",
                                              parser.DataRowNumber,
                                              intCurrentDataRowIndex));

                                            break;
                                    }
                                }
                            }

                            Assert.AreEqual(NUMBER_OF_ITERATIONS_OF_BASE_DATA * NUMBER_OF_ROWS_IN_BASE_DATA, parser.DataRowNumber);
                        }
                    }
                }
            }

            [TestMethod]
            public void ComplexDataDelimiter()
            {
                UnitTests._ValidateResults("ComplexDataDelimiter");
            }

            [TestMethod]
            public void ComplexDataFixed()
            {
                UnitTests._ValidateResults("ComplexDataFixed");
            }

            [TestMethod]
            public void XmlTest()
            {
                XmlDocument xmlResult;
                string strExpectedResult;

                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    strExpectedResult = UnitTests._PrepParserForTest(parser, "XmlTest");

                    xmlResult = parser.GetXml();
                    xmlResult.Normalize();

                    Assert.AreEqual(strExpectedResult, xmlResult.OuterXml);
                }
            }

            [TestMethod]
            public void SkippingRowsAtEndOfBuffer()
            {
                UnitTests._ValidateResults("SkippingRowsAtEndOfBuffer");
            }

            [TestMethod]
            public void SkippingEndingRows()
            {
                UnitTests._ValidateResults("SkippingEndingRows");
            }

            [TestMethod]
            public void SkippingEndingRowsAll()
            {
                UnitTests._ValidateResults("SkippingEndingRowsAll");
            }

            [TestMethod]
            public void SkippingEndingRowsExactlyAll()
            {
                UnitTests._ValidateResults("SkippingEndingRowsExactlyAll");
            }

            [TestMethod]
            public void SkippingBeginningAndEndingRows()
            {
                UnitTests._ValidateResults("SkippingBeginningAndEndingRows");
            }

            [TestMethod]
            public void SkippingBeginningAndEndingRowsAll()
            {
                UnitTests._ValidateResults("SkippingBeginningAndEndingRowsAll");
            }

            [TestMethod]
            public void FirstRowSetsExpectedColumnCountWithHeaderRow()
            {
                UnitTests._ValidateResults("FirstRowSetsExpectedColumnCountWithHeaderRow");
            }

            [TestMethod]
            public void FirstRowSetsExpectedColumnCountWithoutHeaderRow()
            {
                UnitTests._ValidateResults("FirstRowSetsExpectedColumnCountWithoutHeaderRow");
            }

            [TestMethod]
            [ExpectedException(typeof(ParsingException))]
            public void FirstRowSetsExpectedColumnCountWithHeaderRowError()
            {
                UnitTests._ValidateResults("FirstRowSetsExpectedColumnCountWithHeaderRowError");
            }

            [TestMethod]
            [ExpectedException(typeof(ParsingException))]
            public void FirstRowSetsExpectedColumnCountWithoutHeaderRowError()
            {
                UnitTests._ValidateResults("FirstRowSetsExpectedColumnCountWithoutHeaderRowError");
            }

            [TestMethod]
            [ExpectedException(typeof(ParsingException))]
            public void ExpectedColumnCountErrorAdding()
            {
                UnitTests._ValidateResults("ExpectedColumnCountErrorAdding");
            }

            [TestMethod]
            [ExpectedException(typeof(ParsingException))]
            public void ExpectedColumnCountErrorRemoving()
            {
                UnitTests._ValidateResults("ExpectedColumnCountErrorRemoving");
            }

            [TestMethod]
            public void SimpleDelimiterWithControlCharacters()
            {
                UnitTests._ValidateResults("SimpleDelimiterWithControlCharacters");
            }

            [TestMethod]
            public void AddingExtraColumns()
            {
                UnitTests._ValidateResults("AddingExtraColumns");
            }

            [TestMethod]
            public void VariousNewLineFormatsWithoutReturnDelimiter()
            {
                using (StringReader sr = new StringReader("1,2,3\n4,5,6\r\n7,8,9\n\r10,11,12\r13,14,15"))
                {
                    using (GenericParserAdapter parser = new GenericParserAdapter(sr))
                    {
                        using (DataTable dt = parser.GetDataTable())
                        {
                            Assert.IsNotNull(dt);
                            Assert.AreEqual<int>(5, dt.Rows.Count);

                            for (int row = 0; row < 5; ++row)
                            {
                                for (int col = 0; col < 3; ++col)
                                {
                                    Assert.AreEqual(((row * 3) + col + 1).ToString(), dt.Rows[row][col]);
                                }
                            }
                        }
                    }
                }
            }

            [TestMethod]
            public void VariousNewLineFormatsWithReturnDelimiter()
            {
                using (StringReader sr = new StringReader("1\r2\r3\n4\r5\r"))
                {
                    using (GenericParserAdapter parser = new GenericParserAdapter(sr))
                    {
                        parser.ColumnDelimiter = '\r';

                        using (DataTable dt = parser.GetDataTable())
                        {
                            Assert.IsNotNull(dt);
                            Assert.AreEqual<int>(2, dt.Rows.Count);
                            Assert.AreEqual("1", dt.Rows[0][0]);
                            Assert.AreEqual("2", dt.Rows[0][1]);
                            Assert.AreEqual("3", dt.Rows[0][2]);
                            Assert.AreEqual("4", dt.Rows[1][0]);
                            Assert.AreEqual("5", dt.Rows[1][1]);
                            Assert.AreEqual("", dt.Rows[1][2]);
                        }
                    }
                }
            }

            [TestMethod]
            public void BufferMissesEndOfRow()
            {
                using (StringReader sr = new StringReader("12345678\r\n\r\n"))
                using (GenericParserAdapter parser = new GenericParserAdapter(sr))
                {
                    parser.MaxBufferSize = 11;

                    using (DataTable dt = parser.GetDataTable())
                    {
                        Assert.IsNotNull(dt);
                        Assert.AreEqual<int>(1, dt.Rows.Count);
                        Assert.AreEqual("12345678", dt.Rows[0][0]);
                    }
                }
            }
        }

        #endregion TestClasses

        #region Private Methods

        /// <summary>
        /// Loads up the parser for the given test using the test name to identify
        /// which data to load it with.  Plus, it returns a string of the expected
        /// output (when turned into XML).
        /// </summary>
        /// <param name="parser">The parser object to be initialized.</param>
        /// <param name="strTestName">The test name to load up the parser for.</param>
        /// <returns>The expected XML output from the parser.</returns>
        private static string _PrepParserForTest(GenericParserAdapter parser, string strTestName)
        {
            string strTestRoot;
            Assembly assembly;

            // Find the assembly of the UnitTests project.
            assembly = typeof(UnitTests).Assembly;

            // Get the path to the TestDataFolder.
            strTestRoot = string.Format("{0}.{1}.{2}.", typeof(UnitTests).Namespace, TEST_DATA_FOLDER, strTestName);

            // Load the XML configuration for the parser.
            using (Stream srInput = assembly.GetManifestResourceStream(strTestRoot + TEST_CONFIG_FILE))
                parser.Load(srInput);

            // Set the input file, rely on garbage collection to free up the underlying stream.
            parser.SetDataSource(new StreamReader(assembly.GetManifestResourceStream(strTestRoot + TEST_INPUT_FILE)));

            // Return the expected output.
            using (Stream srExpectedOutput = assembly.GetManifestResourceStream(strTestRoot + TEST_EXPECTED_OUTPUT_FILE))
            {
                using (StreamReader sr = new StreamReader(srExpectedOutput))
                    return sr.ReadToEnd();
            }
        }
        /// <summary>
        ///   Creates a <see cref="GenericParserAdapter"/> and loads in the provided test's configuration
        ///   and input file and compares the results to the test's expected outcome.
        /// </summary>
        /// <param name="strTestName">The name of the test to perform.</param>
        private static void _ValidateResults(string strTestName)
        {
            string strExpectedResult;

            using (GenericParserAdapter parser = new GenericParserAdapter())
            {
                strExpectedResult = UnitTests._PrepParserForTest(parser, strTestName);

                using (DataSet dsResult = parser.GetDataSet())
                {
                    using (StringWriter swProducedResult = new StringWriter())
                    {
                        dsResult.WriteXml(swProducedResult);

                        Assert.AreEqual(strExpectedResult, swProducedResult.ToString());
                    }
                }
            }
        }

        #endregion Private Methods
    }
}