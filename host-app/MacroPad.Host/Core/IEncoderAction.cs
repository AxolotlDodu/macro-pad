namespace MacroPad.Host;

public interface IEncoderAction
{
    /// <summary>ticks > 0 = sens horaire, ticks < 0 = sens anti-horaire.
    /// Correspond directement au delta cumulé envoyé par le firmware (int8).</summary>
    void Execute(int ticks);
}
