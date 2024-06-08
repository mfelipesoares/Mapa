using System.Threading;
using UnityEditor;

namespace Needle.Engine.Utils
{
    #if UNITY_EDITOR
    [InitializeOnLoad]
    #endif
    internal static class UnityThreads
    {
        private static readonly Thread _mainThread;
        static UnityThreads()
        {
            _mainThread = Thread.CurrentThread;
        }
        
        /// <summary>
        /// Returns true if we're currently on the main thread
        /// </summary>
        public static bool IsMainThread()
        {
            return Thread.CurrentThread == _mainThread;
        }
    }
}