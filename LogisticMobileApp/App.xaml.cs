namespace LogisticMobileApp
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            Services = serviceProvider;

            // Загружаем сохранённую тему
            var savedTheme = Preferences.Get("AppTheme", "Dark");
            UserAppTheme = savedTheme == "Light" ? AppTheme.Light : AppTheme.Dark;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}