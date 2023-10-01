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


// TODO: 3-d
class ResponseHelper
{
    public static void SendOkResponse(HttpListenerContext context, string responseData)
    {
        SendResponse(context, HttpStatusCode.OK, "text/html", responseData);
    }

    public static void SendErrorResponse(HttpListenerContext context, HttpStatusCode statusCode)
    {
        string pageNotFound = @"
        <!DOCTYPE html>
        <html>
        <head>
            <title>404 - Page Not Found</title>
        </head>
        <body>
            <h1>404 - Page Not Found</h1>
            <p>Sorry, the page you are looking for could not be found</p>
        </body>
        </html>";

        // set the context type in html format
        context.Response.ContentType = "text/html";
        // convert html content into a byte array
        byte[] buffer = Encoding.UTF8.GetBytes(pageNotFound);
        // write the byte array to the output stream
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);

        // Set the provided status code (e.g., HttpStatusCode.NotFound)
        context.Response.StatusCode = (int)statusCode;

        context.Response.OutputStream.Close();
    }

    private static void SendResponse(HttpListenerContext context, HttpStatusCode statusCode, string contentType, string responseData)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(responseData);
        context.Response.ContentType = contentType;
        context.Response.ContentLength64 = bytes.Length;
        context.Response.StatusCode = (int)statusCode;
        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        context.Response.OutputStream.Flush();
        context.Response.OutputStream.Close();
    }
}

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
        // TODO: 2-e: show books 1 to N
        if (!context.Request.QueryString.AllKeys.Contains("cmd")){
            // if the cliend does not specify a command, we do not know what to do
            // so we return a 400 Bad Request
            // TODO: improve the error message
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.OutputStream.Close();
            return;
        } 

        string cmd = context.Request.QueryString["cmd"];

        if (cmd.Equals("list"))
        {
            // list books s to e from the JSON file
            // TODO: handle errors (e or s not specified, not a number, s bigger than e etc.)
            // TODO: 3-c
            string author = context.Request.QueryString["a"];
            string title = context.Request.QueryString["t"];
            
            //List<Book> sublist = books.GetRange(start, end - start + 1);

            List<Book> filteredList = books;

            if (!string.IsNullOrEmpty(author))
            {
                // Filter by exact or partial author's name.
                filteredList = filteredList.Where(book => book.Authors.Any(a => a.Contains(author, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            if (!string.IsNullOrEmpty(title))
            {
                // Filter by exact or partial title.
                filteredList = filteredList.Where(book => book.Title.Contains(title, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // TODO: 4: Feature 1: Sort books by author's name or title
            if (context.Request.QueryString.AllKeys.Contains("sort"))
            {
                string sortType = context.Request.QueryString["sort"];
                // Sort by author's name
                if (sortType.Equals("a", StringComparison.OrdinalIgnoreCase))
                {
                    filteredList = filteredList.OrderBy(book => string.Join(", ", book.Authors)).ToList();
                }
                // Sort by title
                else if (sortType.Equals("t", StringComparison.OrdinalIgnoreCase))
                {
                    filteredList = filteredList.OrderBy(book => book.Title).ToList();
                }
            }

            // TODO: 4: Feature 2: Remove books with empty cells
            if (context.Request.QueryString.AllKeys.Contains("empty") &&
                context.Request.QueryString["empty"].ToLower() == "true")
            {
                filteredList = filteredList.Where(book => !string.IsNullOrEmpty(book.Title) && book.Authors != null && book.Authors.Count > 0 &&
                    !string.IsNullOrEmpty(book.ShortDescription) && !string.IsNullOrEmpty(book.ThumbnailUrl)
                ).ToList();
            }

            // If s and e parameters are provided, apply the start and end limits
            if (context.Request.QueryString.AllKeys.Contains("s") && context.Request.QueryString.AllKeys.Contains("e"))
            {
                int start = Int32.Parse(context.Request.QueryString["s"]);
                int end = Int32.Parse(context.Request.QueryString["e"]);
                filteredList = filteredList.Skip(start).Take(end - start + 1).ToList();
            }

            string response = @"
            <table border=1>
            <tr>
                <th>Title</th>
                <th>Author</th>
                <th>Short Description</th>
                <th>Thumbnail</th>
            </tr>";

            /*
            foreach (Book book in sublist){
                string authors = String.Join(",<br> ", book.Authors);
                response += $@"
                <tr>
                    <td>{book.Title}</td>
                    <td>{authors}</td>
                    <td>{book.ShortDescription}</td>
                    <td><img src='{book.ThumbnailUrl}'/></td>
                </tr>";
            }
            */

            foreach (Book book in filteredList)
            {
                string authors = string.Join(",<br> ", book.Authors);
                response += $@"
                <tr>
                    <td>{book.Title}</td>
                    <td>{authors}</td>
                    <td>{book.ShortDescription}</td>
                    <td><img src='{book.ThumbnailUrl}'/></td>
                </tr>";
            }

            response += "</table>";

            // TODO: 3-d
            ResponseHelper.SendOkResponse(context, response);

            /*
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(response);
            // write HTTP response to the output stream
            // all of the context.response stuff is setting the headers for the HTTP response
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
            context.Response.AddHeader("Last-Modified", DateTime.Now.ToString("r"));
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
            */
        }
        else if (cmd.Equals("random"))
        {
            // return a random book from JSON file
            Random rand = new Random();
            int index = rand.Next(books.Count);
            Book book = books[index];
            string authors = String.Join(",<br> ", book.Authors);
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

            // TODO: 3-d
            ResponseHelper.SendOkResponse(context, response);

            /*
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
            context.Response.OutputStream.Close();
            */
        }
        // TODO: improved 2-d
        else if (cmd.Equals("get"))
        {
            if (!context.Request.QueryString.AllKeys.Contains("n"))
            {
                ResponseHelper.SendErrorResponse(context, HttpStatusCode.BadRequest);
                return;
            }

            int n;
            if (!int.TryParse(context.Request.QueryString["n"], out n))
            {
                ResponseHelper.SendErrorResponse(context, HttpStatusCode.BadRequest);
                return;
            }

            if (n < 0 || n >= books.Count)
            {
                ResponseHelper.SendErrorResponse(context, HttpStatusCode.NotFound);
                return;
            }

            Book book = books[n];
            string authors = string.Join(",<br> ", book.Authors);
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
                    <td><img src='{book.ThumbnailUrl}'/></td>
                </tr>
                </table>
            ";

            ResponseHelper.SendOkResponse(context, response);
        }
        else
        {
            // TODO: 3-d
            // TODO: handle unknown command
            ResponseHelper.SendErrorResponse(context, HttpStatusCode.BadRequest);
        }

        /*
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
        */

        // build the HTML response
        // @ means a multiline string (Java doesn't have this)
        // $ means string interpolation (Java doesn't have this either)
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
    private string[] _indexFiles;
    
    // map extensions to MIME types
    // TODO: put this into a configuration file
    private IDictionary<string, string> _mimeTypeMappings;

    // instance variables
    private Thread _serverThread;
    private string _rootDirectory;
    private HttpListener _listener;
    private int _port;
    private int _numRequests = 0;
    private bool _done = false;
    private Dictionary<string, int> pathsRequested = new Dictionary<string, int>();
    private Dictionary<string, int> wrongPathsRequested = new Dictionary<string, int>();

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

    // TODO: 3-b
    public Dictionary<string, int> WrongPathsRequested
    {
        get { return wrongPathsRequested; }  
    }

    /// <summary>
    /// Construct server with given port.
    /// </summary>
    /// <param name="path">Directory path to serve.</param>
    /// <param name="port">Port of the server.</param>
    public SimpleHTTPServer(string path, int port, string configFilename)
    {
        this.Initialize(path, port, configFilename);
    }

    /// <summary>
    /// Construct server with any open port.
    /// </summary>
    /// <param name="path">Directory path to serve.</param>
    public SimpleHTTPServer(string path, string configFilename)
    {
        //get an empty port
        TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        this.Initialize(path, port, configFilename);
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
                context.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }
        else
        {
            // TODO: 3-b
            string wrongFilename = context.Request.Url.AbsolutePath;
            wrongPathsRequested[wrongFilename] = wrongPathsRequested.GetValueOrDefault(wrongFilename, 0) + 1;
            // This sends a 404 if the file doesn't exist or cannot be read
            // TODO: 3-a: customize the 404 page
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            
            // TODO: 3-d
            /*
            string pageNotFound = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>404 - Page Not Found</title>
            </head>
            <body>
                <h1>404 - Page Not Found</h1>
                <p>Sorry, the page you are looking for could not be found</p>
            </body>
            </html>";
    
            // set the context type in html format
            context.Response.ContentType = "text/html";
            // convert html content into a byte array
            byte[] buffer = Encoding.UTF8.GetBytes(pageNotFound);
            // write the byte array to the output stream
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            */

            ResponseHelper.SendErrorResponse(context, HttpStatusCode.NotFound);
        }
        context.Response.OutputStream.Close();
    }

    /// <summary>
    /// Initializes the server by setting up a listener thread on the given port
    /// </summary>
    /// <param name="path">the path of the root directory to serve files</param>
    /// <param name="port">the port to listen for connections</param>
    /// <param name="configFilename">the name of the JSON configuration file</param>
    private void Initialize(string path, int port, string configFilename)
    {
        this._rootDirectory = path;
        this._port = port;

        // read config file
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        string text = File.ReadAllText(configFilename);
        var config = JsonSerializer.Deserialize<Config>(text, options);
        // assign from the config file
        _mimeTypeMappings = config.MimeTypes;
        _indexFiles = config.IndexFiles.ToArray();

        _serverThread = new Thread(this.Listen);
        _serverThread.Start();
    }
}
