using AssetsTools.NET.Extra;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using UABEAvalonia;
using UABEAvalonia.Plugins;

namespace MeshDataPlugin
{
    public class ImportMeshOption : UABEAPluginOption
    {
        public bool SelectionValidForPlugin(AssetsManager am, UABEAPluginAction action, List<AssetContainer> selection, out string name)
        {
            name = "Import Mesh Data";

            if (action != UABEAPluginAction.Import)
            {
                return false;
            }

            if (selection.Count != 1)
            {
                return false;
            }

            var selected = selection[0];

            if (selected.ClassId != (int)AssetClassID.Mesh)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> ExecutePlugin(Window win, AssetWorkspace workspace, List<AssetContainer> selection)
        {
            if (selection.Count != 1)
            {
                return false;
            }

            var selected = selection[0];
            if (selected.ClassId != (int)AssetClassID.Mesh)
            {
                return false;
            }

            var info = MeshDataInfo.GetFromAssetContainer(workspace, selected);
            if (info == null)
            {
                return false;
            }

            var bundleWorkspace = workspace.parent;
            var file = bundleWorkspace.Files.FirstOrDefault(x => x.Name == info.Path);
            if (file == null)
            {
                return false;
            }

            var selectedFiles = await win.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Open",
                FileTypeFilter =
                [
                    new FilePickerFileType("UABEA mesh data") { Patterns = ["*.meshdata"] }
                ]
            });

            var selectedFilePath = FileDialogUtils.GetOpenFileDialogFiles(selectedFiles).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(selectedFilePath) || !File.Exists(selectedFilePath))
            {
                return false;
            }

            var fileInfo = new FileInfo(selectedFilePath);
            if (fileInfo == null || !fileInfo.Exists || fileInfo.Length != info.Size)
            {
                await MessageBoxUtil.ShowDialog(win, "Import Mesh Data", "Selected file doesn't have valid size.");
                return false;
            }

            var newStream = await WriteFileToStream(fileInfo, info.Offset, info.Size, file.Stream);
            if (newStream == null)
            {
                return false;
            }

            bundleWorkspace.AddOrReplaceFile(newStream, file.Name, file.IsSerialized);

            return true;
        }

        private static async Task<Stream?> WriteFileToStream(FileInfo fileInfo, uint offset, uint size, Stream originalStream)
        {
            // Create a copy of the original stream
            // We use a MemoryStream to hold the modified version
            var outputStream = new MemoryStream();

            // Ensure we start from the beginning of the source stream
            if (originalStream.CanSeek) originalStream.Position = 0;
            await originalStream.CopyToAsync(outputStream);

            // Overwrite at the specified offset
            using (FileStream sourceFileStream = File.OpenRead(fileInfo.FullName))
            {
                // Ensure the output stream is long enough to accommodate the offset + size
                if (outputStream.Length < offset + size)
                {
                    outputStream.SetLength(offset + size);
                }

                outputStream.Position = offset;
                await sourceFileStream.CopyToAsync(outputStream);
            }

            // Reset position for the next consumer
            outputStream.Position = 0;
            return outputStream;
        }
    }
}
