﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace LightTextEditorPlus.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InputButton_OnClick(object sender, RoutedEventArgs e)
        {
            TextEditor.TextEditorCore.AppendText(TextBox.Text);
        }

        private async void DebugButton_OnClick(object sender, RoutedEventArgs e)
        {
            // 给调试使用的按钮，可以在这里编写调试代码

            while (true)
            {
                for (int i = 0; i < 100; i++)
                {
                    TextEditor.TextEditorCore.AppendText(((char) Random.Shared.Next('a', 'z')).ToString());
                    await Task.Delay(10);
                }

                TextEditor.TextEditorCore.AppendText("\r\n");
            }
        }
    }
}
