using System.Windows;
using dotnetCampus.UITest.WPF;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TextEditorStyleTest
{
    [UIContractTestCase]
    public void ChangeStyle()
    {
        "δѡ��ʱ���޸ĵ�ǰ����ַ�������ʽ��ֻ���� StyleChanging �� StyleChanged �¼������������ֱ��".Test(() =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;
            // ֻ���� StyleChanging �� StyleChanged �¼������ò����ˣ�����������Ԫ����
            textEditor.TextEditorCore.LayoutCompleted += (sender, args) =>
            {
                // Assert
                // δѡ��ʱ���޸ĵ�ǰ����ַ�������ʽ��ֻ���� StyleChanging �� StyleChanged �¼������������ֱ��
                Assert.Fail();
            };

            // Action
            textEditor.ToggleBold();
        });

        "�޸���ʽʱ���ȴ��� StyleChanging �¼����ٴ��� StyleChanged �¼�����ֻ����һ��".Test(() =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            int count = 0;
            textEditor.StyleChanging += (sender, args) =>
            {
                Assert.AreEqual(0, count);
                count++;
            };
            textEditor.StyleChanged += (sender, args) =>
            {
                Assert.AreEqual(1, count);
                count++;
            };

            // Action
            textEditor.ToggleBold();

            // Assert
            // ֻ����һ�Σ�һ�������¼�
            Assert.AreEqual(2, count);
        });
    }

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