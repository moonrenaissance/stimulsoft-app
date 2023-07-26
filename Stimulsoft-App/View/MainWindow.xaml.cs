using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Net.Http;
using Stimulsoft.Report;
using Stimulsoft.Base.Json;
using System.Data;
using Stimulsoft.Report.Export;
using System.Threading.Tasks;
using System.Net;

namespace Stimulsoft_App
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private async void startProgram(object sender, RoutedEventArgs e)
        {

            try
            {
                string json = "";

                if (chkDB.IsChecked == true)
                {
                    string notes = await returnJson("https://localhost:7072/api/Notes");
                    notes = notes.Substring(0, notes.Length - 2);

                    string reminders = await returnJson("https://localhost:7072/api/Notes/Reminders");
                    reminders = reminders.Substring(1);

                    json = $"{{ Notes: {notes}}}, {reminders},";
                    string tags = await returnJson("https://localhost:7072/api/Tags");
                    json += " Tags: " + tags + "}";
                }
                else
                {
                    if (fileText.Text == "") 
                    {
                        MessageBox.Show("Выберите файл");                    
                        return;
                    }
                    json = File.ReadAllText(fileText.Text);
                }

                // Создаем отчет
                StiReport report = new StiReport();

                //Достаём mrt       
                report.Load(Properties.Resources.PerfectMRT4);
                dynamic data = JsonConvert.DeserializeObject(json);


                report.Dictionary.Databases.Clear();

                DataSet dataSet = new DataSet();

                //Атрибуты таблиц
                DataTable notesTable = new DataTable("Notes");
                dataSet.Tables.Add(notesTable);
                notesTable.Columns.Add("id", typeof(string));
                notesTable.Columns.Add("title", typeof(string));
                notesTable.Columns.Add("description", typeof(string));
                notesTable.Columns.Add("date", typeof(string));
                notesTable.Columns.Add("dateOfCreation", typeof(string));
   
                DataTable tagsTable = new DataTable("Tags");
                dataSet.Tables.Add(tagsTable);
                tagsTable.Columns.Add("id", typeof(string));
                tagsTable.Columns.Add("title", typeof(string));
                tagsTable.Columns.Add("color", typeof(string));

                DataTable notesTagsTable = new DataTable("Notes_notesTags");
                dataSet.Tables.Add(notesTagsTable);
                notesTagsTable.Columns.Add("noteId", typeof(string));
                notesTagsTable.Columns.Add("tagId", typeof(string));

                
                dynamic jsonObject = JsonConvert.DeserializeObject(json);

                //Заполняем notes
                if(jsonObject.Notes == null) 
                {
                    throw new Exception("Ваш json файл не содержит заметок");
                }

                foreach (dynamic note in jsonObject.Notes)
                {
                    DataRow noteRow = notesTable.NewRow();
                    noteRow["id"] = note.id;
                    noteRow["title"] = note.title;
                    noteRow["description"] = note.description;
                    noteRow["date"] = note.date;
                    noteRow["dateOfCreation"] = note.dateOfCreation;
                    notesTable.Rows.Add(noteRow);

                    foreach (dynamic noteTag in note.notesTags)
                    {
                        DataRow noteTagRow = notesTagsTable.NewRow();
                        noteTagRow["noteId"] = note.id;
                        noteTagRow["tagId"] = noteTag.tagId;
                        notesTagsTable.Rows.Add(noteTagRow);
                    }
                }

                //Заполняем tags
                if(jsonObject.Tags != null)
                {
                    foreach (dynamic tag in jsonObject.Tags)
                    {
                        DataRow tagRow = tagsTable.NewRow();
                        tagRow["id"] = tag.id;
                        tagRow["title"] = tag.title;
                        tagRow["color"] = tag.color;
                        tagsTable.Rows.Add(tagRow);
                    }
                }


                report.RegData("Demo", "Demo", dataSet);
                report.Render();

                StiPdfExportService pdfExportService = new StiPdfExportService();
                pdfExportService.ExportPdf(report, "Report.pdf");

                System.Diagnostics.Process.Start("Report.pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка");
            }

        }

        private void chooseFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Multiselect = false;
            openFileDialog.DefaultExt = ".json";
            openFileDialog.Filter = "JPEG Files (*.json)|*.json";


            //Вызываем метод, позволяющий открыть файл через диалоговое окно
            Nullable<bool> result = openFileDialog.ShowDialog();


            //Достаём файл и выводим на дисплей 
            if (result == true)
            {
                //Открываем документ
                string filename = openFileDialog.FileName;
                fileText.Text = filename;
            }
        }

        private async Task<string> returnJson(string path)
        {
            string json = "";
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            HttpClient client = new HttpClient();
            string url = path;
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                json = await response.Content.ReadAsStringAsync();
            }
            return json;
        }
    }
}
