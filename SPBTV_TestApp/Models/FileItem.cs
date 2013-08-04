using System.Collections.Generic;

namespace SPBTV_TestApp.Models
{
    public class FileItem : BaseModel
    {
        private string _fileName;
        private bool _isChecked;

        public FileItem(string fileName, bool isFolder, FileItem parentFolder = null, List<FileItem> subItems = null)
        {
            FileName = fileName;
            IsFolder = isFolder;
            SubItems = subItems ?? new List<FileItem>();
            Parent = parentFolder;
        }

        public bool IsFolder { get; private set; }
        public List<FileItem> SubItems { get; set; }
        public FileItem Parent { get; set; }

        public string FileName
        {
            get
            {
                if (IsFolder)
                    return _fileName + "/";
                return _fileName;
            }
            private set { _fileName = value; }
        }

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }
    }
}