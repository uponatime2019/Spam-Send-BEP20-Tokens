using SpamSendToken;
using System.Threading;

class Program
{
    static void Main()
    {
        SendTokenHelper.SpamSendToken();
        Thread.Sleep(Timeout.Infinite);
    }
}
