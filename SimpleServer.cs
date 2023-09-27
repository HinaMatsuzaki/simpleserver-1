// SimpleServer based on code by Can Güney Aksakalli
// MIT License - Copyright (c) 2016 Can Güney Aksakalli
// https://aksakalli.github.io/2014/02/24/simple-http-server-with-csparp.html
// modifications by Jaime Spacco

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Web;
using System.Text.Json;


/// <summary>
/// Interface for simple servlets.
/// 
/// </summary>
interface IServlet {
    void ProcessRequest(HttpListenerContext context);
}
/// <summary>
/// BookHandler: Servlet that reads a JSON file and returns a random book
/// as an HTML table with one row.
/// TODO: search for specific books by author or title or whatever
/// </summary>
class BookHandler : IServlet {

    private List<Book> books;

    public BookHandler() {
        // we want to use case-insensitive matching for the JSON properties
        // the json files use lowercae letters, but we want to use uppercase in our C# code
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        string text = File.ReadAllText(@"json/books.json");
        books = JsonSerializer.Deserialize<List<Book>>(text, options);
    }

    public void ProcessRequest(HttpListenerContext context) {
        // TODO: 2-d: show book number N
        int bookNum = 0;
        if (context.Request.QueryString.AllKeys.Contains("n"))
        {
            bookNum = Int32.Parse(context.Request.QueryString["n"]);
        }

        // grab a random book
        Book book = books[bookNum];

        // convert book.Authors, which is a list, into a string with ", <br>" in between each author
        // string.Join() is a very useful method
        string delimiter = ",<br> ";
        string authors = string.Join(delimiter, book.Authors);

        // build the HTML response
        // @ means a multiline string (Java doesn't have this)
        // $ means string interpolation (Java doesn't have this either)
        string response = $@"
        <table border=1>
        <tr>
            <th>Title</th>
            <th>Author</th>
            <th>Short Description</th>
            <th>Long Description</th>
        </tr>
        <tr>
            <td>{book.Title}</td>
            <td>{authors}</td>
            <td>{book.ShortDescription}</td>
            <td>{book.LongDescription}</td>
        </tr>
        </table>
        ";
       
        // write HTTP response to the output stream
        // all of the context.response stuff is setting the headers for the HTTP response
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(response);
        context.Response.ContentType = "text/html";
        context.Response.ContentLength64 = bytes.Length;
        context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
        context.Response.AddHeader("Last-Modified", DateTime.Now.ToString("r"));
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        context.Response.OutputStream.Flush();
    }
}
/// <summary>
/// FooHandler: Servlet that returns a simple HTML page.
/// </summary>
class FooHandler : IServlet {

    public void ProcessRequest(HttpListenerContext context) {
        string response = $@"
            <H1>This is a Servlet Test.</H1>
            <h2>Servlets are a Java thing; there is probably a .NET equivlanet but I don't know it</h2>
            <h3>I am but a humble Java programmer who wrote some Servlets in the 2000s</h3>
            <p>Request path: {context.Request.Url.AbsolutePath}</p>
";
        foreach ( String s in context.Request.QueryString.AllKeys )
            response += $"<p>{s} -> {context.Request.QueryString[s]}</p>\n";

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(response);

        context.Response.ContentType = "text/html";
        context.Response.ContentLength64 = bytes.Length;
        context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
        context.Response.AddHeader("Last-Modified", DateTime.Now.ToString("r"));
        context.Response.StatusCode = (int)HttpStatusCode.OK;

        context.Response.OutputStream.Write(bytes, 0, bytes.Length);

        context.Response.OutputStream.Flush();
    }
}


class SimpleHTTPServer
{
    // bind servlets to a path
    // for example, this means that /foo will be handled by an instance of FooHandler
    // TODO: put these mappings into a configuration file
    private static IDictionary<string, IServlet> _servlets = new Dictionary<string, IServlet>() {
        {"foo", new FooHandler()},
        {"books", new BookHandler()},
    };

    // list of default index files
    // if the client requests a directory (e.g. http://localhost:8080/), 
    // we will look for one of these files
    private readonly string[] _indexFiles = { 
        "index.html", 
        "index.htm", 
        "default.html", 
        "default.htm" 
    };
    
