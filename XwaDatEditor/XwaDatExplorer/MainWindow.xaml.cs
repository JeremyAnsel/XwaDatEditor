﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using JeremyAnsel.Xwa.Dat;
using Microsoft.Win32;

namespace XwaDatExplorer
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private void RunBusyAction(Action action)
        {
            this.RunBusyAction(dispatcher => action());
        }

        private void RunBusyAction(Action<Action<Action>> action)
        {
            this.BusyIndicator.IsBusy = true;

            Action<Action> dispatcherAction = a =>
            {
                this.Dispatcher.Invoke(a);
            };

            Task.Factory.StartNew(state =>
            {
                var disp = (Action<Action>)state;
                disp(() => { this.BusyIndicator.IsBusy = true; });
                action(disp);
                disp(() => { this.BusyIndicator.IsBusy = false; });
            }, dispatcherAction);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.ExecuteOpen(null, null);
        }

        private void ExecuteOpen(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select a folder";
            dialog.DefaultExt = ".dat";
            dialog.CheckFileExists = true;
            dialog.Filter = "DAT files (*.dat)|*.dat";

            string directory;

            if (dialog.ShowDialog(this) == true)
            {
                directory = System.IO.Path.GetDirectoryName(dialog.FileName);
            }
            else
            {
                return;
            }

            this.DataContext = null;

            this.RunBusyAction(disp =>
                {
                    try
                    {
                        var datFiles = System.IO.Directory.EnumerateFiles(directory, "*.DAT")
                            .Select(file => DatFile.FromFile(file))
                            .ToDictionary(
                            t => System.IO.Path.GetFileNameWithoutExtension(t.FileName).ToUpperInvariant(),
                            t => t);

                        disp(() => this.DataContext = datFiles);
                    }
                    catch (Exception ex)
                    {
                        disp(() => MessageBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                });
        }
    }
}
