namespace WatchDog.Service.Frameworks;

/// <summary>
/// �ٵ��豸��Ϣ�ṩ��
/// </summary>
public class FakeSelfHostProvider : ISelfHostProvider
{
    public FakeSelfHostProvider()
    {
        _hostName = "Fake-" + Random.Shared.Next().ToString();
    }

    private readonly string _hostName;

    public Task<string> GetSelfHostAsync()
    {
        return Task.FromResult(_hostName);
    }
}