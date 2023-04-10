﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Utils;

using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Document
{
    /// <summary>
    /// 提供文档管理，只提供数据管理，这里属于更高级的 API 层，将提供更多的细节控制
    /// </summary>
    public class DocumentManager
    {
        /// <inheritdoc cref="T:LightTextEditorPlus.Core.Document.DocumentManager"/>
        public DocumentManager(TextEditor textEditor)
        {
            TextEditor = textEditor;
            CurrentParagraphProperty = new ParagraphProperty();
            _currentRunProperty = textEditor.PlatformProvider.GetPlatformRunPropertyCreator().GetDefaultRunProperty();

            ParagraphManager = new ParagraphManager(textEditor);
            DocumentRunEditProvider = new DocumentRunEditProvider(textEditor);
        }

        #region 框架

        internal TextEditor TextEditor { get; }

        #region DocumentWidth DocumentHeight

        /// <summary>
        /// 文档的宽度。受 <see cref="LightTextEditorPlus.Core.TextEditorCore.SizeToContent"/> 影响
        /// </summary>
        /// 文档的宽度不等于渲染宽度。布局尺寸请参阅 <see cref="TextEditorCore.GetDocumentLayoutBounds"/> 方法
        public double DocumentWidth
        {
            set
            {
                _documentWidth = value;

                TextEditor.RequireDispatchReLayoutAllDocument("DocumentWidthChanged");
            }
            get => _documentWidth;
        }

        private double _documentWidth = double.PositiveInfinity;

        /// <summary>
        /// 文档的高度。受 <see cref="LightTextEditorPlus.Core.TextEditorCore.SizeToContent"/> 影响
        /// </summary>
        /// 文档的高度不等于渲染高度。布局尺寸请参阅 <see cref="TextEditorCore.GetDocumentLayoutBounds"/> 方法
        public double DocumentHeight
        {
            set
            {
                _documentHeight = value;

                TextEditor.RequireDispatchReLayoutAllDocument("DocumentHeightChanged");
            }
            get => _documentHeight;
        }

        private double _documentHeight = double.PositiveInfinity;
        #endregion

        /// <summary>
        /// 文档的字符编辑提供器
        /// </summary>
        /// 和 <see cref="ParagraphManager"/> 不同的是，此属性用来辅助处理字符编辑。而 <see cref="ParagraphManager"/> 用来修改段落
        private DocumentRunEditProvider DocumentRunEditProvider { get; }

        /// <summary>
        /// 段落管理。这是存放所有的字符的属性。字符存放在段落里面
        /// </summary>
        internal ParagraphManager ParagraphManager { get; }

        /// <summary>
        /// 管理光标
        /// </summary>
        private CaretManager CaretManager => TextEditor.CaretManager;

        #region 事件

        ///// <summary>
        ///// 给内部提供的文档尺寸变更事件
        ///// </summary>
        //internal event EventHandler? InternalDocumentSizeChanged; 

        /// <summary>
        /// 给内部提供的文档开始变更事件
        /// </summary>
        internal event EventHandler? InternalDocumentChanging;

        /// <summary>
        /// 给内部提供的文档变更完成事件
        /// </summary>
        internal event EventHandler? InternalDocumentChanged;

        #endregion

        #region Paragraph段落

        /// <summary>
        /// 设置或获取当前文本的默认段落属性。设置之后，只影响新变更的文本，不影响之前的文本
        /// </summary>
        public ParagraphProperty CurrentParagraphProperty { set; get; }

        ///// <summary>
        ///// 当前光标下的段落
        ///// </summary>
        //internal ParagraphData CurrentCaretParagraphData
        //{
        //    get
        //    {
        //        var hitParagraphDataResult = ParagraphManager.GetHitParagraphData(CaretManager.CurrentCaretOffset);
        //        var paragraphData = hitParagraphDataResult.ParagraphData;
        //        return paragraphData;
        //    }
        //}

        /// <summary>
        /// 设置段落属性
        /// </summary>
        /// <param name="paragraphIndex"></param>
        /// <param name="paragraphProperty"></param>
        public void SetParagraphProperty(int paragraphIndex, ParagraphProperty paragraphProperty)
        {
            ParagraphData paragraphData = ParagraphManager.GetParagraph(paragraphIndex);
            SetParagraphProperty(paragraphData, paragraphProperty);
        }

        /// <summary>
        /// 设置 <paramref name="caretOffset"/> 光标所在的段落的段落属性
        /// </summary>
        /// <param name="caretOffset"></param>
        /// <param name="paragraphProperty"></param>
        public void SetParagraphProperty(in CaretOffset caretOffset, ParagraphProperty paragraphProperty)
        {
            ParagraphData paragraphData = ParagraphManager.GetHitParagraphData(caretOffset).ParagraphData;
            SetParagraphProperty(paragraphData, paragraphProperty);
        }

        private void SetParagraphProperty(ParagraphData paragraphData, ParagraphProperty paragraphProperty)
        {
            if (TextEditor.ShouldInsertUndoRedo)
            {
                // 加入撤销重做
                var oldValue = paragraphData.ParagraphProperty;
                var operation = new ParagraphPropertyChangeOperation(TextEditor, oldValue, paragraphProperty, paragraphData.Index);
                TextEditor.UndoRedoProvider.Insert(operation);
            }

            // todo 考虑带项目符号时，需要更多更多的范围
            // 例如当前文本是如下内容：
            // 1. 
            // 2.
            // 3.
            // 然后将 2. 的段落修改为其他项目符号，此时需要更新 3. 段落
            InternalDocumentChanging?.Invoke(this, EventArgs.Empty);
            paragraphData.SetParagraphProperty(paragraphProperty);
            InternalDocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 获取段落属性
        /// </summary>
        public ParagraphProperty GetParagraphProperty(int paragraphIndex)
        {
            return ParagraphManager.GetParagraph(paragraphIndex).ParagraphProperty;
        }

        /// <summary>
        /// 获取段落属性
        /// </summary>
        /// <param name="caretOffset"></param>
        /// <returns></returns>
        public ParagraphProperty GetParagraphProperty(in CaretOffset caretOffset)
            => ParagraphManager.GetHitParagraphData(caretOffset).ParagraphData.ParagraphProperty;

        #endregion

        #region RunProperty

        /// <summary>
        /// 获取当前文本的默认字符属性
        /// </summary>
        public IReadOnlyRunProperty CurrentRunProperty
        {
            private set
            {
                var oldValue = _currentRunProperty;
                _currentRunProperty = value;

                if (TextEditor.ShouldInsertUndoRedo)
                {
                    // 如果不在撤销恢复模式，那就记录一条
                    var operation =
                        new ChangeTextEditorDefaultTextRunPropertyValueOperation(TextEditor, value, oldValue);
                    TextEditor.InsertUndoRedoOperation(operation);
                }
            }
            get => _currentRunProperty;
        }

        private IReadOnlyRunProperty _currentRunProperty;

        /// <inheritdoc cref="P:LightTextEditorPlus.Core.Carets.CaretManager.CurrentCaretRunProperty"/>
        public IReadOnlyRunProperty CurrentCaretRunProperty
        {
            //private set => CaretManager.CurrentCaretRunProperty = value;
            //get => GetCurrentCaretRunProperty();
            get
            {
                // 获取当前光标的字符属性
                // 规则：
                //
                // 有 CaretManager.CurrentCaretRunProperty 时，返回此属性
                // 无任何文本字符时，获取段落和文档的属性
                // 有字符时，非段首则获取字符前一个字符的属性；段首则获取段落的字符属性
                if (CaretManager.CurrentCaretRunProperty is not null)
                {
                    // 有 CaretManager.CurrentCaretRunProperty 时，返回此属性
                    return CaretManager.CurrentCaretRunProperty;
                }

                IReadOnlyRunProperty currentCaretRunProperty;
                if (CharCount == 0)
                {
                    // 无任何文本字符时，获取段落和文档的属性
                    currentCaretRunProperty = CurrentRunProperty;
                }
                else
                {
                    var hitParagraphDataResult = ParagraphManager.GetHitParagraphData(CaretManager.CurrentCaretOffset);
                    var paragraphData = hitParagraphDataResult.ParagraphData;
                    // 为了复用 HitParagraphDataResult 内容，不调用 CurrentCaretParagraphData 属性
                    //paragraphData = CurrentCaretParagraphData;

                    // 当前光标是否在段首
                    if (hitParagraphDataResult.HitOffset.Offset == 0)
                    {
                        // 如果是在段首（当前只判断是文档开头）
                        // 则取此光标之后一个字符的，如果光标之后没有字符了，那只能使用默认的
                        if (paragraphData.CharCount > 0)
                        {
                            // 取段落首个字符的字符属性
                            var charData = paragraphData.GetCharData(new ParagraphCharOffset(0));
                            currentCaretRunProperty = charData.RunProperty;
                        }
                        else
                        {
                            // 这个段落没有字符，那就使用段落默认字符属性，段落没有默认的字符属性，那就采用文档属性
                            currentCaretRunProperty =
                                paragraphData.ParagraphProperty.ParagraphStartRunProperty ??
                                CurrentRunProperty;
                        }
                    }
                    else
                    {
                        // 不在段首，那就取光标前一个字符的文本属性
                        var paragraphCharOffset = new ParagraphCharOffset(hitParagraphDataResult.HitOffset.Offset - 1);
                        var charData = paragraphData.GetCharData(paragraphCharOffset);
                        currentCaretRunProperty = charData.RunProperty;
                    }
                }

                return currentCaretRunProperty;
            }
        }

        #endregion

        #endregion

        #region 公开属性

        /// <summary>
        /// 文档的字符数量。段落之间，使用 `\r\n` 换行符，加入计算为两个字符。包含项目符号
        /// </summary>
        public int CharCount
        {
            get
            {
                var sum = 0;
                foreach (var paragraphData in DocumentRunEditProvider.ParagraphManager.GetParagraphList())
                {
                    sum += paragraphData.CharCount;
                    // 加上换行符的字符
                    sum += ParagraphData.DelimiterLength;
                }

                if (sum > 0)
                {
                    // 证明存在一段以上，那减去最后一段多加上的换行符
                    sum -= ParagraphData.DelimiterLength;
                }

                return sum;
            }
        }

        #endregion

        #region 公开方法

        #region RunProperty

        /// <summary>
        /// 设置当前文本的默认字符属性
        /// </summary>
        /// <typeparam name="T">实际业务端使用的字符属性类型</typeparam>
        public void SetDefaultTextRunProperty<T>(Action<T> config) where T : IReadOnlyRunProperty
        {
            var platformRunPropertyCreator = TextEditor.PlatformProvider.GetPlatformRunPropertyCreator();
            CurrentRunProperty =
                platformRunPropertyCreator.BuildNewProperty(property => config((T) property),
                    CurrentRunProperty);
        }

        /// <summary>
        /// 设置当前光标的字符属性。在光标切走之后，自动失效
        /// </summary>
        /// <typeparam name="T">实际业务端使用的字符属性类型</typeparam>
        /// <param name="config"></param>
        public void SetCurrentCaretRunProperty<T>(Action<T> config) where T : IReadOnlyRunProperty
        {
            // 先获取当前光标的字符属性吧
            IReadOnlyRunProperty currentCaretRunProperty = CurrentCaretRunProperty;

            var platformRunPropertyCreator = TextEditor.PlatformProvider.GetPlatformRunPropertyCreator();
            CaretManager.CurrentCaretRunProperty = platformRunPropertyCreator.BuildNewProperty(
                property => config((T) property),
                currentCaretRunProperty);
        }

        /// <summary>
        /// 将设置一段范围内的文本的字符属性。如传入的 <paramref name="selection"/> 为空，且当前也没有选择内容，则仅修改当前光标的字符属性
        /// </summary>
        /// <typeparam name="T">实际业务端使用的字符属性类型</typeparam>
        /// <param name="config"></param>
        /// <param name="selection">如为空，则采用当前选择内容。如当前也没有选择内容，则仅修改当前光标的字符属性</param>
        public void SetRunProperty<T>(Action<T> config, Selection? selection) where T : IReadOnlyRunProperty
        {
            // 如为空，则采用当前选择内容
            selection ??= CaretManager.CurrentSelection;

            // 如当前也没有选择内容，则仅修改当前光标的字符属性
            if (selection.Value.IsEmpty)
            {
                // 设置当前的光标样式，没有修改文档内容，不需要触发文档变更事件
                if (selection.Value.FrontOffset != CaretManager.CurrentCaretOffset)
                {
                    // 这是在搞什么呀？对一个没有选择内容的地方设置文本字符属性
                    // 这里也不合适抛出异常，可以忽略
                    // 文本库允许你这么做，但是这么做，文本库啥都不干
                    TextEditor.Logger.LogDebug($"[DocumentManager][SetRunProperty] selection is empty, but not equals CurrentCaretOffset. 传入 selection 范围的长度是 0 且起点不等于当前光标坐标。将不会修改任何文本字符属性");
                }
                else
                {
                    SetCurrentCaretRunProperty(config);
                }
            }
            else
            {
                // 修改属性，需要触发样式变更，也就是文档变更
                InternalDocumentChanging?.Invoke(this, EventArgs.Empty);
                // 表示最后一个更改之后的文本字符属性，为了提升性能，不让每个文本字符属性都需要执行 config 函数
                // 用来判断如果相邻两个字符的字符属性是相同的，就可以直接复用，不需要重新执行 config 函数创建新的字符属性对象
                IReadOnlyRunProperty? lastChangedRunProperty = null;
                CharData? lastCharData = null;

                var runList = new ImmutableRunList();

                foreach (var charData in GetCharDataRange(selection.Value))
                {
                    Debug.Assert(charData.CharLayoutData != null, "能够从段落里获取到的，一定是存在在段落里面，因此此属性一定不为空");
                    if (charData.IsLineBreakCharData)
                    {
                        // 是换行的话，需要加上换行的字符
                        runList.Add(new LineBreakRun(lastChangedRunProperty));
                        continue;
                    }

                    IReadOnlyRunProperty currentRunProperty;

                    if (ReferenceEquals(charData.RunProperty, lastCharData?.RunProperty))
                    {
                        // 如果相邻两个 CharData 采用相同的字符属性，那就不需要再创建了，稍微提升一点性能和减少内存占用
                        Debug.Assert(lastChangedRunProperty != null, "当前字符和上一个字符的字符属性相同，证明存在上一个字符，证明存在上一个字符属性");
                        // ReSharper disable once RedundantSuppressNullableWarningExpression
                        currentRunProperty = lastChangedRunProperty!;
                    }
                    else
                    {
                        currentRunProperty = charData.RunProperty;

                        var platformRunPropertyCreator = TextEditor.PlatformProvider.GetPlatformRunPropertyCreator();
                        currentRunProperty = platformRunPropertyCreator.BuildNewProperty(property => config((T) property), currentRunProperty);
                    }

                    runList.Add(new SingleCharImmutableRun(charData.CharObject, currentRunProperty));

                    lastChangedRunProperty = currentRunProperty;
                    lastCharData = charData;
                }

                ReplaceCore(selection.Value, runList);

                // 只触发文档变更，不需要修改光标

                InternalDocumentChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 是否给定范围内的字符属性都满足 <paramref name="predicate"/> 条件。如传入的 <paramref name="selection"/> 为空，且当前也没有选择内容，则仅判断当前光标的字符属性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate"></param>
        /// <param name="selection"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool IsAnyRunProperty<T>(Predicate<T> predicate, Selection? selection = null) where T : IReadOnlyRunProperty
        {
            selection ??= CaretManager.CurrentSelection;
            if (selection.Value.IsEmpty)
            {
                // 获取当前光标的属性即可
                return predicate((T) CurrentCaretRunProperty);
            }
            else
            {
                foreach (var runProperty in GetDifferentRunPropertyRange(selection.Value))
                {
                    if (predicate((T) runProperty))
                    {
                        // 如果满足条件，那就继续判断下一个
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        #endregion

        /// <summary>
        /// 获取给定的 <paramref name="selection"/> 范围有多少不连续相同的字符属性
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        internal IEnumerable<IReadOnlyRunProperty> GetDifferentRunPropertyRange(in Selection selection)
        {
            var runPropertyRange = GetRunPropertyRange(selection);

            return GetDifferentRunPropertyRangeInner(runPropertyRange);

            static IEnumerable<IReadOnlyRunProperty> GetDifferentRunPropertyRangeInner(
                IEnumerable<IReadOnlyRunProperty> runPropertyRange)
            {
                IReadOnlyRunProperty? lastRunProperty = null;

                foreach (var readOnlyRunProperty in runPropertyRange)
                {
                    if (!ReferenceEquals(lastRunProperty, readOnlyRunProperty))
                    {
                        lastRunProperty = readOnlyRunProperty;
                        yield return readOnlyRunProperty;
                    }
                }
            }
        }

        /// <summary>
        /// 获取给定范围的字符属性
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        public IEnumerable<IReadOnlyRunProperty> GetRunPropertyRange(in Selection selection)
        {
            return GetCharDataRange(selection).Select(t => t.RunProperty);
        }

        /// <summary>
        /// 给定选择范围内的所有字符属性
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        public IEnumerable<CharData> GetCharDataRange(in Selection selection)
        {
            // 获取方法：
            // 1. 先获取命中到的首段，取首段的字符
            // 2. 如果首段不够，则获取后续段落，每个段落获取之前，都添加用来表示换行的字符
            if (selection.IsEmpty)
            {
                return Enumerable.Empty<CharData>();
            }

            var result = ParagraphManager.GetHitParagraphData(selection.FrontOffset);
            var remainingLength = selection.Length;

            var takeCount = Math.Min(result.ParagraphData.CharCount - result.HitOffset.Offset, remainingLength);

            var charDataList = result.ParagraphData.ToReadOnlyListSpan(new ParagraphCharOffset(result.HitOffset.Offset),
                takeCount);
            remainingLength -= takeCount;
            IEnumerable<CharData> charDataListResult = charDataList;

            // 继续获取后续段落，如果首段不够的话
            var lastParagraphData = result.ParagraphData;
            var list = ParagraphManager.GetParagraphList();
            for (int i = result.ParagraphData.Index + 1; i < list.Count && remainingLength > 0; i++)
            {
                // 加上段末换行符
                remainingLength -= ParagraphData.DelimiterLength;
                charDataListResult =
                    charDataListResult.Concat(new[] { lastParagraphData.GetLineBreakCharData() });

                var currentParagraphData = list[i];
                takeCount = Math.Min(currentParagraphData.CharCount, remainingLength);
                charDataListResult =
                    charDataListResult.Concat(currentParagraphData.ToReadOnlyListSpan(new ParagraphCharOffset(0), takeCount));
                remainingLength -= takeCount;
                lastParagraphData = currentParagraphData;
            }

            return charDataListResult;
        }

        /// <summary>
        /// 获取不可变的文本块列表。如考虑性能，请优先选择 <see cref="GetCharDataRange"/> 方法
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        public IImmutableRunList GetImmutableRunList(in Selection selection)
        {
            var charDataRange = GetCharDataRange(selection);
            IImmutableRunList list = new ImmutableRunList(charDataRange.Select(t => new SingleCharImmutableRun(t.CharObject, t.RunProperty)));
            return list;
        }

        #region 编辑

        /// <summary>
        /// 追加一段文本
        /// </summary>
        /// 由于追加属于性能优化的高级用法，默认不开放给业务层调用
        internal void AppendText(IImmutableRun run)
        {
            TextEditor.AddLayoutReason(nameof(AppendText));

            InternalDocumentChanging?.Invoke(this, EventArgs.Empty);

            // 设置光标到文档最后，再进行追加。设置光标到文档最后之后，可以自动获取当前光标下的文本字符属性
            var oldCharCount = CharCount;
            CaretManager.CurrentCaretOffset = new CaretOffset(oldCharCount);

            DocumentRunEditProvider.Append(run);

            var newCharCount = CharCount;
            CaretManager.CurrentCaretOffset = new CaretOffset(newCharCount);

            if (TextEditor.ShouldInsertUndoRedo)
            {
                var oldSelection = new Selection(new CaretOffset(oldCharCount), length: 0);
                IImmutableRunList? oldRun = null;
                var newSelection = new Selection(new CaretOffset(oldCharCount), new CaretOffset(newCharCount));

                // 不能直接使用 run 的内容，因为 run 里可能没有写好使用的样式。因此需要获取实际插入的内容，从而获取到实际的插入带样式文本
                var newRun = GetImmutableRunList(newSelection);
                var textChangeOperation = new TextChangeOperation(TextEditor, oldSelection, oldRun, newSelection, newRun);
                TextEditor.UndoRedoProvider.Insert(textChangeOperation);
            }

            InternalDocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 编辑和替换文本
        /// </summary>
        /// <param name="selection"></param>
        /// <param name="run"></param>
        internal void EditAndReplaceRun(in Selection selection, IImmutableRun? run)
        {
            TextEditor.AddLayoutReason("DocumentManager.EditAndReplaceRun");

            EditAndReplaceRunInner(selection, run);
        }

        /// <summary>
        /// 编辑和替换文本
        /// </summary>
        private void EditAndReplaceRunInner(in Selection selection, IImmutableRun? run)
        {
            if (run is TextRun textRun)
            {
                // 特别处理 TextRun 情况，仅仅只是为了提升几乎可以忽略的性能
                EditAndReplaceRunListInner(selection, textRun);
            }
            else if (run is null)
            {
                EditAndReplaceRunListInner(selection, null);
            }
            else
            {
                EditAndReplaceRunListInner(selection, new SingleImmutableRunList(run));
            }
        }

        /// <summary>
        /// 编辑和替换文本
        /// </summary>
        /// <param name="selection"></param>
        /// <param name="run"></param>
        internal void EditAndReplaceRunList(in Selection selection, IImmutableRunList? run)
        {
            TextEditor.AddLayoutReason("DocumentManager.EditAndReplaceRunList");

            EditAndReplaceRunListInner(selection, run);
        }

        /// <summary>
        /// 编辑和替换文本
        /// </summary>
        private void EditAndReplaceRunListInner(in Selection selection, IImmutableRunList? run)
        {
            InternalDocumentChanging?.Invoke(this, EventArgs.Empty);
            // 这里只处理数据变更，后续渲染需要通过 InternalDocumentChanged 事件触发

            ReplaceCore(selection, run);

            var caretOffset = new CaretOffset(selection.FrontOffset.Offset + (run?.CharCount ?? 0));
            CaretManager.CurrentCaretOffset = caretOffset;

            InternalDocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ReplaceCore(in Selection selection, IImmutableRunList? run)
        {
            if (selection.BehindOffset.Offset > CharCount)
            {
                throw new SelectionOutOfRangeException(TextEditor, selection, CharCount);
            }

            if (TextEditor.ShouldInsertUndoRedo)
            {
                // 需要插入撤销恢复，先获取旧的数据，再替换，再获取新的数据
                var oldSelection = selection;
                // 获取旧的数据
                IImmutableRunList oldList = GetImmutableRunList(oldSelection);

                // 执行替换，需要替换之后才能获取到新的数据
                DocumentRunEditProvider.Replace(selection, run);

                // 获取新的数据
                var newSelection = new Selection(selection.FrontOffset, run?.CharCount ?? 0);
                var newList = GetImmutableRunList(newSelection);

                var textChangeOperation = new TextChangeOperation(TextEditor, oldSelection, oldList, newSelection, newList);
                TextEditor.UndoRedoProvider.Insert(textChangeOperation);
            }
            else
            {
                // 不需要插入撤销恢复，那就直接替换
                DocumentRunEditProvider.Replace(selection, run);
            }
        }

        #endregion

        #region 删除

        /// <summary>
        /// 退格删除
        /// </summary>
        /// <param name="count"></param>
        internal void Backspace(int count = 1)
        {
            // 退格键时，有选择就删除选择内容。没选择就删除给定内容
            var caretManager = CaretManager;
            var currentSelection = caretManager.CurrentSelection;
            if (currentSelection.IsEmpty)
            {
                var currentCaretOffset = caretManager.CurrentCaretOffset;
                if (currentCaretOffset.Offset == 0)
                {
                    // 放在文档最前，不能退格
                    return;
                }

                var offset = currentCaretOffset.Offset - count;
                offset = Math.Max(0, offset);
                var length = currentCaretOffset.Offset - offset;
                currentSelection = new Selection(new CaretOffset(offset), length);
            }

            TextEditor.AddLayoutReason(nameof(Backspace) + "退格删除");
            RemoveInner(currentSelection);
        }

        /// <summary>
        /// 帝利特删除
        /// </summary>
        /// <param name="count"></param>
        internal void Delete(int count = 1)
        {
            // 有选择就删除选择内容。没选择就删除光标之后的内容
            var caretManager = CaretManager;
            var currentSelection = caretManager.CurrentSelection;
            if (currentSelection.IsEmpty)
            {
                var currentCaretOffset = caretManager.CurrentCaretOffset;
                var charCount = CharCount;
                if (currentCaretOffset.Offset == charCount)
                {
                    // 光标在文档最后，不能使用帝利特删除
                    return;
                }

                // 获取可以删除的字符数量
                var remainCount = charCount - currentCaretOffset.Offset;
                var deleteCount = Math.Min(count, remainCount);
                currentSelection = new Selection(currentCaretOffset, deleteCount);
            }

            TextEditor.AddLayoutReason(nameof(Delete) + "帝利特删除");
            RemoveInner(currentSelection);
        }

        internal void Remove(in Selection selection)
        {
            if (selection.IsEmpty)
            {
                return;
            }

            TextEditor.AddLayoutReason(nameof(Remove) + "删除范围文本");
            RemoveInner(selection);
        }

        private void RemoveInner(in Selection selection)
        {
            // 删除范围内的文本，等价于将范围内的文本替换为空
            EditAndReplaceRunInner(selection, null);
        }

        #endregion

        #endregion

        #region UndoRedo

        // 这里的方法只允许撤销恢复调用

        /// <summary>
        /// 从撤销重做里面设置回默认的文本字符属性
        /// </summary>
        /// <param name="runProperty"></param>
        internal void SetDefaultTextRunPropertyByUndoRedo(IReadOnlyRunProperty runProperty)
        {
            TextEditor.VerifyInUndoRedoMode();
            CurrentRunProperty = runProperty;
        }

        #endregion
    }
}