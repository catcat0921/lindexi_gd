namespace LightTextEditorPlus.Rendering.Core;

class VerticalSkiaTextRender : HorizontalSkiaTextRender
{
    public VerticalSkiaTextRender(RenderManager renderManager) : base(renderManager)
    {
    }

    public override SkiaTextRenderResult Render(in SkiaTextRenderArgument renderArgument)
    {
        // todo ʵ����Ⱦ
        return base.Render(in renderArgument);
    }


}