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
using System.Text;
using System.Xml;

#endregion Using Directives

namespace GenericParsing
{
    /// <summary>
    ///   The <see cref="GenericParserAdapter"/> is used to modify the <see cref="GenericParser"/>
    ///   to allow it parse a file and place them into various formats.
    /// </summary>
    /// <threadsafety static="false" instance="false"/>
    public class GenericParserAdapter : GenericParser
    {
        #region Constants

        /// <summary>
        ///   Defines the default value for including the file line number (false).
        /// </summary>
        public const bool DefaultIncludeFileLineNumber = false;
        /// <summary>
        ///   Defines the number of skip ending data rows (0).
        /// </summary>
        public const int DefaultSkipEndingDataRows = 0;

        private const string XML_INCLUDE_FILE_LINE_NUMBER = "IncludeFileLineNumber";
        private const string XML_SKIP_ENDING_DATA_ROWS = "SkipEndingDataRows";
        private const string FILE_LINE_NUMBER = "FileLineNumber";

        #endregion Constants

        #region Static Code

        /// <summary>
        ///   Adds a column name to the given <see cref="DataTable"/>, such that
        ///   it ensures a unique column name.
        /// </summary>
        /// <param name="dtData">The <see cref="DataTable"/> to add the column to.</param>
        /// <param name="strColumnName">The desired column name to add.</param>
        private static void AddColumnToTable(DataTable dtData, string strColumnName)
        {
            if (strColumnName != null)
            {
                if (dtData.Columns[strColumnName] == null)
                    dtData.Columns.Add(strColumnName);
                else
                {
                    string strNewColumnName;
                    int intCount = 0;

                    // Looks like we need to generate a new column name.
                    do
                    {
                        strNewColumnName = string.Format("{0}{1}", strColumnName, ++intCount);
                    }
                    while (dtData.Columns[strNewColumnName] != null);

                    dtData.Columns.Add(strNewColumnName);
                }
            }
            else
                dtData.Columns.Add();
        }

        #endregion Static Code

        #region Constructors

        /// <summary>
        ///   Constructs an instance of a <see cref="GenericParserAdapter"/>
        ///   with the default settings.
        /// </summary>
        /// <remarks>
        ///   When using this constructor, the datasource must be set prior to using the parser
        ///   (using <see cref="GenericParser.SetDataSource(string)"/>), otherwise an exception will be thrown.
        /// </remarks>
        public GenericParserAdapter()
            : base()
        {
            this.IncludeFileLineNumber = GenericParserAdapter.DefaultIncludeFileLineNumber;
            this.SkipEndingDataRows = GenericParserAdapter.DefaultSkipEndingDataRows;
        }
        /// <summary>
        ///   Constructs an instance of a <see cref="GenericParserAdapter"/> and sets
        ///   the initial datasource as the file referenced by the string passed in.
        /// </summary>
        /// <param name="strFileName">The file name to set as the initial datasource.</param>
        public GenericParserAdapter(string strFileName)
            : this()
        {
            this.SetDataSource(strFileName);
        }
        /// <summary>
        ///   Constructs an instance of a <see cref="GenericParserAdapter"/> and sets
        ///   the initial datasource as the file referenced by the string passed in with
        ///   the provided encoding.
        /// </summary>
        /// <param name="strFileName">The file name to set as the initial datasource.</param>
        /// <param name="encoding">The <see cref="Encoding"/> of the file being referenced.</param>
        public GenericParserAdapter(string strFileName, Encoding encoding)
            : this()
        {
            this.SetDataSource(strFileName, encoding);
        }
        /// <summary>
        ///   Constructs an instance of a <see cref="GenericParserAdapter"/> and sets
        ///   the initial datasource as the <see cref="TextReader"/> passed in.
        /// </summary>
        /// <param name="txtReader">
        ///   The <see cref="TextReader"/> containing the data to be parsed.
        /// </param>
        public GenericParserAdapter(TextReader txtReader)
            : this()
        {
            this.SetDataSource(txtReader);
        }

        #endregion Constructors

        #region Public Code

