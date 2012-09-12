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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

#endregion Using Directives

namespace GenericParsing
{
    /// <summary>
    /// The <see cref="GenericParser"/> class is designed to be a flexible and efficient manner
    /// of parsing various flat files formats.
    /// </summary>
    /// <threadsafety static="false" instance="false"/>
    public class GenericParser : IDisposable
    {
        #region Constants

        #region Default Values

        /// <summary>
        ///   Defines the default max buffer size (4096).
        /// </summary>
        public const int DefaultMaxBufferSize = 4096;
        /// <summary>
        ///   Defines the max rows value (0 = no limit).
        /// </summary>
        public const int DefaultMaxRows = 0;
        /// <summary>
        ///   Defines the number of skip starting data rows (0).
        /// </summary>
        public const int DefaultSkipStartingDataRows = 0;
        /// <summary>
        ///   Defines the number of expected columns (0 = no limit).
        /// </summary>
        public const int DefaultExpectedColumnCount = 0;
        /// <summary>
        ///   Defines the default first row has a header (false).
        /// </summary>
        public const bool DefaultFirstRowHasHeader = false;
        /// <summary>
        ///   Defines the default value for trim results (false).
        /// </summary>
        public const bool DefaultTrimResults = false;
        /// <summary>
        ///   Defines the default value for stripping control characters (false).
        /// </summary>
        public const bool DefaulStripControlCharacters = false;
        /// <summary>
        ///   Defines the default value for skipping empty rows (true).
        /// </summary>
        public const bool DefaulSkipEmptyRows = true;
        /// <summary>
        ///   Defines the default value for text field type (Delimited).
        /// </summary>
        public const FieldType DefaultTextFieldType = FieldType.Delimited;
        /// <summary>
        ///   Defines the default for first row sets the expected column count (false).
        /// </summary>
        public const bool DefaultFirstRowSetsExpectedColumnCount = false;
        /// <summary>
        ///   Defines the default column delimiter (',').
        /// </summary>
        public const char DefaultColumnDelimiter = ',';
        /// <summary>
        ///   Defines the default text qualifier ('\"').
        /// </summary>
        public const char DefaultTextQualifier = '\"';
        /// <summary>
        ///   Defines the default comment row character ('#').
        /// </summary>
        public const char DefaultCommentCharacter = '#';

        #endregion Default Values

        /// <summary>
        ///   Indicates the current type of row being processed.
        /// </summary>
        private enum RowType
        {
            /// <summary>
            ///   The row type is unknown and needs to be determined.
            /// </summary>
            Unknown = 0,
            /// <summary>
            ///   The row type is a comment row and can be ignored.
            /// </summary>
            CommentRow = 1,
            /// <summary>
            ///   The row type is a header row to name the columns.
            /// </summary>
            HeaderRow = 2,
            /// <summary>
            ///   The row type is a skipped row that is not intended to be extracted.
            /// </summary>
            SkippedRow = 3,
            /// <summary>
            ///   The row type is data row that is intended to be extracted.
            /// </summary>
            DataRow = 4
        }

        #region XmlConfig Constants

        private const string XML_ROOT_NODE = "GenericParser";
        private const string XML_COLUMN_WIDTH = "ColumnWidth";
        private const string XML_COLUMN_WIDTHS = "ColumnWidths";
        private const string XML_MAX_BUFFER_SIZE = "MaxBufferSize";
        private const string XML_MAX_ROWS = "MaxRows";
        private const string XML_SKIP_STARTING_DATA_ROWS = "SkipStartingDataRows";
        private const string XML_EXPECTED_COLUMN_COUNT = "ExpectedColumnCount";
        private const string XML_FIRST_ROW_HAS_HEADER = "FirstRowHasHeader";
        private const string XML_TRIM_RESULTS = "TrimResults";
        private const string XML_STRIP_CONTROL_CHARS = "StripControlChars";
        private const string XML_SKIP_EMPTY_ROWS = "SkipEmptyRows";
        private const string XML_TEXT_FIELD_TYPE = "TextFieldType";
        private const string XML_FIRST_ROW_SETS_EXPECTED_COLUMN_COUNT = "FirstRowSetsExpectedColumnCount";
        private const string XML_COLUMN_DELIMITER = "ColumnDelimiter";
        private const string XML_TEXT_QUALIFIER = "TextQualifier";
        private const string XML_ESCAPE_CHARACTER = "EscapeCharacter";
        private const string XML_COMMENT_CHARACTER = "CommentCharacter";

        private const string XML_SAFE_STRING_DELIMITER = ",";

        #endregion XmlConfig Constants

        #endregion Constants

        #region Static Code

        /// <summary>
        ///   Clones the provided array in a type-friendly way.
        /// </summary>
        /// <typeparam name="T">The type of the array to clone.</typeparam>
        /// <param name="array">The array to clone.</param>
        /// <returns>The cloned version of the array.</returns>
        private static T[] CloneArray<T>(T[] array)
        {
            T[] clone;

            if (array != null)
            {
                clone = new T[array.Length];

                for (int i = 0; i < array.Length; ++i)
                    clone[i] = array[i];
            }
            else
            {
                clone = null;
            }

            return clone;
        }

        #endregion Static Code

        #region Constructors

        /// <summary>
        ///   Constructs an instance of a <see cref="GenericParser"/> with the default settings.
        /// </summary>
        /// <remarks>
        ///   When using this constructor, the datasource must be set prior to using the parser
        ///   (using <see cref="GenericParser.SetDataSource(string)"/>), otherwise an exception will be thrown.
        /// </remarks>
        public GenericParser()
        {
            this.m_ParserState = ParserState.NoDataSource;
            this.m_txtReader = null;
            this.m_blnDisposed = false;
            this.m_objLock = new object();

            this._InitializeConfigurationVariables();
        }
        /// <summary>
        ///   Constructs an instance of a <see cref="GenericParser"/> and sets the initial datasource
        ///   as the file referenced by the string passed in.
        /// </summary>
        /// <param name="strFileName">The file name to set as the initial datasource.</param>
        /// <exception cref="ArgumentNullException">Supplying <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Supplying a filename to a file that does not exist.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public GenericParser(string strFileName)
            : this()
        {
            this.SetDataSource(strFileName);
        }
        /// <summary>
        ///   Constructs an instance of a <see cref="GenericParser"/> and sets the initial datasource
        ///   as the file referenced by the string passed in with the provided encoding.
        /// </summary>
        /// <param name="strFileName">The file name to set as the initial datasource.</param>
        /// <param name="encoding">The <see cref="Encoding"/> of the file being referenced.</param>
        /// <exception cref="ArgumentNullException">Supplying <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Supplying a filename to a file that does not exist.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public GenericParser(string strFileName, Encoding encoding)
            : this()
        {
            this.SetDataSource(strFileName, encoding);
        }
        /// <summary>
        ///   Constructs an instance of a <see cref="GenericParser"/> and sets the initial datasource
        ///   as the <see cref="TextReader"/> passed in.
        /// </summary>
        /// <param name="txtReader">The <see cref="TextReader"/> containing the data to be parsed.</param>
        /// <exception cref="ArgumentNullException">Supplying <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public GenericParser(TextReader txtReader)
            : this()
        {
            this.SetDataSource(txtReader);
        }

        #endregion Constructors

        #region Public Code

