﻿using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Document.DocumentManagers;

[TestClass()]
public class DocumentManagerSetParagraphPropertyTests
{
    [ContractTestCase()]
    public void SetParagraphProperty()
    {
        "给一个包含两段的文本设置段落属性，设置第二个段落的段落属性，不会影响到第一个段落，且可以撤销恢复".Test(() =>
        {
            // Arrange
            var undoRedoProvider = new TestTextEditorUndoRedoProvider();
            var testPlatformProvider = new TestPlatformProvider()
            {
                UndoRedoProvider = undoRedoProvider,
            };
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);
            textEditorCore.SetUndoRedoEnable(false, "先关闭撤销重做，来做一些初始化，减少初始化影响");
            // 创建段落，用于后续测试
            textEditorCore.AppendText("123\r\n123");
            ParagraphProperty oldProperty = textEditorCore.DocumentManager.GetParagraphProperty(1);
            textEditorCore.SetUndoRedoEnable(true, "开启撤销重做，用于测试行为");

            // Action
            // 设置第二个段落的段落属性
            var lineSpacing = 10253; // 用来标识段落
            var paragraphProperty = new ParagraphProperty()
            {
                LineSpacing = lineSpacing
            };

            textEditorCore.DocumentManager.SetParagraphProperty(1, paragraphProperty);

            // Assert
            // 第二个段落被更改了
            var firstParagraphProperty = textEditorCore.DocumentManager.GetParagraphProperty(1);
            Assert.AreSame(paragraphProperty, firstParagraphProperty);

            // 不会影响到第一个段落
            var secondParagraphProperty = textEditorCore.DocumentManager.GetParagraphProperty(0);
            Assert.AreNotSame(paragraphProperty, secondParagraphProperty);

            // 测试撤销重做
            Assert.AreEqual(1, undoRedoProvider.UndoOperationList.Count);
            undoRedoProvider.Undo();
            Assert.AreSame(oldProperty, textEditorCore.DocumentManager.GetParagraphProperty(1));

            // 不会影响到第一个段落
            Assert.AreSame(secondParagraphProperty, textEditorCore.DocumentManager.GetParagraphProperty(0));

            // 恢复一下
            undoRedoProvider.Redo();
            Assert.AreSame(paragraphProperty, textEditorCore.DocumentManager.GetParagraphProperty(1));

            // 不会影响到第一个段落
            Assert.AreSame(secondParagraphProperty, textEditorCore.DocumentManager.GetParagraphProperty(0));
        });

        "给一个包含两段的文本设置段落属性，设置第一个段落的段落属性，不会影响到第二个段落，且可以撤销恢复".Test(() =>
        {
            // Arrange
            var undoRedoProvider = new TestTextEditorUndoRedoProvider();
            var testPlatformProvider = new TestPlatformProvider()
            {
                UndoRedoProvider = undoRedoProvider,
            };
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);
            textEditorCore.SetUndoRedoEnable(false, "先关闭撤销重做，来做一些初始化，减少初始化影响");
            // 创建段落，用于后续测试
            textEditorCore.AppendText("123\r\n123");
            ParagraphProperty oldProperty = textEditorCore.DocumentManager.GetParagraphProperty(0);
            textEditorCore.SetUndoRedoEnable(true, "开启撤销重做，用于测试行为");

            // Action
            // 设置第一个段落的段落属性
            var lineSpacing = 10253; // 用来标识段落
            var paragraphProperty = new ParagraphProperty()
            {
                LineSpacing = lineSpacing
            };

            textEditorCore.DocumentManager.SetParagraphProperty(0, paragraphProperty);

            // Assert
            // 第一个段落被更改了
            var firstParagraphProperty = textEditorCore.DocumentManager.GetParagraphProperty(0);
            Assert.AreSame(paragraphProperty, firstParagraphProperty);

            // 不会影响到第二个段落
            var secondParagraphProperty = textEditorCore.DocumentManager.GetParagraphProperty(1);
            Assert.AreNotSame(paragraphProperty, secondParagraphProperty);

            // 测试撤销重做
            Assert.AreEqual(1, undoRedoProvider.UndoOperationList.Count);
            undoRedoProvider.Undo();
            Assert.AreSame(oldProperty, textEditorCore.DocumentManager.GetParagraphProperty(0));

            // 不会影响到第二个段落
            Assert.AreSame(secondParagraphProperty, textEditorCore.DocumentManager.GetParagraphProperty(1));

            // 恢复一下
            undoRedoProvider.Redo();
            Assert.AreSame(paragraphProperty, textEditorCore.DocumentManager.GetParagraphProperty(0));

            // 不会影响到第二个段落
            Assert.AreSame(secondParagraphProperty, textEditorCore.DocumentManager.GetParagraphProperty(1));
        });
    }
}