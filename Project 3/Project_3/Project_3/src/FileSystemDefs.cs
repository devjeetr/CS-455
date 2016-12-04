/**
 * 
 * Devjeet Roy
 * 11404808
 * 
 * Homework 8 CS422
 * */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace CS422
{
		public abstract class Dir422
		{
	            public abstract string Name { get; }

	            public abstract IList<Dir422> GetDirs();

	            public abstract IList<File422> GetFiles();

	            public abstract Dir422 Parent { get;}

	            public abstract bool ContainsFile(string filename, bool recursive);

	            public abstract bool ContainsDir(string dirName, bool recursive);

	            public abstract File422 GetFile(string fileName);

	            public abstract Dir422 GetDir(string dirName);

	            public abstract File422 CreateFile(string fileName);

	            public abstract Dir422 CreateDir(string dirName);
		}


	public class Utilities{
		public static bool isValidFileName(string name){
			return !(name.Contains("/") || name.Contains("\\")) ;
		}

	}
		public abstract class File422
		{
			public abstract string Name { get; }

			public abstract Dir422 Parent { get;}

			public abstract Stream OpenReadOnly();

			public abstract Stream OpenReadWrite();

		}

		public class StdFSFile : File422 {
			private string m_path;
			private Dir422 m_parent;
			

			public StdFSFile(string Path, Dir422 Parent){
				m_path = Path;
				m_parent = Parent;
			}


			public override string Name 
			{
				get{
					return Path.GetFileName (m_path);
				}
			}
				
			public override Dir422 Parent
			{
				get{
					return m_parent;
				}
			}

			public override Stream OpenReadOnly()
			{
				try
				{
					return new FileStream(m_path, FileMode.Open, FileAccess.Read);
				}
				catch (Exception) {
					return null;
				}

			}

			public override Stream OpenReadWrite(){
				try
				{
					Console.WriteLine("Inside StdFile: {0}", m_path);
					return new FileStream(m_path, FileMode.Open, FileAccess.ReadWrite);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
					// The file could not be opened.
					return null;
				}
			}
		}



		public abstract class FileSys422{
			public abstract Dir422 GetRoot();

			public virtual bool Contains( File422 file){

                return Contains(file.Parent);
			}

			public virtual bool Contains(Dir422 dir){
                while (dir.Parent != null) {
                        dir = dir.Parent;
                }
                
                return Object.ReferenceEquals(dir, GetRoot());
			}

		}
	        
       


        public class StandardFileSystem : FileSys422{
            private StdFSDir m_root;
			
			private StandardFileSystem(StdFSDir root){
				m_root = root;
			}
                public static StandardFileSystem Create(string rootDir){
                    if(!Directory.Exists(rootDir))
                        return null;
                    
                        var root  = new StdFSDir(rootDir, null);
                   
                       return new StandardFileSystem(root); 
                }
                
                // just needs constructor for instatiating root
                public override Dir422 GetRoot()
                {
					return m_root;
                }
        }

		public class StdFSDir : Dir422{
			private string m_path;
			internal Dir422 m_parent;
			
		public override string Name{
			get{
				return new DirectoryInfo(m_path).Name;
			}
		}

		public override Dir422 Parent{
			get{ 
				return m_parent;
			}
		}
			public StdFSDir(string path, Dir422 parent){
		        //if (!Directory.Exists(path)) {
		        //        throw new ArgumentException ();
		        //}
		        m_parent = parent;

		        m_path = path;
			}

		// not recursive
		public override IList<File422> GetFiles()
		{
			List<File422> files = new List<File422>();

			foreach (string file in Directory.GetFiles(m_path))
			{
				files.Add(new StdFSFile(file, this));
			}
			return files;
		}


		// not recursive
		public override IList<Dir422> GetDirs()
		{
			List<Dir422> dirs= new List<Dir422>();

			foreach (var dir in Directory.GetDirectories(m_path))
			{
				dirs.Add(new StdFSDir(dir, this));
			}
			return dirs;
		}
		//
		public override bool  ContainsFile(string fileName, bool recursive){
			// check current dir
			var files = this.GetFiles();

			foreach(var file in files){
				if(file.Name == fileName)
					return true;
			}    

			if(!recursive)
				return false;

			var folders = this.GetDirs();

			foreach(var dir in folders){
				if(dir.ContainsFile(fileName, true))
					return true;
			}

			return false;
		}


		public override bool ContainsDir(string fileName, bool recursive){

			var dirs = this.GetDirs();

			foreach(var file in dirs){
				if(file.Name == fileName)
					return true;
			}    

			if(!recursive)
				return false;


			foreach(var dir in dirs){
				if(dir.ContainsDir(fileName, true))
					return true;
			}

			return false;
		}


		public override Dir422 GetDir(string dirName){

			var dirs = this.GetDirs();

			foreach(var dir in dirs){
				if(dir.Name == dirName)
					return dir;
			}    

			return null;
		}

		public override File422 GetFile(string filename){

			var files = this.GetFiles();

			foreach(var file in files){
				if(file.Name == filename)
					return file;
			}    

			return null;
		}

		// TODO
		// implement this
		public override File422 CreateFile(string fileName){
			if (!Utilities.isValidFileName (fileName)) {
				Console.WriteLine (fileName);
				return null;
			}
			Console.WriteLine("Here is the path: " + m_path);
			
			var fileStream = File.Create(Path.Combine(m_path, fileName));
			
			fileStream.Dispose();

			return new StdFSFile(Path.Combine(m_path, fileName), this);        

		}

		public override Dir422 CreateDir(string dirName){
			if(!Utilities.isValidFileName(dirName))
				return null;

			return new StdFSDir (Path.Combine(m_path, dirName), this);        

		}
	}

	public class MemoryFileSystem : FileSys422
	{
		private readonly Dir422 m_root;

		public MemoryFileSystem()
		{
			m_root = new MemFSDir(".", null);
		}

		public override Dir422 GetRoot()
		{
			return m_root;
		}
	}

	public class MemFSDir : Dir422
	{
		private readonly string m_name;
		private readonly Dir422 m_parent;
		private readonly ConcurrentDictionary<File422, int> m_listOfOpenFiles;
		private readonly List<Dir422> m_directories;
		private readonly List<File422> m_files;

		// use mock object to lock and make this threadsafe
		private readonly object m_lock = new object();


		public override string Name { get { return m_name; } }
		public override Dir422 Parent { get { return m_parent; } }

		public MemFSDir(string name, Dir422 parent)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException("MemFSDir stringname empty");
			}

			m_name = name;
			m_parent = parent;

			m_files = new List<File422>();
			m_directories = new List<Dir422>();
			m_listOfOpenFiles = new ConcurrentDictionary<File422, int>();
		}

		public override IList<Dir422> GetDirs()
		{
			return m_directories;
		}

		public override IList<File422> GetFiles()
		{
			return m_files;
		}

		public override bool ContainsFile(string fileName, bool recursive)
		{
			if (string.IsNullOrEmpty(fileName) || !Utilities.isValidFileName(fileName))
			{
				return false;
			}

			foreach (var file in GetFiles())
			{
				if (file.Name == fileName)
				{
					return true;
				}
			}

			if (recursive)
			{
				foreach (var dir in m_directories)
				{
					if (dir.ContainsFile(fileName, true))
					{
						return true;
					}
				}
			}

			return false;
		}

		public override bool ContainsDir(string dirName, bool recursive)
		{
			if (string.IsNullOrEmpty(dirName) || !Utilities.isValidFileName(dirName))
			{
				return false;
			}

			foreach (var dir in m_directories)
			{
				if (dir.Name == dirName)
				{
					return true;
				}
			}

			if (recursive)
			{
				foreach (var dir in m_directories)
				{
					if (dir.ContainsDir(dirName, true))
					{
						return true;
					}
				}
			}

			return false;
		}

		public override File422 GetFile(string fileName)
		{
			if (string.IsNullOrEmpty(fileName) || !Utilities.isValidFileName(fileName))
			{
				return null;
			}

			foreach (var file in m_files)
			{
				if (file.Name == fileName)
				{
					return file;
				}
			}

			return null;
		}

		public override Dir422 GetDir(string dirName)
		{
			if (string.IsNullOrEmpty(dirName) || !Utilities.isValidFileName(dirName))
			{
				return null;
			}

			foreach (var dir in m_directories)
			{
				if (dir.Name == dirName)
				{
					return dir;
				}
			}

			return null;
		}

		public override File422 CreateFile(string fileName)
		{
			if (string.IsNullOrEmpty(fileName) || !Utilities.isValidFileName(fileName))
			{
				return null;
			}

			var toAdd = new MemFSFile(fileName, this);
			m_files.Add(toAdd);

			return toAdd;
		}

		public override Dir422 CreateDir(string dirName)
		{
			if (string.IsNullOrEmpty(dirName) || !Utilities.isValidFileName(dirName))
			{
				return null;
			}

			var toAdd = new MemFSDir(dirName, this);
			m_directories.Add(toAdd);

			return toAdd;
		}

		internal void OnFileAccessed(object sender, FileAccessEventArgs e)
		{
			MemFSFile file = sender as MemFSFile;
			/**if (file == null)
			{
				// Ensure that the object being sent is a MemFSFile
				throw new InvalidDataException("Object must be a MemFSFile object.");
			}*/

			lock (m_lock)
			{
				if (e.fileOperation == FileAccessEventArgs.FileOperation.Open)
				{
					// The file has been opened somewhere.
					// The mode that it was opened it does not matter from the directory's perspective.
					if (m_listOfOpenFiles.ContainsKey(file))
					{
						m_listOfOpenFiles[file]++;
					}
					else
					{
						m_listOfOpenFiles[file] = 1;
					}
				}
				else
				{
					// The file has been closed somewhere.
					m_listOfOpenFiles[file]--;
					if (m_listOfOpenFiles[file] == 0)
					{
						int outVal;
						// This file is no longer open anywhere.
						// Remove this handle and discard it from the opened files collection, then
						file.FileAccessed -= OnFileAccessed;
						m_listOfOpenFiles.TryRemove(file, out outVal);
					}
				}
			}
		}
	}

	public class MemFSFile : File422
	{
		private readonly string m_name;
		private readonly Dir422 m_parent;
		private int m_openR;
		private int m_openRW;
		private MemoryStream m_fileStream;

		private readonly object _lock = new object();

		internal event FileAccessEventHandler FileAccessed;

		public override string Name { get { return m_name; } }
		public override Dir422 Parent { get { return m_parent; } }

		public MemFSFile(string name, Dir422 parent)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException("Name empty: MemFsFile");
			}

			m_name = name;
			m_parent = parent;

			m_fileStream = new MemoryStream();
		}

		public override Stream OpenReadOnly()
		{
			
			lock (_lock)
			{
				if (m_openRW > 0)
				{
					return null;
				}

				ObservableStream fileStream = new ObservableStream(m_fileStream);

				fileStream.Disposed += HandleReadStreamDisposed;
				m_openR++;
				FileStreamOpened();
				fileStream.Position = 0;

				return fileStream;
			}
		}

		public override Stream OpenReadWrite()
		{
			lock (_lock)
			{
				if (m_openR > 0 || m_openRW > 0)
				{
					return null;
				}

				ObservableStream fileStream = new ObservableStream(m_fileStream);

				fileStream.Disposed += HandleReadWriteStreamDisposed;
				m_openRW++;
				FileStreamOpened();

				return fileStream;
			}
		}

		private void FileStreamOpened()
		{
			var handler = FileAccessed;
			if (null != handler)
			{
				handler(this, new FileAccessEventArgs(FileAccessEventArgs.FileOperation.Open));
			}
		}

		private void FileStreamClosed()
		{
			var handler = FileAccessed;
			if (null != handler)
			{
				handler(this, new FileAccessEventArgs(FileAccessEventArgs.FileOperation.Closed));
			}
		}

		private void HandleReadStreamDisposed()
		{
			lock (_lock)
			{
				m_openR--;
				FileStreamClosed();
			}
		}

		private void HandleReadWriteStreamDisposed()
		{
			lock (_lock)
			{
				m_openRW--;
				FileStreamClosed();
			}
		}
	}

	internal delegate void FileAccessEventHandler(object sender, FileAccessEventArgs e);

	internal class FileAccessEventArgs
	{
		internal enum FileOperation
		{
			Open,
			Closed
		}

		public FileOperation fileOperation;

		public FileAccessEventArgs(FileOperation operation)
		{
			fileOperation = operation;
		}
	}

	public delegate void DisposedEventHandler();

	public class ObservableStream : Stream
	{
		private readonly Stream m_stream;
		private bool m_disposed;


		// Stream properties
		public override bool CanRead { get { return m_stream.CanRead; } }
		public override bool CanSeek { get { return m_stream.CanSeek; } }
		public override bool CanWrite { get { return m_stream.CanWrite; } }
		public override long Length { get { return m_stream.Length; } }
		public override long Position { get { return m_stream.Position; } set { m_stream.Position = value; } }


		public event DisposedEventHandler Disposed;

			public ObservableStream(Stream stream)
		{
			if (null == stream)
			{
				throw new ArgumentNullException("stream is null: ObservableStream");
			}
			Console.WriteLine("Stream position: " + stream.Position);
			m_stream = stream;
		}

		~ObservableStream()
		{
			Dispose(false);
		}

		protected virtual void OnDisposed()
		{
			var handler = Disposed;
			if (null != handler)
			{
				handler();
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (!m_disposed)
			{
				OnDisposed();
				m_disposed = true;
			}
		}

		public override void Flush()
		{
			m_stream.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return m_stream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			m_stream.SetLength(value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return m_stream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			m_stream.Write(buffer, offset, count);
		}
	}
}
