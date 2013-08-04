using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using Coding4Fun.Phone.Controls;
using SPBTV_TestApp.Models;
using SPBTV_TestApp.ViewModels;

namespace SPBTV_TestApp
{
    public partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
            CreateTestData();
        }

        private void OnListBoxClicked(object sender, MouseButtonEventArgs e)
        {
            var mainViewModel = LayoutRoot.DataContext as FileListViewModel;
            var fileItem = ((TextBlock) sender).DataContext as FileItem;

            if (mainViewModel == null || fileItem == null)
                return;
            if (fileItem.IsFolder)
                mainViewModel.TryGoIntoFolder.Execute(fileItem);
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            var mainViewModel = LayoutRoot.DataContext as FileListViewModel;

            if (mainViewModel == null)
            {
                base.OnBackKeyPress(e);
                return;
            }
            mainViewModel.GoBackCommand.Execute(null);
            e.Cancel = true;
            base.OnBackKeyPress(e);
        }

        private void CreateArchive(object sender, EventArgs e)
        {
            var mainViewModel = LayoutRoot.DataContext as FileListViewModel;

            if (mainViewModel == null)
                return;

            var input = new InputPrompt();
            input.Completed += (o, args) => mainViewModel.CreateArchive.Execute(args.Result);
            input.Title = "Enter archive name";
            input.Message = "It will be saved in root folder.";
            input.Show();
        }

        private void ExtractArchive(object sender, EventArgs e)
        {
            var mainViewModel = LayoutRoot.DataContext as FileListViewModel;

            if (mainViewModel == null)
                return;

            var input = new InputPrompt();
            input.Completed += (o, args) => mainViewModel.ExtractArchive.Execute(args.Result);
            input.Title = "Enter folder path";
            input.Message = "Archive will be extracted in root folder.";
            input.Show();
        }

        private void CreateTestData()
        {
            using (IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                fileStorage.CreateDirectory("source");
                fileStorage.CreateDirectory("source/subdirectory");
                fileStorage.CreateDirectory("source/subdirectory/sub");
                fileStorage.CreateDirectory("destination");

                fileStorage.CreateDirectory("result");


                byte[] tempBytes = Encoding.UTF8.GetBytes("I'm a first man on a moon!");
                using (var file = new IsolatedStorageFileStream("source/InTheRoot.txt", FileMode.Create, fileStorage))
                {
                    file.Write(tempBytes, 0, tempBytes.Length);
                }

                using (
                    var file = new IsolatedStorageFileStream("source/subdirectory/HereIAm.txt", FileMode.Create,
                        fileStorage))
                {
                    file.Write(tempBytes, 0, tempBytes.Length);
                }

                var client = new WebClient();
                client.OpenReadCompleted += ImageDownloaded;
                client.OpenReadAsync(new Uri("http://cs418329.vk.me/v418329327/97e8/I4EgST3QLpo.jpg"));
            }
        }

        private void ImageDownloaded(object sender, OpenReadCompletedEventArgs openReadCompletedEventArgs)
        {
            var streamResourceInfo = new StreamResourceInfo(openReadCompletedEventArgs.Result, null);
            using (IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (
                    IsolatedStorageFileStream isolatedStorageFileStream = fileStorage.CreateFile("source/testPic.jpg"))
                {
                    var bitmapImage = new BitmapImage {CreateOptions = BitmapCreateOptions.None};
                    bitmapImage.SetSource(streamResourceInfo.Stream);

                    var writeableBitmap = new WriteableBitmap(bitmapImage);
                    writeableBitmap.SaveJpeg(isolatedStorageFileStream, writeableBitmap.PixelWidth,
                        writeableBitmap.PixelHeight, 0, 85);
                }
            }
        }
    }
}