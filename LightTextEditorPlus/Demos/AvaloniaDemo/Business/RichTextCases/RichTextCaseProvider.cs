using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus.AvaloniaDemo.Business.RichTextCases;

class RichTextCaseProvider
{
    public RichTextCaseProvider()
    {
        Add(editor =>
        {
            // ׷���ı�
            editor.AppendText("׷�ӵ��ı�");
        }, "׷���ı�");

        Add(editor =>
        {
            //editor.TextEditorCore.PlatformProvider.GetPlatformRunPropertyCreator()
            SkiaTextRunProperty runProperty = editor.GetDefaultRunProperty();
            runProperty = runProperty with
            {
                FontSize = 60
            };

            editor.AppendRun(new SkiaTextRun("׷�ӵ��ı�", runProperty));
        }, "�����ı������ֺ�");
    }

    public void Add(Action<TextEditor> action, string name = "")
    {
        Add(new DelegateRichTextCase(action));
    }

    public void Add(IRichTextCase richTextCase)
    {
        _richTextCases.Add(richTextCase);
    }

    public IReadOnlyList<IRichTextCase> RichTextCases => _richTextCases;

    private readonly List<IRichTextCase> _richTextCases = new List<IRichTextCase>();

    public void Debug(TextEditor textEditor)
    {
        RichTextCases[1].Exec(textEditor);
    }
}