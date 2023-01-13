using dotnetCampus.UITest.WPF;

using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

using MSTest.Extensions.Contracts;
using RunProperty = LightTextEditorPlus.Document.RunProperty;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class RunPropertyTest
{
    [UIContractTestCase]
    public void GetGlyphTypeface()
    {
        "û���޸� RunProperty ������������ԣ����Ի�ȡ����ʽ��ͬ�� GlyphTypeface ����".Test(() =>
        {
            var (mainWindow, textEditor) = TestFramework.CreateTextEditorInNewWindow();
            var runPropertyCreator = textEditor.TextEditorPlatformProvider.GetPlatformRunPropertyCreator();

            var styleRunProperty = runPropertyCreator.BuildNewProperty(config =>
            {
                var property = (RunProperty) config;
                property.FontName = new FontName("Arial");
            }, runPropertyCreator.GetDefaultRunProperty()).AsRunProperty();

            var glyphTypeface1 = styleRunProperty.GetGlyphTypeface();

            var runProperty = runPropertyCreator.BuildNewProperty(config =>
            {
                // û���޸� RunProperty �������������
            },styleRunProperty).AsRunProperty();

            var glyphTypeface2 = runProperty.GetGlyphTypeface();

            Assert.AreSame(glyphTypeface1, glyphTypeface2);
        });

        "�޸� RunProperty ���������������Ի�ȡ������� GlyphTypeface ����".Test(() =>
        {
            var (mainWindow, textEditor) = TestFramework.CreateTextEditorInNewWindow();
            var runPropertyCreator = textEditor.TextEditorPlatformProvider.GetPlatformRunPropertyCreator();

            var runProperty = runPropertyCreator.BuildNewProperty(config =>
            {
                var property = (RunProperty) config;
                property.FontName = new FontName("Arial");
            }, runPropertyCreator.GetDefaultRunProperty()).AsRunProperty();

            var glyphTypeface = runProperty.GetGlyphTypeface();

            // ������壬�������ǲ�����ڱ�����
            Assert.AreEqual("Arial", glyphTypeface.FamilyNames.Values.First());
        });

        "���� RunProperty ���󣬿��Ի�ȡ������� GlyphTypeface ����".Test(() =>
        {
            var (mainWindow, textEditor) = TestFramework.CreateTextEditorInNewWindow();
            var runPropertyCreator = textEditor.TextEditorPlatformProvider.GetPlatformRunPropertyCreator();

            var runProperty = runPropertyCreator.GetDefaultRunProperty().AsRunProperty();

            var glyphTypeface = runProperty.GetGlyphTypeface();
            Assert.IsNotNull(glyphTypeface);
        });
    }

    [UIContractTestCase]
    public void Equals()
    {
        "��ȡ������Ĭ�ϵ� RunProperty �����ж���ȣ��������".Test(() =>
        {
            var (mainWindow, textEditor) = TestFramework.CreateTextEditorInNewWindow();
            var runPropertyCreator = textEditor.TextEditorPlatformProvider.GetPlatformRunPropertyCreator();

            // ��ȡ������Ĭ�ϵ� RunProperty ����
            var runProperty1 = runPropertyCreator.GetDefaultRunProperty();
            var runProperty2 = runPropertyCreator.GetDefaultRunProperty();

            // �ж���ȣ��������
            var equals = runProperty1.Equals(runProperty2);
            Assert.AreEqual(true, equals);
        });
    }
}