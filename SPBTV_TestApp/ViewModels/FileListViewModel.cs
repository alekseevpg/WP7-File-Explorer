using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SPBTV_TestApp.Archiver;
using SPBTV_TestApp.IoCContainer;
using SPBTV_TestApp.Libraries;
using SPBTV_TestApp.Models;

namespace SPBTV_TestApp.ViewModels
{
    public class FileListViewModel : BaseModel
    {
        private ICommand _createArchivCommand;
        private FileItem _currentItem;
        private IsolatedStorageFile _currentStore;
        private ICommand _extractCommand;

        private ObservableCollection<FileItem> _files;
        private ICommand _goBackCommand;
        private bool _isBusy;
        private ICommand _tryGoIntoFolder;

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                OnPropertyChanged("IsBusy");
            }
        }

        public IsolatedStorageFile Store
        {
            get
            {
                _currentStore = _currentStore ?? IsolatedStorageFile.GetUserStoreForApplication();
                return _currentStore;
            }
        }

        public ObservableCollection<FileItem> Files
        {
            get { return _files ?? (_files = CreateFileCollection()); }
            set
            {
                _files = value;
                OnPropertyChanged("Files");
            }
        }

        public ICommand TryGoIntoFolder
        {
            get
            {
                _tryGoIntoFolder = _tryGoIntoFolder ?? new DelegateCommand(OnTryGoIntoFolderSelected);
                return _tryGoIntoFolder;
            }
        }

        public ICommand GoBackCommand
        {
            get
            {
                _goBackCommand = _goBackCommand ?? new DelegateCommand(OnGoBackSelected);
                return _goBackCommand;
            }
        }

        public ICommand CreateArchive
        {
            get
            {
                _createArchivCommand = _createArchivCommand ?? new DelegateCommand(OnCreateArchiveSelected);
                return _createArchivCommand;
            }
        }

        public ICommand ExtractArchive
        {
            get
            {
                _extractCommand = _extractCommand ?? new DelegateCommand(OnExtractArchiveSeleted);
                return _extractCommand;
            }
        }

        private ObservableCollection<FileItem> CreateFileCollection()
        {
            var files = new ObservableCollection<FileItem>();
            foreach (FileItem fileItem in LoadFiles("*"))
            {
                files.Add(fileItem);
            }
            return files;
        }


        private List<FileItem> LoadFiles(string sourcePath, FileItem parentFolder = null)
        {
            string root = Path.GetDirectoryName(sourcePath);

            if (!string.IsNullOrEmpty(root))
                root += "/";

            var files = new List<string>(Store.GetFileNames(sourcePath));

            List<FileItem> items = Store.GetDirectoryNames(sourcePath).Select(folderName =>
            {
                var folder = new FileItem(folderName, true, parentFolder);
                folder.SubItems = LoadFiles(root + folderName + "/", folder);
                return folder;
            }).ToList();
            items.AddRange(files.Select(fileName => new FileItem(fileName, false)).ToList());
            return items;
        }


        private void OnTryGoIntoFolderSelected(object param = null)
        {
            var newRoot = param as FileItem;
            if (newRoot == null)
                return;
            var tempCollection = new ObservableCollection<FileItem>();
            foreach (FileItem fileItem in newRoot.SubItems)
            {
                tempCollection.Add(fileItem);
            }
            Files = tempCollection;
            _currentItem = newRoot;
        }

        private void OnGoBackSelected()
        {
            if (_currentItem == null)
                return;
            if (_currentItem.Parent == null)
            {
                Files = CreateFileCollection();
                return;
            }
            var tempCollection = new ObservableCollection<FileItem>();
            foreach (FileItem fileItem in _currentItem.Parent.SubItems)
            {
                tempCollection.Add(fileItem);
            }
            Files = tempCollection;
            _currentItem = _currentItem.Parent;
        }

        private void OnCreateArchiveSelected(object param)
        {
            List<string> list = Files.Where(file => file.IsChecked).Select(source => source.FileName).ToList();

            if (!list.Any())
            {
                MessageBox.Show("Select some file!");
                return;
            }
            IsBusy = true;
            Container.Resolve<IArchiver>().BeginCreateArchive(list, Convert.ToString(param),
                (success, exception) =>
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        IsBusy = false;
                        MessageBox.Show(success ? "Archive created!" : "Error occuried!");
                        Files = CreateFileCollection();
                    }));
        }

        private void OnExtractArchiveSeleted(object param)
        {
            string fileToExtract = Files.Where(file => file.IsChecked && file.FileName.EndsWith(".spb"))
                .Select(source => source.FileName).ToList()
                .FirstOrDefault();

            if (fileToExtract == null)
            {
                MessageBox.Show("Select some file!");
                return;
            }

            IsBusy = true;

            Container.Resolve<IArchiver>().BeginExtractArchive(fileToExtract, Convert.ToString(param),
                (success, exception) =>
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        IsBusy = false;
                        MessageBox.Show(success ? "Archive extracted!" : "Error occuried!");
                        Files = CreateFileCollection();
                    }));
        }
    }
}