using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpAnalyzerTool.Helper;
using SpAnalyzerTool.Models;
using SpAnalyzerTool.View.UserControl;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using userControl = System.Windows.Controls.UserControl;

namespace SpAnalyzerTool.ViewModel
{
    public partial class DatabaseAnalyzerViewModel : ObservableObject
    {
       
        public Action<userControl>? ShowOverlayAction { get; set; }
        public Action? AnalyzeProjectAction { get; set; }


        private readonly AppSettings? settings;

        [ObservableProperty]
        private ObservableCollection<ProcedureUsageInfo>? _obProject = new();

        [ObservableProperty]
        public ProcedureUsageInfo? selectedProcedure ;

        [ObservableProperty]
        public string? projectPath;

        [ObservableProperty]
        public int totalCount;

        [ObservableProperty]
        public int usedCount;
        [ObservableProperty]
        public int unusedCount;


        public DatabaseAnalyzerViewModel(AppSettings settings)
        {
            this.settings = settings;
           
        }
        public DatabaseAnalyzerViewModel()
        {
          

        }



        [RelayCommand]
        public void AddNewProceduerEditor()
        {
           
            var editor = new UC_ProcedureEditor(settings?.DefaultConnectionString);
               

            editor.ExitRequested += (_, _) => ShowOverlayAction?.Invoke(null);

            if (!string.IsNullOrEmpty(ProjectPath))
                editor.RefreshData += (_, _) => AnalyzeProjectAction?.Invoke();

            ShowOverlayAction?.Invoke(editor);
        }


        [RelayCommand]
        public void EditProcedureEditor()
        {
            if (!clsDatabaseHelper.TryValidateConnection(settings?.DefaultConnectionString??string.Empty) ||
                string.IsNullOrEmpty(settings?.DefaultConnectionString))
            {
                MessageBox.Show("يرجى إدخال جملة الاتصال بقاعدة البيانات.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var procedureName = SelectedProcedure?.Procedure;

            if (procedureName == null)
                return;

            var editor  =new UC_ProcedureEditor(settings.DefaultConnectionString, procedureName);

            editor.ExitRequested += (_, _) => ShowOverlayAction?.Invoke(null);

            if (!string.IsNullOrEmpty(ProjectPath))
                editor.RefreshData += (_, _) => AnalyzeProjectAction?.Invoke();

            ShowOverlayAction?.Invoke(editor);
        }

        //تصدير
        [RelayCommand]
        private void ExportResults()
        {
            if (ObProject == null || ObProject.Count == 0)
            {
                MessageBox.Show("لا توجد نتائج لتصديرها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                FileName = "Backup_SP_Results.txt"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var lines = new List<string>
        {
            "Procedure\t\t\t\tLocations",
            "_____________________________________________________________"
        };

                foreach (var info in ObProject)
                {
                    string line = $"{info.Procedure}\t\t\t\t{info.Locations}";
                    lines.Add(line);
                }

                File.WriteAllLines(dlg.FileName, lines, Encoding.UTF8);
                MessageBox.Show("✅ تم تصدير النتائج بنجاح.", "تصدير", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void OpenLocationInEditor(LocationInfo? location)
        {
            if (location == null)
                return;

            try
            {
                if (!File.Exists(location.FullPath))
                {
                    MessageBox.Show("⚠️ الملف غير موجود: " + location.FullPath);
                    return;
                }

                string filePath = location.FullPath;
                string? visualStudioPath = clsVisualStudioPath.GetFirstAvailableEditor();

                if (!string.IsNullOrEmpty(visualStudioPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = visualStudioPath,
                        Arguments = $"\"{filePath}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "notepad.exe",
                        Arguments = $"\"{filePath}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في فتح الملف: " + ex.Message);
            }
        }


    
        public void ToggleSelectAll()
        {
            if (ObProject == null || ObProject.Count == 0)
                return;

            bool allChecked = ObProject.All(p => p.IsSelectedForDelete);
            foreach (var proc in ObProject)
                proc.IsSelectedForDelete = !allChecked;
        }



    }

}
