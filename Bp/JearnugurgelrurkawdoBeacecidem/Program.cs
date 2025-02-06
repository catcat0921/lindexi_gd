
using System.Net;
using Microsoft.ML.OnnxRuntimeGenAI;

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using WatchDog.Uno.WatchDogClient;
using System.Net.Sockets;

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
builder.Services.AddHttpClient();
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

app.MapPost("/Stable-Diffusion-proxy/sdapi/v1/txt2img", async (HttpContext context, [FromServices] HttpClient httpClient, [FromServices] ILogger<ChatSessionLogInfo> logger) =>
{
    logger.LogInformation("Stable-Diffusion-proxy");

    await context.Response.StartAsync();
    try
    {
        var recallHost = "http://127.0.0.1:56622";
        using var streamContent = new StreamContent(context.Request.Body);
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        using var httpResponseMessage = await httpClient.PostAsync($"{recallHost}/sdapi/v1/txt2img", streamContent);
        await httpResponseMessage.Content.CopyToAsync(context.Response.Body);
    }
    catch (Exception e)
    {
        logger.LogWarning(e, "Stable-Diffusion-proxy");
    }
    finally
    {
        await context.Response.CompleteAsync();
    }
});

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

_ = Task.Run(async () =>
{
    var watchDogProvider = WatchDogProvider.CreateFromConfiguration();
    string id = $"Phi3";
    string ip = "";
    try
    {
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            if (networkInterface.Supports(NetworkInterfaceComponent.IPv4))
            {
                var ipInterfaceProperties = networkInterface.
                    GetIPProperties();
                foreach (var unicastIpAddressInformation in ipInterfaceProperties.UnicastAddresses)
                {
                    if (unicastIpAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var address = unicastIpAddressInformation.Address.ToString();
                        ip += address + ";";
                    }
                }
            }
        }
    }
    catch
    {
       // ����
    }

    id = $"{id}-{ip}";

    while (true)
    {
        if (watchDogProvider is null)
        {
            return;
        }

        await watchDogProvider.FeedAsync(new FeedDogInfo("��������", $"Current={current};Total={total}", id));

        await Task.Delay(TimeSpan.FromSeconds(15));
    }
});

app.Run();

record ChatRequest(string Prompt)
{
}

record ChatSessionLogInfo(string Request, string Response)
{
}