using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace Task_manager
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        List<TaskCard> tasks = new List<TaskCard>();
        private static DispatcherTimer GetTimer = new DispatcherTimer();
        private bool isFetching = false;
        Dictionary<int, int> taskVersions = new();
        public MainWindow()
        {

            client.BaseAddress = new Uri("https://localhost:7116/api/");
            InitializeComponent();
            GetTimer.Interval = TimeSpan.FromMilliseconds(750);
            GetTimer.Tick += GetOtherTasks;
            GetTimer.Start();

        }
        private async void GetOtherTasks(object? sender, EventArgs e)
        {
            if (isFetching) return;
            isFetching = true;

            try
            {
                // Send client versions to server
                var query = string.Join("&", taskVersions.Select(kv => $"clientVersions[{kv.Key}]={kv.Value}"));
                var url = string.IsNullOrEmpty(query) ? "TaskCard" : $"TaskCard?{query}";

                var response = await client.GetFromJsonAsync<List<TaskDto>>(url);
                if (response != null && response.Count > 0)
                {
                    foreach (var task in response)
                    {
                        var existing = tasks.FirstOrDefault(t => t.Id == task.Id);
                        if (existing != null)
                        {
                            // update if version changed
                            if (!taskVersions.ContainsKey(task.Id) || taskVersions[task.Id] < task.Version)
                            {
                                existing.Title = task.Title;
                                existing.ContentText = task.Description;
                                taskVersions[task.Id] = task.Version;
                            }
                        }
                        else
                        {
                            // new task
                            var taskCard = new TaskCard
                            {
                                Id = task.Id,
                                Title = task.Title,
                                ContentText = task.Description
                            };
                            taskCard.Edit.Click += EditCardNotYours;
                            taskCard.Delete.Click += DeleteCard;
                            tasks.Add(taskCard);
                            TaskStackPanel.Children.Add(taskCard);
                            taskVersions[task.Id] = task.Version;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting tasks: {ex.Message}");
            }
            finally
            {
                isFetching = false;
            }
        }

        private async void CreateTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var newTaskDto = new TaskDto
            {
                Title = titleTextBox.Text,
                Description = contentTextBox.Text,
            };

            var newTask = new TaskCard
            {
                Title = newTaskDto.Title,
                ContentText = newTaskDto.Description
            };

            newTask.Edit.Click += EditCard;
            newTask.Delete.Click += DeleteCard;

            try
            {
                var response = await client.PostAsJsonAsync("TaskCard", newTaskDto);
                if (response.IsSuccessStatusCode)
                {
                    // deserialize the ID from json
                    var created = await response.Content.ReadFromJsonAsync<CreatedTaskResponse>();
                    newTask.Id = created.Id;
                    InsertTaskInOrder(newTask);
                    RefreshTaskStackPanel();
                }
                else
                {
                    MessageBox.Show(
                        $"Couldn't create task.\nReason: {response.ReasonPhrase}\nStatus: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating task: {ex.Message}");
            }
        }
        private void InsertTaskInOrder(TaskCard newTask)
        {
            // Filter list
            int insertIndex = tasks.FindIndex(t => t.Id > newTask.Id);

            if (insertIndex == -1)
            {
                // If no larger ID found add to the end
                tasks.Add(newTask);
            }
            else
            {
                // insert before larger id
                tasks.Insert(insertIndex, newTask);
            }
        }
        private void RefreshTaskStackPanel()
        {
            // add in correct order
            TaskStackPanel.Children.Clear();
            foreach (var task in tasks)
            {
                TaskStackPanel.Children.Add(task);
            }
        }
        private void DeleteCard(object sender, RoutedEventArgs e)
        {

        }
        private async void EditCard(object sender, RoutedEventArgs e)
        {
            DependencyObject parent = (Button)sender;

            while (parent != null && parent is not TaskCard)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            if (parent is TaskCard taskCard)
            {
                if (!taskCard.IsEditing)
                {
                    taskCard.TitleEditBox.Text = taskCard.Title;
                    taskCard.ContentEditBox.Text = taskCard.ContentText;

                    taskCard.TitleBlock.Visibility = Visibility.Collapsed;
                    taskCard.ContentBlock.Visibility = Visibility.Collapsed;
                    taskCard.TitleEditBox.Visibility = Visibility.Visible;
                    taskCard.ContentEditBox.Visibility = Visibility.Visible;
                    taskCard.EditButton.Content = "Save";
                    taskCard.Edit.Background = Brushes.DeepSkyBlue;

                }
                else
                {
                    taskCard.TitleBlock.Text = taskCard.TitleEditBox.Text;
                    taskCard.ContentBlock.Text = taskCard.ContentEditBox.Text;

                    taskCard.TitleEditBox.Visibility = Visibility.Collapsed;
                    taskCard.ContentEditBox.Visibility = Visibility.Collapsed;
                    taskCard.TitleBlock.Visibility = Visibility.Visible;
                    taskCard.ContentBlock.Visibility = Visibility.Visible;
                    taskCard.EditButton.Content = "Edit";
                    taskCard.Edit.Background = Brushes.Chartreuse;

                    var PatchContent = new
                    {
                        title = taskCard.TitleBlock.Text,
                        description = taskCard.ContentBlock.Text
                    };
                    // disable button while awaiting patch to stop overlapping requests
                    taskCard.Edit.IsEnabled = false;
                    var response = await client.PatchAsJsonAsync($"TaskCard/{taskCard.Id}", PatchContent);
                    taskCard.Edit.IsEnabled = true;
                    if (response.IsSuccessStatusCode)
                    {
                        var updatedTask = await response.Content.ReadFromJsonAsync<TaskDto>();
                        if (updatedTask != null)
                        {
                            // Update the in-memory task
                            var existing = tasks.FirstOrDefault(t => t.Id == updatedTask.Id);
                            if (existing != null)
                            {
                                existing.Title = updatedTask.Title;
                                existing.ContentText = updatedTask.Description;
                            }

                            // Update the visual UI
                            taskCard.TitleBlock.Text = updatedTask.Title;
                            taskCard.ContentBlock.Text = updatedTask.Description;
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Failed to patch task: {response.StatusCode}");
                    }
                }
                taskCard.IsEditing = !taskCard.IsEditing;
            }

        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshTaskStackPanel();
        }
        private void EditCardNotYours(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not Your task! Ask other client to edit it or make it editable *COMING SOON*");
        }
    }

}