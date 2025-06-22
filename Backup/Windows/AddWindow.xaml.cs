using Backup.Database;
using System.Windows;

namespace Backup.Windows
{
    public partial class AddWindow : Window
    {
        private readonly FilesDatabase db = new(); //数据库
        private readonly string style; //窗口类型

        public AddWindow(string style)
        {
            InitializeComponent(); //初始化界面
            this.style = style; //设置窗口类型
        }

        // 加载窗口数据
        private void AddWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (style == "File")
            {
                this.Title = "添加要备份的文件"; //添加要备份的文件
                DescriptionLabel.Content = "源文件地址"; //源文件地址
            }
            else if (style == "Folder")
            {
                this.Title = "添加要备份的文件夹"; //添加要备份的文件夹
                DescriptionLabel.Content = "源文件夹地址"; //源文件夹地址
            }
            else
            {
                this.Title = "添加备份信息"; //添加备份信息
                var backupData = db.GetFileData(int.Parse(style)); //获取备份信息
                DescriptionLabel.Content = backupData.Style == "File" ? "源文件地址" : "源文件夹地址"; //备份文件类型
                TitleTextBox.Text = backupData.FileName; //备份文件名
                SourceLocationTextBox.Text = backupData.SourcePath; //源文件或文件夹地址
                TargetLocationTextBox.Text = backupData.TargetPath; //目标文件夹地址
                DeleteTargetFolderCheckBox.IsChecked = backupData.CleanTargetFloder; //是否删除目标文件夹
            }
        }

        // 关闭窗口
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(); // 关闭窗口
        }

        // 判断输入是否完整
        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            SaveButton.IsEnabled = !string.IsNullOrEmpty(SourceLocationTextBox.Text) &&
                                  !string.IsNullOrEmpty(TargetLocationTextBox.Text) &&
                                  !string.IsNullOrEmpty(TitleTextBox.Text); //判断输入是否完整
        }

        // 添加或更新文件信息
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            bool deleteSource = DeleteTargetFolderCheckBox.IsChecked ?? false; //是否删除源文件夹
            if (style == "File" || style == "Folder")
            {
                db.AddFileData(SourceLocationTextBox.Text, TargetLocationTextBox.Text, TitleTextBox.Text, style, deleteSource); //添加文件信息
            }
            else
            {
                var backupData = db.GetFileData(int.Parse(style)); //获取备份信息
                db.UpdateFileData(int.Parse(style), SourceLocationTextBox.Text, TargetLocationTextBox.Text, TitleTextBox.Text, backupData.Style, deleteSource); //更新备份信息
            }

            MainWindow mainWindow = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault(); //获取主窗口
            mainWindow?.Refresh(); //刷新主窗口
            Close(); //关闭窗口
        }

        // 选择源文件或文件夹
        private void SourceLocationButton_Click(object sender, RoutedEventArgs e)
        {
            if (style == "File")
            {
                SelectFiles();
            }
            else if (style == "Folder")
            {
                SelectFolder();
            }
            else
            {
                var backupData = db.GetFileData(int.Parse(style));
                if (backupData.Style == "File")
                {
                    SelectFiles();
                }
                else if (backupData.Style == "Folder")
                {
                    SelectFolder();
                }
            }
        }

        // 选择源文件
        private void SelectFiles()
        {
            var fileDialog = new OpenFileDialog { Multiselect = true };
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach (string path in fileDialog.FileNames)
                {
                    SourceLocationTextBox.Text += path + "\n";
                }
            }
        }

        // 选择源文件夹
        private void SelectFolder()
        {
            var folderDialog = new FolderBrowserDialog { ShowNewFolderButton = true };
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SourceLocationTextBox.Text += folderDialog.SelectedPath + "\n";
            }
        }

        // 选择目标文件夹
        private void TargetLocationSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new FolderBrowserDialog { ShowNewFolderButton = true }; //创建文件夹对话框
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TargetLocationTextBox.Text = folderDialog.SelectedPath; //选择文件夹路径
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e); //先关闭窗口
            GC.Collect(); //释放资源
        }
    }
}