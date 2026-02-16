using UABEAvalonia.Plugins;

namespace MeshDataPlugin
{
    public class MeshDataPlugin : UABEAPlugin
    {
        public PluginInfo Init()
        {
            var info = new PluginInfo()
            {
                name = "Mesh Import/Export",
                options =
                [
                    new ExportMeshOption(),
                    new ImportMeshOption(),
                    new MeshDumpReplacer(),
                ]
            };
            return info;
        }
    }
}
