using System;
using System.Collections.Generic;

namespace SPBTV_TestApp.Archiver
{
    /// <summary>
    /// To extend Archiver algorithm implement new class from this interface and register at IoC container.
    /// All methods must be async.
    /// </summary>
    public interface IArchiver
    {
        void BeginCreateArchive(List<string> sourcePath, string destinationPath, Action<bool, Exception> callbackAction);

        void BeginExtractArchive(string sourcePath, string destinationPath, Action<bool, Exception> callbackAction);
    }
}