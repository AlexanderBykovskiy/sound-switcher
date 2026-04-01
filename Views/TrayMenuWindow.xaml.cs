namespace SoundSwitcher.Views;

public partial class TrayMenuWindow : System.Windows.Window
{
    private readonly Action _settingsAction;
    private readonly Action _exitAction;

    public TrayMenuWindow(Action settingsAction, Action exitAction, bool darkMode)
    {
        InitializeComponent();
        _settingsAction = settingsAction;
        _exitAction = exitAction;
        ApplyTheme(darkMode);
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        Close();
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    private void OnSettingsClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            Services.StartupLogger.Info("Tray menu: Settings clicked.");
            _settingsAction();
            Close();
        }
        catch (Exception ex)
        {
            Services.StartupLogger.Error(ex, "Tray menu Settings action failed.");
            System.Windows.MessageBox.Show(
                $"Failed to open Settings.{Environment.NewLine}{ex.Message}",
                "SoundSwitcher",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void OnExitClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            Services.StartupLogger.Info("Tray menu: Exit clicked.");
            _exitAction();
        }
        catch (Exception ex)
        {
            Services.StartupLogger.Error(ex, "Tray menu Exit action failed.");
            System.Windows.MessageBox.Show(
                $"Failed to exit.{Environment.NewLine}{ex.Message}",
                "SoundSwitcher",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void ApplyTheme(bool darkMode)
    {
        if (darkMode)
        {
            var background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 40, 40));
            var foreground = System.Windows.Media.Brushes.WhiteSmoke;
            var border = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(72, 72, 72));
            var hover = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 58, 58));

            MenuBorder.Background = background;
            MenuBorder.BorderBrush = border;

            ApplyButtonTheme(SettingsButton, foreground, background, hover, border);
            ApplyButtonTheme(ExitButton, foreground, background, hover, border);
            return;
        }

        var lightBackground = System.Windows.Media.Brushes.White;
        var lightForeground = System.Windows.Media.Brushes.Black;
        var lightBorder = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(190, 190, 190));
        var lightHover = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(238, 238, 238));

        MenuBorder.Background = lightBackground;
        MenuBorder.BorderBrush = lightBorder;

        ApplyButtonTheme(SettingsButton, lightForeground, lightBackground, lightHover, lightBorder);
        ApplyButtonTheme(ExitButton, lightForeground, lightBackground, lightHover, lightBorder);
    }

    private static void ApplyButtonTheme(
        System.Windows.Controls.Button button,
        System.Windows.Media.Brush foreground,
        System.Windows.Media.Brush background,
        System.Windows.Media.Brush hoverBackground,
        System.Windows.Media.Brush borderBrush)
    {
        button.Foreground = foreground;
        button.Background = background;
        button.BorderBrush = System.Windows.Media.Brushes.Transparent;

        var style = new System.Windows.Style(typeof(System.Windows.Controls.Button));
        style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.BackgroundProperty, background));
        style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.ForegroundProperty, foreground));
        style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.BorderBrushProperty, System.Windows.Media.Brushes.Transparent));
        style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.BorderThicknessProperty, new System.Windows.Thickness(1)));
        style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.TemplateProperty, CreateButtonTemplate(hoverBackground, borderBrush)));
        button.Style = style;
    }

    private static System.Windows.Controls.ControlTemplate CreateButtonTemplate(
        System.Windows.Media.Brush hoverBackground,
        System.Windows.Media.Brush borderBrush)
    {
        var borderFactory = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Border));
        borderFactory.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new System.Windows.CornerRadius(7));
        borderFactory.SetValue(System.Windows.Controls.Border.BorderBrushProperty, borderBrush);
        borderFactory.SetValue(System.Windows.Controls.Border.BorderThicknessProperty, new System.Windows.Thickness(0));
        borderFactory.SetBinding(System.Windows.Controls.Border.BackgroundProperty, new System.Windows.Data.Binding("Background")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
        });

        var contentFactory = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.ContentPresenter));
        contentFactory.SetValue(System.Windows.Controls.ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Stretch);
        contentFactory.SetValue(System.Windows.Controls.ContentPresenter.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
        contentFactory.SetBinding(System.Windows.Controls.ContentPresenter.ContentProperty, new System.Windows.Data.Binding("Content")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
        });
        contentFactory.SetBinding(System.Windows.Controls.ContentPresenter.MarginProperty, new System.Windows.Data.Binding("Padding")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
        });
        borderFactory.AppendChild(contentFactory);

        var template = new System.Windows.Controls.ControlTemplate(typeof(System.Windows.Controls.Button))
        {
            VisualTree = borderFactory
        };

        var hoverTrigger = new System.Windows.Trigger { Property = System.Windows.UIElement.IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.BackgroundProperty, hoverBackground));
        template.Triggers.Add(hoverTrigger);

        return template;
    }
}
