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
using System.Runtime.Serialization;
using System.Security.Permissions;

#endregion Using Directives

namespace GenericParsing
{
    /// <summary>
    ///   <see cref="ParsingException"/> is an exception class meant for states where
    ///   the parser can no longer continue parsing due to the data found in the
    ///   data-source.
    /// </summary>
    [Serializable]
    public class ParsingException : Exception
    {
        #region Constants

        private const string SERIALIZATION_COLUMN_NUMBER = "ColumnNumber";
        private const string SERIALIZATION_FILE_ROW_NUMBER = "FileRowNumber";

        #endregion Constants

        #region Constructors

        /// <summary>
        ///   Creates a new <see cref="ParsingException"/> with default values.
        /// </summary>
        public ParsingException() : base()
        {
            /* Intentionally left blank */
        }
        /// <summary>
        ///   Creates a new <see cref="ParsingException"/> containing a message and the
        ///   file line number that the error occured.
        /// </summary>
        /// <param name="strMessage">
        ///   The message indicating the root cause of the error.
        /// </param>
        /// <param name="intFileRowNumber">The file line number the error occured on.</param>
        /// <param name="intColumnNumber">The column number the error occured on.</param>
        public ParsingException(string strMessage, int intFileRowNumber, int intColumnNumber)
            : base(strMessage)
        {
            this.m_intFileRowNumber = intFileRowNumber;
            this.m_intColumnNumber = intColumnNumber;
        }
        /// <summary>
        ///   Creates a new <see cref="ParsingException"/> with seralized data.
        /// </summary>
        /// <param name="sInfo">
        ///   The <see cref="SerializationInfo"/> that contains information
        ///   about the exception.
        /// </param>
        /// <param name="sContext">
        ///   The <see cref="StreamingContext"/> that contains information
        ///   about the source/destination of the exception.
        /// </param>
        protected ParsingException(SerializationInfo sInfo, StreamingContext sContext)
            : base(sInfo, sContext)
        {
            this.m_intFileRowNumber = sInfo.GetInt32(SERIALIZATION_FILE_ROW_NUMBER);
            this.m_intColumnNumber = sInfo.GetInt32(SERIALIZATION_COLUMN_NUMBER);
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        ///   The line number in the file that the exception was thrown at.
        /// </summary>
        public int FileRowNumber
        {
            get
            {
                return this.m_intFileRowNumber;
            }
        }
        /// <summary>
        ///   The column number in the file that the exception was thrown at.
        /// </summary>
        public int ColumnNumber
        {
            get
            {
                return this.m_intColumnNumber;
            }
        }

        #endregion Public Properties

        #region Private Members

        private int m_intFileRowNumber;
        private int m_intColumnNumber;

        #endregion Private Members

        #region Overridden Methods

        /// <summary>
        ///   When overridden in a derived class, sets the <see cref="SerializationInfo"/> 
        ///   with information about the exception.
        /// </summary>
        /// <param name="info">
        ///   The <see cref="SerializationInfo"/> that holds the serialized object data
        ///   about the exception being thrown.
        /// </param>
        /// <param name="context">
        ///   The <see cref="StreamingContext"/> that contains contextual information about the source
        ///   or destination.
        /// </param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(SERIALIZATION_FILE_ROW_NUMBER, this.m_intFileRowNumber);
            info.AddValue(SERIALIZATION_COLUMN_NUMBER, this.m_intColumnNumber);
        }

        #endregion Overridden Methods
    }
}
