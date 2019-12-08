using System;
using System.IO;
using System.Diagnostics;

namespace Crazy.ClientNet
{
    public class MessageBlock
    {
        public MessageBlock(Int32 maxLength)
        {
            _cache = new byte[maxLength];
            _rdPtr = 0;
            _wrPtr = 0;
        }

        /// <summary>
        /// Get data length.
        /// </summary>
        /// <value>Data length.</value>
        public Int32 Length
        {
            get
            {
                return (_wrPtr - _rdPtr);
            }
        }

        /// <summary>
        /// Get current space for write data.
        /// </summary>
        /// <value>The cache space.</value>
        public Int32 Space
        {
            get
            {
                return (_cache.Length - _wrPtr);
            }
        }

        /// <summary>
        /// Write the specified data to cache.
        /// </summary>
        /// <param name="dataBlock">Data block to be writed.</param>
        /// <param name="dataLen">Data block length.</param>
        /// <returns>The length has writed to cahche.</returns>
        public Int32 Write(byte[] dataBlock, Int32 dataLen)
        {
            /// Shit
            Debug.Assert(dataBlock != null);
            Debug.Assert(dataLen > 0);
            Debug.Assert(dataBlock.Length >= dataLen);

            /// What hell ?
            if (dataLen > Space)
                return 0;

            /// Copy to cache
            /// 将datablock写入当前流中
            Array.Copy(dataBlock, 0, _cache, _wrPtr, dataLen);
            
            _wrPtr += dataLen;

            /// Return copied length
            return dataLen;
        }

        /// <summary>
        /// Read 4 bytes as int32 value.
        /// </summary>
        /// <returns>The int32 value.</returns>
        public Int32 ReadInt32()
        {
            //Debug.Assert(Length >= sizeof(Int32));

            /// Convert and move ahead read pointer.
            Int32 rValue = BitConverter.ToInt32(_cache, _rdPtr);
            _rdPtr += sizeof(Int32);

            return rValue;
        }

        /// <summary>
        /// Read 2 bytes as UInt16 value.
        /// </summary>
        /// <returns>The int32 value.</returns>
        public UInt16 ReadUInt16()
        {
            UInt16 rValue = BitConverter.ToUInt16(_cache, _rdPtr);
            _rdPtr += sizeof(UInt16);

            return rValue;
        }

        /// <summary>
        /// Peek 4 bytes as int32 value.
        /// </summary>
        /// <returns>The int32 value.</returns>
        public Int32 PeekInt32()
        {
            Debug.Assert(Length >= sizeof(Int32));

            return
                BitConverter.ToInt32(_cache, _rdPtr);
        }

        /// <summary>
        /// Move adhead read pointer by given offset value.
        /// </summary>
        /// <returns>New read pointer.</returns>
        /// <param name="ahOffset">Move ahead offset.</param>
        public Int32 ReadPtr(Int32 ahOffset)
        {
            _rdPtr += ahOffset;
            Debug.Assert(_rdPtr >= 0 && _rdPtr <= _wrPtr);

            return _rdPtr;
        }

        /// <summary>
        /// Get memory stream to contain specific length data an move read ptr ahead.
        /// </summary>
        /// <returns>The memory stream hold specific data.</returns>
        /// <param name="dataLen">Data length.</param>
        public MemoryStream GetStream(Int32 dataLen)
        {
            Debug.Assert(dataLen <= Length);

            MemoryStream newStream = new MemoryStream(_cache, _rdPtr, dataLen, false);
            _rdPtr += dataLen;
            return newStream;
        }

        /// <summary>
        /// Normalizes data to align with the base.
        /// </summary>
        public void Crunch()
        {
            /// Go away
            if (_rdPtr == 0)
                return;

            /// Not neccessary
            if (_rdPtr < _cache.Length / 5)
                return;

            /// Go simple way
            if (Length == 0)
            {
                _rdPtr = 0;
                _wrPtr = 0;
            }
            /// Ops, copy, copy
            else
            {
                Int32 length = Length;
                //byte[] holdBuf = new byte[length];
                //Array.Copy(_cache, _rdPtr, holdBuf, 0, length);
                //Array.Copy(holdBuf, _cache, length);
                Array.Copy(_cache, _rdPtr, _cache, 0, length);
                _rdPtr = 0;
                _wrPtr = length;
            }
        }

        private byte[] _cache;
        private Int32 _rdPtr;
        private Int32 _wrPtr;
    }
}

