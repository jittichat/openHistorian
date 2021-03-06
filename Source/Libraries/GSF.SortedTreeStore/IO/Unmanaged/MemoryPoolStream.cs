﻿//******************************************************************************************************
//  MemoryPoolStream.cs - Gbtc
//
//  Copyright © 2014, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  5/1/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//
//******************************************************************************************************

using GSF.IO.Unmanaged;

namespace GSF.IO.Unmanaged
{
    /// <summary>
    /// Provides a in memory stream that uses pages that are pooled in the unmanaged buffer pool.
    /// </summary>
    public partial class MemoryPoolStream
        : MemoryPoolStreamCore, ISupportsBinaryStream
    {
        #region [ Members ]

        /// <summary>
        /// The size of each page.
        /// </summary>
        private readonly int m_blockSize;

        /// <summary>
        /// Releases all the resources used by the <see cref="MemoryPoolStream"/> object.
        /// </summary>
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="MemoryPoolStream"/> using the default <see cref="MemoryPool"/>.
        /// </summary>
        public MemoryPoolStream()
            : this(Globals.MemoryPool)
        {
        }

        /// <summary>
        /// Create a new <see cref="MemoryPoolStream"/>
        /// </summary>
        public MemoryPoolStream(MemoryPool pool)
            : base(pool)
        {
            m_blockSize = pool.PageSize;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the unit size of an individual block
        /// </summary>
        public int BlockSize
        {
            get
            {
                return m_blockSize;
            }
        }

        /// <summary>
        /// Gets if the stream can be written to.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the number of available simultaneous read/write sessions.
        /// </summary>
        /// <remarks>This value is used to determine if a binary stream can be cloned
        /// to improve read/write/copy performance.</remarks>
        int ISupportsBinaryStream.RemainingSupportedIoSessions
        {
            get
            {
                return int.MaxValue;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Aquire an IO Session.
        /// </summary>
        public BinaryStreamIoSessionBase CreateIoSession()
        {
            return new IoSession(this);
        }

        /// <summary>
        /// Creates a new binary from an IO session
        /// </summary>
        /// <returns></returns>
        public BinaryStreamBase CreateBinaryStream()
        {
            return new BinaryStream(this);
        }

        #endregion
    }
}