namespace WatchDog.Service.Frameworks;

/// <summary>
/// ���ڼ�����豸�Ƿ����
/// </summary>
public interface IMasterHostStatusChecker
{
    /// <summary>
    /// ������豸�Ƿ����
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    Task<bool> CheckMasterHostEnableAsync(string host);
}