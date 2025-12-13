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
            var grid = new Grid
            {
                Margin = new Thickness(10, 8, 10, 8)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto,
                SharedSizeGroup = "Labels"
            });

            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });

            var label = new TextBlock
            {
                Text = prop.Name + ":",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Style = textBlockStyle
            };

            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            FrameworkElement control;

            if (prop.PropertyType == typeof(bool))
            {
                control = new CheckBox
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Style = checkBoxStyle,
                };

                control.SetBinding(
                    ToggleButton.IsCheckedProperty,
                    new Binding(prop.Name)
                    {
                        Source = settings,
                        Mode = BindingMode.TwoWay
                    });
            }
            else if (prop.PropertyType == typeof(double))
            {
                control = new TextBox
                {
                    Style = textBoxStyle,
                    Height = 30,
                    Margin = new Thickness(5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };

                control.SetBinding(
                    TextBox.TextProperty,
                    new Binding(prop.Name)
                    {
                        Source = settings,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                    });
            }
            else if (prop.PropertyType == typeof(string) && prop.Name == "FontFamily")
            {
                var fonts = Fonts.SystemFontFamilies
                    .OrderBy(f => f.Source)
                    .ToList();

                control = new ComboBox
                {
                    ItemsSource = fonts,
                    Margin = new Thickness(5),
                    Style = comboBoxStyle
                };

                control.SetBinding(
                    Selector.SelectedItemProperty,
                    new Binding(prop.Name)
                    {
                        Source = settings,
                        Mode = BindingMode.TwoWay,
                        Converter = new FontFamilyConverter()
                    });
            }
            else if (prop.PropertyType == typeof(string))
            {
                control = new TextBox
                {
                    Style = textBoxStyle,
                    Height = 30,
                    Margin = new Thickness(5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };

                control.SetBinding(
                    TextBox.TextProperty,
                    new Binding(prop.Name)
                    {
                        Source = settings,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                    });
            }
            else
            {
                continue;
            }

            Grid.SetColumn(control, 1);
            grid.Children.Add(control);

            stack.Children.Add(grid);
        }
    }

}