        /// <summary>
        ///   Gets or sets whether or not the <see cref="GenericParser.FileRowNumber"/> from where
        ///   the data was retrieved should be included as part of the result set.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Default: <see langword="false"/> 
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public bool IncludeFileLineNumber
        {
            get
            {
                return this.m_blnIncludeFileLineNumber;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_blnIncludeFileLineNumber = value;
            }
        }
        /// <summary>
        ///   Gets or sets the number of rows of data to ignore at the end of the file.
        /// </summary>
        /// <value>The number of data rows to skip at the end of the datasource</value>
        /// <remarks>
        ///   <para>
        ///     A value of zero will ensure no rows are ignored.
        ///   </para>
        ///   <para>
        ///     Default: 0 
        ///   </para>
        ///   <para>
        ///     If parsing has started, this value cannot be updated.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public int SkipEndingDataRows
        {
            get
            {
                return this.m_intSkipEndingDataRows;
            }
            set
            {
                if (this.m_ParserState == ParserState.Parsing)
                    throw new InvalidOperationException("Parsing has already begun, close the existing parse first.");

                this.m_intSkipEndingDataRows = value;

                if (this.m_intSkipEndingDataRows < 0)
                    this.m_intSkipEndingDataRows = 0;
            }
        }

        /// <summary>
        ///   Generates an <see cref="XmlDocument"/> based on the data stored within
        ///   the entire data source after it was parsed.
        /// </summary>
        /// <returns>
        ///   The <see cref="XmlDocument"/> containing all of the data in the data
        ///   source.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   Attempting to read without properly setting up the <see cref="GenericParserAdapter"/>.
        /// </exception>
        /// <exception cref="ParsingException">
        ///   Thrown in the situations where the <see cref="GenericParserAdapter"/> cannot continue
        ///   due to a conflict between the setup and the data being parsed.
        /// </exception>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (GenericParserAdapter p = new GenericParserAdapter(@"C:\MyData.txt"))
        ///       XmlDocument xmlDoc = p.GetXml();
        ///   </code>
        /// </example>
        public XmlDocument GetXml()
        {
            DataSet dsData;
            XmlDocument xmlDocument = null;

            dsData = this.GetDataSet();

            if (dsData != null)
            {
                xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(dsData.GetXml());
            }

            return xmlDocument;
        }
        /// <summary>
        ///   Generates a <see cref="DataSet"/> based on the data stored within
        ///   the entire data source after it was parsed.
        /// </summary>
        /// <returns>
        ///   The <see cref="DataSet"/> containing all of the data in the
        ///   data source.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   Attempting to read without properly setting up the <see cref="GenericParserAdapter"/>.
        /// </exception>
        /// <exception cref="ParsingException">
        ///   Thrown in the situations where the <see cref="GenericParserAdapter"/> cannot continue
        ///   due to a conflict between the setup and the data being parsed.
        /// </exception>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (GenericParserAdapter p = new GenericParserAdapter(@"C:\MyData.txt"))
        ///       DataSet dsResults = p.GetDataSet();
        ///   </code>
        /// </example>
        public DataSet GetDataSet()
        {
            DataTable dtData;
            DataSet dsData = null;

            dtData = this.GetDataTable();

            if (dtData != null)
            {
                dsData = new DataSet();
                dsData.Tables.Add(dtData);
            }

            return dsData;
        }
        /// <summary>
        ///   Generates a <see cref="DataTable"/> based on the data stored within
        ///   the entire data source after it was parsed.
        /// </summary>
        /// <returns>
        ///   The <see cref="DataTable"/> containing all of the data in the data
        ///   source.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   Attempting to read without properly setting up the <see cref="GenericParserAdapter"/>.
        /// </exception>
        /// <exception cref="ParsingException">
        ///   Thrown in the situations where the <see cref="GenericParserAdapter"/> cannot continue
        ///   due to a conflict between the setup and the data being parsed.
        /// </exception>
        /// <example>
        ///   <code lang="C#" escaped="true">
        ///     using (GenericParserAdapter p = new GenericParserAdapter(@"C:\MyData.txt"))
        ///       DataTable dtResults = p.GetDataTable();
        ///   </code>
        /// </example>
        public DataTable GetDataTable()
        {
            DataRow drRow;
            DataTable dtData;
            int intCreatedColumns, intSkipRowsAtEnd;

            dtData = new DataTable();
            dtData.BeginLoadData();

            intCreatedColumns = 0;

            while (this.Read())
            {
                // See if we have the appropriate number of columns.
                if (this.m_lstColumnNames.Count > intCreatedColumns)
                {
                    // Add in our column to store off the file line number.
                    if (this.m_blnIncludeFileLineNumber && (intCreatedColumns < 1))
                        dtData.Columns.Add(GenericParserAdapter.FILE_LINE_NUMBER);

                    for (int intColumnIndex = intCreatedColumns; intColumnIndex < this.m_lstColumnNames.Count; ++intColumnIndex, ++intCreatedColumns)
                        GenericParserAdapter.AddColumnToTable(dtData, this.m_lstColumnNames[intColumnIndex]);
                }

                if (!this.IsCurrentRowEmpty || !this.SkipEmptyRows)
                {
                    drRow = dtData.NewRow();

                    if (this.m_blnIncludeFileLineNumber)
                    {
                        drRow[0] = this.FileRowNumber;

                        // Now, add in the data retrieved from the current row.
                        for (int intColumnIndex = 0; intColumnIndex < this.m_lstData.Count; ++intColumnIndex)
                            drRow[intColumnIndex + 1] = this.m_lstData[intColumnIndex];
                    }
                    else
                    {
                        // Since we don't have to account for the row number, just place the value right into the data row.
                        drRow.ItemArray = this.m_lstData.ToArray();
                    }

                    dtData.Rows.Add(drRow);
                }
            }

            intSkipRowsAtEnd = this.m_intSkipEndingDataRows;

            // Remove any rows at the end that need to be skipped.
            while ((intSkipRowsAtEnd-- > 0) && (dtData.Rows.Count > 0))
                dtData.Rows.RemoveAt(dtData.Rows.Count - 1);

            dtData.EndLoadData();

            return dtData;
        }

