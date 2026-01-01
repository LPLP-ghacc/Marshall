using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using MarshallApp.Models;

namespace MarshallApp.Services;

public static class SettingsManager
{
    private const string SettingsPath = "settings.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    
    public static void Save(UserSettings config)
    {
        var json = JsonSerializer.Serialize(config, Options);
        File.WriteAllText(SettingsPath, json);
    }

    public static async Task<UserSettings> Load()
    {
        if (!File.Exists(SettingsPath))
        {
            "No UserSettings file, loading Default settings...".Log();
            return UserSettings.Default;
        }
        
        try
        {
            var json = await File.ReadAllTextAsync(SettingsPath);
            return JsonSerializer.Deserialize<UserSettings>(json) ?? UserSettings.Default;
        }
        catch (Exception ex)
        {
            ex.Message.Log();
            return UserSettings.Default;
        }
    }

    // ReSharper disable once InconsistentNaming
    public static void GenerateUI(ScrollViewer scrollViewer, UserSettings settings)
    {
        var props = typeof(UserSettings).GetProperties();
        var stack = new StackPanel();
        Grid.SetIsSharedSizeScope(stack, true);
        scrollViewer.Content = stack;
    
        var textBoxStyle = (Style)Application.Current.Resources["FlatTextBoxStyle"]!;
        var textBlockStyle = (Style)Application.Current.Resources["FlatTextBlockStyle"]!;
        var comboBoxStyle = (Style)Application.Current.Resources["FlatComboBoxStyle"]!;
        var checkBoxStyle = (Style)Application.Current.Resources["FlatCheckBoxStyle"]!;
    
        foreach (var prop in props)
        {
            var displayName = prop.GetCustomAttributes(typeof(DisplayNameAttribute), false)
                .Cast<DisplayNameAttribute>()
                .FirstOrDefault()?.Name ?? prop.Name + ":";
            
            var grid = new Grid { Margin = new Thickness(10, 8, 10, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "Labels" });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    
            var label = new TextBlock
            {
                Text = displayName,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Style = textBlockStyle
            };
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);
    
            FrameworkElement? control = prop.PropertyType switch
            {
                { } t when t == typeof(bool) => new CheckBox
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Style = checkBoxStyle
                }.WithBinding(ToggleButton.IsCheckedProperty, new Binding(prop.Name) { Source = settings, Mode = BindingMode.TwoWay }),
    
                { } t when t == typeof(double) => new TextBox
                {
                    Style = textBoxStyle,
                    Height = 30,
                    Margin = new Thickness(5),
                    VerticalContentAlignment = VerticalAlignment.Center
                }.WithBinding(TextBox.TextProperty, new Binding(prop.Name)
                {
                    Source = settings,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                }),
    
                { } t when t == typeof(int) => new TextBox
                {
                    Style = textBoxStyle,
                    Height = 30,
                    Margin = new Thickness(5),
                    VerticalContentAlignment = VerticalAlignment.Center
                }.WithBinding(TextBox.TextProperty, new Binding(prop.Name)
                {
                    Source = settings,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                }),
                
                { } t when t == typeof(string) && prop.Name == "FontFamily" => new ComboBox
                {
                    ItemsSource = Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList(),
                    Margin = new Thickness(5),
                    Style = comboBoxStyle
                }.WithBinding(Selector.SelectedItemProperty, new Binding(prop.Name)
                {
                    Source = settings,
                    Mode = BindingMode.TwoWay,
                    Converter = new FontFamilyConverter()
                }),
    
                { } t when t == typeof(string) => new TextBox
                {
                    Style = textBoxStyle,
                    Height = 30,
                    Margin = new Thickness(5),
                    VerticalContentAlignment = VerticalAlignment.Center
                }.WithBinding(TextBox.TextProperty, new Binding(prop.Name)
                {
                    Source = settings,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                }),
    
                _ => null
            };
    
            if (control == null) continue;
    
            var separator = prop.GetCustomAttributes(typeof(SeparatorAttribute), true);
            if (separator.Length > 0 && separator[0] is SeparatorAttribute separatorAttr)
            {
                var sep = new Separator();
                stack.Children.Add(sep);
            }
            
            var header = prop.GetCustomAttributes(typeof(HeaderAttribute), true);
            if (header.Length > 0 && header[0] is HeaderAttribute headerAttribute)
            {
                var headerTextBlock = new TextBlock()
                {
                    Foreground = (Brush)Application.Current.Resources["PrimaryTextBrush"]!,
                    Margin = new Thickness(5),
                    FontSize = 13,
                    FontWeight =  FontWeights.Bold,
                    Text = headerAttribute.Title
                };
                
                stack.Children.Add(headerTextBlock);
            }
            
            Grid.SetColumn(control, 1);
            grid.Children.Add(control);
            stack.Children.Add(grid);
        }
    }
    
    // Extension method for cleaner binding syntax
    private static T WithBinding<T>(this T element, DependencyProperty property, Binding binding) where T : FrameworkElement
    {
        element.SetBinding(property, binding);
        return element;
    }
    
}