        /// <summary>
        ///    Gets whether or not the instance has been disposed of.
        /// </summary>
        /// <value>
        ///   <para>
        ///     <see langword="true"/> - Indicates the instance has be disposed of.
        ///   </para>
        ///   <para>
        ///     <see langword="false"/> - Indicates the instance has not be disposed of.
        ///   </para>
        /// </value>
        public bool IsDisposed
        {
            get
            {
                return this.m_blnDisposed;
            }
        }
        /// <summary>
        ///   Gets or sets an integer array indicating the number of characters needed for each column.
        /// </summary>
        /// <value>An int[] containing the number of spaces for each column.</value>
        /// <remarks>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        ///   <para>
        ///     By setting this property, the <see cref="TextFieldType"/> and <see cref="ExpectedColumnCount"/> are automatically updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Passing in an empty array or an
        /// array of values that have a number less than one.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public int[] ColumnWidths
        {
            get
            {
                return GenericParser.CloneArray(this.m_iaColumnWidths);
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_iaColumnWidths = GenericParser.CloneArray(value);

                if (value == null)
                {
                    this.m_textFieldType = FieldType.Delimited;
                    this.m_intExpectedColumnCount = 0;
                }
                else
                {
                    if (this.m_iaColumnWidths.Length < 1)
                        throw new ArgumentOutOfRangeException("value", "ColumnWidths cannot be an empty array.");

                    // Make sure all of the ColumnWidths are valid.
                    for (int intColumnIndex = 0; intColumnIndex < this.m_iaColumnWidths.Length; ++intColumnIndex)
                    {
                        if (this.m_iaColumnWidths[intColumnIndex] < 1)
                            throw new ArgumentOutOfRangeException("value", "ColumnWidths cannot contain a number less than one.");
                    }

                    this.m_textFieldType = FieldType.FixedWidth;
                    this.m_intExpectedColumnCount = this.m_iaColumnWidths.Length;
                }
            }
        }
        /// <summary>
        ///   Gets or sets the maximum size of the internal buffer used to cache the data.
        /// </summary>
        /// <value>The maximum size of the internal buffer to cache data from the datasource.</value>
        /// <remarks>
        ///   <para>
        ///     Maintaining the smallest number possible here improves memory usage, but
        ///     trades it off for higher CPU usage. The <see cref="MaxBufferSize"/> must
        ///     be at least the size of one column of data, plus the Max(column delimiter
        ///     width, row delimiter width).
        ///   </para>
        ///   <para>
        ///     Default: 4096
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Setting the value to something less than one.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public int MaxBufferSize
        {
            get
            {
                return this.m_intMaxBufferSize;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                if (value > 0)
                    this.m_intMaxBufferSize = value;
                else
                    throw new ArgumentOutOfRangeException("value", value, "The MaxBufferSize must be greater than 0.");
            }
        }
        /// <summary>
        ///   Gets or sets the maximum number of rows to parse.
        /// </summary>
        /// <value>The maximum number of rows to parse.</value>
        /// <remarks>
        ///   <para>
        ///     Setting the value to zero will cause all of the rows to be returned.
        ///   </para>
        ///  <para>
        ///    Default: 0
        ///  </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public int MaxRows
        {
            get
            {
                return this.m_intMaxRows;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_intMaxRows = value;

                if (this.m_intMaxRows < 0)
                    this.m_intMaxRows = 0;
            }
        }
        /// <summary>
        ///   Gets or sets the number of rows of data to ignore at the start of the file.
        /// </summary>
        /// <value>The number of data rows to initially skip in the datasource.</value>
        /// <remarks>
        ///   <para>
        ///     The header row (if present) and comment rows will not be taken into account
        ///     when determining the number of rows to skip. Setting the value to zero will
        ///     cause no rows to be ignored.
        ///   </para>
        ///   <para>
        ///     Default: 0
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public int SkipStartingDataRows
        {
            get
            {
                return this.m_intSkipStartingDataRows;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_intSkipStartingDataRows = value;

                if (this.m_intSkipStartingDataRows < 0)
                    this.m_intSkipStartingDataRows = 0;
            }
        }
        /// <summary>
        ///   Gets or sets the number of rows of data that have currently been parsed.
        /// </summary>
        /// <value>The number of rows of data that have been parsed.</value>
        /// <remarks>The DataRowNumber property is read-only.</remarks>
        public int DataRowNumber
        {
            get
            {
                return this.m_intDataRowNumber;
            }
        }
        /// <summary>
        ///   Gets or sets how many rows in the file have been parsed.
        /// </summary>
        /// <value>The number of rows in the file that have been parsed.</value>
        /// <remarks>The <see cref="FileRowNumber"/> property is read-only and includes all
        /// rows possible (header, comment, and data).</remarks>
        public int FileRowNumber
        {
            get
            {
                return this.m_intFileRowNumber;
            }
        }
        /// <summary>
        ///   Gets or sets the expected number of columns to find in the data.  If
        ///   the number of columns differs, an exception will be thrown.
        /// </summary>
        /// <value>The number of columns expected per row of data.</value>
        /// <remarks>
        ///   <para>
        ///     Setting the value to zero will cause the <see cref="GenericParser"/> to ignore
        ///     the column count in case the number changes per row.
        ///   </para>
        ///   <para>
        ///     Default: 0
        ///   </para>
        ///   <para>
        ///     By setting this property, the <see cref="TextFieldType"/> and <see cref="ColumnWidths"/>
        ///     are automatically updated.
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public int ExpectedColumnCount
        {
            get
            {
                return this.m_intExpectedColumnCount;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_intExpectedColumnCount = value;

                if (this.m_intExpectedColumnCount < 0)
                    this.m_intExpectedColumnCount = 0;

                // Make sure the ExpectedColumnCount matches the column width's
                // supplied.
                if ((this.m_textFieldType == FieldType.FixedWidth)
                 && (this.m_iaColumnWidths != null)
                 && (this.m_iaColumnWidths.Length != this.m_intExpectedColumnCount))
                {
                    // Null it out to force the proper column width's to be supplied.
                    this.m_iaColumnWidths = null;
                    this.m_textFieldType = FieldType.Delimited;
                }
            }
        }
        /// <summary>
        ///   Gets or sets whether or not the first row of data in the file contains
        ///   the header information.
        /// </summary>
        /// <value>
        ///   <para>
        ///     <see langword="true"/> - Header found on first 'data row'.
        ///   </para>
        ///   <para>
        ///     <see langword="false"/> - Header row does not exist.
        ///   </para>
        /// </value>
        /// <remarks>
        ///   <para>
        ///     Default: <see langword="false"/>
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public bool FirstRowHasHeader
        {
            get
            {
                return this.m_blnFirstRowHasHeader;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_blnFirstRowHasHeader = value;
            }
        }
        /// <summary>
        ///   Gets or sets whether or not to trim the values for each column.
        /// </summary>
        /// <value>
        ///   <para>
        ///     <see langword="true"/> - Indicates to trim the resulting strings.
        ///   </para>
        ///   <para>
        ///     <see langword="false"/> - Indicates to not trim the resulting strings.
        ///   </para>
        /// </value>
        /// <remarks>
        ///   <para>
        ///     Trimming only occurs on the strings if they are not text qualified.
        ///     So by placing values in quotes, it preserves all whitespace within
        ///     quotes.
        ///   </para>
        ///   <para>
        ///     Default: <see langword="false"/>
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public bool TrimResults
        {
            get
            {
                return this.m_blnTrimResults;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_blnTrimResults = value;
            }
        }
        /// <summary>
        ///   Gets or sets whether or not to strip control characters out of the input.
        /// </summary>
        /// <value>
        ///   <para>
        ///     <see langword="true"/> - Indicates to remove control characters from the input.
        ///   </para>
        ///   <para>
        ///     <see langword="false"/> - Indicates to leave control characters in the input.
        ///   </para>
        /// </value>
        /// <remarks>
        ///   <para>
        ///     Setting this to <see langword="true"/> can cause a performance boost.
        ///   </para>
        ///   <para>
        ///     Default: <see langword="false"/>
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public bool StripControlChars
        {
            get
            {
                return this.m_blnStripControlChars;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_blnStripControlChars = value;
            }
        }
        /// <summary>
        ///   Gets or sets whether or not to skip empty rows in the input.
        /// </summary>
        /// <value>
        ///   <para>
        ///     <see langword="true"/> - Indicates to skip empty rows in the input.
        ///   </para>
        ///   <para>
        ///     <see langword="false"/> - Indicates to include empty rows in the input.
        ///   </para>
        /// </value>
        /// <remarks>
        ///   <para>
        ///     Default: <see langword="true"/>
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public bool SkipEmptyRows
        {
            get
            {
                return this.m_blnSkipEmptyRows;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_blnSkipEmptyRows = value;
            }
        }
        /// <summary>
        ///   Gets whether or not the current row is an empty row.
        /// </summary>
        public bool IsCurrentRowEmpty
        {
            get
            {
                return this.m_blnIsCurrentRowEmpty;
            }
        }
        /// <summary>
        ///   Gets or sets the <see cref="FieldType"/> of the data encoded in the rows.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     By setting <see cref="ColumnWidths"/>, this property is automatically set.
        ///   </para>
        ///   <para>
        ///     Default: <see cref="FieldType.Delimited"/>
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public FieldType TextFieldType
        {
            get
            {
                return this.m_textFieldType;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_textFieldType = value;

                if (this.m_textFieldType == FieldType.FixedWidth)
                {
                    this.m_chColumnDelimiter = null;
                    this.m_blnFirstRowSetsExpectedColumnCount = false;
                }
                else
                {
                    this.m_iaColumnWidths = null;
                }
            }
        }
        /// <summary>
        ///   Gets or sets the number of columns in the header/first data row determines
        ///   the expected number of columns in the data.
        /// </summary>
        /// <value>
        ///   <para>
        ///     <see langword="true"/> - Indicates the data's column count should match the header/first data row's column count.
        ///   </para>
        ///   <para>
        ///     <see langword="false"/> - Indicates the data's column count does not necessarily match the header/first data row's column count.
        ///   </para>
        /// </value>
        /// <remarks>
        ///   <para>
        ///     If set to <see langword="true"/>, <see cref="FieldType"/> will automatically be set to <see langword="false"/>.
        ///   </para>
        ///   <para>
        ///     Default: <see langword="false"/>
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public bool FirstRowSetsExpectedColumnCount
        {
            get
            {
                return this.m_blnFirstRowSetsExpectedColumnCount;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_blnFirstRowSetsExpectedColumnCount = value;

                // If set to true, unset fixed width as it makes no sense.
                if (value)
                    this.TextFieldType = FieldType.Delimited;
            }
        }
        /// <summary>
        ///   Gets the <see cref="ParserState"/> value indicating the current
        ///   internal state of the parser.
        /// </summary>
        /// <value>The <see cref="State"/> property is read-only and is used to return
        /// information about the internal state of the parser.</value>
        public ParserState State
        {
            get
            {
                return this.m_ParserState;
            }
        }
        /// <summary>
        ///   Gets or sets the character used to match the end of a column of data.
        /// </summary>
        /// <value>Contains the character used to delimit a column.</value>
        /// <remarks>
        ///   <para>
        ///     By setting this property, the <see cref="TextFieldType"/> is automatically
        ///     updated. This is only meaningful when performing delimited parsing.
        ///   </para>
        ///   <para>
        ///     Default: ','
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public char? ColumnDelimiter
        {
            get
            {
                return m_chColumnDelimiter;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");
                else
                {
                    this.m_chColumnDelimiter = value;
                    this.m_textFieldType = (value == null) ? FieldType.FixedWidth : FieldType.Delimited;
                }
            }
        }
        /// <summary>
        ///   Gets or sets the character that is used to enclose a string that would otherwise
        ///   be potentially trimmed (Ex. "  this  ").
        /// </summary>
        /// <value>
        ///   The character used to enclose a string, so that row/column delimiters are ignored
        ///   and whitespace is preserved.
        /// </value>
        /// <remarks>
        ///   <para>
        ///     The Text Qualifiers must be present at the beginning and end of the column to
        ///     have them properly removed from the ends of the string.  Furthermore, for a
        ///     string that has been enclosed with the text qualifier, if the text qualifier is
        ///     doubled up inside the string, the characters will be treated as an escape for
        ///     the literal character of the text qualifier (ie. "This""Test" will translate
        ///     with only one double quote inside the string).
        ///   </para>
        ///   <para>
        ///     Setting this to <see langword="null"/> can cause a performance boost, if none of the values are
        ///     expected to require escaping.
        ///   </para>
        ///   <para>
        ///     Default: '\"'
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public char? TextQualifier
        {
            get
            {
                return this.m_chTextQualifier;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_chTextQualifier = value;
            }
        }
        /// <summary>
        ///   Gets or sets the character that is used to escape a character (Ex. "\"This\"").
        /// </summary>
        /// <value>The character used to escape row/column delimiters and the text qualifier.</value>
        /// <remarks>
        ///   <para>
        ///     Upon parsing the file, the escaped characters will be stripped out, leaving
        ///     the desired character in place.  To produce the escaped character, use the
        ///     escaped character twice (Ex. \\).  Text qualifiers are already assumed to be
        ///     escaped if used twice.
        ///   </para>
        ///   <para>
        ///     Setting this to <see langword="null"/> can cause a performance boost, if none of the values are
        ///     expected to require escaping.
        ///   </para>
        ///   <para>
        ///     Default: null
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public char? EscapeCharacter
        {
            get
            {
                return this.m_chEscapeCharacter;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_chEscapeCharacter = value;
            }
        }
        /// <summary>
        ///   Gets or sets the character that is used to mark the beginning of a row that contains
        ///   purely comments and that should not be parsed.
        /// </summary>
        /// <value>
        ///   The character used to indicate the current row is to be ignored as a comment.
        /// </value>
        /// <remarks>
        ///   <para>
        ///     Default: '#'
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public char? CommentCharacter
        {
            get
            {
                return this.m_chCommentCharacter;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_chCommentCharacter = value;
            }
        }
        /// <summary>
        ///   Gets the data found in the current row of data by the column index.
        /// </summary>
        /// <value>The value of the column at the given index.</value>
        /// <param name="intColumnIndex">The index of the column to retreive.</param>
        /// <remarks>
        ///   If the column is outside the bounds of the columns found or the column
        ///   does not possess a name, it will return <see langword="null"/>.
        /// </remarks>
        public string this[int intColumnIndex]
        {
            get
            {
                if ((intColumnIndex > -1) && (intColumnIndex < this.m_lstData.Count))
                    return this.m_lstData[intColumnIndex];
                else
                    return null;
            }
        }
        /// <summary>
        ///   Gets the data found in the current row of data by the column name.
        /// </summary>
        /// <value>The value of the column with the given column name.</value>
        /// <param name="strColumnName">The name of the column to retreive.</param>
        /// <remarks>
        ///   If the header has yet to be parsed (or no header exists), the property will
        ///   return <see langword="null"/>.
        /// </remarks>
        public string this[string strColumnName]
        {
            get
            {
                return this[this._GetColumnIndex(strColumnName)];
            }
        }
        /// <summary>
        ///   Gets the number of columns found in the current row.
        /// </summary>
        /// <value>The number of data columns found in the current row.</value>
        /// <remarks>The <see cref="ColumnCount"/> property is read-only.  The number of columns per row can differ, if allowed.</remarks>
        public int ColumnCount
        {
            get
            {
                return this.m_lstData.Count;
            }
        }
        /// <summary>
        ///   Gets the largest column count found thusfar from parsing.
        /// </summary>
        /// <value>The largest column count found thusfar from parsing.</value>
        /// <remarks>The <see cref="LargestColumnCount"/> property is read-only. The LargestColumnCount can increase due to rows with additional data.</remarks>
        public int LargestColumnCount
        {
            get
            {
                return this.m_lstColumnNames.Count;
            }
        }

