
using System.Net;
using Microsoft.ML.OnnxRuntimeGenAI;

using System.Text;

var folder = @"C:\lindexi\Phi3\directml-int4-awq-block-128\";
if (!Directory.Exists(folder))
{
    folder = Path.GetFullPath(".");
}

var model = new Model(folder);
var tokenizer = new Tokenizer(model);

var semaphoreSlim = new SemaphoreSlim(initialCount: 1, maxCount: 1);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5017");

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapPost("/Chat", async (ChatRequest request, HttpContext context) =>
{
    var response = context.Response;
    response.StatusCode = (int) HttpStatusCode.OK;
    await response.StartAsync();
    await semaphoreSlim.WaitAsync();

    try
    {
        var streamWriter = new StreamWriter(response.Body);

        var prompt = request.Prompt;

        var generatorParams = new GeneratorParams(model);

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
    }
    finally
    {
        semaphoreSlim.Release();
        await response.CompleteAsync();
    }
});

app.Run();

record ChatRequest(string Prompt)
{
}