        /// <summary>
        ///   Loads the base <see cref="GenericParser"/> class from the
        ///   <see cref="XmlDocument"/> and then retrieves additional information
        ///    from the Xml that is specific to the <see cref="GenericParserAdapter"/>.
        /// </summary>
        /// <param name="xmlConfig">
        ///   The <see cref="XmlDocument"/> containing the configuration information.
        /// </param>
        /// <exception cref="ArgumentException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentException"/> could be thrown.</exception>
        /// <exception cref="ArgumentNullException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentNullException"/> could be thrown.</exception>
        /// <exception cref="ArgumentOutOfRangeException">In the event that the XmlConfig file contains a value that is invalid,
        /// an <see cref="ArgumentOutOfRangeException"/> could be thrown.</exception>
        /// <exception cref="InvalidOperationException">Attempting to modify the configuration, while parsing.</exception>
        public override void Load(XmlDocument xmlConfig)
        {
            XmlElement xmlElement;

            // Load the base information for the GenericParser.
            base.Load(xmlConfig);

            // Initialize the value for the file line number.
            this.m_blnIncludeFileLineNumber = GenericParserAdapter.DefaultIncludeFileLineNumber;
            this.m_intSkipEndingDataRows = GenericParserAdapter.DefaultSkipEndingDataRows;

            /////////////////////////////////////////////
            // Load the rest of the information that's //
            // specific to the GenericParserAdapter.   //
            /////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_INCLUDE_FILE_LINE_NUMBER];

            if ((xmlElement != null) && (xmlElement.InnerText != null))
                this.IncludeFileLineNumber = Convert.ToBoolean(xmlElement.InnerText);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.DocumentElement[XML_SKIP_ENDING_DATA_ROWS];

            if ((xmlElement != null) && (xmlElement.InnerText != null))
                this.SkipEndingDataRows = Convert.ToInt32(xmlElement.InnerText);
        }
        /// <summary>
        ///   Saves the configuration of the <see cref="GenericParserAdapter"/>
        ///   to an <see cref="XmlDocument"/>.
        /// </summary>
        /// <returns>
        ///   The <see cref="XmlDocument"/> that will store the configuration
        ///   information of the current setup of the <see cref="GenericParserAdapter"/>.
        /// </returns>
        public override XmlDocument Save()
        {
            XmlDocument xmlConfig = base.Save();
            XmlElement xmlElement;

            ///////////////////////////////////////////////////////////////
            // Take the document and insert the additional configuration //
            // specific to the GenericParserAdapter.                     //
            ///////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.CreateElement(XML_INCLUDE_FILE_LINE_NUMBER);
            xmlElement.InnerText = this.IncludeFileLineNumber.ToString();
            xmlConfig.DocumentElement.AppendChild(xmlElement);

            /////////////////////////////////////////////////////////////

            xmlElement = xmlConfig.CreateElement(XML_SKIP_ENDING_DATA_ROWS);
            xmlElement.InnerText = this.m_intSkipEndingDataRows.ToString();
            xmlConfig.DocumentElement.AppendChild(xmlElement);

            return xmlConfig;
        }

        #endregion Public Code

        #region Private Code

        private bool m_blnIncludeFileLineNumber;
        private int m_intSkipEndingDataRows;

        #endregion Private Code
    }
}