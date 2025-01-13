namespace WatchDog.Service.Frameworks;

public class MasterHostStatusChecker : IMasterHostStatusChecker
{
    public MasterHostStatusChecker(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private readonly IHttpClientFactory _httpClientFactory;

    public async Task<bool> CheckMasterHostEnableAsync(string host)
    {
        var httpClient = _httpClientFactory.CreateClient();
        if (host.StartsWith("Fake-"))
        {
            return true;
        }

        try
        {
            // �������ʹ�� HttpClient ȥ����һ�£������Ƿ����
            var url = $"http://{host}/Dog/Enable";
            var result = await httpClient.GetStringAsync(url);
            _ = result;
            // �����жϷ���ֵ��ֻҪ�ܷ��ʾͿ���
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}