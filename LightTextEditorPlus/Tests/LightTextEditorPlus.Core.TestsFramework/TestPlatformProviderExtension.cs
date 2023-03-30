namespace LightTextEditorPlus.Core.TestsFramework;

public static class TestPlatformProviderExtension
{
    /// <summary>
    /// �̶��ַ��ߴ磬������Բ���
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static TestPlatformProvider UsingFixedCharSizeCharInfoMeasurer(this TestPlatformProvider provider)
    {
        provider.CharInfoMeasurer = new FixedCharSizeCharInfoMeasurer();
        return provider;
    }
}