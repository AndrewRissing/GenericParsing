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
using System.Collections;
using System.Collections.Specialized;

#endregion Using Directives

namespace GenericParsing
{
  /* The following code is commented out because it is not needed.  If supporting legacy
   * code prior to generics, you may want to use this class as an alternative to List<string>,
   * if working under the Compact Framework.
   */

#if false

    /// <summary>
    /// <para>
    ///   Internal class used to mimic a <see cref="StringCollection" /> with a string array
    ///   as the storage medium rather than an <see cref="ArrayList" />.
    /// </para>
    /// <para>
    ///   This class is not intended to be a complete replacement of the
    ///   <see cref="StringCollection" /> class.
    /// </para>
    /// </summary>
    internal class StringArray
    {
        #region Constants

        private const int INITIAL_SIZE = 16;

        #endregion Constants

        #region Constructors

        /// <summary>
        ///   Create a new <see cref="StringArray" /> with default size.
        /// </summary>
        public StringArray()
            : this(INITIAL_SIZE)
        {
            /* Nothing */
        }

        /// <summary>
        /// Create a new <see cref="StringArray" /> with the given size.
        /// </summary>
        /// <param name="intCapacity">The size desired to allocate for the array.</param>
        public StringArray(int intCapacity)
        {
            this.m_intItemCount = 0;
            this.m_saCollection = new string[intCapacity];
        }


        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets the allocated size of the <see cref="StringArray" />.
        /// </summary>
        public int Capacity
        {
            get
            {
                return this.m_saCollection.Length;
            }
            set
            {
                if (this.Capacity != value)
                {
                    if (this.Count > value)
                        throw new ArgumentOutOfRangeException("value");

                    if (value > 0)
                    {
                        string[] saNewCollection = new string[value];

                        if (this.Count > 0)
                            Array.Copy(this.m_saCollection, 0, saNewCollection, 0, this.Count);

                        this.m_saCollection = saNewCollection;
                    }
                    else
                    {
                        this.m_saCollection = new string[INITIAL_SIZE];
                    }
                }
            }
        }

        /// <summary>
        /// Gets the total number of elements in the <see cref="StringArray" />.
        /// </summary>
        public int Count
        {
            get
            {
                return this.m_intItemCount;
            }
        }

        /// <summary>
        /// Retreives an element from the <see cref="StringArray" /> at the specified index.
        /// </summary>
        public string this[int index]
        {
            get
            {
                if ((index < 0) || (index >= this.Capacity))
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                return this.m_saCollection[index];
            }
            set
            {
                if ((index < 0) || (index >= this.Capacity))
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                this.m_saCollection[index] = value;
            }
        }


        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Removes all of the items from the <see cref="StringArray" />.
        /// </summary>
        public void Clear()
        {
            Array.Clear(this.m_saCollection, 0, this.Count);
            this.m_intItemCount = 0;
        }

        /// <summary>
        /// Adds a new string to the <see cref="StringArray" />.
        /// </summary>
        /// <param name="strValue">The new string to add to the <see cref="StringArray" />.</param>
        /// <returns>The index of the newly added string.</returns>
        public int Add(string strValue)
        {
            if (this.Capacity == this.Count)
                this._EnsureCapacity(this.Capacity + 1);

            this.m_saCollection[this.Count] = strValue;

            return ++this.m_intItemCount;
        }


        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Ensures that the <see cref="StringArray" /> has atleast a certain size.
        /// </summary>
        /// <param name="intMinimumSize">The desired size to guarantee.</param>
        private void _EnsureCapacity(int intMinimumSize)
        {
            if (this.Capacity < intMinimumSize)
            {
                int intNewSize = (this.m_saCollection == null) ? INITIAL_SIZE : (this.m_saCollection.Length * 2);

                if (intNewSize < intMinimumSize)
                    intNewSize = intMinimumSize;

                this.Capacity = intNewSize;
            }
        }


        #endregion Private Methods

        #region Private Members

        private int m_intItemCount;
        private string[] m_saCollection;

        #endregion Private Members
    }

#endif
}