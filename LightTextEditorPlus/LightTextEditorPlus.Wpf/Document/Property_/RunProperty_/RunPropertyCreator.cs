using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Media3D;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Document;

class RunPropertyCreator : PlatformRunPropertyCreatorBase<RunProperty>
{
    public RunPropertyCreator(TextEditor textEditor)
    {
        TextEditor = textEditor;
        _runPropertyPlatformManager = new RunPropertyPlatformManager(textEditor);
    }

    public TextEditor TextEditor { get; }

    protected override RunProperty OnGetDefaultRunProperty()
    {
        return new RunProperty(_runPropertyPlatformManager);
    }

    private readonly RunPropertyPlatformManager _runPropertyPlatformManager;

    public override IReadOnlyRunProperty ToPlatformRunProperty(ICharObject charObject, IReadOnlyRunProperty baseRunProperty)
    {
        if (baseRunProperty is RunProperty runProperty)
        {
            if (!ReferenceEquals(runProperty.RunPropertyPlatformManager, _runPropertyPlatformManager))
            {
                // �Ǵ�����ƽ̨�����ģ�
                var message = $"""
                               ��ǰ�����ַ����Բ����ɵ�ǰ�ı�����Դ�����������������ܽ������ı����ַ����Դ��������ǰ�ı���
                               ��ȷ��������ַ������ɵ�ǰ�ı�����Դ������������
                               ��ʹ�� with �ؼ��ִ� DefaultRunProperty ���Դ������µ��ַ����ԡ�
                               ��ǰ�ı���DebugName='{TextEditor.TextEditorCore.DebugName}';�ַ�����RunProperty������Դ���ı���DebugName='{runProperty.RunPropertyPlatformManager.TextEditor.TextEditorCore.DebugName}'
                               """;
                TextEditor. Logger.LogWarning(message);
                if (TextEditor.IsInDebugMode)
                {
                    throw new TextEditorDebugException(message);
                }

                // ���Լ���
                runProperty = runProperty with
                {
                    RunPropertyPlatformManager = _runPropertyPlatformManager
                };
            }

            // todo �������������ﴦ���ַ�����������

            return runProperty;
        }
        else
        {
            // �õײ�ȥ�׳��쳣
            return base.ToPlatformRunProperty(charObject, baseRunProperty);
        }
    }

    protected override RunProperty OnUpdateMarkerRunProperty(RunProperty? markerRunProperty, RunProperty styleRunProperty)
    {
        if (markerRunProperty is null)
        {
            return styleRunProperty;
        }
        else
        {
            return styleRunProperty with
            {
                FontName = markerRunProperty.FontName,
            };
        }
    }
}