    // map extensions to MIME types
    // TODO: put this into a configuration file
    private static IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
        #region extension to MIME type list
        {".asf", "video/x-ms-asf"},
        {".asx", "video/x-ms-asf"},
        {".avi", "video/x-msvideo"},
        {".bin", "application/octet-stream"},
        {".cco", "application/x-cocoa"},
        {".crt", "application/x-x509-ca-cert"},
        {".css", "text/css"},
        {".deb", "application/octet-stream"},
        {".der", "application/x-x509-ca-cert"},
        {".dll", "application/octet-stream"},
        {".dmg", "application/octet-stream"},
        {".ear", "application/java-archive"},
        {".eot", "application/octet-stream"},
        {".exe", "application/octet-stream"},
        {".flv", "video/x-flv"},
        {".gif", "image/gif"},
        {".hqx", "application/mac-binhex40"},
        {".htc", "text/x-component"},
        {".htm", "text/html"},
        {".html", "text/html"},
        {".ico", "image/x-icon"},
        {".img", "application/octet-stream"},
        {".iso", "application/octet-stream"},
        {".jar", "application/java-archive"},
        {".jardiff", "application/x-java-archive-diff"},
        {".jng", "image/x-jng"},
        {".jnlp", "application/x-java-jnlp-file"},
        {".jpeg", "image/jpeg"},
        {".jpg", "image/jpeg"},
        {".js", "application/x-javascript"},
        {".mml", "text/mathml"},
        {".mng", "video/x-mng"},
        {".mov", "video/quicktime"},
        {".mp3", "audio/mpeg"},
        {".mpeg", "video/mpeg"},
        {".mpg", "video/mpeg"},
        {".msi", "application/octet-stream"},
        {".msm", "application/octet-stream"},
        {".msp", "application/octet-stream"},
        {".pdb", "application/x-pilot"},
        {".pdf", "application/pdf"},
        {".pem", "application/x-x509-ca-cert"},
        {".pl", "application/x-perl"},
        {".pm", "application/x-perl"},
        {".png", "image/png"},
        {".prc", "application/x-pilot"},
        {".ra", "audio/x-realaudio"},
        {".rar", "application/x-rar-compressed"},
        {".rpm", "application/x-redhat-package-manager"},
        {".rss", "text/xml"},
        {".run", "application/x-makeself"},
        {".sea", "application/x-sea"},
        {".shtml", "text/html"},
        {".sit", "application/x-stuffit"},
        {".swf", "application/x-shockwave-flash"},
        {".tcl", "application/x-tcl"},
        {".tk", "application/x-tcl"},
        {".txt", "text/plain"},
        {".war", "application/java-archive"},
        {".wbmp", "image/vnd.wap.wbmp"},
        {".wmv", "video/x-ms-wmv"},
        {".xml", "text/xml"},
        {".xpi", "application/x-xpinstall"},
        {".zip", "application/zip"},
        #endregion
    };

    // instance variables
    private Thread _serverThread;
    private string _rootDirectory;
    private HttpListener _listener;
    private int _port;
    private int _numRequests = 0;
    private bool _done = false;
    private Dictionary<string, int> pathsRequested = new Dictionary<string, int>();

    public int Port
    {
        get { return _port; }
        private set { _port = value; }
    }

    //TODO: 2-b: add a command line feature to query the total number of requests
    public int NumRequests
    {
        get { return _numRequests; }
        private set { _numRequests = value; }
    }

    // TODO: 2-c
    public Dictionary<string, int> PathsRequested
    {
        get { return pathsRequested; }  
    }

    /// <summary>
    /// Construct server with given port.
    /// </summary>
    /// <param name="path">Directory path to serve.</param>
    /// <param name="port">Port of the server.</param>
    public SimpleHTTPServer(string path, int port)
    {
        this.Initialize(path, port);
    }

    /// <summary>
    /// Construct server with any open port.
    /// </summary>
    /// <param name="path">Directory path to serve.</param>
    public SimpleHTTPServer(string path)
    {
        //get an empty port
        TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        this.Initialize(path, port);
    }

    /// <summary>
    /// Stop server and dispose all functions.
    /// </summary>
    public void Stop()
    {
        _done = true;
        _listener.Close();
    }

    private void Listen()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
        _listener.Start();
        while (!_done)
        {
            Console.WriteLine("Waiting for connection...");
            try
            {
                HttpListenerContext context = _listener.GetContext();
                //TODO: 2-b
                NumRequests += 1;
                Process(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        Console.WriteLine("Server stopped!");
    }

    /// <summary>
    /// Process an incoming HTTP request with the given context.
    /// </summary>
    /// <param name="context"></param>
    private void Process(HttpListenerContext context)
    {
        string filename = context.Request.Url.AbsolutePath;
        // TODO: 2-c: add a command line feature to query the number of times each URL has been requested
        // keep track of how many times each path was requested
        // include the leading slash in the path
        pathsRequested[filename] = pathsRequested.GetValueOrDefault(filename, 0) + 1;
        /*
        if (pathsRequested. ContainsKey(filename))
            pathsRequested(filename) += 1;
        else
            pathsRequested.Add(filename, 1);
        */
        
        // remove leading slash
        filename = filename.Substring(1);
        Console.WriteLine($"{filename} is the path");

        // check if the path is mapped to a servlet
        if (_servlets.ContainsKey(filename))
        {
            _servlets[filename].ProcessRequest(context);
            return;
        }

        // if the path is empty (i.e. http://blah:8080/ which yields hte path /)
        // look for a default index filename
        if (string.IsNullOrEmpty(filename))
        {
            foreach (string indexFile in _indexFiles)
            {
                if (File.Exists(Path.Combine(_rootDirectory, indexFile)))
                {
                    filename = indexFile;
                    break;
                }
            }
        }

        // search for the file in the root directory
        // this means we are serving the file, if we can find it
        filename = Path.Combine(_rootDirectory, filename);

        if (File.Exists(filename))
        {
            try
            {
                Stream input = new FileStream(filename, FileMode.Open);
                
                //Adding permanent http response headers
                string mime;
                context.Response.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime) ? mime : "application/octet-stream";
                context.Response.ContentLength64 = input.Length;
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filename).ToString("r"));

                byte[] buffer = new byte[1024 * 16];
                int nbytes;
                while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                    context.Response.OutputStream.Write(buffer, 0, nbytes);
                input.Close();
                
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.OutputStream.Flush();
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

        }
        else
        {
            // This sends a 404 if the file doesn't exist or cannot be read
            // TODO: customize the 404 page
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }
        
        context.Response.OutputStream.Close();
    }

    /// <summary>
    /// Initializes the server by setting up a listener thread on the given port
    /// </summary>
    /// <param name="path">the path of the root directory to serve files</param>
    /// <param name="port">the port to listen for connections</param>
    private void Initialize(string path, int port)
    {
        this._rootDirectory = path;
        this._port = port;
        _serverThread = new Thread(this.Listen);
        _serverThread.Start();
    }


}
