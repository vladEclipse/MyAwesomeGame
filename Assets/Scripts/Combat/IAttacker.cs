/// <summary>
/// Тот, кто умеет атаковать (игрок или моб). Событие Attacked вызывается в момент удара.
/// </summary>
public interface IAttacker
{
    event System.Action Attacked;
}
