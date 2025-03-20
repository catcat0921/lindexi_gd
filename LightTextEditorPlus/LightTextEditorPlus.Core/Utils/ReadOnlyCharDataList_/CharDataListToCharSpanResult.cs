using System;
using System.Buffers;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Utils;

/// <summary>
/// �� <see cref="CharData"/> �б�ת��Ϊ <see cref="char"/> �б�Ľ������Ҫ�����ͷţ�������黹������
/// </summary>
public readonly struct CharDataListToCharSpanResult : IDisposable
{
    internal CharDataListToCharSpanResult(char[] buffer, int length, ArrayPool<char> arrayPool)
    {
        _buffer = buffer;
        _length = length;
        _arrayPool = arrayPool;
    }

    /// <summary>
    /// ת������ַ�����
    /// </summary>
    public ReadOnlySpan<char> CharSpan => _buffer.AsSpan(0, _length);

    private readonly char[] _buffer;
    private readonly int _length;
    private readonly ArrayPool<char> _arrayPool;

    /// <inheritdoc />
    public void Dispose()
    {
        _arrayPool.Return(_buffer);
    }
}