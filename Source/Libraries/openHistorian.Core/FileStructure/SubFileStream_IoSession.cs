﻿//******************************************************************************************************
//  SubFileStream_IoSession.cs - Gbtc
//
//  Copyright © 2013, Grid Protection Alliance.  All Rights Reserved.
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
//  6/15/2012 - Steven E. Chisholm
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using GSF;
using GSF.IO.Unmanaged;
using openHistorian.FileStructure.IO;

namespace openHistorian.FileStructure
{
    public partial class SubFileStream
    {
        /// <summary>
        /// An IoSession for the sub file stream.
        /// </summary>
        private unsafe class IoSession : BinaryStreamIoSessionBase
        {
            #region [ Members ]

            /// <summary>
            /// The address parser
            /// </summary>
            private IndexParser m_parser;

            /// <summary>
            /// The shadow copier if the address translation allows for editing.
            /// </summary>
            private ShadowCopyAllocator m_pager;

            private readonly SubFileStream m_stream;

            /// <summary>
            /// Contains the read/write buffer.
            /// </summary>
            private SubFileDiskIoSessionPool m_ioSessions;

            private readonly bool m_isReadOnly;
            private readonly int m_blockDataLength;
            private readonly uint m_lastEditedBlock;
            private int m_shiftBits;

            #endregion

            #region [ Constructors ]

            public IoSession(SubFileStream stream)
            {
                m_stream = stream;
                m_shiftBits = BitMath.CountTrailingOnes((uint)m_stream.m_blockSize - 1u);
                m_lastEditedBlock = stream.m_dataReader.LastCommittedHeader.LastAllocatedBlock;
                m_isReadOnly = stream.m_isReadOnly;
                m_blockDataLength = m_stream.m_blockSize - FileStructureConstants.BlockFooterLength;
                m_ioSessions = new SubFileDiskIoSessionPool(stream.m_dataReader, stream.m_fileHeaderBlock, stream.m_subFile, stream.m_isReadOnly);

                if (m_isReadOnly)
                {
                    m_parser = new IndexParser(m_ioSessions);
                }
                else
                {
                    m_pager = new ShadowCopyAllocator(m_ioSessions);
                    m_parser = m_pager;
                }
            }

            #endregion

            #region [ Properties ]

            private DiskIoSession DataIoSession
            {
                get
                {
                    return m_ioSessions.SourceData;
                }
            }

            #endregion

            #region [ Methods ]

            /// <summary>
            /// Releases all the resources used by the <see cref="IoSession"/> object.
            /// </summary>
            public override void Dispose()
            {
                if (!IsDisposed)
                {
                    try
                    {
                        OnBaseStatusChanged(new IoSessionStatusChangedEventArgs(IoSessionStatusChanged.Disposed));

                        if (m_ioSessions != null)
                        {
                            m_ioSessions.Dispose();
                            m_ioSessions = null;
                        }
                    }
                    finally
                    {
                        m_parser = null;
                        m_pager = null;
                        IsDisposed = true; // Prevent duplicate dispose.
                    }
                }
            }

            /// <summary>
            /// Sets the current usage of the <see cref="BinaryStreamIoSessionBase"/> to null.
            /// </summary>
            public override void Clear()
            {
                if (IsDisposed || m_ioSessions.IsDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                m_ioSessions.Clear();
            }

            public void ClearIndexCache(IndexParser mostRecentParser)
            {
                if (IsDisposed || m_ioSessions.IsDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                m_parser.ClearIndexCache(mostRecentParser);
            }

            private static int Divide(long value, int shiftBits, int divisior)
            {
                long minValue = 1 << shiftBits;
                int result = 0;
                while (value >= minValue)
                {
                    int intermediateResult = (int)(value >> shiftBits);
                    result += intermediateResult;
                    value -= intermediateResult * divisior;
                }
                if (value >= divisior)
                    result++;
                return result;
            }

            public override void GetBlock(BlockArguments args)
            {
                int blockDataLength = m_blockDataLength;
                long pos = args.position;
                if (IsDisposed || m_ioSessions.IsDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (pos < 0)
                    throw new ArgumentOutOfRangeException("position", "cannot be negative");
                if (pos >= (long)blockDataLength * (uint.MaxValue - 1))
                    throw new ArgumentOutOfRangeException("position", "position reaches past the end of the file.");

                uint physicalBlockIndex;
                uint indexPosition;

                if (pos <= uint.MaxValue) //64-bit divide is 2 times slower
                    indexPosition = ((uint)pos / (uint)blockDataLength);
                else
                    indexPosition = (uint)((ulong)pos / (ulong)blockDataLength); //64-bit signed divide is twice as slow as 64-bit unsigned.

                args.firstPosition = (long)indexPosition * blockDataLength;
                args.length = blockDataLength;

                if (args.isWriting)
                {
                    //Writing
                    if (m_isReadOnly)
                        throw new Exception("File is read only");
                    bool wasShadowPaged;
                    physicalBlockIndex = m_pager.VirtualToShadowPagePhysical(indexPosition, out wasShadowPaged);

                    if (wasShadowPaged)
                        m_stream.ClearIndexNodeCache(this, m_parser);

                    if (physicalBlockIndex == 0)
                        throw new Exception("Failure to shadow copy the page.");

                    DataIoSession.WriteToExistingBlock(physicalBlockIndex, BlockType.DataBlock, indexPosition);
                    args.firstPointer = (IntPtr)DataIoSession.Pointer;
                    args.supportsWriting = true;
                }
                else
                {
                    //Reading
                    physicalBlockIndex = m_parser.VirtualToPhysical(indexPosition);
                    if (physicalBlockIndex <= 0)
                        throw new Exception("Page does not exist");

                    DataIoSession.Read(physicalBlockIndex, BlockType.DataBlock, indexPosition);
                    args.firstPointer = (IntPtr)DataIoSession.Pointer;
                    args.supportsWriting = !m_isReadOnly && physicalBlockIndex > m_lastEditedBlock;
                }
            }

            #endregion
        }
    }
}