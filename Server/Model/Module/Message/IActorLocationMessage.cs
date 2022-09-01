namespace ET
{
    /// <summary>
    /// 内网消息
    /// </summary>
    public interface IActorLocationMessage: IActorRequest
    {
    }

    /// <summary>
    /// 内网本地请求
    /// </summary>
    public interface IActorLocationRequest: IActorRequest
    {
    }

    /// <summary>
    /// 内网本地回复
    /// </summary>
    public interface IActorLocationResponse: IActorResponse
    {
    }
}