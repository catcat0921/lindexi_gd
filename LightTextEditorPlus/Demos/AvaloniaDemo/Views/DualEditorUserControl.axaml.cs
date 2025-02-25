using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LightTextEditorPlus.Core.Events;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class DualEditorUserControl : UserControl
{
    public DualEditorUserControl()
    {
        InitializeComponent();
        
        LeftTextEditor.AppendText("""
                                  ���Ͽɲ�������Ҷ�������Ϸ��Ҷ�䡣
                                  ��Ϸ��Ҷ������Ϸ��Ҷ����
                                  ��Ϸ��Ҷ�ϣ���Ϸ��Ҷ����
                                  """);
        LeftTextEditor.SetFontSize(60);
        LeftTextEditor.TextEditorCore.DocumentChanged += LeftTextEditor_DocumentChanged;
        LeftTextEditor.TextEditorCore.LayoutCompleted += LeftTextEditor_LayoutCompleted;

        RightTextEditor.AppendText("""
                                   �������������������˲ɡ���ҶһƬ�̣��·�ɱ̺������֪Ϸ�֣�Ѱ���ٷ�����
                                   ���������Ҷ�������������Ҷ����
                                   ���������Ҷ�ϣ����������Ҷ����
                                   """);
        RightTextEditor.SetFontSize(60);
        RightTextEditor.TextEditorCore.DocumentChanged += RightTextEditor_DocumentChanged;
        RightTextEditor.TextEditorCore.LayoutCompleted += RightTextEditor_LayoutCompleted;
    }

    private void LeftTextEditor_LayoutCompleted(object? sender, LayoutCompletedEventArgs e)
    {
        //LeftTextEditor.TextEditorCore.DocumentManager.pa
    }

    private void LeftTextEditor_DocumentChanged(object? sender, System.EventArgs e)
    {
    }

    private void RightTextEditor_LayoutCompleted(object? sender, LayoutCompletedEventArgs e)
    {
    }

    private void RightTextEditor_DocumentChanged(object? sender, System.EventArgs e)
    {
    }
}