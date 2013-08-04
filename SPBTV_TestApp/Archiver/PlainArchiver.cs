using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace SPBTV_TestApp.Archiver
{
    /// <summary>
    /// Implemention of plain archive structure.
    /// All data stores at simple xml format.
    /// </summary>
    public class PlainArchiver : IArchiver
    {
        private const string DirectoryNode = "Directory";
        private const string FileNode = "File";
        private const string PathAttribute = "Path";
        private readonly IsolatedStorageFile _isolatedStorage;

        public PlainArchiver()
        {
            _isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
        }

        public void BeginCreateArchive(List<string> sources, string destinationPath, Action<bool, Exception> callbackAction)
        {
            if (callbackAction == null)
                throw new ArgumentNullException("callbackAction");
            ThreadPool.QueueUserWorkItem(obj =>
            {
                try
                {
                    var archive = new XElement("root");
                    foreach (string sourcePath in sources)
                    {
                        XElement xmlStructure = CreatePackageFromPath(sourcePath);
                        archive.Add(xmlStructure);
                    }
                    using (IsolatedStorageFileStream file = _isolatedStorage.CreateFile(destinationPath.TrimEnd('\\', '/') + ".spb"))
                    {
                        archive.Save(file);
                    }
                    callbackAction.Invoke(true, null);
                }
                catch (Exception exception)
                {
                    callbackAction.Invoke(false, exception);
                }
            });
        }

        public void BeginExtractArchive(string sourcePath, string destinationPath, Action<bool, Exception> callbackAction)
        {
            if (callbackAction == null)
                throw new ArgumentNullException("callbackAction");

            if (!_isolatedStorage.FileExists(sourcePath))
                callbackAction.Invoke(false, new FileNotFoundException());

            ThreadPool.QueueUserWorkItem(obj =>
            {
                try
                {
                    using (IsolatedStorageFileStream fileStream = _isolatedStorage.OpenFile(sourcePath, FileMode.Open))
                    {
                        XElement root = XDocument.Load(fileStream).Element("root");
                        if (root != null)
                        {
                            foreach (XElement element in root.Elements(DirectoryNode))
                            {
                                ParseElementToFile(element, destinationPath);
                            }
                            foreach (XElement element in root.Elements(FileNode))
                            {
                                ParseElementToFile(element, destinationPath);
                            }
                        }
                    }
                    callbackAction.Invoke(true, null);
                }
                catch (Exception exception)
                {
                    callbackAction.Invoke(false, exception);
                }
            });
        }

        private void ParseElementToFile(XElement rootElement, string rootFolder = "")
        {
            if (rootElement == null)
                throw new ArgumentNullException("rootElement");

            XAttribute rootPath = rootElement.Attribute(PathAttribute);
            if (rootPath == null)
                return;

            IEnumerable<XElement> elements = rootElement.Elements();
            IList<XElement> xElements = elements as IList<XElement> ?? elements.ToList();
            if (!xElements.Any())
            {
                if (rootElement.Name == DirectoryNode)
                    _isolatedStorage.CreateDirectory(rootFolder + "/" + rootPath.Value);
                XAttribute xAttribute = rootElement.Attribute(PathAttribute);
                if (rootElement.Name == FileNode && xAttribute != null)
                    CreateFile(rootFolder, xAttribute.Value, rootElement.Value);
                return;
            }
            foreach (XElement child in xElements)
            {
                XAttribute childPath = child.Attribute(PathAttribute);
                if (childPath == null)
                    continue;

                if (child.Name == DirectoryNode)
                {
                    _isolatedStorage.CreateDirectory(rootFolder + "/" + rootPath.Value + "/" + childPath.Value);
                    ParseElementToFile(child, rootFolder + "/" + rootPath.Value + "/");
                }

                if (child.Name == FileNode)
                    CreateFile(rootFolder + "/" + rootPath.Value, childPath.Value, child.Value);
            }
        }

        private void CreateFile(string folderPath, string fileName, string baseString)
        {
            if (!_isolatedStorage.DirectoryExists(folderPath))
                _isolatedStorage.CreateDirectory(folderPath);
            using (IsolatedStorageFileStream file = _isolatedStorage.CreateFile(folderPath + "\\" + fileName))
            {
                byte[] encodedValue = Convert.FromBase64String(baseString);
                file.Write(encodedValue, 0, encodedValue.Length);
            }
        }

        private XElement CreatePackageFromPath(string sourcePath)
        {
            if (sourcePath != null && _isolatedStorage.FileExists(sourcePath))
            {
                string result;
                using (var file = new IsolatedStorageFileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                        _isolatedStorage))
                {
                    var buffer = new byte[32*1024];
                    using (var ms = new MemoryStream())
                    {
                        int read;
                        while ((read = file.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }
                        result = Convert.ToBase64String(ms.ToArray());
                    }
                }
                return new XElement(FileNode, new XAttribute(PathAttribute, sourcePath), result);
            }

            if (sourcePath != null && _isolatedStorage.DirectoryExists(sourcePath))
            {
                var directoryElement = new XElement(DirectoryNode,
                    new XAttribute(PathAttribute, Path.GetDirectoryName(sourcePath).Split('\\').Last()));

                var files = new List<string>(_isolatedStorage.GetFileNames(sourcePath));
                foreach (string filePath in files)
                {
                    string result;
                    using (
                        var file = new IsolatedStorageFileStream(sourcePath + filePath, FileMode.Open, FileAccess.Read,
                            FileShare.Read, _isolatedStorage))
                    {
                        var buffer = new byte[32*1024];
                        using (var ms = new MemoryStream())
                        {
                            int read;
                            while ((read = file.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ms.Write(buffer, 0, read);
                            }
                            result = Convert.ToBase64String(ms.ToArray());
                        }
                    }
                    var fileElement = new XElement(FileNode, new XAttribute(PathAttribute, filePath), result);
                    directoryElement.Add(fileElement);
                }

                foreach (string directory in _isolatedStorage.GetDirectoryNames(sourcePath))
                {
                    string directoryPath = sourcePath + "/" + directory + "/";
                    XElement subDirectories = CreatePackageFromPath(directoryPath);
                    directoryElement.Add(subDirectories);
                }
                return directoryElement;
            }
            return new XElement("empty");
        }
    }
}