using EloBuddy.SDK.Events;

namespace BanSharpDetector
{
    class Program
    {
        static void Main()
        {
            Loading.OnLoadingComplete += (args) => new BanSharpDetector().Load();
        }
    }
}
