namespace ET
{
    /// <summary>
    /// 内网消息
    /// </summary>
    public interface IActorMessage: IMessage
    {
    }

    /// <summary>
    /// 内网请求
    /// </summary>
    public interface IActorRequest: IRequest
    {
    }

    /// <summary>
    /// 内网回复
    /// </summary>
    public interface IActorResponse: IResponse
    {
    }
}