using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;

class LowerCaseNamingPolicy : JsonNamingPolicy
{
  public override string ConvertName(string name) =>
      name?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(name));
}

static class Tools
{
  public static JsonSerializerOptions JsonSerializeOptions = new()
  {
    PropertyNamingPolicy = new LowerCaseNamingPolicy(),
    IncludeFields = true
  };

  public static JsonSerializerOptions JsonDeserializeOptions = new()
  {
    IncludeFields = true
  };

  public static bool IsTuple(Type type)
  {
    var fields = type.GetFields();

    if (fields.Length == 0)
    {
      return false;
    }

    for (var i = 0; i < fields.Length; i++)
    {
      if (fields[i].Name != $"Item{i + 1}")
      {
        return false;
      }
    }

    return true;
  }
}

public static class Log
{
  public static void WriteException(Exception exception)
  {
    var shortened = ShortenException(exception);

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(shortened);
    Console.ResetColor();
  }

  static string ShortenException(Exception exception)
  {
    string message = exception.Message;

    string stackTrace = exception.StackTrace ?? "";
    string[] lines = stackTrace.Split("\n");
    string[] filteredLines = lines.Where(line => line.Contains(Environment.CurrentDirectory)).ToArray();

    string? innerExceptionMessage = exception.InnerException?.Message;

    string full;
    if (innerExceptionMessage != null)
    {
      full = message + "\n" + innerExceptionMessage + "\n" + string.Join("\n", filteredLines);
    }
    else
    {
      full = message + "\n" + string.Join("\n", filteredLines);
    }

    return full;
  }
}

public class Server
{
  readonly HttpListener _listener;
  HttpListenerContext? _context = null;

  public Server(int port)
  {
    _listener = new HttpListener();
    _listener.Prefixes.Add($"http://*:{port}/");
    _listener.Start();
  }

  public Request WaitForRequest()
  {
    while (true)
    {
      _context?.Response.Close();
      _context = _listener.GetContext();
      var path = getPath(_context);

      if (isCustomeRequest(_context))
      {
        return new Request(_context, path);
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

  string getPath(HttpListenerContext context)
  {
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

  bool isCustomeRequest(HttpListenerContext context)
  {
    return context.Request.Headers["X-Is-Custom"] == "true";
  }
}

public class Request(HttpListenerContext context, string path)
{
  readonly HttpListenerContext _context = context;

  public string Name { get; } = path;

  public T GetParams<T>()
  {
    var streamReader = new StreamReader(_context.Request.InputStream, _context.Request.ContentEncoding);
    var jsonStr = streamReader.ReadToEnd();

    if (Tools.IsTuple(typeof(T)))
    {
      jsonStr = TupliseArrayJsonStr(jsonStr);
    }

    return JsonSerializer.Deserialize<T>(jsonStr, Tools.JsonDeserializeOptions)!;
  }



  public void Respond<T>(T value)
  {
    string jsonStr = JsonSerializer.Serialize(value, Tools.JsonSerializeOptions);

    if (Tools.IsTuple(typeof(T)))
    {
      jsonStr = ArrayifyTupleJsonStr(jsonStr);
    }

    jsonStr = $"{{\"data\": {jsonStr}}}";
    var bytes = Encoding.UTF8.GetBytes(jsonStr);
    _context.Response.OutputStream.Write(bytes);
  }

  public void SetStatusCode(int statusCode)
  {
    _context.Response.StatusCode = statusCode;
  }

  static string TupliseArrayJsonStr(string arrayJsonStr)
  {
    var arrayJsonObj = JsonNode.Parse(arrayJsonStr)!.AsArray();
    var tuplisedObj = new JsonObject();

    int count = 1;
    foreach (var item in arrayJsonObj)
    {
      tuplisedObj[$"Item{count}"] = item?.DeepClone();
      count++;
    }

    var tuplisedStr = tuplisedObj.ToJsonString();

    return tuplisedStr;
  }

  static string ArrayifyTupleJsonStr(string tupleJsonStr)
  {
    var jsonObj = JsonNode.Parse(tupleJsonStr)!.AsObject();

    var arrJsonObj = new JsonArray();

    foreach (var field in jsonObj)
    {
      arrJsonObj.Add(field.Value!.DeepClone());
    }

    var arrJsonStr = arrJsonObj.ToJsonString();

    return arrJsonStr;
  }
}

public class DbBase : DbContext
{
  readonly string _name;
  readonly bool _isNewlyCreated;

  public DbBase(string name) : base()
  {
    _name = name;

    _isNewlyCreated = Database.EnsureCreated();
    Database.ExecuteSqlRaw("PRAGMA journal_mode = DELETE;");
  }

  public bool IsNewlyCreated()
  {
    return _isNewlyCreated;
  }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    optionsBuilder.UseSqlite($"Data Source={_name}.sqlite");
  }
}