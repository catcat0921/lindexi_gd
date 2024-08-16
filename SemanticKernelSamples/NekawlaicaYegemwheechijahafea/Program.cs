// See https://aka.ms/new-console-template for more information

using Microsoft.KernelMemory;

// 这里的演示代码需要用到 AzureAI 的支持，需要提前申请好，申请地址：https://aka.ms/oai/access

var endpoint = "https://lindexi.openai.azure.com/"; // 请换成你的地址
var apiKey = File.ReadAllText(@"C:\lindexi\CA\Key"); // 请换成你的密钥

var kernel = new KernelMemoryBuilder()
    .WithSimpleFileStorage("Folder")
    .WithoutTextGenerator()
    .WithAzureOpenAITextEmbeddingGeneration(new AzureOpenAIConfig()
    {
        Endpoint = endpoint,
        APIKey = apiKey,
        Deployment = "Embedding",
        APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
        Auth = AzureOpenAIConfig.AuthTypes.APIKey
    })
    .Build();
await kernel.ImportTextAsync("本文记录在 WPF 项目里面设置 IncludePackageReferencesDuringMarkupCompilation 属性为 False 导致了项目所安装的分析器不能符合预期工作 设置 IncludePackageReferencesDuringMarkupCompilation 属性为 false 将配置 WPF 在构建 XAML 过程中创建的 tmp.csproj 过程中将不引用依赖的 nuget 包。分析器默认也是通过 nuget 包方式安装的，这就导致了分析器项目没有被 tmp.csproj 项目正确使用到 如果项目里面有代码依赖分析器生成的影响语义的代码，那这部分代码将会构建不通过");

await kernel.ImportTextAsync("在 dotnet 6 时，官方为了适配好 Source Generators 功能，于是默认就将 WPF 的 XAML 构建过程中，引入第三方库的 cs 文件，这个功能默认设置为开启。刚好源代码包为了修复在使用 dotnet 6 SDK 之前，在 WPF 的构建 XAML 过程中，不包含第三方库的代码文件，从而使用黑科技将源代码包加入到 WPF 构建 XAML 中。在 VisualStudio 升级到 2022 版本，或者是升级 dotnet sdk 到 dotnet 6 版本，将会更新构建调度，让源代码包里的代码文件被加入两次，从而构建失败\r\n构建失败的提示如下\r\n\r\n```\r\nC:\\Program Files\\dotnet\\sdk\\6.0.101\\Sdks\\Microsoft.NET.Sdk\\targets\\Microsoft.NET.Sdk.DefaultItems.Shared.targets(190,5): error NETSDK1022: 包含了重复的“Compile”项。.NET SDK 默认包含你项目目录中的“Compile”项。可从项目文件中删除这些项；如果希望将其显式包含在项目文件中，可将“EnableDefaultCompileItems”属性设置为“false”。有关详细信息，请参阅 https://aka.ms/sdkimplicititems。重复项为: \r\n```重复的原因是 WPF 在 .NET SDK 里修复了在 XAML 构建过程中，没有引用 NuGet 包里面的文件。而源代码包许多都是在此修复之前打出来的，源代码包为了修复在 XAML 里面没有引用文件，就强行加上修复逻辑引用文件。而在 dotnet 6 修复了之后，自然就会导致引用了多次 修复方法很简单，在不更改源代码包的前提下，可以在 csproj 项目文件里加入以下代码```xml\r\n    <IncludePackageReferencesDuringMarkupCompilation>False</IncludePackageReferencesDuringMarkupCompilation>\r\n```");

await kernel.ImportTextAsync("默认情况下的 WPF 项目都是带 -windows 的 TargetFramework 方式，但有一些项目是不期望加上 -windows 做平台限制的，本文将介绍如何实现不添加 -windows 而引用 WPF 框架 对于一些特殊的项目来说，也许只是在某些模块下期望引用 WPF 的某些类型，而不想自己的项目限定平台。这时候可以去掉 `-windows` 换成 FrameworkReference 的方式 通过 `<FrameworkReference Include=\"Microsoft.WindowsDesktop.App.WPF\" />` 即可设置对 WPF 程序集的引用，也就是仅仅只是将 WPF 的程序集取出来当成引用，而不是加上 WPF 的负载");

await kernel.ImportTextAsync("dotnet 如何访问到 UNO 框架里面的 internal 不公开成员? 核心原理是基于 UNO 框架里面的 InternalsVisibleToAttribute 程序集特性，指定给到 SamplesApp 等程序集可见。因此只需要新建一个程序集，设置 AssemblyName 为 SamplesApp 即可");

var searchResult = await kernel.SearchAsync("如何访问 UNO 不公开成员");

if (searchResult.NoResult)
{
    Console.WriteLine("没有找到相关项");
    return;
}

foreach (var citation in searchResult.Results)
{
    // 大部分情况下，只取第一项
    foreach (var partition in citation.Partitions)
    {
        Console.WriteLine($"关联性： {partition.Relevance:0.00} 内容： {partition.Text}");
    }
}

// 提问会提炼答案，由于没有配置，将会失败
var answer = await kernel.AskAsync("为什么分析器和源代码冲突");

Console.WriteLine("Hello, World!");
