/// <summary>
/// Тот, кто умеет атаковать (игрок или моб). Событие Attacked вызывается в момент удара.
/// Нужен, чтобы ActorAnimator работал с игроком и мобом одинаково.
/// </summary>
public interface IAttacker
{
    event System.Action Attacked;
}
