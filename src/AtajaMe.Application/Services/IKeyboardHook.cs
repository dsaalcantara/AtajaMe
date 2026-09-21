using System.Threading.Channels;

namespace AtajaMe.Application.Services;

/// <summary>
/// Contrato del hook global de teclado. Permite capturar pulsaciones a bajo nivel
/// y exponerlas a través de un canal asíncrono no bloqueante.
/// </summary>
public interface IKeyboardHook : IDisposable
{
    /// <summary>
    /// Canal de lectura de eventos de teclado de bajo nivel.
    /// </summary>
    ChannelReader<RawKeyEvent> KeyEvents { get; }

    /// <summary>
    /// Indica si el hook global se encuentra actualmente instalado y activo.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Instala el hook global de teclado en el sistema.
    /// </summary>
    void Start();

    /// <summary>
    /// Desinstala el hook global de teclado y libera el manejador del sistema.
    /// </summary>
    void Stop();
}
