﻿using System;
using System.IO;

namespace Nyerguds.Util
{
    /// <summary>
    /// A wrapper for a stream that allows using it in a StreamWriter in a way that
    /// prevents the stream from being closed when the StreamWriter is disposed. Can be used as:
    /// <i>using (StreamWriter sw = new StreamWriter(new NonDisposingStream(stream), encoding))</i>
    /// </summary>
    /// <author>Maarten Meuris</author>
    public class NonDisposingStream : Stream
    {
        readonly Stream stream;

        /// <summary>
        /// A wrapper for a stream that allows using it in a StreamWriter in a way that
        /// prevents the stream from being closed when the StreamWriter is disposed. Can be used as:
        /// <i>using (StreamWriter sw = new StreamWriter(new NonDisposingStream(stream), encoding))</i>
        /// </summary>
        /// <param name="stream">The stream to wrap.</param>
        public NonDisposingStream(Stream stream)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public override bool CanRead => this.stream.CanRead;

        public override bool CanSeek => this.stream.CanSeek;

        public override bool CanTimeout => this.stream.CanTimeout;

        public override bool CanWrite => this.stream.CanWrite;

        public override long Length => this.stream.Length;

        public override long Position
        {
            get => this.stream.Position;
            set => this.stream.Position = value;
        }

        public override int ReadTimeout
        {
            get => this.stream.ReadTimeout;
            set => this.stream.ReadTimeout = value;
        }

        public override int WriteTimeout
        {
            get => this.stream.WriteTimeout;
            set => this.stream.WriteTimeout = value;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return this.stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return this.stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void Close()
        {
            // do nothing. Do not close the stream.
        }

        //public void Dispose()
        // No need to override this one; it just calls Close()

        protected override void Dispose(bool disposing)
        {
            // do nothing. Do not close the stream.
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return this.stream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            this.stream.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            this.stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.stream.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            return this.stream.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.stream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            this.stream.WriteByte(value);
        }
    }
}