using System.Windows;
using dotnetCampus.UITest.WPF;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TextEditorTest
{
    [UIContractTestCase]
    public void LayoutTest()
    {
        "���� TextEditor �Ŀ�ȣ�����Ӱ�쵽���ֵ��������".Test(async () =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            // Action
            // ���� TextEditor �Ŀ�ȣ�����Ϊ 35 ��ȣ�����ѡ��΢���ź� 30 ���壬�ǾͷŲ��������ַ�
            textEditor.Width = 35;
            textEditor.SetFontName("΢���ź�");
            textEditor.SetFontSize(30);

            // �����������ַ���Ԥ�ƾ��ܱ�����Ϊ����
            textEditor.TextEditorCore.AppendText("һ��");

            // Assert
            await textEditor.WaitForRenderCompletedAsync();

            await TestFramework.FreezeTestToDebug();
            var renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();
            // ������Ⱦһ������
            var paragraphRenderInfo = renderInfoProvider.GetParagraphRenderInfoList().First();
            var lineRenderInfoList = paragraphRenderInfo.GetLineRenderInfoList().ToList();
            Assert.AreEqual(2,lineRenderInfoList.Count);
        });
    }

    [UIContractTestCase]
    public void AppendTestAfterSetRunProperty()
    {
        "��׷��һ���ı������޸ĵ�ǰ������ԣ���׷��һ���ı������Է���Ԥ�ڵ���ʾ������ʽ��ͬ���ı�".Test(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            // ��׷��һ���ı�
            textEditor.TextEditorCore.AppendText("123");

            // ���޸ĵ�ǰ�������
            textEditor.SetRunProperty(runProperty => runProperty.FontSize = 15);

            // ��׷��һ���ı�
            textEditor.TextEditorCore.AppendText("123");

            // ���Է���Ԥ�ڵ���ʾ������ʽ��ͬ���ı�
            // �ȿ���ȥ��
            await TestFramework.FreezeTestToDebug();
        });
    }

    [UIContractTestCase]
    public void AppendText()
    {
        "���յ��ı���׷�� 123 �ַ�����������ʾ�� 123 ���ı�".Test(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            textEditor.TextEditorCore.AppendText("123");

            await TestFramework.FreezeTestToDebug();
        });
    }
}