using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DynamicData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using AssetBundlePatcher;
using AssetBundlePatcher.ViewModels;
using File = System.IO.File;
using Avalonia.Platform.Storage;

namespace AssetBundlePatcher.Views
{
    public partial class MainWindow : Window
    {
        public static MainWindow? mainWindow;
        public MainWindow()
        {
            InitializeComponent();
            Console.SetOut(new CustomTextWriter(this.ConsoleTextBlock));
            mainWindow = this;
            Console.WriteLine("Started Application");
            if (!Avalonia.Controls.Design.IsDesignMode)
            {
            }
            OptionsText.Text = "Options: Default\nThis application has been stripped down significantly to become more generalized.\nI don't really recommend anyone use it, as there are much better options out there!";
            CustomWorldIDDockPanel.IsVisible = false;
            //BundleLocator.ReindexWorldsOnThread();
        }

        public void SelectBundleClick(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);

            // Start async operation to open the dialog.
            var files = topLevel?.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Asset Bundle",
                AllowMultiple = false
            });
            files?.Wait();
            if (files?.Result.Count >= 1)
            {
                Console.WriteLine(files.Result[0].Path.AbsolutePath);
                //"file:///"
                ((MainWindowViewModel)this.DataContext).SceneEditorStart(files.Result[0].Path.ToString().Substring(8));
                EditSceneEditorPath(files.Result[0].Path.ToString().Substring(8));
                SceneEditorButtonClick(null, null);
            }
        }

        public void EditSceneEditorPath(string sceneEditorPath)
        {
            SceneEditorPath.Text = "Path: " + sceneEditorPath;
        }

        public void HomeButtonClick(object sender, RoutedEventArgs args)
        {
            this.ConsoleScrollViewer.IsVisible = false;
            this.HomeSection.IsVisible = true;
            this.SettingsViewer.IsVisible = false;
            this.SceneEditorPanel.IsVisible = false;
            Console.WriteLine("Switched To Home");
        }
        public void SceneEditorButtonClick(object sender, RoutedEventArgs args)
        {
            this.ConsoleScrollViewer.IsVisible = false;
            this.HomeSection.IsVisible = false;
            this.SettingsViewer.IsVisible = false;
            this.SceneEditorPanel.IsVisible = true;
            Console.WriteLine("Switched To Scene Editor");
        }

        public void ConsoleButtonClick(object sender, RoutedEventArgs args)
        {
            this.ConsoleScrollViewer.IsVisible = true;
            this.HomeSection.IsVisible = false;
            this.SettingsViewer.IsVisible = false;
            this.SceneEditorPanel.IsVisible = false;
            Console.WriteLine("Switched To Console");
        }
        public void SettingsButtonClick(object sender, RoutedEventArgs args)
        {
            this.ConsoleScrollViewer.IsVisible = false;
            this.HomeSection.IsVisible = false;
            this.SettingsViewer.IsVisible = true;
            this.SceneEditorPanel.IsVisible = false;
            Console.WriteLine("Switched To Settings");
        }

        

        public void OnConsoleTextChanged(object sender, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Property.Name == "Text")
            {
                ConsoleScrollViewer.ScrollToEnd();
            }
        }

        

        public void MoveToSceneEditorClick(object sender, RoutedEventArgs args)
        {
            //string wrldId = GetSelectedWorld(true);
            //SceneEditorWorldID.Text = wrldId;
            SceneEditorButtonClick(null, null);
            SceneEditorStartClick(null, null);
        }
        
        public void SceneEditorStartClick(object sender, RoutedEventArgs args)
        {
            ((MainWindowViewModel)this.DataContext).SceneEditorStart();
        }
        public void SceneEditorResetClick(object sender, RoutedEventArgs args)
        {
            ((MainWindowViewModel)this.DataContext).SceneEditorReset();
        }
        public void SceneEditorSaveClick(object sender, RoutedEventArgs args)
        {
            ((MainWindowViewModel)this.DataContext).SceneEditorSave();
        }

        private void Binding(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }
    }



}