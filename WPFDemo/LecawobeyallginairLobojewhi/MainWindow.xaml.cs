﻿using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LecawobeyallginairLobojewhi;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button1_OnClick(object sender, RoutedEventArgs e)
    {
        Task.Run(() =>
        {
            _asyncLocal.Value = new Foo()
            {
                Name = "Hello, World!"
            };

            _list.Add(() =>
            {
                var foo = _asyncLocal.Value;
                MessageBox.Show(foo?.Name);
            });
        });
    }

    private readonly List<Action> _list = [];

    private void Button2_OnClick(object sender, RoutedEventArgs e)
    {
        var foo = _asyncLocal.Value;
        MessageBox.Show(foo?.Name);

        foreach (var action in _list)
        {
            action();
        }
    }

    private readonly AsyncLocal<Foo> _asyncLocal = new AsyncLocal<Foo>();
}

public record Foo
{
    public string? Name { get; set; }
}