//  GenericParsing
//  Copyright © 2018 Andrew Rissing
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

using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using Xunit;

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

        public class Properties
        {
            [Fact]
            public void EmptyArrayColumnWidths()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    using (GenericParserAdapter gp = new GenericParserAdapter())
                        gp.ColumnWidths = new int[0];
                });
            }

            [Fact]
            public void InvalidArrayColumnWidths()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    using (GenericParserAdapter gp = new GenericParserAdapter())
                        gp.ColumnWidths = new int[3] { 1, -2, 4 };
                });
            }

            [Fact]
            public void InvalidMaxBufferSize()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    using (GenericParserAdapter gp = new GenericParserAdapter())
                        gp.MaxBufferSize = -1024;
                });
            }

            [Fact]
            public void OutOfBoundsValues()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.MaxRows = -64;
                    gp.SkipStartingDataRows = -2346;
                    gp.ExpectedColumnCount = -12312;

                    Assert.Equal(0, gp.MaxRows);
                    Assert.Equal(0, gp.SkipStartingDataRows);
                    Assert.Equal(0, gp.ExpectedColumnCount);
                }
            }

            [Fact]
            public void FirstRowSetsExpectedColumnCountToTrue()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.TextFieldType = FieldType.FixedWidth;
                    gp.ColumnWidths = new int[] { 1, 2, 3 };

                    Assert.False(gp.FirstRowSetsExpectedColumnCount);
                    Assert.Equal(FieldType.FixedWidth, gp.TextFieldType);
                    Assert.NotNull(gp.ColumnWidths);

                    gp.FirstRowSetsExpectedColumnCount = true;

                    Assert.True(gp.FirstRowSetsExpectedColumnCount);
                    Assert.Equal(FieldType.Delimited, gp.TextFieldType);
                    Assert.Null(gp.ColumnWidths);
                }
            }

            [Fact]
            public void SettingExpectedColumnCount()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.TextFieldType = FieldType.FixedWidth;
                    gp.ColumnWidths = new int[] { 1, 2, 4 };

                    Assert.Equal(FieldType.FixedWidth, gp.TextFieldType);
                    Assert.NotNull(gp.ColumnWidths);
                    Assert.Equal(3, gp.ColumnWidths.Length);

                    gp.ExpectedColumnCount = 4;

                    Assert.Equal(FieldType.Delimited, gp.TextFieldType);
                    Assert.Null(gp.ColumnWidths);
                    Assert.Equal(4, gp.ExpectedColumnCount);
                }
            }

            [Fact]
            public void FixedWidthToTrue()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.TextFieldType = FieldType.Delimited;
                    gp.FirstRowSetsExpectedColumnCount = true;
                    gp.ColumnDelimiter = ',';

                    Assert.Equal(FieldType.Delimited, gp.TextFieldType);
                    Assert.True(gp.FirstRowSetsExpectedColumnCount);
                    Assert.NotNull(gp.ColumnDelimiter);

                    gp.TextFieldType = FieldType.FixedWidth;

                    Assert.Equal(FieldType.FixedWidth, gp.TextFieldType);
                    Assert.False(gp.FirstRowSetsExpectedColumnCount);
                    Assert.Null(gp.ColumnDelimiter);
                }
            }

            [Fact]
            public void ClearingColumnWidths()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.TextFieldType = FieldType.FixedWidth;
                    gp.ColumnWidths = new int[] { 1, 2, 3 };

                    Assert.Equal(FieldType.FixedWidth, gp.TextFieldType);
                    Assert.NotNull(gp.ColumnWidths);
                    Assert.Equal(3, gp.ColumnWidths.Length);

                    gp.ColumnWidths = null;

                    Assert.Equal(FieldType.Delimited, gp.TextFieldType);
                    Assert.Null(gp.ColumnWidths);
                }
            }

            [Fact]
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
                        Assert.True(gp.Read());

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

            [Fact]
            public void ColumnCountAndLargestColumnCountConstant()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    using (StringReader sr = new StringReader(string.Format("1,2,3{0}4,5,6{0}7,8,9{0}", Environment.NewLine)))
                    {
                        gp.SetDataSource(sr);

                        while (gp.Read())
                        {
                            Assert.Equal(3, gp.ColumnCount);
                            Assert.Equal(3, gp.LargestColumnCount);
                        }
                    }
                }
            }

            [Fact]
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
                            Assert.True(gp.Read());
                            Assert.Equal(intRow, gp.ColumnCount);
                            Assert.Equal(intRow, gp.LargestColumnCount);
                        }
                    }
                }
            }

            [Fact]
            public void ProtectedArrayProperties()
            {
                int[] intArray;

                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    intArray = new int[] { 1, 2, 3, 4, 5 };
                    gp.ColumnWidths = intArray;

                    Assert.NotSame(intArray, gp.ColumnWidths);
                    Assert.Equal(intArray.Length, gp.ColumnWidths.Length);

                    for (int i = 0; i < intArray.Length; ++i)
                        Assert.Equal(intArray[i], gp.ColumnWidths[i]);
                }
            }

            [Fact]
            public void ExpectedColumnCountExceptionAtEndOfRowAndFileRowNumber()
            {
                string inputData = @"A;B;C;""D; "";E
4150000480; 0001; 1; A; 807D
""da""; ""ta""; """"; ""data""; ""data""; ""data""";

                using (StringReader sr = new StringReader(inputData))
                using (GenericParserAdapter parser = new GenericParserAdapter(sr))
                {
                    parser.ColumnDelimiter = ';';
                    parser.SkipStartingDataRows = 0;
                    parser.IncludeFileLineNumber = true;
                    parser.MaxBufferSize = 4096;
                    parser.TextQualifier = '\"';
                    parser.ExpectedColumnCount = 5;

                    try
                    {
                        parser.GetDataTable();
                        Assert.Fail("You should not get here");
                    }
                    catch (ParsingException ex)
                    {
                        Assert.Equal(2, ex.FileRowNumber);
                    }
                }
            }

            [Fact]
            public void ExpectedColumnCountExceptionInMiddleOfRowAndFileRowNumber()
            {
                string inputData = @"A;B;C;""D; "";E
4150000480; 0001; 1; A; 807D
""da""; ""ta""; """"; ""data""; ""data""; ""data""; ""data""";

                using (StringReader sr = new StringReader(inputData))
                using (GenericParserAdapter parser = new GenericParserAdapter(sr))
                {
                    parser.ColumnDelimiter = ';';
                    parser.SkipStartingDataRows = 0;
                    parser.IncludeFileLineNumber = true;
                    parser.MaxBufferSize = 4096;
                    parser.TextQualifier = '\"';
                    parser.ExpectedColumnCount = 5;

                    try
                    {
                        parser.GetDataTable();
                        Assert.Fail("You should not get here");
                    }
                    catch (ParsingException ex)
                    {
                        Assert.Equal(2, ex.FileRowNumber);
                    }
                }
            }
        }

        public class Miscellaneous
        {
            [Fact]
            public void NoDataSource()
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using (GenericParserAdapter parser = new GenericParserAdapter())
                        parser.Read();
                });
            }

            [Fact]
            public void SettingDataSourceTwice()
            {
                Assert.Throws<ObjectDisposedException>(() =>
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
                });
            }

            [Fact]
            public void BufferTooSmall()
            {
                Assert.Throws<ParsingException>(() =>
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
                });
            }

            [Fact]
            public void DelimitedWithoutSetup()
            {
                Assert.Throws<InvalidOperationException>(() =>
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
                });
            }

            [Fact]
            public void FixedWidthWithoutSetup()
            {
                Assert.Throws<InvalidOperationException>(() =>
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
                });
            }

            [Fact]
            public void TooManyColumnsInFixedWidth()
            {
                Assert.Throws<ParsingException>(() =>
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
                });
            }

            [Fact]
            public void NullFileName()
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    using (GenericParserAdapter parser = new GenericParserAdapter())
                        parser.SetDataSource((string)null);
                });
            }

            [Fact]
            public void NonExistentFile()
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    using (GenericParserAdapter parser = new GenericParserAdapter())
                        parser.SetDataSource(@"C:\1234\5678\910ajb.txt");
                });
            }

            [Fact]
            public void NullTextReader()
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    using (GenericParserAdapter parser = new GenericParserAdapter())
                        parser.SetDataSource((TextReader)null);
                });
            }

            [Fact]
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

                    Assert.Equal<char?>(parserA.ColumnDelimiter, parserB.ColumnDelimiter);
                    Assert.Equal<char?>(parserA.CommentCharacter, parserB.CommentCharacter);
                    Assert.Equal<char?>(parserA.EscapeCharacter, parserB.EscapeCharacter);
                    Assert.Equal(parserA.StripControlChars, parserB.StripControlChars);
                    Assert.Equal(parserA.SkipEmptyRows, parserB.SkipEmptyRows);
                    Assert.Equal(parserA.FirstRowSetsExpectedColumnCount, parserB.FirstRowSetsExpectedColumnCount);
                    Assert.Equal(parserA.ExpectedColumnCount, parserB.ExpectedColumnCount);
                    Assert.Equal(parserA.FirstRowHasHeader, parserB.FirstRowHasHeader);
                    Assert.Equal(parserA.TextFieldType, parserB.TextFieldType);
                    Assert.Equal(parserA.IncludeFileLineNumber, parserB.IncludeFileLineNumber);
                    Assert.Equal(parserA.MaxBufferSize, parserB.MaxBufferSize);
                    Assert.Equal(parserA.MaxRows, parserB.MaxRows);
                    Assert.Equal(parserA.SkipStartingDataRows, parserB.SkipStartingDataRows);
                    Assert.Equal(parserA.SkipEndingDataRows, parserB.SkipEndingDataRows);
                    Assert.Equal(parserA.TextQualifier, parserB.TextQualifier);
                    Assert.Equal(parserA.TrimResults, parserB.TrimResults);
                }
            }

            [Fact]
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

                    Assert.Equal(parserA.ColumnWidths.Length, parserB.ColumnWidths.Length);

                    for (int i = 0; i < parserA.ColumnWidths.Length; ++i)
                        Assert.Equal(parserA.ColumnWidths[i], parserB.ColumnWidths[i]);

                    Assert.Equal(parserA.CommentCharacter, parserB.CommentCharacter);
                    Assert.Equal(parserA.EscapeCharacter, parserB.EscapeCharacter);
                    Assert.Equal(parserA.StripControlChars, parserB.StripControlChars);
                    Assert.Equal(parserA.SkipEmptyRows, parserB.SkipEmptyRows);
                    Assert.Equal(parserA.FirstRowSetsExpectedColumnCount, parserB.FirstRowSetsExpectedColumnCount);
                    Assert.Equal(parserA.ExpectedColumnCount, parserB.ExpectedColumnCount);
                    Assert.Equal(parserA.FirstRowHasHeader, parserB.FirstRowHasHeader);
                    Assert.Equal(parserA.TextFieldType, parserB.TextFieldType);
                    Assert.Equal(parserA.IncludeFileLineNumber, parserB.IncludeFileLineNumber);
                    Assert.Equal(parserA.MaxBufferSize, parserB.MaxBufferSize);
                    Assert.Equal(parserA.MaxRows, parserB.MaxRows);
                    Assert.Equal(parserA.SkipStartingDataRows, parserB.SkipStartingDataRows);
                    Assert.Equal(parserA.SkipEndingDataRows, parserB.SkipEndingDataRows);
                    Assert.Equal(parserA.TextQualifier, parserB.TextQualifier);
                    Assert.Equal(parserA.TrimResults, parserB.TrimResults);
                }
            }

            [Fact]
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

                            Assert.Equal(strExpectedResult, swProducedResult.ToString());

                            parser.Close();
                            Assert.True(parser.State == ParserState.Finished);
                        }
                    }

                    /*********************************************************************************/

                    strExpectedResult = UnitTests._PrepParserForTest(parser, "ComplexDataFixed");

                    using (DataSet dsResult = parser.GetDataSet())
                    {
                        using (StringWriter swProducedResult = new StringWriter())
                        {
                            dsResult.WriteXml(swProducedResult);

                            Assert.Equal(strExpectedResult, swProducedResult.ToString());

                            parser.Close();
                            Assert.True(parser.State == ParserState.Finished);
                        }
                    }
                }
            }

            [Fact]
            public void BoundsCheckOnColumns()
            {
                string strExpectedResult;

                // Check this with a header.
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    strExpectedResult = UnitTests._PrepParserForTest(parser, "ReadingInHeader");

                    // Make sure we are able to read one row of data.
                    Assert.True(parser.Read());

                    // Checking using integers to index the columns.
                    Assert.Null(parser.GetColumnName(-3));
                    Assert.Null(parser.GetColumnName(30));
                    Assert.Equal("a", parser.GetColumnName(0));

                    // Checking using integers to index the columns.
                    Assert.Equal(0, parser.GetColumnIndex("a"));
                    Assert.Equal(-1, parser.GetColumnIndex("foobar"));
                }

                // Check this without a header.
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    strExpectedResult = UnitTests._PrepParserForTest(parser, "SimpleDelimiter");

                    // Make sure we are able to read one row of data.
                    Assert.True(parser.Read());

                    // Checking using integers to index the columns.
                    Assert.Null(parser.GetColumnName(-3));
                    Assert.Null(parser.GetColumnName(30));
                    Assert.Null(parser.GetColumnName(0));

                    // Checking using integers to index the columns.
                    Assert.Equal(-1, parser.GetColumnIndex("a"));
                    Assert.Equal(-1, parser.GetColumnIndex("foobar"));
                }
            }

            [Fact]
            public void BoundsCheckOnData()
            {
                string strExpectedResult;

                // Check this with a header.
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    strExpectedResult = UnitTests._PrepParserForTest(parser, "ReadingInHeader");

                    // Make sure we are able to read one row of data.
                    Assert.True(parser.Read());

                    // Checking using integers to index the columns.
                    Assert.Null(parser[-1]);
                    Assert.Null(parser[10]);
                    Assert.Equal("3", parser[2]);

                    // Checking using integers to index the columns.
                    Assert.Null(parser["foobar"]);
                    Assert.Equal("1", parser["a"]);
                }

                // Check this without a header.
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    strExpectedResult = UnitTests._PrepParserForTest(parser, "SimpleDelimiter");

                    // Make sure we are able to read one row of data.
                    Assert.True(parser.Read());

                    // Checking using integers to index the columns.
                    Assert.Null(parser[-1]);
                    Assert.Null(parser[10]);
                    Assert.Equal("c", parser[2]);

                    // Checking using integers to index the columns.
                    Assert.Null(parser["foobar"]);
                    Assert.Null(parser["a"]);
                }
            }

            [Fact]
            public void BoundsCheckOnDataWithColumnNameComparisonMode()
            {
                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    UnitTests._PrepParserForTest(parser, "ReadingInHeader");
                    parser.ColumnNameComparisonMode = null;

                    Assert.True(parser.Read());
                    Assert.Equal("1", parser["a"]);
                    Assert.Null(parser["A"]);
                    Assert.Equal("6", parser["f"]);
                    Assert.Null(parser["F"]);
                }

                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    UnitTests._PrepParserForTest(parser, "ReadingInHeader");
                    parser.ColumnNameComparisonMode = StringComparison.InvariantCulture;

                    Assert.True(parser.Read());
                    Assert.Equal("1", parser["a"]);
                    Assert.Null(parser["A"]);
                    Assert.Equal("6", parser["f"]);
                    Assert.Null(parser["F"]);
                }

                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    UnitTests._PrepParserForTest(parser, "ReadingInHeader");
                    parser.ColumnNameComparisonMode = StringComparison.InvariantCultureIgnoreCase;

                    Assert.True(parser.Read());
                    Assert.Equal("1", parser["a"]);
                    Assert.Equal("1", parser["A"]);
                    Assert.Equal("6", parser["f"]);
                    Assert.Equal("6", parser["F"]);
                }
            }

            [Fact]
            public void NotStrippingControlCharactersInEscapedText()
            {
                const string INPUT = "[	\\\"	],\"[	\\\"	]\"";

                using (StringReader sr = new StringReader(INPUT))
                using (GenericParser gp = new GenericParser(sr))
                {
                    gp.EscapeCharacter = '\\';

                    Assert.True(gp.Read());
                    Assert.Equal<int>(2, gp.ColumnCount);
                    Assert.Equal("[	\"	]", gp[0]);
                    Assert.Equal("[	\"	]", gp[1]);
                }
            }

            [Fact]
            public void HandlingShortInput()
            {
                const string INPUT = "abcd";

                using (StringReader sr = new StringReader(INPUT))
                using (GenericParser gp = new GenericParser(sr))
                {
                    Assert.True(gp.Read());
                    Assert.Equal<int>(1, gp.ColumnCount);
                    Assert.Equal(INPUT, gp[0]);
                }
            }

            [Fact]
            public void SetDataSourceNullEncoding()
            {
                const string TEST_FILE = "Test.txt";

                Assert.Throws<ArgumentNullException>(() =>
                {
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
                });
            }

            [Fact]
            public void SpecializedConstructors()
            {
                Assert.Throws<ObjectDisposedException>(() =>
                {
                    using (StringReader sr = new StringReader(string.Format("a,b,c,d{0}1,2,3,4{0}", Environment.NewLine)))
                    {
                        using (GenericParserAdapter parser = new GenericParserAdapter(sr))
                        {
                            using (DataTable dtResult = parser.GetDataTable())
                            {
                                Assert.Single(dtResult.Select("[Column1] = 'a' AND [Column2] = 'b' AND [Column3] = 'c' AND [Column4] = 'd'"));
                                Assert.Single(dtResult.Select("[Column1] = '1' AND [Column2] = '2' AND [Column3] = '3' AND [Column4] = '4'"));
                            }
                        }

                        // This should throw an ObjectDisposedException.
                        sr.Peek();
                    }
                });
            }

            [Fact]
            public void DisposeEvent()
            {
                bool blnDisposeCalled = false;

                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.Disposed += (sender, e) =>
                    {
                        blnDisposeCalled = true;
                    };

                    Assert.False(blnDisposeCalled);
                }

                Assert.True(blnDisposeCalled);
            }

            [Fact]
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

                    Assert.False(blnDisposeCalled);
                }

                Assert.True(blnDisposeCalled);
            }
        }

        public class ProperParsing
        {
            [Fact]
            public void HeaderRowWithoutData()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.FirstRowHasHeader = true;

                    Assert.True(gp.FirstRowHasHeader);

                    using (StringReader sr = new StringReader("a,b,c,d"))
                    {
                        gp.SetDataSource(sr);

                        using (DataTable dt = gp.GetDataTable())
                        {
                            Assert.NotNull(dt);
                            Assert.Equal(4, dt.Columns.Count);
                            Assert.Equal("a", dt.Columns[0].ColumnName);
                            Assert.Equal("b", dt.Columns[1].ColumnName);
                            Assert.Equal("c", dt.Columns[2].ColumnName);
                            Assert.Equal("d", dt.Columns[3].ColumnName);
                            Assert.Equal(0, dt.Rows.Count);
                        }
                    }
                }
            }

            [Fact]
            public void HeaderRowWithoutDataWithNewLine()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.FirstRowHasHeader = true;

                    Assert.True(gp.FirstRowHasHeader);

                    using (StringReader sr = new StringReader("a,b,c,d\r\n"))
                    {
                        gp.SetDataSource(sr);

                        using (DataTable dt = gp.GetDataTable())
                        {
                            Assert.NotNull(dt);
                            Assert.Equal(4, dt.Columns.Count);
                            Assert.Equal("a", dt.Columns[0].ColumnName);
                            Assert.Equal("b", dt.Columns[1].ColumnName);
                            Assert.Equal("c", dt.Columns[2].ColumnName);
                            Assert.Equal("d", dt.Columns[3].ColumnName);
                            Assert.Equal(0, dt.Rows.Count);
                        }
                    }
                }
            }

            [Fact]
            public void HeaderRowWithoutDataAndIncludeFileLineNumber()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.FirstRowHasHeader = true;
                    gp.IncludeFileLineNumber = true;

                    Assert.True(gp.FirstRowHasHeader);

                    using (StringReader sr = new StringReader("a,b,c,d"))
                    {
                        gp.SetDataSource(sr);

                        using (DataTable dt = gp.GetDataTable())
                        {
                            Assert.NotNull(dt);
                            Assert.Equal(5, dt.Columns.Count);
                            Assert.Equal("FileLineNumber", dt.Columns[0].ColumnName);
                            Assert.Equal("a", dt.Columns[1].ColumnName);
                            Assert.Equal("b", dt.Columns[2].ColumnName);
                            Assert.Equal("c", dt.Columns[3].ColumnName);
                            Assert.Equal("d", dt.Columns[4].ColumnName);
                            Assert.Equal(0, dt.Rows.Count);
                        }
                    }
                }
            }

            [Fact]
            public void EmptyDataSource()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    using (StringReader sr = new StringReader(string.Empty))
                    {
                        gp.SetDataSource(sr);

                        using (DataTable dt = gp.GetDataTable())
                        {
                            Assert.NotNull(dt);
                            Assert.Empty(dt.Columns);
                            Assert.Equal(0, dt.Rows.Count);
                        }
                    }
                }
            }

            [Fact]
            public void HeaderRowWithoutDataWithExpectedColumnCount()
            {
                using (GenericParserAdapter gp = new GenericParserAdapter())
                {
                    gp.FirstRowHasHeader = true;
                    gp.FirstRowSetsExpectedColumnCount = true;

                    Assert.True(gp.FirstRowHasHeader);
                    Assert.True(gp.FirstRowSetsExpectedColumnCount);
                    Assert.Equal(0, gp.ExpectedColumnCount);

                    using (StringReader sr = new StringReader("a,b,c,d"))
                    {
                        gp.SetDataSource(sr);

                        using (DataTable dt = gp.GetDataTable())
                        {
                            Assert.NotNull(dt);
                            Assert.Equal(4, dt.Columns.Count);
                            Assert.Equal(0, dt.Rows.Count);
                            Assert.Equal(4, gp.ExpectedColumnCount);
                        }
                    }
                }
            }

            [Fact]
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
                            Assert.NotNull(dt);
                            Assert.Equal(4, dt.Columns.Count);
                            Assert.Equal(1, dt.Rows.Count);
                        }
                    }
                }
                finally
                {
                    File.Delete(TEST_FILE);
                }
            }

            [Fact]
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
                                    Assert.Equal(NUMBER_OF_COLUMNS_IN_BASE_DATA, parser.ColumnCount);

                                    Assert.Equal("Column1", parser.GetColumnName(0));
                                    Assert.Equal("Column2", parser.GetColumnName(1));
                                    Assert.Equal("Column3", parser.GetColumnName(2));
                                    Assert.Equal("Column4", parser.GetColumnName(3));
                                    Assert.Equal("Column5", parser.GetColumnName(4));
                                    Assert.Equal("Column6", parser.GetColumnName(5));

                                    intCurrentDataRowIndex = (parser.DataRowNumber - 1) % NUMBER_OF_ROWS_IN_BASE_DATA;

                                    switch (intCurrentDataRowIndex)
                                    {
                                        case 0:
                                            Assert.Equal("a", parser[0]);
                                            Assert.Equal("b", parser[1]);
                                            Assert.Equal("c", parser[2]);
                                            Assert.Equal("d", parser[3]);
                                            Assert.Equal("e", parser[4]);
                                            Assert.Equal("f", parser[5]);
                                            break;

                                        case 1:
                                            Assert.Equal("1", parser[0]);
                                            Assert.Equal("2", parser[1]);
                                            Assert.Equal("3", parser[2]);
                                            Assert.Equal("4", parser[3]);
                                            Assert.Equal("5", parser[4]);
                                            Assert.Equal("6", parser[5]);
                                            break;

                                        case 2:
                                            Assert.Equal("g", parser[0]);
                                            Assert.Equal("h", parser[1]);
                                            Assert.Equal("i", parser[2]);
                                            Assert.Equal("j", parser[3]);
                                            Assert.Equal("k", parser[4]);
                                            Assert.Equal("l", parser[5]);
                                            break;

                                        case 3:
                                            Assert.Equal("7", parser[0]);
                                            Assert.Equal("8", parser[1]);
                                            Assert.Equal("9", parser[2]);
                                            Assert.Equal("10", parser[3]);
                                            Assert.Equal("11", parser[4]);
                                            Assert.Equal("12", parser[5]);
                                            break;

                                        case 4:
                                            Assert.Equal("m", parser[0]);
                                            Assert.Equal("n", parser[1]);
                                            Assert.Equal("o", parser[2]);
                                            Assert.Equal("p", parser[3]);
                                            Assert.Equal("q", parser[4]);
                                            Assert.Equal("r", parser[5]);
                                            break;

                                        case 5:
                                            Assert.Equal("13", parser[0]);
                                            Assert.Equal("14", parser[1]);
                                            Assert.Equal("15", parser[2]);
                                            Assert.Equal("16", parser[3]);
                                            Assert.Equal("17", parser[4]);
                                            Assert.Equal("18", parser[5]);
                                            break;

                                        case 6:
                                            Assert.Equal("t", parser[0]);
                                            Assert.Equal("u", parser[1]);
                                            Assert.Equal("v", parser[2]);
                                            Assert.Equal("w", parser[3]);
                                            Assert.Equal("x", parser[4]);
                                            Assert.Equal("y", parser[5]);
                                            break;

                                        case 7:
                                            Assert.Equal("19", parser[0]);
                                            Assert.Equal("20", parser[1]);
                                            Assert.Equal("21", parser[2]);
                                            Assert.Equal("22", parser[3]);
                                            Assert.Equal("23", parser[4]);
                                            Assert.Equal("24", parser[5]);
                                            break;

                                        default:

                                            Assert.Fail(string.Format("Invalid state reached when parsing (DataRowNumber: {0}, CurrentDataRowIndex: {1})",
                                              parser.DataRowNumber,
                                              intCurrentDataRowIndex));

                                            break;
                                    }
                                }
                            }

                            Assert.Equal(NUMBER_OF_ITERATIONS_OF_BASE_DATA * NUMBER_OF_ROWS_IN_BASE_DATA, parser.DataRowNumber);
                        }
                    }
                }
            }

            [Fact]
            public void XmlTest()
            {
                XmlDocument xmlResult;
                string strExpectedResult;

                using (GenericParserAdapter parser = new GenericParserAdapter())
                {
                    strExpectedResult = UnitTests._PrepParserForTest(parser, "XmlTest");

                    xmlResult = parser.GetXml();
                    xmlResult.Normalize();

                    Assert.Equal(strExpectedResult, xmlResult.OuterXml);
                }
            }

            [Theory]
            [InlineData("ReadingInHeaderAfterComments")]
            [InlineData("MaxRowsIgnoresCommentsHeader")]
            [InlineData("HandlingDuplicateColumnNames")]
            [InlineData("RowWithoutColumnDelimiter")]
            [InlineData("TrimmingResults")]
            [InlineData("ReadLastRowWithoutRowDelimiter")]
            [InlineData("ReadLastRowWithRowDelimiter")]
            [InlineData("IncludeLineNumber")]
            [InlineData("UnicodeSupported")]
            [InlineData("ComplexDataDelimiter")]
            [InlineData("ComplexDataFixed")]
            [InlineData("SimpleDelimiter")]
            [InlineData("SimpleFixedWidth")]
            [InlineData("SkippingComments")]
            [InlineData("SkippingEmptyRowsWithDelimiter")]
            [InlineData("SkippingEmptyRowsWithFixedWidth")]
            [InlineData("NotSkippingEmptyRowsDelimiter")]
            [InlineData("NotSkippingEmptyRowsDelimiterWithoutFileRowNumber")]
            [InlineData("NotSkippingEmptyRowsDelimiterWithHeaderWithoutFileRowNumber")]
            [InlineData("NotSkippingEmptyRowsDelimiterWithHeaderWithFileRowNumber")]
            [InlineData("NotSkippingEmptyRowsFixedWidth")]
            [InlineData("BasicSkipRows")]
            [InlineData("BasicMaxRows")]
            [InlineData("DifferentColumnDelimiter")]
            [InlineData("BasicTextQualifiers")]
            [InlineData("TextQualifiersWithDelimiters")]
            [InlineData("TextQualifierNotClosedAtEnd")]
            [InlineData("TextQualifierBeginningAndEnd")]
            [InlineData("TextQualifierWithoutEscape")]
            [InlineData("EscapeWithoutTextQualifier")]
            [InlineData("BasicEscapedCharacters")]
            [InlineData("EscapedCharacterAtEndOfRowDelimiter")]
            [InlineData("EscapedCharacterAtEndOfRowFixedWidth")]
            [InlineData("EscapedCharacterAtEndOfFile")]
            [InlineData("ReadingInHeader")]
            [InlineData("SkippingRowsAtEndOfBuffer")]
            [InlineData("SkippingAllRows")]
            [InlineData("SkippingEndingRows")]
            [InlineData("SkippingEndingRowsAll")]
            [InlineData("SkippingEndingRowsExactlyAll")]
            [InlineData("SkippingBeginningAndEndingRows")]
            [InlineData("SkippingBeginningAndEndingRowsAll")]
            [InlineData("FirstRowSetsExpectedColumnCountWithHeaderRow")]
            [InlineData("FirstRowSetsExpectedColumnCountWithoutHeaderRow")]
            [InlineData("SimpleDelimiterWithControlCharacters")]
            [InlineData("AddingExtraColumns")]
            public void SuccessfulScenarios(string testCase)
            {
                UnitTests._ValidateResults(testCase);
            }

            [Theory]
            [InlineData("FirstRowSetsExpectedColumnCountWithHeaderRowError")]
            [InlineData("FirstRowSetsExpectedColumnCountWithoutHeaderRowError")]
            [InlineData("ExpectedColumnCountErrorAdding")]
            [InlineData("ExpectedColumnCountErrorRemoving")]
            public void ParsingExceptionScenarios(string testCase)
            {
                Assert.Throws<ParsingException>(() => UnitTests._ValidateResults(testCase));
            }

            [Fact]
            public void VariousNewLineFormatsWithoutReturnDelimiter()
            {
                using (StringReader sr = new StringReader("1,2,3\n4,5,6\r\n7,8,9\n\r10,11,12\r13,14,15"))
                {
                    using (GenericParserAdapter parser = new GenericParserAdapter(sr))
                    {
                        using (DataTable dt = parser.GetDataTable())
                        {
                            Assert.NotNull(dt);
                            Assert.Equal<int>(5, dt.Rows.Count);

                            for (int row = 0; row < 5; ++row)
                            {
                                for (int col = 0; col < 3; ++col)
                                {
                                    Assert.Equal(((row * 3) + col + 1).ToString(), dt.Rows[row][col]);
                                }
                            }
                        }
                    }
                }
            }

            [Fact]
            public void VariousNewLineFormatsWithReturnDelimiter()
            {
                using (StringReader sr = new StringReader("1\r2\r3\n4\r5\r"))
                {
                    using (GenericParserAdapter parser = new GenericParserAdapter(sr))
                    {
                        parser.ColumnDelimiter = '\r';

                        using (DataTable dt = parser.GetDataTable())
                        {
                            Assert.NotNull(dt);
                            Assert.Equal<int>(2, dt.Rows.Count);
                            Assert.Equal("1", dt.Rows[0][0]);
                            Assert.Equal("2", dt.Rows[0][1]);
                            Assert.Equal("3", dt.Rows[0][2]);
                            Assert.Equal("4", dt.Rows[1][0]);
                            Assert.Equal("5", dt.Rows[1][1]);
                            Assert.Equal("", dt.Rows[1][2]);
                        }
                    }
                }
            }

            [Fact]
            public void BufferMissesEndOfRow()
            {
                using (StringReader sr = new StringReader("12345678\r\n\r\n"))
                using (GenericParserAdapter parser = new GenericParserAdapter(sr))
                {
                    parser.MaxBufferSize = 11;

                    using (DataTable dt = parser.GetDataTable())
                    {
                        Assert.NotNull(dt);
                        Assert.Equal<int>(1, dt.Rows.Count);
                        Assert.Equal("12345678", dt.Rows[0][0]);
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

                        Assert.Equal(strExpectedResult, swProducedResult.ToString());
                    }
                }
            }
        }

        #endregion Private Methods
    }
}