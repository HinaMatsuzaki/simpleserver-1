using System.Text.Json;

static void TestJSON() {
    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    string text = File.ReadAllText(@"json/books.json");
    var books = JsonSerializer.Deserialize<List<Book>>(text, options);

    Book book = books[4];
    Console.WriteLine($"title: {book.Title}");
    Console.WriteLine($"authors: {book.Authors[0]}");
}

// TODO: 2-a: add a command line feature to give a better error message for unknown comman line options
static void TestServer() {
    SimpleHTTPServer server = new SimpleHTTPServer("files", 8080);
    string helpMessage = @"You can use the following commands:
        help - display this help message    
        stop - stop the server
        numreqs - display the number of requests
        paths - display the number of times each path was requested
";
    Console.WriteLine($"Server started!\n{helpMessage}");

    while (true)
    {
        Console.Write("> ");
        // read line from console
        String command = Console.ReadLine();
        if (command.Equals("stop"))
        {
            server.Stop();
            break;
        }
        else if (command.Equals("help"))
        {
            Console.WriteLine(helpMessage);
        }
        else if (command.Equals("numreqs"))
        {
            Console.WriteLine($"Number of requests: {server.NumRequests}");
        }
        else if (command.Equals("paths"))
        {
            foreach (var path in server.PathsRequested)
            {
                Console.WriteLine($"{path.Key}: {path.Value}");
            }
        }
        else
        {
            Console.WriteLine($"Unknown command: {command}");
        }
    }
}

//TestJSON();
TestServer();
