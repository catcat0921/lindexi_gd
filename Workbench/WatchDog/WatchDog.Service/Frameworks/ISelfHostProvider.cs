namespace WatchDog.Service.Frameworks;

/// <summary>
/// �Լ���ǰ�豸��������Ϣ�ṩ��
/// </summary>
public interface ISelfHostProvider
{
    Task<string> GetSelfHostAsync();
}