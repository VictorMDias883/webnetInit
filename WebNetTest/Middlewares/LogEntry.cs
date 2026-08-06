public class LogEntry
{
    public string Id {get; set;} =  Guid.NewGuid().ToString();
    public string Method {get;set;} = string.Empty;
    public string Path {get;set;} = string.Empty;
    public int StatusCode{get;set;}
    public  long ElapsedMs {get;set;}
    public  string? Ip {get;set;}
    public DateTime TimeStamp {get;set;} =  DateTime.UtcNow;
}