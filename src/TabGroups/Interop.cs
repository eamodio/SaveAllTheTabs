using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;

namespace TabGroups.Interop
{
    public static class Ole32
    {
        [DllImport("Ole32.dll", EntryPoint = "CreateStreamOnHGlobal")]
        public static extern void CreateStreamOnHGlobal(IntPtr hGlobal, [MarshalAs(UnmanagedType.Bool)] bool deleteOnRelease, [Out] out Microsoft.VisualStudio.OLE.Interop.IStream stream);
    }

    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class VsOleStream : MemoryStream, IStream
    {
        void IStream.Read(byte[] pv, uint cb, out uint pcbRead)
        {
            var bytesRead = Read(pv, 0, (int)cb);
            pcbRead = (uint)bytesRead;
        }

        void IStream.Write(byte[] pv, uint cb, out uint pcbWritten)
        {
            Write(pv, 0, (int)cb);
            pcbWritten = cb;
        }

        void IStream.Seek(LARGE_INTEGER dlibMove, uint dwOrigin, ULARGE_INTEGER[] plibNewPosition)
        {
            var pos = base.Seek(dlibMove.QuadPart, (SeekOrigin)dwOrigin);
            plibNewPosition[0].QuadPart = (ulong)pos;
        }

        void IStream.SetSize(ULARGE_INTEGER libNewSize)
        {
            throw new NotImplementedException();
        }

        void IStream.CopyTo(IStream pstm, ULARGE_INTEGER cb, ULARGE_INTEGER[] pcbRead, ULARGE_INTEGER[] pcbWritten)
        {
            throw new NotImplementedException();
        }

        void IStream.Commit(uint grfCommitFlags)
        {
            Flush();
        }

        void IStream.Revert()
        {
            throw new NotImplementedException();
        }

        void IStream.LockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, uint dwLockType)
        {
            throw new NotImplementedException();
        }

        void IStream.UnlockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, uint dwLockType)
        {
            throw new NotImplementedException();
        }

        void IStream.Stat(Microsoft.VisualStudio.OLE.Interop.STATSTG[] pstatstg, uint grfStatFlag)
        {
            pstatstg[0].cbSize = new ULARGE_INTEGER {QuadPart = (ulong)Length};
        }

        void IStream.Clone(out IStream ppstm)
        {
            throw new NotImplementedException();
        }

        void ISequentialStream.Read(byte[] pv, uint cb, out uint pcbRead)
        {
            ((IStream)this).Read(pv, cb, out pcbRead);
        }

        void ISequentialStream.Write(byte[] pv, uint cb, out uint pcbWritten)
        {
            ((IStream)this).Write(pv, cb, out pcbWritten);
        }
    }
}