        /// <summary>
        ///   Sets the file as the datasource.
        /// </summary>
        /// <remarks>
        ///   If the parser is currently parsing a file, all data associated
        ///   with the previous file is lost and the parser is reset back to
        ///   its initial values.
        /// </remarks>
        /// <param name="strFileName">The <see cref="string"/> containing the name of the file
        /// to set as the data source.</param>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (GenericParser p = new GenericParser())
        ///       p.SetDataSource(@"C:\MyData.txt");
        ///   </code>
        /// </example>
        /// <exception cref="ArgumentNullException">Supplying <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Supplying a filename to a file that does not exist.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public void SetDataSource(string strFileName)
        {
            this.SetDataSource(strFileName, Encoding.UTF8);
        }
        /// <summary>
        ///   Sets the file as the datasource using the provided encoding.
        /// </summary>
        /// <remarks>
        ///   If the parser is currently parsing a file, all data associated
        ///   with the previous file is lost and the parser is reset back to
        ///   its initial values.
        /// </remarks>
        /// <param name="strFileName">The <see cref="string"/> containing the name of the file
        /// to set as the data source.</param>
        /// <param name="encoding">The <see cref="Encoding"/> of the file being referenced.</param>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (GenericParser p = new GenericParser())
        ///       p.SetDataSource(@"C:\MyData.txt", Encoding.ASCII);
        ///   </code>
        /// </example>
        /// <exception cref="ArgumentNullException">Supplying <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Supplying a filename to a file that does not exist.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public void SetDataSource(string strFileName, Encoding encoding)
        {
            if (this.m_ParserState == ParserState.Parsing)
                throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");
            if (strFileName == null)
                throw new ArgumentNullException("strFileName", "The filename cannot be a null value.");
            if (!File.Exists(strFileName))
                throw new ArgumentException(string.Format("File, {0}, does not exist.", strFileName), "strFileName");
            if (encoding == null)
                throw new ArgumentNullException("encoding", "The encoding cannot be a null value.");

            // Clean up the existing text reader if it exists.
            if (this.m_txtReader != null)
                this.m_txtReader.Dispose();

            this.m_ParserState = ParserState.Ready;
            this.m_txtReader = new StreamReader(strFileName, encoding, true);
        }
        /// <summary>
        ///   Sets the <see cref="TextReader"/> as the datasource.
        /// </summary>
        /// <param name="txtReader">The <see cref="TextReader"/> that contains the data to be parsed.</param>
        /// <remarks>
        ///   If the parser is currently parsing a file, all data associated with the
        ///   previous file is lost and the parser is reset back to its initial values.
        /// </remarks>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (GenericParser p = new GenericParser())
        ///       using (StreamReader srReader = new StreamReader(@"C:\MyData.txt"))
        ///         p.SetDataSource(srReader);
        ///   </code>
        /// </example>
        /// <exception cref="ArgumentNullException">Supplying <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public void SetDataSource(TextReader txtReader)
        {
            if (this.m_ParserState == ParserState.Parsing)
                throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");
            if (txtReader == null)
                throw new ArgumentNullException("txtReader", "The text reader cannot be a null value.");

            // Clean up the existing text reader if it exists.
            if (this.m_txtReader != null)
                this.m_txtReader.Dispose();

            this.m_ParserState = ParserState.Ready;
            this.m_txtReader = txtReader;
        }

