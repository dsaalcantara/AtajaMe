namespace AtajaMe.Application.Services;

/// <summary>
/// Contrato para detectar si el control con foco actual en el sistema operativo es un campo
/// sensible (como un campo de contraseñas o credenciales), según el requisito RF-08.
/// </summary>
public interface ISensitiveControlDetector
{
    /// <summary>
    /// Determina si el control enfocado actualmente es un campo protegido o de contraseña.
    /// Si devuelve true, la captura y expansión deben cancelarse inmediatamente y el búfer debe purgarse.
    /// </summary>
    bool IsSensitiveControlFocused();
}
