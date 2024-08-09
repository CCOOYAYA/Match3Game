public interface IGameBoardAction
{
    /// <summary>
    /// 每帧调用, 返回: true => 继续执行, false => 完成action, 销毁VFX并删去引用
    /// </summary>
    bool Tick();
}