        /// <summary>
        ///   <para>
        ///     Parses the data-source till it arrives at one row of data.
        ///   </para>
        /// </summary>
        /// <returns>
        ///   <para>
        ///     <see langword="true"/> - Successfully parsed a new data row.
        ///   </para>
        ///   <para>
        ///     <see langword="false"/> - No new data rows were found.
        ///   </para>
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     If it finds a header, and its expecting a header row, it will not stop
        ///     at the row and continue on till it has found a row of data.
        ///   </para>
        ///   <para>
        ///     Internally, the header row is treated as a data row, but will not cause
        ///     the parser to stop after finding it.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   Attempting to read without properly setting up the <see cref="GenericParser"/>.
        /// </exception>
        /// <exception cref="ParsingException">
        ///   Thrown in the situations where the <see cref="GenericParser"/> cannot continue
        ///   due to a conflict between the setup and the data being parsed.
        /// </exception>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (GenericParser p = new GenericParser(@"C:\MyData.txt"))
        ///     {
        ///       while(p.Read())
        ///       {
        ///         // Put code here to retrieve results of the read.
        ///       }
        ///     }
        ///   </code>
        /// </example>
        public bool Read()
        {
            // Setup some internal variables for the parsing.
            this._InitializeParse();

            // Do we need to stop parsing rows.
            if (this.m_ParserState == ParserState.Finished)
                return false;

            // Read character by character into the buffer, until we reach the end of the data source.
            while (this._GetNextCharacter())
            {
                // If the row type is unknown, we're at the beginning of the row and need to determine its type.
                if (this.m_RowType == RowType.Unknown)
                {
                    this._ParseRowType();

                    // If we finished due to reading comments, break out.
                    if (this.m_ParserState == ParserState.Finished)
                        return false;
                }

                if (this.m_textFieldType == FieldType.Delimited)
                {
                    if (this.m_chCurrentChar == this.m_chEscapeCharacter)
                    {
                        this.m_blnContainsEscapedCharacters = true;

                        if (this._GetNextCharacter())
                        {
                            continue;
                        }
                        else
                        {
                            // We ran out of data, so break out.
                            break;
                        }
                    }
                    else if (((this.m_intReadIndex - 1) == this.m_intStartOfCurrentColumnIndex) && (this.m_chCurrentChar == this.m_chTextQualifier))
                    {
                        this._SkipToEndOfText();
                        continue;
                    }
                }

                // See if we have reached the end of a line.
                if (((this.m_chCurrentChar == '\r') && (this.m_chColumnDelimiter != '\r')) || (this.m_chCurrentChar == '\n'))
                {
                    // Make sure we update the state and extract columns as necessary.
                    this._HandleEndOfRow(this.m_intReadIndex - 2);

                    // Read the next character, if it is a newline, keep the state as is. Otherwise, roll back the index.
                    if (this._GetNextCharacter() && (((this.m_chCurrentChar != '\r') || (this.m_chColumnDelimiter == '\r')) && (this.m_chCurrentChar != '\n')))
                        --this.m_intReadIndex;

                    // If we were in a data row, we need to stop.
                    if ((this.m_RowType == RowType.DataRow) && ((this.m_lstData.Count > 0) || !this.m_blnSkipEmptyRows))
                    {
                        return true;
                    }
                    else
                    {
                        this.m_RowType = RowType.Unknown;
                        continue;
                    }
                }

                if (((this.m_textFieldType == FieldType.Delimited)
                  && (this.m_chCurrentChar == this.m_chColumnDelimiter))
                 || ((this.m_textFieldType == FieldType.FixedWidth)
                  && (this.m_lstData.Count < this.m_iaColumnWidths.Length)
                  && ((this.m_intReadIndex - this.m_intStartOfCurrentColumnIndex) >= this.m_iaColumnWidths[this.m_lstData.Count])))
                {
                    // Move back one character to get the last character in the column
                    // (ended with column delimiter).
                    if ((this.m_RowType == RowType.DataRow) || (this.m_RowType == RowType.HeaderRow))
                    {
                        if (this.m_textFieldType == FieldType.Delimited)
                            this._ExtractColumn(this.m_intReadIndex - 2);
                        else
                            this._ExtractColumn(this.m_intReadIndex - 1);
                    }

                    // Update the column specific flags.
                    this.m_blnIsCurrentRowEmpty = false;
                    this.m_blnFoundTextQualifierAtStart = false;
                    this.m_blnContainsEscapedCharacters = false;
                    this.m_intStartOfCurrentColumnIndex = this.m_intReadIndex;
                    continue;
                }
            }

            // We ran out of data, flush out the last row and return.
            this._HandleEndOfRow(this.m_intReadIndex - 1);

            return ((this.m_lstData.Count > 0) || !this.m_blnSkipEmptyRows || (this.m_blnHeaderRowFound && (this.m_RowType == RowType.HeaderRow)));
        }

        /// <summary>
        /// Loads the configuration of the <see cref="GenericParser"/> object from an <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="xrConfigXmlFile">The <see cref="XmlReader"/> containing the XmlConfig file to load configuration from.</param>
        /// <exception cref="ArgumentException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentException"/> could be thrown.</exception>
        /// <exception cref="ArgumentNullException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentNullException"/> could be thrown.</exception>
        /// <exception cref="ArgumentOutOfRangeException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentOutOfRangeException"/> could be thrown.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (FileStream fs = new FileStream(@"C:\MyData.txt", FileMode.Open))
        ///       using (XmlTextReader xmlTextReader = new XmlTextReader(fs))
        ///         using (GenericParser p = new GenericParser())
        ///           p.Load(xmlTextReader);
        ///   </code>
        /// </example>
        public void Load(XmlReader xrConfigXmlFile)
        {
            if (this.m_ParserState == ParserState.Parsing)
                throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

            XmlDocument xmlConfig = new XmlDocument();

            xmlConfig.Load(xrConfigXmlFile);

            this.Load(xmlConfig);
        }
        /// <summary>
        /// Loads the configuration of the <see cref="GenericParser"/> object from an <see cref="TextReader"/>.
        /// </summary>
        /// <param name="trConfigXmlFile">The <see cref="TextReader"/> containing the XmlConfig file to load configuration from.</param>
        /// <exception cref="ArgumentException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentException"/> could be thrown.</exception>
        /// <exception cref="ArgumentNullException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentNullException"/> could be thrown.</exception>
        /// <exception cref="ArgumentOutOfRangeException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentOutOfRangeException"/> could be thrown.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (StreamReader sr = new StreamReader(@"C:\MyData.txt"))
        ///       using (GenericParser p = new GenericParser())
        ///         p.Load(sr);
        ///   </code>
        /// </example>
        public void Load(TextReader trConfigXmlFile)
        {
            if (this.m_ParserState == ParserState.Parsing)
                throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

            XmlDocument xmlConfig = new XmlDocument();

            xmlConfig.Load(trConfigXmlFile);

            this.Load(xmlConfig);
        }
        /// <summary>
        /// Loads the configuration of the <see cref="GenericParser"/> object from an <see cref="Stream"/>.
        /// </summary>
        /// <param name="sConfigXmlFile">The <see cref="Stream"/> containing the XmlConfig file to load configuration from.</param>
        /// <exception cref="ArgumentException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentException"/> could be thrown.</exception>
        /// <exception cref="ArgumentNullException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentNullException"/> could be thrown.</exception>
        /// <exception cref="ArgumentOutOfRangeException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentOutOfRangeException"/> could be thrown.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (FileStream fs = new FileStream(@"C:\MyData.txt", FileMode.Open))
        ///       using (GenericParser p = new GenericParser())
        ///         p.Load(fs);
        ///   </code>
        /// </example>
        public void Load(Stream sConfigXmlFile)
        {
            if (this.m_ParserState == ParserState.Parsing)
                throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

            XmlDocument xmlConfig = new XmlDocument();

            xmlConfig.Load(sConfigXmlFile);

            this.Load(xmlConfig);
        }
        /// <summary>
        /// Loads the configuration of the <see cref="GenericParser"/> object from a file on the file system.
        /// </summary>
        /// <param name="strConfigXmlFile">The full path to the XmlConfig file on the file system.</param>
        /// <exception cref="ArgumentException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentException"/> could be thrown.</exception>
        /// <exception cref="ArgumentNullException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentNullException"/> could be thrown.</exception>
        /// <exception cref="ArgumentOutOfRangeException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentOutOfRangeException"/> could be thrown.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (GenericParser p = new GenericParser())
        ///       p.Load(@"C:\MyData.txt");
        ///   </code>
        /// </example>
        public void Load(string strConfigXmlFile)
        {
            if (this.m_ParserState == ParserState.Parsing)
                throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

            XmlDocument xmlConfig = new XmlDocument();

            xmlConfig.Load(strConfigXmlFile);

            this.Load(xmlConfig);
        }
        /// <summary>
        /// Loads the configuration of the <see cref="GenericParser"/> object from an <see cref="XmlDocument"/>.
        /// </summary>
        /// <param name="xmlConfig">The <see cref="XmlDocument"/> object containing the configuration information.</param>
        /// <exception cref="ArgumentException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentException"/> could be thrown.</exception>
        /// <exception cref="ArgumentNullException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentNullException"/> could be thrown.</exception>
        /// <exception cref="ArgumentOutOfRangeException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentOutOfRangeException"/> could be thrown.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     XmlDocument xmlConfig = new XmlDocument();
        ///     xmlConfig.Load(strConfigXmlFile);
        ///
        ///     using (GenericParser p = new GenericParser())
        ///       p.Load(xmlConfig);
        ///   </code>
        /// </example>
        public virtual void Load(XmlDocument xmlConfig)
        {
            XmlElement xmlElement;

            if (this.m_ParserState == ParserState.Parsing)
                throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

            // Reset all of the configuration variables.
            this._InitializeConfigurationVariables();

            ////////////////////////////////////////////////////////////////////
            // Access each element and load the contents of the configuration //
            // into the current GenericParser object.                         //
            ////////////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_COLUMN_WIDTHS];

            if ((xmlElement != null) && (xmlElement.ChildNodes.Count > 0))
            {
                List<int> lstColumnWidths = new List<int>(xmlElement.ChildNodes.Count);

                foreach (XmlElement xmlColumnWidth in xmlElement.ChildNodes)
                    if (xmlColumnWidth.Name == XML_COLUMN_WIDTH)
                        lstColumnWidths.Add(Convert.ToInt32(xmlColumnWidth.InnerText));

                if (lstColumnWidths.Count > 0)
                    this.ColumnWidths = lstColumnWidths.ToArray();
            }

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_MAX_BUFFER_SIZE];

            if ((xmlElement != null) && (xmlElement.InnerText != null))
                this.MaxBufferSize = Convert.ToInt32(xmlElement.InnerText);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_MAX_ROWS];

