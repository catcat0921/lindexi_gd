namespace WatchDog.Service.Contexts;

/// <summary>
/// ��ȡ������Ϣ���
/// </summary>
/// <param name="SelfIsMaster">��ǰ�Ƿ�������豸</param>
/// <param name="MasterHost">���豸��ַ</param>
public record MasterHostResult(bool SelfIsMaster, string MasterHost);