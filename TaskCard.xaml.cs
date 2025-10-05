using System.Security.Cryptography;
using System.Windows.Controls;

namespace Task_manager
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
        public partial class TaskCard : UserControl
        {
             public TaskCard()
                 {
                 InitializeComponent();
                 }

        private string _title;
        public bool IsEditing { get; set; } = false;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                TitleBlock.Text = value; // update UI
            }
        }

        private string _contentText;
        public string ContentText
        {
            get => _contentText;
            set
            {
                _contentText = value;
                ContentBlock.Text = value; // update UI
            }
        }
        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                IDBlock.Text = $"Task Id: {_id}"; // automatically update UI
            }
        }

        // Expose Buttons so caller can attach events
        public Button Edit => EditButton;
                 public Button Delete => DeleteButton;
        }
}
