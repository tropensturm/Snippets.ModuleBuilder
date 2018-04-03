using System;

namespace Snippets.PoCLibrary
{
    [Serializable]
    public static class Core
    {
        public static string WhoAmI()
        {
            return $"PoCLibrary.Core.WhoAmI @{ System.Reflection.Assembly.GetExecutingAssembly().ImageRuntimeVersion }";
        }
    }
}
