﻿using GSF;
using GSF.IO.Unmanaged;
using GSF.IO.Unmanaged.Test;
using NUnit.Framework;
using openHistorian.FileStructure;

namespace openHistorian.Archive.Test.FileStructure
{
    [TestFixture]
    internal class BenchmarkSubFileStreamTest
    {
        [Test]
        public void TestSubFileStream()
        {
            const int BlockSize = 256;
            Assert.AreEqual(Globals.MemoryPool.AllocatedBytes, 0L);
            //string file = Path.GetTempFileName();
            //System.IO.File.Delete(file);
            try
            {
                //using (FileSystemSnapshotService service = FileSystemSnapshotService.CreateFile(file))
                using (TransactionService service = TransactionService.CreateInMemory(BlockSize))
                {
                    using (TransactionalEdit edit = service.BeginEditTransaction())
                    {
                        SubFileStream fs = edit.CreateFile(SubFileName.Empty);
                        BinaryStream bs = new BinaryStream(fs);

                        for (int x = 0; x < 20000000; x++)
                            bs.Write(1L);

                        bs.Position = 0;

                        BinaryStreamBenchmark.Run(bs, false);

                        bs.Dispose();
                        fs.Dispose();
                        edit.CommitAndDispose();
                    }
                }
            }
            finally
            {
                //System.IO.File.Delete(file);
            }

            Assert.AreEqual(Globals.MemoryPool.AllocatedBytes, 0L);
            Assert.IsTrue(true);
        }
    }
}