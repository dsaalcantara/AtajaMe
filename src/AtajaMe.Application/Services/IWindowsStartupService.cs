namespace AtajaMe.Application.Services;

/// <summary>
/// Contrato para gestionar el inicio automático de la aplicación con Windows (RF-33).
/// </summary>
public interface IWindowsStartupService
{
    /// <summary>
    /// Comprueba si la aplicación está configurada para iniciar automáticamente con el inicio de sesión del usuario.
    /// </summary>
    bool IsStartupEnabled();

    /// <summary>
    /// Habilita o deshabilita el inicio automático en el registro de Windows.
    /// </summary>
    /// <param name="enable">Indica si debe habilitarse el inicio automático.</param>
    /// <param name="startMinimized">Indica si debe iniciarse con el flag --minimized.</param>
    void SetStartup(bool enable, bool startMinimized = false);
}
