using System;
using ASCOM.LocalServer;

// Compile with the production ReferenceCountedObject.cs. No COM or registry access.
namespace ASCOM.LocalServer
{
    public static class Server
    {
        public static int Count, Shutdowns;
        public static int IncrementObjectCount() { return ++Count; }
        public static int DecrementObjectCount() { return --Count; }
        public static void ExitIf() { if (Count == 0) ++Shutdowns; }
    }
}
class Client : ReferenceCountedObjectBase, IDisposable
{
    public void Dispose() { ReleaseServerReference(); GC.SuppressFinalize(this); }
}
class LifetimeTests
{
    static void Assert(bool result) { if (!result) throw new Exception("Server lifetime regression"); }
    static void Main()
    {
        var first = new Client(); var second = new Client();
        Assert(Server.Count == 2);
        first.Dispose(); Assert(Server.Count == 1 && Server.Shutdowns == 0);
        first.Dispose(); Assert(Server.Count == 1 && Server.Shutdowns == 0);
        second.Dispose(); Assert(Server.Count == 0 && Server.Shutdowns == 1);
        second.Dispose(); Assert(Server.Count == 0 && Server.Shutdowns == 1);
        Console.WriteLine("PASS: releasing one client preserves the other; repeated disposal releases once.");
    }
}
