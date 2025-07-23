using System.IO;
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
      var type = GetRequestType();

      if (type == "custom")
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
        if (type == "document")
        {
          path = "website/pages/404.html";
        }
        else
        {
          continue;
        }
      }

      _context.Response.ContentType = GetRequestType() switch
      {
        "document" => "text/html; charset=utf-8",
        "script" => "application/javascript",
        _ => "",
      };

      _context.Response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");
      _context.Response.Headers.Add("Pragma", "no-cache");
      _context.Response.Headers.Add("Expires", "Thu, 01 Jan 1970 00:00:00 GMT");

      var fileBytes = File.ReadAllBytes(path);
      _context.Response.OutputStream.Write(fileBytes);
    }
  }

  string GetRequestType()
  {
    var secFetchDest = _context!.Request.Headers["Sec-Fetch-Dest"];
    if (secFetchDest != null && secFetchDest != "empty")
    {
      return secFetchDest;
    }

    var isCustomRequest = _context.Request.Headers["X-Custom-Request"];
    if (isCustomRequest != null && isCustomRequest == "true")
    {
      return "custom";
    }

    return "empty";
  }

  string GetPath()
  {
    var context = _context!;

    var path = context.Request.Url!.AbsolutePath[1..];

    if (GetRequestType() == "script" &&
      !path.EndsWith(".js"))
    {
      path += ".js";
    }

    return path;
  }
}
