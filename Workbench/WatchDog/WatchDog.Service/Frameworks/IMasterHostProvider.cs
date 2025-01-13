using WatchDog.Service.Contexts;

namespace WatchDog.Service.Frameworks;

/// <summary>
/// ���ڻ�ȡ���豸��Ϣ
/// </summary>
public interface IMasterHostProvider
{
    /// <summary>
    /// ��ȡ���豸��Ϣ
    /// </summary>
    /// <returns></returns>
    Task<MasterHostResult> GetMasterHostAsync();
}