using System.IO;
using System.Linq;
using System.Net;

namespace Project.ServerUtilities;

//
// Summary:
// Provides server utilities for handling HTTP requests and responses.
// This class allows for custom request handling and response formatting.
// It includes methods for reading request parameters and sending responses.
// It also supports custom request paths and handles static file serving.
public class Server
{
  readonly HttpListener _listener;
  HttpListenerContext? _context = null;

  //
  // Summary:
  // Initializes a new instance of the Server class with the specified port.
  //  //
  // Parameters:
  //   port:
  //     The port on which the server will listen for requests.
  //  // Returns:
  //   A new instance of the Server class.
  public Server(int port)
  {
    _listener = new HttpListener();
    _listener.Prefixes.Add($"http://*:{port}/");
    _listener.Start();
  }

  //
  // Summary:
  // Waits for an incoming request and returns a Request object representing it.
  // Returns:
  // A Request object representing the incoming request.
  public Request WaitForRequest()
  {
    while (true)
    {
      _context?.Response.Close();
      _context = _listener.GetContext();
      var path = GetPath();

      if (IsCustomeRequest())
      {
        return new Request(_context, path);
      }

      if (path == "favicon.ico")
      {
        path = "website/favicon.ico";
      }

      if (!File.Exists(path))
      {
        _context.Response.StatusCode = 404;
        if (_context.Request.AcceptTypes?.Contains("text/html") ?? false)
        {
          path = "website/pages/404.html";
        }
        else
        {
          continue;
        }
      }

      var fileExtension = path.Split(".").Last();
      _context.Response.ContentType = fileExtension switch
      {
        "html" => "text/html; charset=utf-8",
        "js" => "application/javascript",
        _ => "",
      };

      _context.Response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");
      _context.Response.Headers.Add("Pragma", "no-cache");
      _context.Response.Headers.Add("Expires", "Thu, 01 Jan 1970 00:00:00 GMT");

      var fileBytes = File.ReadAllBytes(path);
      _context.Response.OutputStream.Write(fileBytes);
    }
  }

  //
  // Summary:
  // Gets the path of the request, adjusting for custom requests and static file serving.
  // Returns:
  //   The adjusted path of the request.
  string GetPath()
  {
    var context = _context!;

    var path = context.Request.Url!.AbsolutePath[1..];

    if ((context.Request.AcceptTypes?.Contains("text/html") ?? false) &&
      !path.EndsWith(".html"))
    {
      path += ".html";
    }
    else if ((context.Request.UrlReferrer?.AbsolutePath.EndsWith(".js") ?? false) &&
      !path.EndsWith(".js"))
    {
      path += ".js";
    }

    return path;
  }

  bool IsCustomeRequest()
  {
    return _context!.Request.Headers["X-Is-Custom"] == "true";
  }
}
