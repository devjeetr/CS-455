// Devjeet Roy
// 11404808

using System;
using System.IO;

namespace CS422
{
	public class ConcatStream: Stream{
		private Stream A;
		private Stream B;
		private bool fixedLength = false;
		private bool canRead, canWrite, canSeek, lengthSupported;
		private long position;
		private long length;

		public ConcatStream(Stream first, Stream second){
			if(first == null || second == null)
				throw new ArgumentException("null stream passed to ConcatStream(Stream, Stream)");

			if(!first.CanSeek)
				throw new ArgumentException("Length property not found in first stream");

			if (first.CanSeek && second.CanSeek) {
				lengthSupported = true;
				length = first.Length + second.Length;
			} else {
				lengthSupported = false;
			}


			canSeek = first.CanSeek && second.CanSeek;
			canRead = first.CanRead && second.CanRead;
			canWrite = first.CanWrite && second.CanWrite;

			A = first;
			B = second;

			position = 0;

			fixedLength = false;
		}


		public ConcatStream(Stream first, Stream second, long fixedLen){
			if(first == null || second == null || fixedLen < 0)
				throw new ArgumentException("null stream passed to ConcatStream(Stream, Stream)");


			fixedLength = true;
			lengthSupported = true;

			position = 0;
			length = fixedLen;

			A = first;
			B = second;

			canSeek = first.CanSeek && second.CanSeek;
			canRead = first.CanRead && second.CanRead;
			canWrite = first.CanWrite && second.CanWrite;
		}

		// Properties
		public override long Position {
			get{ 
				return position;
			}

			set{
				Seek (value, SeekOrigin.Begin);
			}
		}

		public override void Flush(){
			throw new NotSupportedException ();
		}

		public override void SetLength(long len){
			if (!lengthSupported)
				throw new NotSupportedException ();

			if (len > A.Length && len <= this.Length) {
				int seekPosition = Convert.ToInt32 (len - A.Length);
				B.SetLength (seekPosition);
			} else if (len <= A.Length) {
				A.SetLength (len);
				B.SetLength (0);
			} else if (len > this.Length) {
				if (!fixedLength) {
					// expand B to new size
					B.SetLength(len - A.Length);
				}	
			}
			this.length = len;
		}

		public override bool CanRead{
			get{
				return canRead;
			}
		}

		public override bool CanWrite{
			get{ 
				return canWrite;
			}
		}

		public override bool CanSeek{
			get{ 
				return canSeek;
			}
		}

		public override long Length{
			get{ 
				if (!lengthSupported)
					throw new NotSupportedException ();
				
				if (fixedLength)
					return length;
				else
					return A.Length + B.Length;
			}
		}

		// Methods
		// TODO
		// Add support for fixed length property
		public override int Read(byte[] buffer, int offset, int count)
		{	
			if (!canRead)
				throw new NotSupportedException ();

			if (buffer.Length < offset + count) {
				throw new ArgumentException (string.Format("buffer.Length < offset + count, " + 
				"buffer.length: {0}, offset: {1}, count: {2}", buffer.Length, offset, count));
			}

			if(lengthSupported && count + Position > Length)
				throw new ArgumentException(String.Format("ConcatStream.Read: Invalid count, Position={0}, Length={1}, Count={2}",
					Position, Length, count));

			resetPositions ();
			int totalBytesRead = 0;

			if (position < A.Length) {
				int available = Convert.ToInt32(A.Length - A.Position);
				int bytesToRead = count > available ? available : count;

				int bytesRead = A.Read(buffer, offset, bytesToRead);

				count -= bytesToRead;
				offset += bytesToRead;
				position += bytesRead;
				totalBytesRead += bytesRead;
			}


			if (count > 0) {
				int bytesRead = B.Read (buffer, offset, count);

				position += bytesRead;
				totalBytesRead += bytesRead;
			}
			return totalBytesRead;
		}

		void resetPositions(){
			
			if (position >= A.Length) {
				A.Seek (A.Length, SeekOrigin.Begin);
				long remaining = position - A.Length;

				if (remaining >= 0) {
					if(B.CanSeek)
						B.Seek (remaining, SeekOrigin.Begin);
				}
			} else {
				if(B.CanSeek)
					B.Seek(0, SeekOrigin.Begin);
				
				A.Seek (position, SeekOrigin.Begin);
			}
		}

		public override void Write(byte[] buffer, int offset, int count){
			if (!canWrite)
				throw new NotSupportedException ();

			if (buffer.Length < offset + count) {
				throw new ArgumentException ("invalid buffer offset and count");
			}

			resetPositions ();
			// If this stream is fixed length, then 
			// we must truncate 'count' to the length
			/*if ()
				throw new ArgumentException ("Expanding not supported on fixed length stream");
			*/
			// if B is not expandable

			// Position = A.Position  + B.Position here
			int bufferCounter = offset;

			if (position < A.Length) {
				int bytesAvailable = Convert.ToInt32(A.Length - A.Position);

				// make sure we don't write more bytes than available
				int bytesToWrite = bytesAvailable < count ? bytesAvailable : count;

				A.Write(buffer, Convert.ToInt32(bufferCounter), Convert.ToInt32(bytesToWrite));

				// update counters
				count -= Convert.ToInt32(bytesToWrite);
				bufferCounter += bytesToWrite;
				position += bytesToWrite;
			}

			// TODO: 
			//	if Position > A.Length && A.Position + B.Position != Position
			//	and if B can't seek, then throw an exception
			//	else seek B to appropriate position
			if (count > 0) {
				if (position - A.Length != B.Position) {
					if (!B.CanSeek)
						throw new ArgumentException ("Unable to write to B");
					else {
						B.Seek (position - A.Length, SeekOrigin.Begin);
					}
				}

				// if B

				try{
					if(this.fixedLength && position + count > Length)
						throw new NotSupportedException();
					
					Console.WriteLine (Convert.ToInt32 (B.Length - B.Position));
					B.Write (buffer, Convert.ToInt32(bufferCounter), count);
				} catch(NotSupportedException){


					B.Write (buffer, Convert.ToInt32(bufferCounter), Convert.ToInt32(B.Length - B.Position));
				}

				position += count;
			}

		}

		// TODO
		// add support for fixed length
		public override long Seek(long offset, SeekOrigin origin)
		{
			if (!CanSeek)
				throw new NotSupportedException ("Seek operation not supported by ConcatStream");

			// set position according
			// to the SeekOrigin provided
			switch (origin) {
			case SeekOrigin.Begin: 
				position = offset;
				break;
			case SeekOrigin.Current: 
				position += offset;
				break;
			case SeekOrigin.End: 
				position = Length + offset;
				break;
			}

			// Don't truncate position because
			// according to MSDN:
			//
			// "Seeking to any location beyond the length of the stream is supported."
			// https://msdn.microsoft.com/en-us/library/system.io.stream.seek(v=vs.110).aspx

			//if (position > Length)
			//	position = Length;

			// truncate negative values to 0
			if (position < 0)
				position = 0;

			if (position < A.Length) {
				// be sure to reset B's stream position
				A.Seek (position, SeekOrigin.Begin);
				B.Seek (0, SeekOrigin.Begin);
			} else {
				// edge case when A.length = 0
				if(A.Length > 0)
					A.Seek (A.Length - 1, SeekOrigin.Begin);

				long seekPosition = position - A.Length;

				// be sure to not seek past B's bounds
				if(seekPosition < B.Length)
					B.Seek (seekPosition, SeekOrigin.Begin);
			}

			return 0;	
		}
	}
}
