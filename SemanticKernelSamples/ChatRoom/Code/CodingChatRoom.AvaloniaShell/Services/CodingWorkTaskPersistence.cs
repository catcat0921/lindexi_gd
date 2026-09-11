using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed record CodingWorkTaskRecord
{
    public Guid WorkTaskId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string? WorkspacePath { get; init; }

    public string? CurrentSessionId { get; init; }
}

internal sealed record CodingWorkTaskDocument
{
    public int SchemaVersion { get; init; } = 1;

    public Guid? ActiveWorkTaskId { get; init; }

    public IReadOnlyList<CodingWorkTaskRecord> Tasks { get; init; } = [];
}

internal sealed class CodingWorkTaskStore
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly FileInfo _file;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public CodingWorkTaskStore(FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);
        _file = file;
    }

    public async Task<CodingWorkTaskDocument?> LoadAsync(CancellationToken cancellationToken = default)
    {
        _file.Refresh();
        if (!_file.Exists)
        {
            return null;
        }

        await using FileStream stream = _file.OpenRead();
        CodingWorkTaskDocument? document = await JsonSerializer.DeserializeAsync<CodingWorkTaskDocument>(
            stream,
            s_jsonOptions,
            cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            throw new JsonException("工作任务配置文件内容为空。");
        }

        if (document.SchemaVersion != 1)
        {
            throw new InvalidOperationException($"不支持的工作任务配置版本：{document.SchemaVersion}。");
        }

        return document;
    }

    public async Task SaveAsync(CodingWorkTaskDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(_file.DirectoryName!);
            string temporaryPath = _file.FullName + ".tmp";
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, document, s_jsonOptions, cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, _file.FullName, overwrite: true);
            _file.Refresh();
        }
        finally
        {
            _writeGate.Release();
        }
    }
}
