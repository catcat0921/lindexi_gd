﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LightTextEditorPlus.TextEditorPlus.Render;

namespace LightTextEditorPlus.TextEditorPlus.Layout
{
    /// <summary>
    /// 视觉呈现容器
    /// </summary>
    internal class TextView : FrameworkElement
    {
        public TextView(RenderManager renderManager)
        {
            _renderManager = renderManager;
            _layers = new UIElementCollection(this, this);
        }

        #region 属性

        /// <inheritdoc />
        protected override int VisualChildrenCount => _layers.Count;

        /// <inheritdoc />
        protected override Visual GetVisualChild(int index) => _layers[index];

        #endregion

        private readonly RenderManager _renderManager;
        private readonly UIElementCollection _layers;
    }
}
