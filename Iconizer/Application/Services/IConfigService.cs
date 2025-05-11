using Iconizer.Domain;

namespace Iconizer.Application.Services
{
    public interface IConfigService
    {
        /// <summary>
        /// Carga la configuración desde el JSON. Devuelve null o un ConfigData vacío si no existe.
        /// </summary>
        ConfigData? Load(string path);

        /// <summary>
        /// Guarda la configuración en disco, serializándola a JSON con indentación.
        /// </summary>
        void Save(ConfigData config, string path);
    }
}