using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CS422
{
    class FilesWebService : WebService
    {
        private String UPLOAD_SCRIPT = @"<script>
											function selectedFileChanged(fileInput, urlPrefix)
											{
											document.getElementById('uploadHdr').innerText = 'Uploading ' + fileInput.files[0].name + '...';
											// Need XMLHttpRequest to do the upload
											if (!window.XMLHttpRequest)
											{
											alert('Your browser does not support XMLHttpRequest. Please update your browser.');
											return;
											}
											// Hide the file selection controls while we upload
											var uploadControl = document.getElementById('uploader');
											if (uploadControl)
											{
											uploadControl.style.visibility = 'hidden';
											}
											// Build a URL for the request
											if (urlPrefix.lastIndexOf('/') != urlPrefix.length - 1)
											{
											urlPrefix += '/';
											}
											var uploadURL = urlPrefix + fileInput.files[0].name;
											// Create the service request object
											var req = new XMLHttpRequest();
											req.open('PUT', uploadURL);
											console.log(uploadURL);
											req.onreadystatechange = function()
											{
												document.getElementById('uploadHdr').innerText = 'Upload (request status == ' + req.status + ')';
												location.reload();
											};
											req.send(fileInput.files[0]);
											}
										</script>";

        private String FILE_UPLOADER_HTML = "<hr><h3 id='uploadHdr'>Upload</h3><br>" +
                                            "<input id=\"uploader\" type='file' " +
                                            "onchange='selectedFileChanged(this,\"{0}\")' /><hr>";

        //  JPEG, PNG, PDF, MP4, TXT, HTML and XML files
        private string[] CONTENT_TYPES = new string[]{"image/jpeg", "image/png",
                                                        "application/pdf", "video/mp4", "text/plain",
                                                        "text/html", "application/xml"};
        private string[][] CONTENT_FILE_TYPES = new string[][]{new string[]{".jpg", ".jpeg"},
                                new string[]{".png"}, new string[]{".pdf"}, new string[]{".mp4"},
                                new string[]{".txt"}, new string[]{".html"}, new string[]{".xml"}};
        private const string RESPONSE_FORMAT =
                            @"<html>
									{0}
									 <h1>Folders</h1>
									 
									 {1}
									 <h1>Files</h1>
											{2}
									 <br>
									 {3}
							</html>";

		private const int CHUNK_SIZE = 10000 * 20;

        private const string RESPONSE_ENTRY_FORMAT =
            @"<a href='{0}'>{1}</a>
									 <br>";

        private FileSys422 fileSystem;

        private bool uploadAllowed = true;

        public FilesWebService(FileSys422 fs)
        {
            fileSystem = fs;
        }


        Dir422 getParentDir(string path)
        {
            var dirStructure = path.Split('/');

            if (dirStructure.Length == 1 && dirStructure[0].Length == 0)
                return fileSystem.GetRoot();

            // Console.WriteLine (dirStructure [0]);
            var root = fileSystem.GetRoot();

            int i = 0;
            for (i = 0; i < dirStructure.Length - 1; i++)
            {
                if (!root.ContainsDir(dirStructure[i], false))
                    return null;
                else
                    root = root.GetDir(dirStructure[i]);
            }


            return root;
        }

        public override void Handler(WebRequest req)
        {
            if (req.Method == "PUT")
                handlePutRequest(req);
            else if (req.Method == "GET")
                handleGetRequest(req);
        }

        private void handleGetRequest(WebRequest req)
        {
            // TODO maybe change this
            var url = req.RequestTarget.Substring("/files/".Length);

            if (url.Length == 0)
            {
                // Console.WriteLine ("Root requested");
                req.WriteHTMLResponse(BuildDirHTML(fileSystem.GetRoot()));
                return;
            }

            if (url.EndsWith("/"))
                url = url.Remove(url.Length - 1, 1);

            var dir = getParentDir(url);

			//Console.WriteLine ("----------------------->");
			req.Print();
            var tokens = url.Split('/');
            var fileOrFolderName = tokens[tokens.Length - 1];

            if (dir == null)
            {
                // Console.WriteLine ("Not found null");
                return;
            }
            else if (dir.ContainsFile(fileOrFolderName, false))
            {
                // Console.WriteLine ("file");
                SendFile(dir.GetFile(fileOrFolderName), req);
            }
            else if (dir.ContainsDir(fileOrFolderName, false))
            {
                // Console.WriteLine ("folder: {0}, parent: {1}", fileOrFolderName, dir.GetDir (fileOrFolderName).Name);
                req.Headers = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

                req.WriteHTMLResponse(BuildDirHTML(dir.GetDir(fileOrFolderName)));
            }
            else
            {
                req.WriteNotFoundResponse(@"<h1>File not found</h1>");
            }
            Console.WriteLine("done");
        }


        private void handlePutRequest(WebRequest req)
        {
            ConcatStream stream = req.Body as ConcatStream;
            long totalFileSize = long.Parse(req.Headers["Content-Length"]);

            Byte[] buf = new Byte[int.MaxValue < totalFileSize ? int.MaxValue : totalFileSize];

            // Remove base url
            var path = System.Uri.UnescapeDataString(req.MethodArguments.Replace(@"/files", ""));

            var fileName = Path.GetFileName(path);
            var dirName = Path.GetDirectoryName(path);

            var dir = fileSystem.GetRoot().GetDir(dirName.Substring(1));
            var file = dir.GetFile(fileName);

            if (file != null)
            {
                req.WriteResponse("400 BadRequest", "<h1>file exists already!!!!!!!</h1>");
                return;
            }

            file = dir.CreateFile(fileName);

            var writeStream = file.OpenReadWrite();

            int totalReadSize;
            while (totalFileSize > 0)
            {
                // Read whatever we can into buff
                long readSize = totalFileSize < buf.Length ? totalFileSize : buf.Length;

                totalReadSize = stream.Read(buf, 0, Convert.ToInt32(readSize));
                totalFileSize -= totalReadSize;

                // now write these bytes to disk

                if (totalReadSize <= 0)
                    break;

                writeStream.Write(buf, 0, totalReadSize);
            }

            writeStream.Dispose();
            writeStream.Close();

            req.WriteHTMLResponse(@"<h1>File uploaded successfully</h1>");
        }

        string getPath(Dir422 directory)
        {
            string path = "";
            var x = directory;
            if (x.Parent == null)
                return "/files/";

            while (x != null && x.Parent != null)
            {
                path = x.Name + @"/" + path;
                x = x.Parent;
            }

            return "/files/" + path;

        }

        string BuildDirHTML(Dir422 directory)
        {

            var files = directory.GetFiles();
            var dirs = directory.GetDirs();
            // Console.WriteLine ("inside build {0} dirs, {1} files",dirs.Count, files.Count);

            var dirStr = "";
            var path = getPath(directory);
            // Console.WriteLine ("-------------------------");
            // Console.WriteLine (path);

            foreach (var dir in dirs)
            {
                dirStr += String.Format(RESPONSE_ENTRY_FORMAT, path + dir.Name, dir.Name);
            }
            var fileStr = "";
            foreach (var file in files)
            {
                fileStr += String.Format(RESPONSE_ENTRY_FORMAT, path + file.Name, file.Name);
            }

            String fileUploadScript = uploadAllowed ? UPLOAD_SCRIPT : "";
            String fileUploadHtml = uploadAllowed ? String.Format(FILE_UPLOADER_HTML, getPath(directory)) : "";

            return String.Format(RESPONSE_FORMAT, fileUploadScript, dirStr, fileStr, fileUploadHtml);
        }


        public override string ServiceURI
        {
            get
            {
                return "/files";
            }
        }


        void SendFile(File422 file, WebRequest req)
        {
			
            // check if request contains range header
            if (req.Headers.ContainsKey("Range"))
            {
				SendFileUsingRangeRequest(file, CHUNK_SIZE, req);
            }
            else
            {   //replace old headers
				req.Headers = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

				//now send
				SendFileRegular(file, CHUNK_SIZE, req);
            }
        }

		private const string PARTIAL_CONTENT_STATUS_MSG = "206 Partial Content";

		/// <summary>
		///  
		/// </summary>
		/// <param name="request">Request.</param>
		/// <param name="fileName">File name.</param>
		/// <param name="sizeToSend">Size to send.</param>
		void SendPartialContentHeaderResponse(WebRequest request, String fileName, long start, long end, long fileSize) {
			long sizeToSend = end - start + 1;

			// Reset request headers
			request.Headers = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

			// only need to send 1 response
			string contentType = GetContentType(fileName);
			if (contentType != null)
				request.Headers["Content-Type"] = contentType;
			else
				request.Headers["Content-Type"] = "text/plain";

			// Set content length
			request.Headers["Accept-Ranges"] = "bytes";
			request.Headers["Content-Length"] = String.Format("{0}", sizeToSend);
			request.Headers["Content-Range"] = String.Format("bytes {0}-{1}/{2}", start, end, fileSize);

			//req.WriteResponse("206 Partial Content", fileContents);
			string headers = request.getHeaderString(Convert.ToInt32(sizeToSend));
			string response = request.getResponseString(PARTIAL_CONTENT_STATUS_MSG, headers, "");
			Console.WriteLine("Response:");
			Console.WriteLine(response);
			byte[] httpHeaderBytes = System.Text.UnicodeEncoding.UTF8.GetBytes(response);

			request.NetworkStream.Write(httpHeaderBytes, 0, httpHeaderBytes.Length);
		}







		private const string INVALID_RANGE_PARTIAL_CONTENT_STATUS = "416 Range Not Satisfiable";

		void SendFileUsingRangeRequest(File422 file, long chunkSize, WebRequest req) { 

			var range = req.Headers["Range"];
			Match match = Regex.Match(range, @".*bytes=([0-9]+)-([0-9]*)");

			long start = long.Parse(match.Groups[1].Value);
			long end = -1;

			// if end match not found then value remains 0
			if (match.Groups.Count > 2 && match.Groups[2].Value.Length > 0)
				end = long.Parse(match.Groups[2].Value);
			
			FileStream fileStream = (System.IO.FileStream)file.OpenReadOnly();

			long fileSize = fileStream.Length;

			if (start > end && end > 0)
				req.WriteResponse(INVALID_RANGE_PARTIAL_CONTENT_STATUS, 
				                  String.Format("Invalid Range Header Specified: start > end: {0} > {1}", 
				                                start, end));

			if (start > fileSize)
				req.WriteResponse(INVALID_RANGE_PARTIAL_CONTENT_STATUS,
				                          String.Format("Invalid Range Header Specified: start > fileSize: {0} > {1}", 
				                                        start, fileSize));

			if (end <= 0)
			{
				end = fileSize - 1;
			}

			long sizeToSend = end - start + 1;

			if (true)//(sizeToSend  < chunkSize)
			{
				
				//Send header:
				SendPartialContentHeaderResponse(req, file.Name, start, end, fileSize);

				// now send file
				SendFileData(fileStream, chunkSize, req.NetworkStream, start, end);

				req.NetworkStream.Close();
			}
			else
			{
				// need to send multiple responses
				string contentType = GetContentType(file.Name);
				if (contentType != null)
					req.Headers["Content-Type"] = contentType;
				
				long offset = start;
				long sent = 0;
				req.Headers["Accept-Ranges"] = "bytes";
				req.Headers["Content-Range"] = String.Format("bytes {0}-{1}/{2}", start, end, sizeToSend);

				while (sent <= sizeToSend)
				{
					long currentSize = (sizeToSend - sent) < chunkSize ? sizeToSend - sent : chunkSize;

					if (offset + currentSize > fileSize)
						currentSize = fileSize - offset + 1;


					if (currentSize <= 0)
						break;

					var fileContents = GetFileRange(fileStream, offset, offset + currentSize);

					//throw new NotImplementedException();
					//req.WriteResponse("206 PartialContent", fileContents);

					offset += currentSize + 1;
					sent += currentSize;
				}
			}
		}


		void SendFileData(FileStream fileStream, long chunkSize, Stream networkStream, long start, long end) {

			Console.WriteLine("End: {0}", end);
			end = end <= 0 ? fileStream.Length - 1 : end;

			int offset = (int)start;
			
			long sizeToSend = end - start + 1;

			Console.WriteLine(fileStream.Position);
			Console.WriteLine("start: {0}, end: {1}, sizeToSend: {2}, offset: {3}", 
			                  offset, end, sizeToSend, offset);
			int totalBytesSent = 0;

			byte[] fileData = GetFileRange(fileStream, start, end);

			// now send file
			while (totalBytesSent < sizeToSend)
			{
				long remaining = end - offset + 1;

				if (remaining <= 0)
					break;

				long currentSizeToRead = remaining < chunkSize ? remaining : chunkSize;

				Console.WriteLine("remaining:D " + remaining);
				try
				{
					networkStream.Write(fileData, totalBytesSent, (int)currentSizeToRead);
				}
				catch (IOException e)
				{
					Console.WriteLine("exception: ");
					Console.WriteLine(e.Message);
					Console.WriteLine("Connection closed by client!!");
					break;
				}

				totalBytesSent += (int)currentSizeToRead;

				offset += (int)currentSizeToRead;

				//Console.WriteLine("SendFileData: byetsRead - {0}", bytesRead);

			}

			Console.WriteLine("\n\n\nstart: {0}, end: {1}, sizeToSend: {2}, offset: {3}",
							  offset, end, sizeToSend, offset);
			Console.WriteLine("TotalBytes Sent: {0}", totalBytesSent);
			networkStream.Close();
		}

		/**
		 * 
		 *  Regular file response
		 * */
		void SendFileRegular(File422 file, long chunkSize, WebRequest req)
        {
			FileStream fileStream = (System.IO.FileStream)file.OpenReadOnly();
			Stream networkStream = req.NetworkStream;

			// Set Content type
			string contentType = GetContentType(file.Name);

			req.Headers["Accept-Range"] = "bytes";
			if (contentType != null)
				req.Headers["Content-Type"] = contentType;
			else
				req.Headers["Content-Type"] = "text/plain";


			// first send headers
			string headerString = req.getHeaderString((int) fileStream.Length);
			string responseString = req.getResponseString("200 OK", headerString, "");

			byte[] responseBytes = System.Text.UnicodeEncoding.UTF8.GetBytes(responseString);

			networkStream.Write(responseBytes, 0, responseBytes.Length);

			// Now send file data
			SendFileData(fileStream, chunkSize, networkStream, 0, -1);

			fileStream.Close();
			networkStream.Close();
			Console.WriteLine("Done");
        }

        string GetContentType(string fileName)
        {
            string extension = Path.GetExtension(fileName);

            for (int i = 0; i < CONTENT_FILE_TYPES.Length; i++)
            {
                for (int j = 0; j < CONTENT_FILE_TYPES[i].Length; j++)
                {
                    if (CONTENT_FILE_TYPES[i][j] == extension)
                        return CONTENT_TYPES[i];
                }
            }

            return null;
        }


        byte[] GetFileRange(Stream fileStream, long start, long end)
        {

            long size = end - start + 1;
            if (start + size >= fileStream.Length)
                size = fileStream.Length - start;

            byte[] buf = new byte[size];

            fileStream.Seek(start, SeekOrigin.Begin);
            fileStream.Read(buf, 0, Convert.ToInt32(size));

            return buf;
        }


    }
}

