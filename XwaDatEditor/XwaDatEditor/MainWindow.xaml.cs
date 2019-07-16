using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JeremyAnsel.Xwa.Dat;
using Microsoft.Win32;

namespace XwaDatEditor
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;
            this.ExecuteNew(null, null);
        }

        private static readonly DependencyProperty DatFileProperty = DependencyProperty.Register("DatFile", typeof(DatFile), typeof(MainWindow));

        public DatFile DatFile
        {
            get { return (DatFile)this.GetValue(DatFileProperty); }
            set
            {
                this.SetValue(DatFileProperty, null);
                this.SetValue(DatFileProperty, value);
            }
        }

        private static readonly DependencyProperty DatGroupIdProperty = DependencyProperty.Register("DatGroupId", typeof(short), typeof(MainWindow));

        public short DatGroupId
        {
            get { return (short)this.GetValue(DatGroupIdProperty); }
            set
            {
                this.SetValue(DatGroupIdProperty, value);
            }
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

                try
                {
                    action(disp);
                }
                catch (Exception ex)
                {
                    disp(() => MessageBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                }

                disp(() => { this.BusyIndicator.IsBusy = false; });
            }, dispatcherAction);
        }

        private void ExecuteNew(object sender, ExecutedRoutedEventArgs e)
        {
            this.RunBusyAction(disp =>
                {
                    var dat = new DatFile();

                    disp(() => this.DatFile = dat);
                });
        }

        private void ExecuteOpen(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.DefaultExt = ".dat";
            dialog.CheckFileExists = true;
            dialog.Filter = "DAT files (*.dat)|*.dat";

            string fileName;

            if (dialog.ShowDialog(this) == true)
            {
                fileName = dialog.FileName;
            }
            else
            {
                return;
            }

            this.RunBusyAction(disp =>
                {
                    try
                    {
                        var dat = DatFile.FromFile(fileName);
                        disp(() => this.DatFile = dat);
                    }
                    catch (Exception ex)
                    {
                        disp(() => MessageBox.Show(this, fileName + "\n" + ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                });
        }

        private void ExecuteSave(object sender, ExecutedRoutedEventArgs e)
        {
            var dat = this.DatFile;

            if (dat == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(dat.FileName))
            {
                this.ExecuteSaveAs(null, null);
                return;
            }

            this.RunBusyAction(disp =>
                {
                    try
                    {
                        dat.Save(dat.FileName);

                        disp(() => this.DatFile = dat);
                    }
                    catch (Exception ex)
                    {
                        disp(() => MessageBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                });
        }

        private void ExecuteSaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            var dat = this.DatFile;

            if (dat == null)
            {
                return;
            }

            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".dat";
            dialog.Filter = "DAT files (*.dat)|*.dat";
            dialog.FileName = System.IO.Path.GetFileName(dat.FileName);

            string fileName;

            if (dialog.ShowDialog(this) == true)
            {
                fileName = dialog.FileName;
            }
            else
            {
                return;
            }

            this.RunBusyAction(disp =>
                {
                    try
                    {
                        dat.Save(fileName);
                        disp(() => this.DatFile = dat);
                    }
                    catch (Exception ex)
                    {
                        disp(() => MessageBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                });
        }

        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            var dat = this.DatFile;

            if (dat == null)
            {
                return;
            }

            short groupId = this.DatGroupId;

            this.RunBusyAction(disp =>
                {
                    var group = new DatGroup(groupId);

                    dat.Groups.Add(group);

                    disp(() => this.DatFile = dat);
                    disp(() => this.GroupsList.SelectedIndex = this.GroupsList.Items.Count - 1);
                });
        }

        private void DeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            var dat = this.DatFile;

            if (dat == null)
            {
                return;
            }

            int index = this.GroupsList.SelectedIndex;

            if (index == -1)
            {
                return;
            }

            this.RunBusyAction(disp =>
                {
                    dat.Groups.RemoveAt(index);

                    disp(() => this.GroupsList.SelectedIndex = -1);
                    disp(() => this.DatFile = dat);
                });
        }

        private void NewImage_Click(object sender, RoutedEventArgs e)
        {
            var group = this.GroupsList.SelectedItem as DatGroup;

            if (group == null)
            {
                return;
            }

            int imageId = group.Images.Count == 0 ? 0 : group.Images.Max(t => t.ImageId) + 1;

            this.RunBusyAction(disp =>
                {
                    var image = new DatImage(group.GroupId, (short)imageId);
                    group.Images.Add(image);

                    disp(() => this.DatFile = this.DatFile);
                    disp(() => this.ImagesList.SelectedIndex = this.ImagesList.Items.Count - 1);
                });
        }

        private void AddImage_Click(object sender, RoutedEventArgs e)
        {
            var group = this.GroupsList.SelectedItem as DatGroup;

            if (group == null)
            {
                return;
            }

            var dialog = new OpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.DefaultExt = ".png";
            dialog.Filter = "Images (*.png, *.bmp, *.jpg)|*.png;*.bmp;*.jpg|PNG files (*.png)|*.png|BMP files (*.bmp)|*.bmp|JPG files (*.jpg)|*.jpg";

            string fileName;

            if (dialog.ShowDialog(this) == true)
            {
                fileName = dialog.FileName;
            }
            else
            {
                return;
            }

            int imageId = group.Images.Count == 0 ? 0 : group.Images.Max(t => t.ImageId) + 1;

            this.RunBusyAction(disp =>
                {
                    try
                    {
                        var image = DatImage.FromFile(group.GroupId, (short)imageId, fileName);

                        group.Images.Add(image);

                        disp(() => this.DatFile = this.DatFile);
                        disp(() => this.ImagesList.SelectedIndex = this.ImagesList.Items.Count - 1);
                    }
                    catch (Exception ex)
                    {
                        disp(() => MessageBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                });
        }

        private void ReplaceImage_Click(object sender, RoutedEventArgs e)
        {
            var image = this.ImagesList.SelectedItem as DatImage;

            if (image == null)
            {
                return;
            }

            var dialog = new OpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.DefaultExt = ".png";
            dialog.Filter = "Images (*.png, *.bmp, *.jpg)|*.png;*.bmp;*.jpg|PNG files (*.png)|*.png|BMP files (*.bmp)|*.bmp|JPG files (*.jpg)|*.jpg";

            string fileName;

            if (dialog.ShowDialog(this) == true)
            {
                fileName = dialog.FileName;
            }
            else
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                try
                {
                    image.ReplaceWithFile(fileName);

                    disp(() => this.DatFile = this.DatFile);
                }
                catch (Exception ex)
                {
                    disp(() => MessageBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });
        }

        private void ConvertImage7_Click(object sender, RoutedEventArgs e)
        {
            var image = this.ImagesList.SelectedItem as DatImage;

            if (image == null)
            {
                return;
            }

            this.RunBusyAction(disp =>
                {
                    image.ConvertToFormat7();

                    disp(() => this.DatFile = this.DatFile);
                });
        }

        private void ConvertImage23_Click(object sender, RoutedEventArgs e)
        {
            var image = this.ImagesList.SelectedItem as DatImage;

            if (image == null)
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                image.ConvertToFormat23();

                disp(() => this.DatFile = this.DatFile);
            });
        }

        private void ConvertImage24_Click(object sender, RoutedEventArgs e)
        {
            var image = this.ImagesList.SelectedItem as DatImage;

            if (image == null)
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                image.ConvertToFormat24();

                disp(() => this.DatFile = this.DatFile);
            });
        }

        private void ConvertImage25_Click(object sender, RoutedEventArgs e)
        {
            var image = this.ImagesList.SelectedItem as DatImage;

            if (image == null)
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                image.ConvertToFormat25();

                disp(() => this.DatFile = this.DatFile);
            });
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            var group = this.GroupsList.SelectedItem as DatGroup;

            if (group == null)
            {
                return;
            }

            int index = this.ImagesList.SelectedIndex;

            if (index == -1)
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                group.Images.RemoveAt(index);

                disp(() => this.ImagesList.SelectedIndex = -1);
                disp(() => this.DatFile = this.DatFile);
            });
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            var image = this.ImagesList.SelectedItem as DatImage;

            if (image == null)
            {
                return;
            }

            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".png";
            dialog.Filter = "Images (*.png, *.bmp)|*.png;*.bmp|PNG files (*.png)|*.png|BMP files (*.bmp)|*.bmp";
            dialog.FileName = image.GroupId + "-" + image.ImageId;

            string fileName;

            if (dialog.ShowDialog(this) == true)
            {
                fileName = dialog.FileName;
            }
            else
            {
                return;
            }

            this.RunBusyAction(disp =>
                {
                    image.Save(fileName);
                });
        }

        private void SetImageColorKey_Click(object sender, RoutedEventArgs e)
        {
            var image = this.ImagesList.SelectedItem as DatImage;

            if (image == null)
            {
                return;
            }

            Color colorKey = this.DatImageColorKey.SelectedColor;

            this.RunBusyAction(disp =>
                {
                    image.MakeColorTransparent(colorKey.R, colorKey.G, colorKey.B);
                    disp(() => this.DatFile = this.DatFile);
                });
        }

        private void SetImageColorKeyRange_Click(object sender, RoutedEventArgs e)
        {
            var image = this.ImagesList.SelectedItem as DatImage;

            if (image == null)
            {
                return;
            }

            Color colorKey0 = this.DatImageColorKey0.SelectedColor;
            Color colorKey1 = this.DatImageColorKey1.SelectedColor;

            this.RunBusyAction(disp =>
            {
                image.MakeColorTransparent(colorKey0.R, colorKey0.G, colorKey0.B, colorKey1.R, colorKey1.G, colorKey1.B);
                disp(() => this.DatFile = this.DatFile);
            });
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var image = (Image)sender;

            if (image == null)
            {
                return;
            }

            var source = image.Source as BitmapSource;

            if (source == null)
            {
                return;
            }

            var position = e.GetPosition(image);

            int x = Math.Max(Math.Min((int)(position.X * source.PixelWidth / image.ActualWidth), source.PixelWidth - 1), 0);
            int y = Math.Max(Math.Min((int)(position.Y * source.PixelHeight / image.ActualHeight), source.PixelHeight - 1), 0);

            byte[] pixel = new byte[4];

            source.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, source.PixelWidth * 4, 0);

            var color = Color.FromRgb(pixel[2], pixel[1], pixel[0]);

            this.DatImageColorKey.SelectedColor = color;
            this.DatImageColorKey0.SelectedColor = color;
            this.DatImageColorKey1.SelectedColor = color;
        }
    }
}
