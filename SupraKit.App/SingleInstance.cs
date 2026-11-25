namespace SupraKit.App;

/// <summary>
/// Instância única do aplicativo. A primeira instância fica com o mutex nomeado e passa a
/// escutar o evento ShowEvent; instâncias seguintes sinalizam o evento e encerram, fazendo a
/// janela existente aparecer (inclusive se estava oculta/minimizada na bandeja) e vir para frente.
/// O escopo é a sessão do usuário (Local\\…): cada sessão do Windows tem a sua instância.
/// </summary>
internal static class SingleInstance
{
    internal const string MutexName = @"Local\SupraKit.SingleInstance";
    internal const string ShowEventName = @"Local\SupraKit.ShowWindow";

    /// <summary>Assume a instância única: retorna o mutex quando esta é a primeira; null quando já havia uma instância (ela já foi sinalizada para vir à frente).</summary>
    internal static Mutex? TryAcquirePrimaryInstance()
    {
        var mutex = new Mutex(false, MutexName);
        try
        {
            if (mutex.WaitOne(0, false)) return mutex; // primeira instância
        }
        catch (AbandonedMutexException)
        {
            return mutex; // a instância anterior morreu sem liberar; esta assume o posto
        }
        mutex.Dispose();
        return null;
    }

    /// <summary>Sinaliza à instância em execução para vir para frente. Tolerante à corrida de inicialização (a primeira pode ainda não ter criado o evento): tenta algumas vezes antes de desistir.</summary>
    internal static void NotifyPrimaryInstance()
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            try
            {
                using var show = EventWaitHandle.OpenExisting(ShowEventName);
                show.Set();
                return;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                Thread.Sleep(40);
            }
        }
    }
}
