﻿using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorEditTest
{
    [ContractTestCase]
    public void Remove()
    {
        "删除超过文本字符数量，抛出异常".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            // 随便传入一点文本，然后调用删除
            textEditorCore.AppendText("12");

            // 从 1 开始，删除 2 个字符，就刚好超过文本字符数量
            var selection = new Selection(new CaretOffset(1), length: 2);

            // Assert
            Assert.ThrowsException<SelectionOutOfRangeException>(() =>
            {
                // Action
                textEditorCore.Remove(selection);
            });
        });

        "对文本调用 Remove 传入空选择，啥都不会发生".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            // 随便传入一点文本，然后调用删除空白选择
            textEditorCore.AppendText("12");

            // 预期啥都不会发生，也就是不会触发布局等变更事件
            textEditorCore.DocumentChanging += (sender, args) =>
            {
                Assert.Fail("对文本调用 Remove 传入空选择，啥都不会发生");
            };

            // Action
            textEditorCore.Remove(new Selection(new CaretOffset(0), 0));

            // Assert
            // 不会删除字符
            Assert.AreEqual(2, textEditorCore.DocumentManager.CharCount);
        });
    }

    [ContractTestCase]
    public void Delete()
    {
        "对文本调用 Delete 删除，可以删除光标之后一个字符".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            // 输入两个字符，用来调用 Delete 删除
            textEditorCore.AppendText("12");
            // 然后将光标移动到第零个字符后面，用于按下 Delete 删除
            // 第零个字符后面的光标坐标是 1 的值
            textEditorCore.CurrentCaretOffset = new CaretOffset(1);

            // Action
            textEditorCore.Delete();

            // Assert
            Assert.AreEqual(1, textEditorCore.DocumentManager.CharCount);
            var paragraphLineRenderInfo = textEditorCore.GetRenderInfo().GetParagraphRenderInfoList().First().GetLineRenderInfoList().First();
            var text = paragraphLineRenderInfo.LineLayoutData.GetCharList()[0].CharObject.ToText();
            // 在第零个字符后面，删除 "2" 这个字符
            Assert.AreEqual("1", text);
        });

        "对空文本调用 Delete 删除，啥都不会发生".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());

            // 啥都不做，这就是一个空文本
            textEditorCore.DocumentChanging += (sender, args) =>
            {
                Assert.Fail("对空文本调用 Delete 删除，啥都不会发生");
            };

            // Action
            textEditorCore.Delete();

            // Assert
            Assert.AreEqual(0, textEditorCore.DocumentManager.CharCount);
        });
    }

    [ContractTestCase]
    public void Backspace()
    {
        "对文本字符串为 abc 的文本执行退格，可以删除最后一个字符，且光标在删除后的最后一个字符之后".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            // 先追加文本，用于后续删除
            textEditorCore.AppendText("abc");

            textEditorCore.CurrentCaretOffset = textEditorCore.DocumentManager.GetDocumentEndCaretOffset();

            // Action
            // 执行退格，可以删除最后一个字符
            textEditorCore.Backspace();

            // Assert
            // 可以删除最后一个字符，且光标在删除后的最后一个字符之后
            Assert.AreEqual("ab", textEditorCore.GetText());
            Assert.AreEqual(2, textEditorCore.CurrentCaretOffset.Offset);
        });

        "在段首执行 Backspace 退格，可以删除段，和前面一段合成一段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            // 先追加两段，用于后续删除
            textEditorCore.AppendText("1\r\n2");
            // 移动光标在段首
            textEditorCore.CurrentCaretOffset = new CaretOffset(3);

            // Action
            // 在段首执行 Backspace 退格
            textEditorCore.Backspace();

            // Assert
            // 可以删除段，和前面一段合成一段
            Assert.AreEqual("12", textEditorCore.GetText());
        });

        "对空段执行 Backspace 退格，可以删除空段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            // 先追加一个字符加一段，用于后续删除
            textEditorCore.AppendText("1\r\n");
            // 追加之后，光标在文档最后，也就是在空段
            // 此时不需要修改光标了

            // Action
            // 对空段执行 Backspace 退格
            textEditorCore.Backspace();

            // Assert
            // 删除之后，就只剩下一个字符
            Assert.AreEqual("1", textEditorCore.GetText());
        });

        "对只有一个字符的文本执行 Backspace 退格，可以删除所有文本".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());
            // 先追加一个字符，用于后续删除
            textEditorCore.AppendText("1");

            // Action
            textEditorCore.Backspace();

            // Assert
            // 可以删除所有文本，等于文本字符数量是空
            Assert.AreEqual(0, textEditorCore.DocumentManager.CharCount);
            // 删除之后，依然存在一段
            Assert.AreEqual(1, textEditorCore.DocumentManager.ParagraphManager.GetParagraphList().Count);
            Assert.AreEqual(1, textEditorCore.DocumentManager.ParagraphManager.GetParagraphList()[0].LineLayoutDataList.Count);
        });
    }
}