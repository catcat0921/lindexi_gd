using StackExchange.Redis.Extensions.Core.Abstractions;
using WatchDog.Service.Contexts;

namespace WatchDog.Service.Frameworks;

/// <summary>
/// ���� Redis �����豸�ṩ��
/// </summary>
public class RedisMasterHostProvider : IMasterHostProvider, IDisposable
{
    public RedisMasterHostProvider(IRedisClient redisClient, ISelfHostProvider selfHostProvider, IMasterHostStatusChecker masterHostStatusChecker)
    {
        _redisClient = redisClient;
        _selfHostProvider = selfHostProvider;
        _masterHostStatusChecker = masterHostStatusChecker;
    }

    private readonly IRedisClient _redisClient;

    private readonly ISelfHostProvider _selfHostProvider;
    private readonly IMasterHostStatusChecker _masterHostStatusChecker;

    private SemaphoreSlim SemaphoreSlim { get; } = new SemaphoreSlim(1);

    public async Task<MasterHostResult> GetMasterHostAsync()
    {
        // һ������ֻ��ͬʱ��һ���߳���ִ�У����ٸ��Ӷ�
        await SemaphoreSlim.WaitAsync();
        try
        {
            var result = await GetMasterHostAsyncInner();
            _cacheMasterHost = result.MasterHost;
            if (result.SelfIsMaster)
            {
                _ = UpdateSelfMaterAsync(result.MasterHost);
            }
            return result;
        }
        finally
        {
            SemaphoreSlim.Release();
        }
    }

    private const string RedisLockKey = "WatchDog-MasterLock-4ACF7B3F-222D-469C-B99D-5E3966FFB422";
    private string? _selfHost;

    private async Task<MasterHostResult> GetMasterHostAsyncInner()
    {
        _selfHost ??= await _selfHostProvider.GetSelfHostAsync();
        var selfHost = _selfHost;
        // �Ȼ�ȡһ�飬���ִ����ˣ����Է���һ�¡�����ܹ�ͨ����֤�����豸����
        // ������ִ��ڵľ����Լ������Լ��������豸

        for (int i = 0; i < 1000; i++)
        {
            var (success, masterHost) = await TryGetMasterHostAsync();
            if (success)
            {
                if (masterHost == selfHost)
                {
                    // ��������Լ����ǾͲ���Ҫ���ж��Ƿ������
                    return new MasterHostResult(SelfIsMaster: true, selfHost);
                }
                else
                {
                    // �ж�һ���Ƿ����
                    var enable = await _masterHostStatusChecker.CheckMasterHostEnableAsync(masterHost);
                    if (enable)
                    {
                        return new MasterHostResult(SelfIsMaster: false, masterHost);
                    }
                    else
                    {
                        // �����ã��Ǿͼ������µ�ע���߼�
                    }
                }
            }
            else
            {
                // û��ȡ�ɹ������������ע���߼�
            }

            // û�����豸���Ǿͳ����Լ�ע��
            await _redisClient.Db0.AddAsync(RedisLockKey, selfHost, TimeSpan.FromHours(1));

            // ע��֮�󣬵�һ�£��ٴγ��Ի�ȡ
            await Task.Delay(100);
        }

        throw new InvalidOperationException();

        async Task<(bool Success, string MasterHost)> TryGetMasterHostAsync()
        {
            var masterHost = await _redisClient.Db0.GetAsync<string>(RedisLockKey);
            if (string.IsNullOrEmpty(masterHost))
            {
                // ���������豸
                return (false, string.Empty);
            }

            if (string.Equals(masterHost, _cacheMasterHost))
            {
                // �ͻ������ͬ��������̷��أ�����Ҫ�ȴ�
                return (true, masterHost);
            }

            for (int i = 0; i < 1000; i++)
            {
                await Task.Delay(300);
                var host = await _redisClient.Db0.GetAsync<string>(RedisLockKey);
                if (string.IsNullOrEmpty(host))
                {
                    return (false, string.Empty);
                }

                if (host == masterHost)
                {
                    // ���λ�ȡ��ͬ������֤�������豸
                    return (true, host);
                }
                else
                {
                    masterHost = host;
                }
            }

            return (false, string.Empty);
        }
    }

    private string? _cacheMasterHost;

    /// <summary>
    /// �����Լ������豸��Ϣ����ֹ Redis ����
    /// </summary>
    /// <param name="selfHost"></param>
    /// <returns></returns>
    private async Task UpdateSelfMaterAsync(string selfHost)
    {
        if (_running)
        {
            return;
        }

        _running = true;

        try
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                await _redisClient.Db0.AddAsync(RedisLockKey, selfHost, TimeSpan.FromHours(1));
            }
        }
        finally
        {
            _running = false;
        }
    }

    private bool _running;

    public void Dispose()
    {
        SemaphoreSlim.Dispose();
    }
}