            if ((xmlElement != null) && (xmlElement.InnerText != null))
                this.MaxRows = Convert.ToInt32(xmlElement.InnerText);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_SKIP_STARTING_DATA_ROWS];

            if ((xmlElement != null) && (xmlElement.InnerText != null))
                this.SkipStartingDataRows = Convert.ToInt32(xmlElement.InnerText);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_EXPECTED_COLUMN_COUNT];

            if ((xmlElement != null) && (xmlElement.InnerText != null))
                this.ExpectedColumnCount = Convert.ToInt32(xmlElement.InnerText);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_FIRST_ROW_HAS_HEADER];

            if ((xmlElement != null) && (xmlElement.InnerText != null))
                this.FirstRowHasHeader = Convert.ToBoolean(xmlElement.InnerText);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_TRIM_RESULTS];

            if ((xmlElement != null) && (xmlElement.InnerText != null))
                this.TrimResults = Convert.ToBoolean(xmlElement.InnerText);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_STRIP_CONTROL_CHARS];

            if ((xmlElement != null) && (xmlElement.InnerText != null))
                this.StripControlChars = Convert.ToBoolean(xmlElement.InnerText);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_SKIP_EMPTY_ROWS];

            if ((xmlElement != null) && (xmlElement.InnerText != null))
                this.SkipEmptyRows = Convert.ToBoolean(xmlElement.InnerText);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_TEXT_FIELD_TYPE];

            if ((xmlElement != null) && (xmlElement.InnerText != null) && Enum.IsDefined(typeof(FieldType), xmlElement.InnerText))
                this.TextFieldType = (FieldType)Enum.Parse(typeof(FieldType), xmlElement.InnerText);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_FIRST_ROW_SETS_EXPECTED_COLUMN_COUNT];

            if ((xmlElement != null) && (xmlElement.InnerText != null))
                this.FirstRowSetsExpectedColumnCount = Convert.ToBoolean(xmlElement.InnerText);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_COLUMN_DELIMITER];

            if ((xmlElement != null) && (xmlElement.InnerText != null) && (xmlElement.InnerText.Length > 0))
                this.ColumnDelimiter = Convert.ToChar(Convert.ToInt32(xmlElement.InnerText));
            else
                this.ColumnDelimiter = null;

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_TEXT_QUALIFIER];

            if ((xmlElement != null) && (xmlElement.InnerText != null) && (xmlElement.InnerText.Length > 0))
                this.TextQualifier = Convert.ToChar(Convert.ToInt32(xmlElement.InnerText));
            else
                this.TextQualifier = null;

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_ESCAPE_CHARACTER];

            if ((xmlElement != null) && (xmlElement.InnerText != null) && (xmlElement.InnerText.Length > 0))
                this.EscapeCharacter = Convert.ToChar(Convert.ToInt32(xmlElement.InnerText));
            else
                this.EscapeCharacter = null;

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_COMMENT_CHARACTER];

            if ((xmlElement != null) && (xmlElement.InnerText != null) && (xmlElement.InnerText.Length > 0))
                this.CommentCharacter = Convert.ToChar(Convert.ToInt32(xmlElement.InnerText));
            else
                this.CommentCharacter = null;
        }

        /// <summary>
        ///   Saves the configuration to a <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="xwXmlConfig">The XmlWriter to save the the <see cref="XmlDocument"/> to.</param>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (XmlTextWriter xwXmlConfig = new XmlTextWriter(@"C:\MyData.txt", Encoding.Default))
        ///       using (GenericParser p = new GenericParser())
        ///         p.Save(xwXmlConfig);
        ///   </code>
        /// </example>
        public void Save(XmlWriter xwXmlConfig)
        {
            XmlDocument xmlConfig = this.Save();

            xmlConfig.Save(xwXmlConfig);
        }
        /// <summary>
        ///   Saves the configuration to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="twXmlConfig">The TextWriter to save the <see cref="XmlDocument"/> to.</param>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (StringWriter sw = new StringWriter())
        ///       using (GenericParser p = new GenericParser())
        ///         p.Save(sw);
        ///   </code>
        /// </example>
        public void Save(TextWriter twXmlConfig)
        {
            XmlDocument xmlConfig = this.Save();

            xmlConfig.Save(twXmlConfig);
        }
        /// <summary>
        ///   Saves the configuration to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="sXmlConfig">The stream to save the <see cref="XmlDocument"/> to.</param>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (FileStream fs = new FileStream(@"C:\MyData.txt", FileMode.Create))
        ///       using (GenericParser p = new GenericParser())
        ///         p.Save(fs);
        ///   </code>
        /// </example>
        public void Save(Stream sXmlConfig)
        {
            XmlDocument xmlConfig = this.Save();

            xmlConfig.Save(sXmlConfig);
        }
        /// <summary>
        ///   Saves the configuration to the file system.
        /// </summary>
        /// <param name="strConfigXmlFile">The file name to save the <see cref="XmlDocument"/> to.</param>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (GenericParser p = new GenericParser())
        ///       p.Load(@"C:\MyData.txt");
        ///   </code>
        /// </example>
        public void Save(string strConfigXmlFile)
        {
            XmlDocument xmlConfig = this.Save();

            xmlConfig.Save(strConfigXmlFile);
        }
        /// <summary>
        ///   Saves the configuration to an <see cref="XmlDocument"/>.
        /// </summary>
        /// <returns>The <see cref="XmlDocument"/> containing the configuration information.</returns>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (GenericParser p = new GenericParser())
        ///       XmlDocument xmlConfig = p.Save();
        ///   </code>
        /// </example>
        public virtual XmlDocument Save()
        {
            XmlDocument xmlConfig = new XmlDocument();
            XmlDeclaration xmlDeclaration;
            XmlElement xmlRoot, xmlElement, xmlSubElement;

            // Create the XML declaration
            xmlDeclaration = xmlConfig.CreateXmlDeclaration("1.0", "utf-8", null);

            // Create the root element
            xmlRoot = xmlConfig.CreateElement(XML_ROOT_NODE);
            xmlConfig.InsertBefore(xmlDeclaration, xmlConfig.DocumentElement);
            xmlConfig.AppendChild(xmlRoot);

            ////////////////////////////////////////////////////////////////////
            // Save each of the pertinent configurable settings of the        //
            // GenericParser object into the XmlDocument.                     //
            ////////////////////////////////////////////////////////////////////

            if ((this.m_textFieldType == FieldType.FixedWidth) && (this.m_iaColumnWidths != null))
            {
                xmlElement = xmlConfig.CreateElement(XML_COLUMN_WIDTHS);
                xmlRoot.AppendChild(xmlElement);

                // Create the column width elements underneath the column widths node.
                foreach (int intColumnWidth in this.m_iaColumnWidths)
                {
                    xmlSubElement = xmlConfig.CreateElement(XML_COLUMN_WIDTH);
                    xmlSubElement.InnerText = intColumnWidth.ToString();
                    xmlElement.AppendChild(xmlSubElement);
                }
            }

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.CreateElement(XML_MAX_BUFFER_SIZE);
            xmlElement.InnerText = this.m_intMaxBufferSize.ToString();
            xmlRoot.AppendChild(xmlElement);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.CreateElement(XML_MAX_ROWS);
            xmlElement.InnerText = this.m_intMaxRows.ToString();
            xmlRoot.AppendChild(xmlElement);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.CreateElement(XML_SKIP_STARTING_DATA_ROWS);
            xmlElement.InnerText = this.m_intSkipStartingDataRows.ToString();
            xmlRoot.AppendChild(xmlElement);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.CreateElement(XML_EXPECTED_COLUMN_COUNT);
            xmlElement.InnerText = this.m_intExpectedColumnCount.ToString();
            xmlRoot.AppendChild(xmlElement);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.CreateElement(XML_FIRST_ROW_HAS_HEADER);
            xmlElement.InnerText = this.m_blnFirstRowHasHeader.ToString();
            xmlRoot.AppendChild(xmlElement);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.CreateElement(XML_TRIM_RESULTS);
            xmlElement.InnerText = this.m_blnTrimResults.ToString();
            xmlRoot.AppendChild(xmlElement);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.CreateElement(XML_STRIP_CONTROL_CHARS);
            xmlElement.InnerText = this.m_blnStripControlChars.ToString();
            xmlRoot.AppendChild(xmlElement);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.CreateElement(XML_SKIP_EMPTY_ROWS);
            xmlElement.InnerText = this.m_blnSkipEmptyRows.ToString();
            xmlRoot.AppendChild(xmlElement);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.CreateElement(XML_TEXT_FIELD_TYPE);
            xmlElement.InnerText = this.m_textFieldType.ToString();
            xmlRoot.AppendChild(xmlElement);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.CreateElement(XML_FIRST_ROW_SETS_EXPECTED_COLUMN_COUNT);
            xmlElement.InnerText = this.m_blnFirstRowSetsExpectedColumnCount.ToString();
            xmlRoot.AppendChild(xmlElement);

            /////////////////////////////////////////////////////////////

            if (this.m_textFieldType == FieldType.Delimited)
            {
                xmlElement = xmlConfig.CreateElement(XML_COLUMN_DELIMITER);
                xmlElement.InnerText = Convert.ToInt32(this.m_chColumnDelimiter).ToString();
                xmlRoot.AppendChild(xmlElement);
            }

            /////////////////////////////////////////////////////////////

            if (this.m_chTextQualifier.HasValue)
            {
                xmlElement = xmlConfig.CreateElement(XML_TEXT_QUALIFIER);
                xmlElement.InnerText = Convert.ToInt32(this.m_chTextQualifier).ToString();
                xmlRoot.AppendChild(xmlElement);
            }

            /////////////////////////////////////////////////////////////

            if (this.m_chEscapeCharacter.HasValue)
            {
                xmlElement = xmlConfig.CreateElement(XML_ESCAPE_CHARACTER);
                xmlElement.InnerText = Convert.ToInt32(this.m_chEscapeCharacter).ToString();
                xmlRoot.AppendChild(xmlElement);
            }

            /////////////////////////////////////////////////////////////

            if (this.m_chCommentCharacter.HasValue)
            {
                xmlElement = xmlConfig.CreateElement(XML_COMMENT_CHARACTER);
                xmlElement.InnerText = Convert.ToInt32(this.m_chCommentCharacter).ToString();
                xmlRoot.AppendChild(xmlElement);
            }

            return xmlConfig;
        }

        /// <summary>
        ///   Releases the underlying resources of the <see cref="GenericParser"/>.
        /// </summary>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (GenericParser p = new GenericParser())
        ///     {
        ///       p.SetDataSource(@"C:\MyData.txt");
        ///     
        ///       while(p.Read())
        ///       {
        ///         // Put code here to retrieve results of the read.
        ///       }
        ///     }
        ///   </code>
        /// </example>
        public void Close()
        {
            this._CleanUpParser(false);
        }
        /// <summary>
        ///   Returns the index of the column based on its name.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     <see langword="null"/> column name is not a valid name for a column.
        ///   </para>
        ///   <para>
        ///     If the column is not found, the column index will be -1.
        ///   </para>
        /// </remarks>
        /// <param name="strColumnName">The name of the column to get the index for.</param>
        /// <returns>The index of the column with the name strColumnName. If none exists, -1 will be returned.</returns>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     int intID, intPrice;
        ///     bool blnGotIndices = false;
        ///     
        ///     using (GenericParser p = new GenericParser())
        ///     {
        ///       p.SetDataSource(@"C:\MyData.txt");
        ///       p.FirstRowHasHeader = true;
        ///     
        ///       while(p.Read())
        ///       {
        ///         if (!blnGotIndices)
        ///         {
        ///           blnGotIndices = true;
        ///           intID = p.GetColumnIndex("ID");
        ///           intPrice = p.GetColumnIndex("Price");
        ///         }
        ///       
        ///         // Put code here to retrieve results of the read.
        ///       }
        ///     }
        ///   </code>
        /// </example>
        public int GetColumnIndex(string strColumnName)
        {
            return this._GetColumnIndex(strColumnName);
        }
        /// <summary>
        ///   Returns the name of the column based on its index.
        /// </summary>
        /// <param name="intColumnIndex">The column index to return the name for.</param>
        /// <remarks>
        ///   If the column is not found or the index is outside the range
        ///   of possible columns, <see langword="null"/> will be returned.
        /// </remarks>
        /// <returns>The name of the column at the given index, if none exists <see langword="null"/> is returned.</returns>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     string strColumn1, strColumn2;
        ///     bool blnGotColumnNames = false;
        ///     
        ///     using (GenericParser p = new GenericParser())
        ///     {
        ///       p.SetDataSource(@"C:\MyData.txt");
        ///       p.FirstRowHasHeader = true;
        ///     
        ///       while(p.Read())
        ///       {
        ///         if (!blnGotColumnNames)
        ///         {
        ///           blnGotColumnNames = true;
        ///           strColumn1 = p.GetColumnIndex(0);
        ///           strColumn2 = p.GetColumnIndex(1);
        ///         }
        ///       
        ///         // Put code here to retrieve results of the read.
        ///       }
        ///     }
        ///   </code>
        /// </example>
        public string GetColumnName(int intColumnIndex)
        {
            return this._GetColumnName(intColumnIndex);
        }

        /// <summary>
        ///   Releases all of the underlying resources used by this instance.
        /// </summary>
        /// <remarks>
        ///   Calls <see cref="Dispose(bool)"/> with blnDisposing set to <see langword="true"/>
        ///   to free unmanaged and managed resources.
        /// </remarks>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Occurs when this instance is diposed of.
        /// </summary>
        public event EventHandler Disposed;

        #endregion Public Code

        #region Protected Code

        /// <summary>
        ///   The current <see cref="ParserState"/> of the parser.
        /// </summary>
        protected ParserState m_ParserState;
        /// <summary>
        ///   The current values of all the parsed columns within the row.
        /// </summary>
        protected List<string> m_lstData;
        /// <summary>
        ///   The current values of all the parsed column headers within the row.
        /// </summary>
        protected List<string> m_lstColumnNames;

        /// <summary>
        /// Raises the <see cref="Disposed"/> Event.
        /// </summary>
        protected virtual void OnDisposed()
        {
            if (this.Disposed != null)
                this.Disposed(this, EventArgs.Empty);
        }
        /// <summary>
        ///   Releases the all unmanaged resources used by this instance and optionally releases the managed resources.
        /// </summary>
        /// <param name="blnDisposing">
        ///   <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool blnDisposing)
        {
            lock (this.m_objLock)
            {
                if (!this.m_blnDisposed)
                {
                    this._CleanUpParser(true);
                    this.m_blnDisposed = true;
                }
            }

            try
            {
                this.OnDisposed();
            }
            catch
            {
                /* Do nothing */
            }
        }

        #endregion Protected Code

        #region Private Code

        #region Configuration Data

        private FieldType m_textFieldType;
        private int[] m_iaColumnWidths;
        private int m_intMaxBufferSize;
        private int m_intMaxRows;
        private int m_intSkipStartingDataRows;
        private int m_intExpectedColumnCount;
        private bool m_blnFirstRowHasHeader;
        private bool m_blnTrimResults;
        private bool m_blnStripControlChars;
        private bool m_blnSkipEmptyRows;
        private bool m_blnFirstRowSetsExpectedColumnCount;
        private char? m_chColumnDelimiter;
        private char? m_chTextQualifier;
        private char? m_chEscapeCharacter;
        private char? m_chCommentCharacter;

        #endregion Configuration Data

        #region Parsing Variables

        private TextReader m_txtReader;

        private bool m_blnIsCurrentRowEmpty;
        private bool m_blnHeaderRowFound;
        private bool m_blnFoundTextQualifierAtStart;
        private bool m_blnContainsEscapedCharacters;

        private int m_intStartIndexOfNewData;
        private int m_intNumberOfCharactersInBuffer;
        private int m_intDataRowNumber;
        private int m_intFileRowNumber;
        private int m_intReadIndex;
        private int m_intStartOfCurrentColumnIndex;

        private char m_chCurrentChar;
        private char[] m_caBuffer;

        private RowType m_RowType;

        #endregion Parsing Variables

        private object m_objLock;
        private bool m_blnDisposed;

        /// <summary>
        ///   Initializes internal variables that are maintained for internal tracking
        ///   of state during parsing.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///   In the event that the <see cref="GenericParser"/> wasn't setup properly, this exception will be thrown.
        /// </exception>
        private void _InitializeParse()
        {
            switch (this.m_ParserState)
            {
                /////////////////////////////////////////////////////////////////////////////////////////////////////

                case ParserState.NoDataSource:
                    throw new InvalidOperationException("No data source was supplied to parse.");

                /////////////////////////////////////////////////////////////////////////////////////////////////////

                case ParserState.Ready:

                    // Peform a quick sanity check to make sure we're setup properly.
                    if ((this.m_textFieldType == FieldType.FixedWidth) && (this.m_iaColumnWidths == null))
                        throw new InvalidOperationException("Column widths were not set in order to parse fixed width data.");
                    if ((this.m_textFieldType == FieldType.Delimited) && !this.m_chColumnDelimiter.HasValue)
                        throw new InvalidOperationException("Column delimiter was not set in order to parse delimited data.");

                    this.m_ParserState = ParserState.Parsing;
                    this.m_RowType = RowType.Unknown;

                    this.m_blnHeaderRowFound = false;
                    this.m_intStartIndexOfNewData = 0;
                    this.m_intDataRowNumber = 0;
                    this.m_intFileRowNumber = 0;
                    this.m_intReadIndex = 0;
                    this.m_intNumberOfCharactersInBuffer = 0;
                    this.m_intStartOfCurrentColumnIndex = -1;

                    if (this.m_lstData == null)
                        this.m_lstData = new List<string>();
                    else
                        this.m_lstData.Clear();

                    if (this.m_lstColumnNames == null)
                        this.m_lstColumnNames = new List<string>();
                    else
                        this.m_lstColumnNames.Clear();

                    // Only allocate the buffers if they are null or improperly sized.
                    if ((this.m_caBuffer == null) || (this.m_caBuffer.Length != this.m_intMaxBufferSize))
                        this.m_caBuffer = new char[this.m_intMaxBufferSize];

                    break;

                /////////////////////////////////////////////////////////////////////////////////////////////////////

                case ParserState.Parsing:

                    this.m_lstData.Clear();

                    // Have we hit the max row count?
                    if ((this.m_intMaxRows > 0) && ((this.m_intDataRowNumber - this.m_intSkipStartingDataRows) >= this.m_intMaxRows))
                    {
                        // We're done, so clean up the text reader.
                        this.m_txtReader.Dispose();
                        this.m_txtReader = null;
                        this.m_ParserState = ParserState.Finished;
                    }
                    else
                        this.m_RowType = RowType.Unknown;

                    break;

                /////////////////////////////////////////////////////////////////////////////////////////////////////

                case ParserState.Finished:
                default:

                    // Nothing.
                    break;
            }
        }
        /// <summary>
        ///   Gets the next character from the input buffer (and refills it if necessary and possible).
        /// </summary>
        /// <returns>
        ///   <para>
        ///     <see langword="true"/> - A new character was read from the data source.
        ///   </para>
        ///   <para>
        ///     <see langword="false"/> - No more characters are available in the data source.
        ///   </para>
        /// </returns>
        private bool _GetNextCharacter()
        {
            int intCharactersRead;

            // See if we have any more characters left in the input buffer.
            if (this.m_intReadIndex >= this.m_intNumberOfCharactersInBuffer)
            {
                // Make sure we haven't finished.
                if (this.m_ParserState == ParserState.Finished)
                    return false;

                // Move the leftover data in the buffer to the front and start over (only if this isn't the initial load).
                if (this.m_intStartOfCurrentColumnIndex > -1)
                    this._CopyRemainingDataToFront(this.m_intStartOfCurrentColumnIndex);

                // Read the next block of characters into the input buffer.
                intCharactersRead = this.m_txtReader.ReadBlock(this.m_caBuffer, this.m_intStartIndexOfNewData, (this.m_intMaxBufferSize - this.m_intStartIndexOfNewData));
                this.m_intNumberOfCharactersInBuffer = intCharactersRead + this.m_intStartIndexOfNewData;
                this.m_intReadIndex = this.m_intStartIndexOfNewData;

                if (intCharactersRead < 1)
                {
                    // We're done, so clean up the text reader.
                    this.m_txtReader.Dispose();
                    this.m_txtReader = null;
                    this.m_ParserState = ParserState.Finished;

                    return false;
                }
            }

            this.m_chCurrentChar = this.m_caBuffer[this.m_intReadIndex++];
            return true;
        }
        /// <summary>
        ///   Reads till a non-comment row is found.
        /// </summary>
        private void _SkipCommentRows()
        {
            // We start at the comment character, so get the next and keep reading till we find a new line.
            while (this._GetNextCharacter())
            {
                // Check for the end of a row.
                if (((this.m_chCurrentChar == '\r') && (this.m_chColumnDelimiter != '\r')) || (this.m_chCurrentChar == '\n'))
                {
                    ++this.m_intFileRowNumber;

                    // Read the next character and read another if its a row delimiter.
                    if (!this._GetNextCharacter()
                     || ((((this.m_chCurrentChar == '\r') && (this.m_chColumnDelimiter != '\r')) || (this.m_chCurrentChar == '\n')) && !this._GetNextCharacter())
                     || (this.m_chCurrentChar != this.m_chCommentCharacter))
                    {
                        // Ran out of data or the next character is not a comment row.
                        break;
                    }
                }
            }
        }
        /// <summary>
        ///   Reads till the end of the text is found.
        /// </summary>
        private void _SkipToEndOfText()
        {
            this.m_blnFoundTextQualifierAtStart = true;

            while (this._GetNextCharacter())
            {
                if (this.m_chCurrentChar == this.m_chEscapeCharacter)
                {
                    this.m_blnContainsEscapedCharacters = true;

                    if (this._GetNextCharacter())
                    {
                        continue;
                    }
                    else
                    {
                        // We ran out of data, so break out.
                        break;
                    }
                }

                // If the next character is a text qualifier, make sure it isn't the case of "a""c".
                if (this.m_chCurrentChar == this.m_chTextQualifier)
                {
                    if (!this._GetNextCharacter())
                    {
                        // We ran out of data, so break out.
                        break;
                    }
                    else if (this.m_chCurrentChar == this.m_chTextQualifier)
                    {
                        // Skip the escaped text qualifier and continue looking for the end.
                        this.m_blnContainsEscapedCharacters = true;
                        continue;
                    }
                    else
                    {
                        // Backup the index if its greater than zero and break out.
                        if (this.m_intReadIndex > 0)
                            --this.m_intReadIndex;

                        break;
                    }
                }
            }
        }
        /// <summary>
        ///   Removes all references to internally allocated resources.  Depending on
        ///   <paramref name="blnCompletely"/>, it will free up all of the internal resources
        ///   to prepare the instance for disposing.
        /// </summary>
        /// <param name="blnCompletely">
        ///   <para>
        ///     <see langword="true"/> - Clean-up the entire parser (used for disposing the instance).
        ///   </para>
        ///   <para>
        ///     <see langword="false"/> - Clean-up the parser to all it to be reused later.
        ///   </para>
        /// </param>
        private void _CleanUpParser(bool blnCompletely)
        {
            this.m_ParserState = ParserState.Finished;

            if (this.m_txtReader != null)
                this.m_txtReader.Dispose();

            this.m_txtReader = null;
            this.m_caBuffer = null;
            this.m_lstData = null;
            this.m_lstColumnNames = null;

            if (blnCompletely)
            {
                this.m_iaColumnWidths = null;
                this.m_chColumnDelimiter = null;
            }
        }
        /// <summary>
        ///   Examines the beginning of the row and the current state information
        ///   to determine how the parser will interpret the next line and updates
        ///   the internal RowType accordingly.
        /// </summary>
        private void _ParseRowType()
        {
            // Skip past any comment rows we find.
            if (this.m_chCurrentChar == this.m_chCommentCharacter)
            {
                this.m_RowType = RowType.CommentRow;
                this._SkipCommentRows();

                // If we finished, we need to break out.
                if (this.m_ParserState == ParserState.Finished)
                    return;
            }

            this.m_intStartOfCurrentColumnIndex = this.m_intReadIndex - 1;
            this.m_blnContainsEscapedCharacters = false;
            this.m_blnIsCurrentRowEmpty = true;

            if (this.m_blnFirstRowHasHeader && !this.m_blnHeaderRowFound)
                this.m_RowType = RowType.HeaderRow;
            else if (this.m_intDataRowNumber < this.m_intSkipStartingDataRows)
                this.m_RowType = RowType.SkippedRow;
            else
                this.m_RowType = RowType.DataRow;
        }
        /// <summary>
        ///   Takes the data parsed from the row and places it into the ColumnNames collection.
        /// </summary>
        private void _SetColumnNames()
        {
            // Since the current data row was a header row, reset the flag to an empty row.
            this.m_blnIsCurrentRowEmpty = true;

            this.m_blnHeaderRowFound = true;
            this.m_lstColumnNames.AddRange(this.m_lstData);
            this.m_lstData.Clear();
        }
        /// <summary>
        ///   Handles the logic necessary for updating state due to a row ending.
        /// </summary>
        /// <param name="intEndOfDataIndex">The index of the last character in the column.</param>
        /// <exception cref="ParsingException">
        ///   If parsing a fixed width format and the number of columns found differs
        ///   what was expected, this exception will be thrown.
        /// </exception>
        private void _HandleEndOfRow(int intEndOfDataIndex)
        {
            bool blnIsColumnEmpty;

            blnIsColumnEmpty = (intEndOfDataIndex < this.m_intStartOfCurrentColumnIndex);

            // Determine if we have an empty row or not.
            this.m_blnIsCurrentRowEmpty &= blnIsColumnEmpty;

            // Increment our file row counter to help with debugging in case of an error in syntax.
            ++this.m_intFileRowNumber;

            // Make sure we don't have an empty row by seeing if we have some data somewhere.
            if (!this.m_blnIsCurrentRowEmpty || !this.m_blnSkipEmptyRows)
            {
                if ((this.m_RowType == RowType.DataRow) || (this.m_RowType == RowType.SkippedRow))
                    ++this.m_intDataRowNumber;

                if ((!blnIsColumnEmpty || (!this.m_blnIsCurrentRowEmpty && (this.m_textFieldType == FieldType.Delimited)))
                 && ((this.m_RowType == RowType.DataRow) || (this.m_RowType == RowType.HeaderRow)))
                    this._ExtractColumn(intEndOfDataIndex);

                // Update the column specific flags.
                this.m_blnFoundTextQualifierAtStart = false;
                this.m_blnContainsEscapedCharacters = false;
                this.m_intStartOfCurrentColumnIndex = this.m_intReadIndex;
            }

            // Ensure that we have some data, before trying to do something with it.
            // This prevents problems with empty rows.
            if (this.m_lstData.Count > 0)
            {
                // Have we got a row that meets our expected number of columns.
                if ((this.m_intExpectedColumnCount > 0) && (this.m_lstData.Count != this.m_intExpectedColumnCount))
                    throw this._CreateParsingException(string.Format("Expected column count of {0} not found.", this.m_intExpectedColumnCount));

                // If we have a valid row, update the expected column count if we have the flag set.
                // This only makes sense when using delimiters, as fixed width would have already set this value.
                if ((this.m_textFieldType == FieldType.Delimited) && (this.m_lstData.Count > 0) && this.m_blnFirstRowSetsExpectedColumnCount)
                    this.m_intExpectedColumnCount = this.m_lstData.Count;

                if (this.m_RowType == RowType.HeaderRow)
                    this._SetColumnNames();
            }
        }
        /// <summary>
        ///   Takes a range within the character buffer and extracts the desired
        ///   string from within it and places it into the DataArray.  If an escape
        ///   character has been set, the escape characters are stripped out and the
        ///   unescaped string is returned.
        /// </summary>
        /// <param name="intEndOfDataIndex">The index of the last character in the column.</param>
        /// <exception cref="ParsingException">
        ///   In the event that the <see cref="ExpectedColumnCount"/> is set to a value of greater
        ///   than zero (which is by default for a fixed width format) and the number of columns
        ///   found differs from what's expected, this exception will be thrown.
        /// </exception>
        private void _ExtractColumn(int intEndOfDataIndex)
        {
            int intStartOfDataIndex, intRemovedCharacters;
            bool blnTrimResults, blnInText;

            // Make sure we haven't exceeded our expected column count.
            if ((this.m_intExpectedColumnCount > 0) && (this.m_lstData.Count >= this.m_intExpectedColumnCount))
                throw this._CreateParsingException(string.Format("Current column {0} exceeds ExpectedColumnCount of {1}.",
                  this.m_lstData.Count + 1,
                  this.m_intExpectedColumnCount));

            // If we have a length less than 1 character, it means we have an empty string, so bypass this logic.
            if (intEndOfDataIndex >= this.m_intStartOfCurrentColumnIndex)
            {
                // Handle quoted text by stripping off any text qualifiers, if they are present.
                if (this.m_blnFoundTextQualifierAtStart && (this.m_caBuffer[intEndOfDataIndex] == this.m_chTextQualifier))
                {
                    // Only trim on non-textqualified strings.
                    blnTrimResults = false;
                    blnInText = true;

                    intStartOfDataIndex = this.m_intStartOfCurrentColumnIndex + 1;
                    --intEndOfDataIndex;
                }
                else
                {
                    blnTrimResults = this.m_blnTrimResults;
                    blnInText = false;

                    intStartOfDataIndex = this.m_intStartOfCurrentColumnIndex;
                }

                // Before trimming the results, we need to check to see if we need to strip control characters.
                if (this.m_blnStripControlChars || this.m_blnContainsEscapedCharacters)
                {
                    intRemovedCharacters = 0;

                    // Escape out all of the control characters by sliding down the subsequent characters over them.
                    for (int intSource = intStartOfDataIndex, intDestination = intStartOfDataIndex; intSource <= intEndOfDataIndex; ++intSource)
                    {
                        // For every control character found, we must move up the source indice and increment the stripped counter.
                        if (this.m_blnStripControlChars && char.IsControl(this.m_caBuffer[intSource]))
                        {
                            ++intRemovedCharacters;
                            continue;
                        }
                        else if ((this.m_caBuffer[intSource] == this.m_chEscapeCharacter) || (blnInText && (this.m_caBuffer[intSource] == this.m_chTextQualifier)))
                        {
                            ++intRemovedCharacters;

                            // If we hit an escape character or a text qualifier, it must be an escaped character.
                            if (++intSource > intEndOfDataIndex)
                                break;
                        }
                        else if (intRemovedCharacters == 0)
                        {
                            // If we haven't found any characters to remove, just continue onto the next character.
                            ++intDestination;
                            continue;
                        }

                        this.m_caBuffer[intDestination++] = this.m_caBuffer[intSource];
                    }

                    // For every stripped character, we must decrement the ending indice.
                    intEndOfDataIndex -= intRemovedCharacters;
                }

                if (blnTrimResults)
                {
                    // Move up the beginning indice if we have white-space.
                    while ((intStartOfDataIndex <= intEndOfDataIndex) && char.IsWhiteSpace(this.m_caBuffer[intStartOfDataIndex]))
                        ++intStartOfDataIndex;

                    // Move up the ending indice if we have white-space.
                    while ((intStartOfDataIndex <= intEndOfDataIndex) && char.IsWhiteSpace(this.m_caBuffer[intEndOfDataIndex]))
                        --intEndOfDataIndex;
                }

                // Add the results to the string collection of data.
                this.m_lstData.Add(new string(this.m_caBuffer, intStartOfDataIndex, intEndOfDataIndex - intStartOfDataIndex + 1));
            }
            else
            {
                this.m_lstData.Add(string.Empty);
            }

            // If we're extending beyond the supplied column headings, add a new column.
            if ((!this.m_blnFirstRowHasHeader || this.m_blnHeaderRowFound) && (this.m_lstData.Count > this.m_lstColumnNames.Count))
                this.m_lstColumnNames.Add(null);
        }
        /// <summary>
        ///   When the buffer has reached the end of its parsing and there are no more
        ///   complete columns to be parsed, the remaining data must be moved up to the
        ///   front of the buffer so that the next batch of data can be appended to
        ///   the end.
        /// </summary>
        /// <param name="intStartIndex">The index that starts the beginning of the data to be moved.</param>
        /// <exception cref="ParsingException">In the event that the entire buffer is full and a single
        /// column cannot be parsed from it, parsing can no longer continue.</exception>
        private void _CopyRemainingDataToFront(int intStartIndex)
        {
            // Make sure we haven't exceeded our buffer size.
            if ((intStartIndex == 0) && (this.m_intNumberOfCharactersInBuffer == this.m_intMaxBufferSize))
            {
                throw this._CreateParsingException("MaxBufferSize exceeded. Try increasing the buffer size.");
            }
            else if (this.m_RowType != RowType.CommentRow)
            {
                int intLength = (this.m_intNumberOfCharactersInBuffer - intStartIndex);

                // Shift the value from the end of the buffer to the beginning.
                if (intStartIndex > 0)
                    Array.Copy(this.m_caBuffer, intStartIndex, this.m_caBuffer, 0, intLength);

                // Set the next position to begin placing data.
                this.m_intStartIndexOfNewData = intLength;
                this.m_intReadIndex = intLength;
                this.m_intStartOfCurrentColumnIndex = 0;
            }
            else
            {
                // Throw away the data in the buffer if we're in a comment row.
                this.m_intStartIndexOfNewData = 0;
                this.m_intReadIndex = 0;
                this.m_intStartOfCurrentColumnIndex = 0;
            }
        }
        /// <summary>
        ///   Returns the name of the Column based on its ColumnIndex.
        /// </summary>
        /// <param name="intColumnIndex">The column index to return the name for.</param>
        /// <remarks>
        ///   If the column is not found or the index is outside the range
        ///   of possible columns, <see langword="null"/> will be returned.
        /// </remarks>
        /// <returns>The name of the column at the given ColumnIndex, if
        /// none exists <see langword="null"/> is returned.</returns>
        private string _GetColumnName(int intColumnIndex)
        {
            if (this.m_blnHeaderRowFound && ((intColumnIndex > -1) && (intColumnIndex < this.m_lstColumnNames.Count)))
                return this.m_lstColumnNames[intColumnIndex];
            else
                return null;
        }
        /// <summary>
        ///   Returns the index of the Column based on its Name.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     <see langword="null"/> column name is not a valid name for a column.
        ///   </para>
        ///   <para>
        ///     If the column is not found, the column index will be -1.
        ///   </para>  
        /// </remarks>
        /// <param name="strColumnName">The name of the column to find the index for.</param>
        /// <returns>The index of the column with the name strColumnName.
        /// If none exists, -1 will be returned.</returns>
        private int _GetColumnIndex(string strColumnName)
        {
            if (this.m_blnHeaderRowFound && (strColumnName != null))
                return this.m_lstColumnNames.IndexOf(strColumnName);
            else
                return -1;
        }
        /// <summary>
        ///   Creates a detailed message for a parsing exception and then throws it.
        /// </summary>
        /// <param name="strMessage">The exception specific information to go into the <see cref="ParsingException"/>.</param>
        /// <returns>The <see cref="ParsingException"/> with the provided message.</returns>
        private ParsingException _CreateParsingException(string strMessage)
        {
            int intColumnNumber;

            intColumnNumber = (this.m_lstData != null) ? this.m_lstData.Count : -1;

            return new ParsingException(
                string.Format("{0} [Row: {1}, Column: {2}]",
                    strMessage,
                    this.m_intFileRowNumber,
                    intColumnNumber),
                this.m_intFileRowNumber,
                intColumnNumber);
        }
        /// <summary>
        ///   Initializes the parsing variables for the GenericParser.
        /// </summary>
        private void _InitializeConfigurationVariables()
        {
            this.m_iaColumnWidths = null;
            this.m_intMaxBufferSize = GenericParser.DefaultMaxBufferSize;
            this.m_intMaxRows = GenericParser.DefaultMaxRows;
            this.m_intSkipStartingDataRows = GenericParser.DefaultSkipStartingDataRows;
            this.m_intExpectedColumnCount = GenericParser.DefaultExpectedColumnCount;
            this.m_blnFirstRowHasHeader = GenericParser.DefaultFirstRowHasHeader;
            this.m_blnTrimResults = GenericParser.DefaultTrimResults;
            this.m_blnStripControlChars = GenericParser.DefaulStripControlCharacters;
            this.m_blnSkipEmptyRows = GenericParser.DefaulSkipEmptyRows;
            this.m_textFieldType = GenericParser.DefaultTextFieldType;
            this.m_blnFirstRowSetsExpectedColumnCount = GenericParser.DefaultFirstRowSetsExpectedColumnCount;
            this.m_chColumnDelimiter = GenericParser.DefaultColumnDelimiter;
            this.m_chTextQualifier = GenericParser.DefaultTextQualifier;
            this.m_chEscapeCharacter = null;
            this.m_chCommentCharacter = GenericParser.DefaultCommentCharacter;
        }

        #endregion Private Code
    }
}