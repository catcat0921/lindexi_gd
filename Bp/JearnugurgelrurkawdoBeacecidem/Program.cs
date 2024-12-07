
using System.Net;
using Microsoft.ML.OnnxRuntimeGenAI;

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;

var folder = @"C:\lindexi\Phi3\directml-int4-awq-block-128\";
if (!Directory.Exists(folder))
{
    folder = Path.GetFullPath(".");
}

Model model = new Model(folder);

var semaphoreSlim = new SemaphoreSlim(initialCount: 1, maxCount: 1);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5017");
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSimpleConsole());
// Add services to the container.

var logFile = "ChatLog.txt";
var chatSessionFolder = "ChatSession";
Directory.CreateDirectory(chatSessionFolder);

var app = builder.Build();

// Configure the HTTP request pipeline.

//Task.Run(async () =>
//{
//    var httpClient = new HttpClient();
//    var text = await httpClient.GetStringAsync("http://127.0.0.1:5017/Status");
//    var response = await httpClient.PostAsync("http://127.0.0.1:5017/Chat", JsonContent.Create(new ChatRequest("����")));
//});

int current = 0;
int total = 0;

app.MapGet("/Status", () => $"Current={current};Total={total}");

app.MapPost("/Chat", async (ChatRequest request, HttpContext context, [FromServices] ILogger<ChatSessionLogInfo> logger) =>
{
    var response = context.Response;
    response.StatusCode = (int) HttpStatusCode.OK;
    await response.StartAsync();
    await semaphoreSlim.WaitAsync();

    Interlocked.Increment(ref current);
    Interlocked.Increment(ref total);
    var sessionName = $"{DateTime.Now:yyyy-MM-dd_HHmmssfff}";

    try
    {
        var headerDictionary = context.Request.Headers;
        string traceId = string.Empty;
        if (headerDictionary.TryGetValue("X-APM-TraceId", out StringValues traceIdValue))
        {
            traceId = traceIdValue.ToString();
        }

        var streamWriter = new StreamWriter(response.Body);

        var prompt = request.Prompt;

        logger.LogInformation($"Session={sessionName};TraceId={traceId}\r\nPrompt={request.Prompt}");

        var generatorParams = new GeneratorParams(model);

        using var tokenizer = new Tokenizer(model);
        var sequences = tokenizer.Encode(prompt);

        generatorParams.SetSearchOption("max_length", 1024);
        generatorParams.SetInputSequences(sequences);
        generatorParams.TryGraphCaptureWithMaxBatchSize(1);

        using var tokenizerStream = tokenizer.CreateStream();
        using var generator = new Generator(model, generatorParams);

        var stringBuilder = new StringBuilder();

        while (!generator.IsDone())
        {
            generator.ComputeLogits();
            generator.GenerateNextToken();

            // ÿ��ֻ�����һ�� Token ֵ
            // ��Ҫ���� tokenizerStream �Ľ��뽫��תΪ����ɶ����ı�
            // ���ڲ���ÿһ�� Token ����Ӧһ���ʣ������Ҫ���� tokenizerStream ѹ�����ת����������ֱ�ӵ��� tokenizer.Decode ���������ߵ��� tokenizer.Decode ������ÿ�ζ�ȫ��ת��

            var text = Decode();

            // ��Щʱ����� decodeText ��һ�����ı�����Щʱ����һ������
            // ���ı��Ŀ���ԭ������Ҫ��� token �������һ������
            // �� tokenizerStream �ײ��Ѿ������������������������Ҫ��� Token �������һ�����ʵ�����£��Զ��ϲ����ڶ�� Token �м�� Token �����ؿ��ַ��������һ�� Token �ŷ�����ɵĵ���
            if (!string.IsNullOrEmpty(text))
            {
                stringBuilder.Append(text);
            }

            await streamWriter.WriteAsync(text);
            await streamWriter.FlushAsync();


            string? Decode()
            {
                // ����� tokenSequences ����������� sequences ������� Token ����
                ReadOnlySpan<int> tokenSequences = generator.GetSequence(0);
                // ȡ���һ�����н���Ϊ�ı�
                var decodeText = tokenizerStream.Decode(tokenSequences[^1]);

                //// ��ǰȫ�����ı�
                //var allText = tokenizer.Decode(tokenSequences);

                return decodeText;
            }
        }

        var responseText = stringBuilder.ToString();
        var contents = $"""
                        Session={sessionName}
                        TraceId={traceId}
                        ----------------
                        Request: 
                        {prompt}
                        ----------------
                        Response:
                        {responseText}
                        =================

                        """;
        await File.AppendAllTextAsync
        (
            logFile,
            contents
        );

        logger.LogInformation(contents);

        var chatSessionLogInfo = new ChatSessionLogInfo(prompt, responseText);
        var chatSessionLogInfoJson = JsonSerializer.Serialize(chatSessionLogInfo, new JsonSerializerOptions()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        var sessionLogFile = Path.Join(chatSessionFolder, $"{sessionName}_{Path.GetRandomFileName()}.txt");
        await File.WriteAllTextAsync(sessionLogFile, chatSessionLogInfoJson);
    }
    finally
    {
        semaphoreSlim.Release();
        await response.CompleteAsync();
        Interlocked.Decrement(ref current);
    }
});

app.Run();

record ChatRequest(string Prompt)
{
}

record ChatSessionLogInfo(string Request, string Response)
{
}