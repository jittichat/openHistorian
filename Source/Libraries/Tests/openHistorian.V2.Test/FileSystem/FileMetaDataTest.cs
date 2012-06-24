﻿////******************************************************************************************************
////  FileMetaDataTest.cs - Gbtc
////
////  Copyright © 2012, Grid Protection Alliance.  All Rights Reserved.
////
////  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
////  the NOTICE file distributed with this work for additional information regarding copyright ownership.
////  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
////  not use this file except in compliance with the License. You may obtain a copy of the License at:
////
////      http://www.opensource.org/licenses/eclipse-1.0.php
////
////  Unless agreed to in writing, the subject software distributed under the License is distributed on an
////  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
////  License for the specific language governing permissions and limitations.
////
////  Code Modification History:
////  ----------------------------------------------------------------------------------------------------
////  11/23/2011 - Steven E. Chisholm
////       Generated original version of source code. 
////       
////
////******************************************************************************************************

//using System;
//using System.IO;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

//namespace openHistorian.V2.FileSystem
//{
//    [TestClass()]
//    public class FileMetaDataTest
//    {
//        [TestMethod()]
//        public void Test()
//        {
//            Assert.AreEqual(Globals.BufferPool.AllocatedBytes, 0L);
//            Random rand = new Random();
//            int fileIdNumber = rand.Next(int.MaxValue);
//            Guid fileExtension = Guid.NewGuid();
//            int fileFlags = rand.Next(int.MaxValue);
//            int dataBlock1 = rand.Next(int.MaxValue);
//            int singleRedirect = rand.Next(int.MaxValue);
//            int doubleRedirect = rand.Next(int.MaxValue);
//            int tripleRedirect = rand.Next(int.MaxValue);

//            FileMetaData node = FileMetaData.CreateFileMetaData(fileIdNumber, fileExtension);
//            node.FileFlags = fileFlags;
//            node.DirectBlock = dataBlock1;
//            node.SingleIndirectBlock = singleRedirect;
//            node.DoubleIndirectBlock = doubleRedirect;
//            node.TripleIndirectBlock = tripleRedirect;
//            FileMetaData node2 = saveItem(node);

//            if (node2.FileIdNumber != fileIdNumber) throw new Exception();
//            if (node2.FileExtension != fileExtension) throw new Exception();
//            if (node2.FileFlags != fileFlags) throw new Exception();
//            if (node2.DirectBlock != dataBlock1) throw new Exception();
//            if (node2.SingleIndirectBlock != singleRedirect) throw new Exception();
//            if (node2.DoubleIndirectBlock != doubleRedirect) throw new Exception();
//            if (node2.TripleIndirectBlock != tripleRedirect) throw new Exception();
//            Assert.IsTrue(true);

//            Assert.AreEqual(Globals.BufferPool.AllocatedBytes, 0L);
//        }

//        private static FileMetaData saveItem(FileMetaData node)
//        {
//            //Serialize the header
//            MemoryStream stream = new MemoryStream();
//            node.Save(new BinaryWriter(stream));

//            stream.Position = 0;
//            //load the header
//            FileMetaData node2 = FileMetaData.OpenFileMetaData(new BinaryReader(stream));

//            CheckEqual(node2, node);

//            FileMetaData node3 = node2.CloneEditableCopy();

//            CheckEqual(node2, node3);
//            return node3;

//        }
//        internal static void CheckEqual(FileMetaData RO, FileMetaData RW)
//        {
//            if (!RO.AreEqual(RW)) throw new Exception();
//        }

//        /// <summary>
//        /// Determines if the two objects are equal in value.
//        /// </summary>
//        /// <param name="a">the object to compare this class to</param>
//        /// <returns></returns>
//        /// <remarks>A debug function</remarks>
//        internal bool AreEqual(FileMetaData a)
//        {
//            if (a == null)
//                return false;

//            if (FileIdNumber != a.FileIdNumber) return false;
//            if (FileExtension != a.FileExtension) return false;
//            if (FileFlags != a.FileFlags) return false;
//            if (DirectBlock != a.DirectBlock) return false;
//            if (SingleIndirectBlock != a.SingleIndirectBlock) return false;
//            if (DoubleIndirectBlock != a.DoubleIndirectBlock) return false;
//            if (TripleIndirectBlock != a.TripleIndirectBlock) return false;
//            return true;
//        }
    
//    }
//}
