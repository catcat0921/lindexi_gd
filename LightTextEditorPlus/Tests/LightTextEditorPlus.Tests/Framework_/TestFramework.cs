using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dotnetCampus.UITest.WPF;
using CSharpMarkup.Wpf;
using LightTextEditorPlus.Demo;
using static CSharpMarkup.Wpf.Helpers;
using Application = System.Windows.Application;
using Window = System.Windows.Window;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TestFramework
{
    private static Application _application;

    [AssemblyInitialize]
    public static void InitializeApplication(TestContext testContext)
    {
        //var fieldInfo = typeof(Application).GetField("_resourceAssembly", BindingFlags.NonPublic | BindingFlags.Static);
        //fieldInfo!.SetValue(null, typeof(App).Assembly);

        UITestManager.InitializeApplication(() =>
        {
            _application = new Application()
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown,
            };
            return _application;
        });
    }

    [AssemblyCleanup]
    public static void CleanApplication()
    {
        _application.Dispatcher.InvokeAsync(() =>
        {
            _application.Shutdown();
        });
    }

    public static TextEditTestContext CreateTextEditorInNewWindow()
    {
        //var mainWindow = new Window()
        //{
        //    Width = 1000,
        //    Height = 700,
        //    Content = Border
        //    (
        //        BorderThickness: Thickness(1),
        //        BorderBrush: Brushes.Blue,
        //        Child: Grid
        //        (
        //            new TextEditor()
        //            {
        //                Width = 600,
        //                Height = 600,
        //                HorizontalAlignment = HorizontalAlignment.Left,
        //                VerticalAlignment = VerticalAlignment.Stretch,
        //            }.Out(out var textEditor)
        //        )
        //    ).Margin(10).UI
        //};

        var mainWindow = new MainWindow();
        var textEditor = mainWindow.TextEditor;
        textEditor.Width = 1000;

        mainWindow.Show();
        return new TextEditTestContext(mainWindow, textEditor);
    }

    /// <summary>
    /// ���ڵ����¸����˽⵽��ǰ�Ƿ����Ԥ�ڣ�����ͣ�����߼����������½���˷����˳����� Debug ����Ч
    /// </summary>
    /// <returns></returns>
    public static async Task FreezeTestToDebug()
    {
        if (IsDebug())
        {
            for (int i = 0; i < int.MaxValue; i++)
            {
                // �����������ϵ�����˳��߼�
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }

    public static bool IsDebug()
    {
#if DEBUG
        return Debugger.IsAttached;
#else
        return false;
#endif
    }
}