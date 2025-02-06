using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;

//using WatchDog.Core.Context;
//using WatchDog.Service.Contexts;

namespace WatchDog.Uno.WatchDogClient;

internal class WatchDogProvider
{
    public WatchDogProvider(string host)
    {
        Host = host;

        _httpClient = CreateHttpClient();
    }

    private readonly HttpClient _httpClient;

    private HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(Host);
        return httpClient;
    }

    public string Host { get; }

    public async Task<FeedDogResponse?> FeedAsync(FeedDogInfo feedDogInfo)
    {
        var request = new FeedDogRequest(feedDogInfo);
        var response = await _httpClient.PostAsJsonAsync("Dog/Feed", request);
        return await response.Content.ReadFromJsonAsync<FeedDogResponse>();
    }

    //public async Task<GetWangResponse?> GetWangAsync(GetWangInfo getWangInfo)
    //{
    //    var request = new GetWangRequest(getWangInfo);
    //    var response = await _httpClient.PostAsJsonAsync("Dog/Wang", request);
    //    return await response.Content.ReadFromJsonAsync<GetWangResponse>();
    //}

    //public async Task<MuteResponse?> MuteAsync(MuteInfo muteInfo)
    //{
    //    var request = new MuteRequest(muteInfo);
    //    var response = await _httpClient.PostAsJsonAsync("Dog/Mute", request);
    //    return await response.Content.ReadFromJsonAsync<MuteResponse>();
    //}

    public static WatchDogProvider? CreateFromConfiguration()
    {
        var defaultConfigurationFile = @"C:\lindexi\Configuration\WatchDog.coin";
        var configurationFile = defaultConfigurationFile;

        if (!System.IO.File.Exists(configurationFile))
        {
            return null;
        }

        var appConfigurator = ConfigurationFactory.FromFile(configurationFile, RepoSyncingBehavior.Static).CreateAppConfigurator();
        var host = appConfigurator.Of<WatchDogConfiguration>().Host;
        if (string.IsNullOrEmpty(host))
        {
            return null;
        }

        return new WatchDogProvider(host);
    }

    class WatchDogConfiguration : Configuration
    {
        public string Host
        {
            set => SetValue(value);
            get => GetString();
        }
    }
}

/// <summary>
/// ι����Ϣ
/// </summary>
/// <param name="Name">����</param>
/// <param name="Status">״̬</param>
/// <param name="Id">Id�ţ���Ϊ�����Զ�����</param>
/// <param name="DelaySecond">ι��������ӳ�ʱ�䣬����ʱ��ͱ���ҧ</param>
/// <param name="MaxDelayCount">���Ĵ�����һ���� 1 ��ֵ</param>
/// <param name="NotifyIntervalSecond">֪ͨ�ļ��ʱ��</param>
/// <param name="NotifyMaxCount">����֪ͨ������Ĭ��������֪ͨ</param>
/// <param name="MaxCleanTimeSecond">��������˶�ö�û����Ӧ�������ι����Ϣ��Ĭ���� 7 ��</param>
public record FeedDogInfo
(
    string Name,
    string Status,
    string? Id = null,
    uint DelaySecond = 60,
    uint MaxDelayCount = 1,
    uint NotifyIntervalSecond = 60 * 30,
    int NotifyMaxCount = -1,
    int MaxCleanTimeSecond = 60 * 60 * 24 * 7
);

public record FeedDogRequest(FeedDogInfo FeedDogInfo);

public record FeedDogResponse(FeedDogResult FeedDogResult);

/// <summary>
/// ι���Ľ�������ص���ι������Ϣ
/// </summary>
/// <param name="Id"></param>
/// <param name="DelaySecond"></param>
/// <param name="MaxDelayCount"></param>
/// <param name="NotifyIntervalSecond"></param>
/// <param name="NotifyMaxCount"></param>
/// <param name="RegisterTime">ע��ʱ��</param>
/// <param name="IsRegister">��һ���Ƿ����ע��ģ��״�ι��</param>
public record FeedDogResult(string Id, uint DelaySecond, uint MaxDelayCount, uint NotifyIntervalSecond, int NotifyMaxCount, DateTimeOffset RegisterTime, bool IsRegister);