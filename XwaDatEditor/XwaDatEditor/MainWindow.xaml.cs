using System;
using System.Globalization;
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

        private void UpdateDatFile()
        {
            var groupIndex = this.GroupsList.SelectedIndex;
            var images = this.ImagesList.SelectedItems.Cast<DatImage>().ToList();

            this.DatFile = this.DatFile;

            this.GroupsList.SelectedIndex = groupIndex;
            this.ImagesList.SelectedItems.Clear();

            foreach (DatImage image in images)
            {
                this.ImagesList.SelectedItems.Add(image);
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

        private void UpGroup_Click(object sender, RoutedEventArgs e)
        {
            var dat = this.DatFile;

            if (dat == null)
            {
                return;
            }

            int groupIndex = this.GroupsList.SelectedIndex;

            if (groupIndex == -1 || groupIndex == 0)
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                var group = dat.Groups[groupIndex];

                dat.Groups.RemoveAt(groupIndex);
                dat.Groups.Insert(groupIndex - 1, group);

                disp(() => this.GroupsList.SelectedIndex = -1);
                disp(() => this.DatFile = dat);
                disp(() => this.GroupsList.SelectedItem = group);
            });
        }

        private void DownGroup_Click(object sender, RoutedEventArgs e)
        {
            var dat = this.DatFile;

            if (dat == null)
            {
                return;
            }

            int groupIndex = this.GroupsList.SelectedIndex;

            if (groupIndex == -1 || groupIndex == dat.Groups.Count - 1)
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                var group = dat.Groups[groupIndex];

                dat.Groups.RemoveAt(groupIndex);
                dat.Groups.Insert(groupIndex + 1, group);

                disp(() => this.GroupsList.SelectedIndex = -1);
                disp(() => this.DatFile = dat);
                disp(() => this.GroupsList.SelectedItem = group);
            });
        }

        private void SortGroup_Click(object sender, RoutedEventArgs e)
        {
            var dat = this.DatFile;

            if (dat == null)
            {
                return;
            }

            var groups = this.GroupsList.SelectedItems.Cast<DatGroup>().ToList();

            this.RunBusyAction(disp =>
            {
                var orderedGroups = dat.Groups
                    .OrderBy(t => t.GroupId)
                    .ToList();

                dat.Groups.Clear();

                foreach (var group in orderedGroups)
                {
                    dat.Groups.Add(group);
                }

                disp(() => this.GroupsList.SelectedIndex = -1);
                disp(() => this.DatFile = dat);
            });
        }

        private void ConvertAllImage7_Click(object sender, RoutedEventArgs e)
        {
            DatFile dat = this.DatFile;

            this.RunBusyAction(disp =>
            {
                dat.ConvertToFormat7();

                disp(() => this.UpdateDatFile());
            });
        }

        private void ConvertAllImage23_Click(object sender, RoutedEventArgs e)
        {
            DatFile dat = this.DatFile;

            this.RunBusyAction(disp =>
            {
                dat.ConvertToFormat23();

                disp(() => this.UpdateDatFile());
            });
        }

        private void ConvertAllImage24_Click(object sender, RoutedEventArgs e)
        {
            DatFile dat = this.DatFile;

            this.RunBusyAction(disp =>
            {
                dat.ConvertToFormat24();

                disp(() => this.UpdateDatFile());
            });
        }

        private void ConvertAllImage25_Click(object sender, RoutedEventArgs e)
        {
            DatFile dat = this.DatFile;

            this.RunBusyAction(disp =>
            {
                dat.ConvertToFormat25();

                disp(() => this.UpdateDatFile());
            });
        }

        private void ConvertAllImage25C_Click(object sender, RoutedEventArgs e)
        {
            DatFile dat = this.DatFile;

            this.RunBusyAction(disp =>
            {
                dat.ConvertToFormat25Compressed();

                disp(() => this.UpdateDatFile());
            });
        }

        private void DeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            var dat = this.DatFile;

            if (dat == null)
            {
                return;
            }

            if (this.GroupsList.SelectedIndex == -1)
            {
                return;
            }

            var items = this.GroupsList.SelectedItems;

            this.RunBusyAction(disp =>
                {
                    foreach (DatGroup item in items)
                    {
                        dat.Groups.Remove(item);
                    }

                    disp(() => this.GroupsList.SelectedIndex = -1);
                    disp(() => this.DatFile = dat);
                });
        }

        private void SaveGroupDat_Click(object sender, RoutedEventArgs e)
        {
            if (this.GroupsList.SelectedIndex == -1)
            {
                return;
            }

            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".dat";
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

            var groups = this.GroupsList.SelectedItems;

            this.RunBusyAction(disp =>
            {
                var dat = new DatFile();

                foreach (DatGroup group in groups)
                {
                    var datGroup = new DatGroup(group.GroupId);
                    dat.Groups.Add(datGroup);

                    foreach (DatImage image in group.Images)
                    {
                        DatImage datImage = DatImage.FromMemory(
                            image.GroupId,
                            image.ImageId,
                            image.Format,
                            image.Width,
                            image.Height,
                            image.ColorsCount,
                            image.GetRawData());

                        datGroup.Images.Add(datImage);
                    }
                }

                dat.Save(fileName);
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
            dialog.Multiselect = true;

            if (dialog.ShowDialog(this) == false)
            {
                return;
            }

            int imageId = group.Images.Count == 0 ? 0 : group.Images.Max(t => t.ImageId) + 1;

            this.RunBusyAction(disp =>
                {
                    try
                    {
                        var images = Enumerable.Range(0, dialog.FileNames.Length)
                        .Select(i => Tuple.Create(i, dialog.FileNames[i]))
                        .AsParallel()
                        .AsOrdered()
                        .Select(t => DatImage.FromFile(group.GroupId, (short)(imageId + t.Item1), t.Item2))
                        .AsSequential();

                        foreach (var image in images)
                        {
                            group.Images.Add(image);
                        }

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
            if (this.ImagesList.SelectedIndex == -1)
            {
                return;
            }

            var images = this.ImagesList.SelectedItems;

            this.RunBusyAction(disp =>
                {
                    images
                    .Cast<DatImage>()
                    .AsParallel()
                    .ForAll(image => image.ConvertToFormat7());

                    disp(() => this.UpdateDatFile());
                });
        }

        private void ConvertImage23_Click(object sender, RoutedEventArgs e)
        {
            if (this.ImagesList.SelectedIndex == -1)
            {
                return;
            }

            var images = this.ImagesList.SelectedItems;

            this.RunBusyAction(disp =>
            {
                images
                .Cast<DatImage>()
                .AsParallel()
                .ForAll(image => image.ConvertToFormat23());

                disp(() => this.UpdateDatFile());
            });
        }

        private void ConvertImage24_Click(object sender, RoutedEventArgs e)
        {
            if (this.ImagesList.SelectedIndex == -1)
            {
                return;
            }

            var images = this.ImagesList.SelectedItems;

            this.RunBusyAction(disp =>
            {
                images
                .Cast<DatImage>()
                .AsParallel()
                .ForAll(image => image.ConvertToFormat24());

                disp(() => this.UpdateDatFile());
            });
        }

        private void ConvertImage25_Click(object sender, RoutedEventArgs e)
        {
            if (this.ImagesList.SelectedIndex == -1)
            {
                return;
            }

            var images = this.ImagesList.SelectedItems;

            this.RunBusyAction(disp =>
            {
                images
                .Cast<DatImage>()
                .AsParallel()
                .ForAll(image => image.ConvertToFormat25());

                disp(() => this.UpdateDatFile());
            });
        }

        private void ConvertImage25C_Click(object sender, RoutedEventArgs e)
        {
            if (this.ImagesList.SelectedIndex == -1)
            {
                return;
            }

            var images = this.ImagesList.SelectedItems;

            this.RunBusyAction(disp =>
            {
                images
                .Cast<DatImage>()
                .AsParallel()
                .ForAll(image => image.ConvertToFormat25Compressed());

                disp(() => this.UpdateDatFile());
            });
        }

        private void UpImage_Click(object sender, RoutedEventArgs e)
        {
            var dat = this.DatFile;

            if (dat == null)
            {
                return;
            }

            var group = this.GroupsList.SelectedItem as DatGroup;

            if (group == null)
            {
                return;
            }

            int imageIndex = this.ImagesList.SelectedIndex;

            if (imageIndex == -1 || imageIndex == 0)
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                var image = group.Images[imageIndex];

                group.Images.RemoveAt(imageIndex);
                group.Images.Insert(imageIndex - 1, image);

                disp(() => this.ImagesList.SelectedIndex = -1);
                disp(() => this.DatFile = dat);
                disp(() => this.ImagesList.SelectedItem = image);
            });
        }

        private void DownImage_Click(object sender, RoutedEventArgs e)
        {
            var dat = this.DatFile;

            if (dat == null)
            {
                return;
            }

            var group = this.GroupsList.SelectedItem as DatGroup;

            if (group == null)
            {
                return;
            }

            int imageIndex = this.ImagesList.SelectedIndex;

            if (imageIndex == -1 || imageIndex == group.Images.Count - 1)
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                var image = group.Images[imageIndex];
                group.Images.RemoveAt(imageIndex);
                group.Images.Insert(imageIndex + 1, image);

                disp(() => this.ImagesList.SelectedIndex = -1);
                disp(() => this.DatFile = dat);
                disp(() => this.ImagesList.SelectedItem = image);
            });
        }

        private void SortImage_Click(object sender, RoutedEventArgs e)
        {
            var dat = this.DatFile;

            if (dat == null)
            {
                return;
            }

            var group = this.GroupsList.SelectedItem as DatGroup;

            if (group == null)
            {
                return;
            }

            this.RunBusyAction(disp =>
            {
                var orderedImages = group.Images
                    .OrderBy(t => t.GroupId)
                    .ThenBy(t => t.ImageId)
                    .ToList();

                group.Images.Clear();

                foreach (var image in orderedImages)
                {
                    group.Images.Add(image);
                }

                disp(() => this.ImagesList.SelectedIndex = -1);
                disp(() => this.DatFile = dat);
            });
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            var group = this.GroupsList.SelectedItem as DatGroup;

            if (group == null)
            {
                return;
            }

            if (this.ImagesList.SelectedIndex == -1)
            {
                return;
            }

            var items = this.ImagesList.SelectedItems;

            this.RunBusyAction(disp =>
            {
                foreach (DatImage item in items)
                {
                    group.Images.Remove(item);
                }

                disp(() => this.ImagesList.SelectedIndex = -1);
                disp(() => this.DatFile = this.DatFile);
            });
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            var images = this.ImagesList.SelectedItems;

            if (images.Count == 0)
            {
                return;
            }

            DatImage image = images[0] as DatImage;
            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".png";
            dialog.Filter = "Images (*.png, *.bmp)|*.png;*.bmp|PNG files (*.png)|*.png|BMP files (*.bmp)|*.bmp";
            dialog.FileName = image.GroupId + "-" + image.ImageId;

            string fileName, directory, extension;

            if (dialog.ShowDialog(this) == true)
            {
                fileName = dialog.FileName;
                directory = fileName.Substring(0, fileName.LastIndexOf('\\'));
                extension = fileName.Substring(fileName.LastIndexOf('.'));
            }
            else
            {
                return;
            }

            this.RunBusyAction(disp =>
                {
                    foreach (DatImage img in images)
                    {
                        img.Save(directory + '\\' + img.GroupId + "-" + img.ImageId + extension);
                    }
                });
        }

        private void SaveImageDat_Click(object sender, RoutedEventArgs e)
        {
            if (this.GroupsList.SelectedIndex == -1 || this.ImagesList.SelectedIndex == -1)
            {
                return;
            }

            var group = (DatGroup)this.GroupsList.SelectedItem;

            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".dat";
            dialog.Filter = "DAT files (*.dat)|*.dat";
            dialog.FileName = group.GroupId.ToString();

            string fileName;

            if (dialog.ShowDialog(this) == true)
            {
                fileName = dialog.FileName;
            }
            else
            {
                return;
            }

            var images = this.ImagesList.SelectedItems;

            this.RunBusyAction(disp =>
            {
                var dat = new DatFile();
                var datGroup = new DatGroup(group.GroupId);
                dat.Groups.Add(datGroup);

                foreach (DatImage image in images)
                {
                    DatImage datImage = DatImage.FromMemory(
                        image.GroupId,
                        image.ImageId,
                        image.Format,
                        image.Width,
                        image.Height,
                        image.ColorsCount,
                        image.GetRawData());

                    datGroup.Images.Add(datImage);
                }

                dat.Save(fileName);
            });
        }

        private void SetImageColorKey_Click(object sender, RoutedEventArgs e)
        {
            var image = this.ImagesList.SelectedItem as DatImage;

            if (image == null)
            {
                return;
            }

            if (this.DatImageColorKey.SelectedColor is null)
            {
                return;
            }

            Color colorKey = this.DatImageColorKey.SelectedColor.Value;

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

            if (this.DatImageColorKey0.SelectedColor is null || this.DatImageColorKey1.SelectedColor is null)
            {
                return;
            }

            Color colorKey0 = this.DatImageColorKey0.SelectedColor.Value;
            Color colorKey1 = this.DatImageColorKey1.SelectedColor.Value;

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

        private void GroupId_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = (TextBox)sender;

            if (!short.TryParse(textbox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out short groupId))
            {
                return;
            }

            foreach (DatImage image in this.ImagesList.SelectedItems)
            {
                image.GroupId = groupId;
            }

            var selectedItems = this.ImagesList.SelectedItems.Cast<DatImage>().ToList();
            this.ImagesList.Items.Refresh();
            selectedItems.ForEach(t => this.ImagesList.SelectedItems.Add(t));
        }

        private void OffsetX_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = (TextBox)sender;

            if (!int.TryParse(textbox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int offsetX))
            {
                return;
            }

            foreach (DatImage image in this.ImagesList.SelectedItems)
            {
                image.OffsetX = offsetX;
            }

            var selectedItems = this.ImagesList.SelectedItems.Cast<DatImage>().ToList();
            this.ImagesList.Items.Refresh();
            selectedItems.ForEach(t => this.ImagesList.SelectedItems.Add(t));
        }

        private void OffsetY_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = (TextBox)sender;

            if (!int.TryParse(textbox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int offsetY))
            {
                return;
            }

            foreach (DatImage image in this.ImagesList.SelectedItems)
            {
                image.OffsetY = offsetY;
            }

            var selectedItems = this.ImagesList.SelectedItems.Cast<DatImage>().ToList();
            this.ImagesList.Items.Refresh();
            selectedItems.ForEach(t => this.ImagesList.SelectedItems.Add(t));
        }
    }
}
