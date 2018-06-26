namespace KartaMobiExporter
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += (sender, args) => DataContext
                = new MainWindowViewModel();
        }
    }
}