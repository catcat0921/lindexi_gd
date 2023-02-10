using System.Windows;
using dotnetCampus.UITest.WPF;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TextEditorStyleTest
{
    [UIContractTestCase]
    public void ToggleBold()
    {
        "δѡ��ʱ������ ToggleBold ���ı���ǰ������üӴ֣�׷���ı�֮�󣬵�ǰ�����ַ������ǼӴ�".Test(async () =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            // Action
            // �����ȡ������δѡ���
            // ֱ�ӵ��� ToggleBold ����
            textEditor.ToggleBold();

            // ׷���ı���Ԥ��׷���ı����ᵼ�µ�ǰ�����ַ����Բ��Ӵ�
            textEditor.TextEditorCore.AppendText("a");

            // Assert
            // ��ǰ�����ַ������ǼӴ�
            Assert.AreEqual(FontWeights.Bold, textEditor.CurrentCaretRunProperty.FontWeight);
            await TestFramework.FreezeTestToDebug();
        });

        "δѡ��ʱ�����Ե��� ToggleBold ���ı���ǰ������üӴ�".Test(async () =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            // Action
            // �����ȡ������δѡ���
            // ֱ�ӵ��� ToggleBold ����
            textEditor.ToggleBold();

            // Assert
            Assert.AreEqual(FontWeights.Bold, textEditor.CurrentCaretRunProperty.FontWeight);

            // ���µ��� ToggleBold ����ȥ���Ӵ�
            textEditor.ToggleBold();
            Assert.AreEqual(FontWeights.Normal, textEditor.CurrentCaretRunProperty.FontWeight);

            await TestFramework.FreezeTestToDebug();
        });
    }
}