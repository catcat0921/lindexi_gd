using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class DualEditorUserControl : UserControl
{
    public DualEditorUserControl()
    {
        InitializeComponent();

        LeftTextEditor.TextEditorCore.DocumentChanged += LeftTextEditor_DocumentChanged;
        LeftTextEditor.TextEditorCore.LayoutCompleted += LeftTextEditor_LayoutCompleted;
        LeftTextEditor.SetFontSize(60);
        LeftTextEditor.AppendText("""
                                  ���Ͽɲ�������Ҷ�������Ϸ��Ҷ�䡣
                                  ��Ϸ��Ҷ������Ϸ��Ҷ����
                                  ��Ϸ��Ҷ�ϣ���Ϸ��Ҷ����
                                  """);

        RightTextEditor.TextEditorCore.DocumentChanged += RightTextEditor_DocumentChanged;
        RightTextEditor.TextEditorCore.LayoutCompleted += RightTextEditor_LayoutCompleted;
        RightTextEditor.SetFontSize(60);
        RightTextEditor.AppendText("""
                                   �������������������˲ɡ���ҶһƬ�̣��·�ɱ̺������֪Ϸ�֣�Ѱ���ٷ�����
                                   ���������Ҷ�������������Ҷ����
                                   ���������Ҷ�ϣ����������Ҷ����
                                   """);

        _isInitialized = true;

        var leftList = LeftTextEditor.TextEditorCore.ParagraphList;
        var rightList = RightTextEditor.TextEditorCore.ParagraphList;
        Debug.Assert(leftList.Count == rightList.Count);

        for (var i = 0; i < leftList.Count; i++)
        {
            _associatedParagraphList.Add((leftList[i], rightList[i]));
        }
    }

    private readonly List<(ITextParagraph Left, ITextParagraph Right)> _associatedParagraphList = [];

    private bool _isInitialized;

    private void LeftTextEditor_LayoutCompleted(object? sender, LayoutCompletedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }
        UpdateDualEditorAlignment();
    }

    private void LeftTextEditor_DocumentChanged(object? sender, System.EventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }
    }


    private void RightTextEditor_LayoutCompleted(object? sender, LayoutCompletedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        UpdateDualEditorAlignment();
    }

    private void RightTextEditor_DocumentChanged(object? sender, System.EventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }
        else
        {
            
        }
    }

    private void UpdateDualEditorAlignment()
    {
        if (LeftTextEditor.IsDirty || RightTextEditor.IsDirty)
        {
            return;
        }

        var leftList = LeftTextEditor.TextEditorCore.ParagraphList;
        var rightList = RightTextEditor.TextEditorCore.ParagraphList;

        RenderInfoProvider leftRenderInfoProvider = LeftTextEditor.TextEditorCore.GetRenderInfo();
        RenderInfoProvider rightRenderInfoProvider = RightTextEditor.TextEditorCore.GetRenderInfo();

        for (var i = 0; i < leftList.Count; i++)
        {
            ITextParagraph leftParagraph = leftList[i];
            ITextParagraph rightParagraph = rightList[i];

            LeftTextEditor.TextEditorCore.DocumentManager.SetParagraphProperty(leftParagraph, leftParagraph.ParagraphProperty with
            {
                
            });
        }
    }
}