using AssetsTools.NET.Extra;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using UABEAvalonia;
using UABEAvalonia.Plugins;

namespace MeshDataPlugin
{
    public class ExportMeshOption : UABEAPluginOption
    {
        public bool SelectionValidForPlugin(AssetsManager am, UABEAPluginAction action, List<AssetContainer> selection, out string name)
        {
            name = "Export Mesh Data";

            if (action != UABEAPluginAction.Export)
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

            var selectedFile = await win.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                DefaultExtension = "meshdata",
                Title = "Save as..."
            });

            string? selectedFilePath = FileDialogUtils.GetSaveFileDialogFile(selectedFile);
            if (selectedFilePath == null)
            {
                return false;
            }

            SaveStreamSlice(file.Stream, info.Offset, info.Size, selectedFilePath);

            return true;
        }

        static void SaveStreamSlice(
            Stream stream,
            uint offset,
            uint size,
            string selectedFilePath)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            }

            if (stream.CanSeek)
            {
                stream.Seek(offset, SeekOrigin.Begin);
            }
            else
            {
                // Manually discard bytes up to offset
                byte[] discard = new byte[81920];
                uint remaining = offset;

                while (remaining > 0)
                {
                    int toRead = (int)Math.Min(discard.Length, remaining);
                    int read = stream.Read(discard, 0, toRead);

                    if (read == 0)
                        throw new EndOfStreamException("Unexpected end of stream while skipping.");

                    remaining -= (uint)read;
                }
            }

            // Write exactly `size` bytes
            using var output = new FileStream(
                selectedFilePath,
                FileMode.Create,   // overwrite
                FileAccess.Write,
                FileShare.None);

            byte[] buffer = new byte[81920];
            uint bytesLeft = size;

            while (bytesLeft > 0)
            {
                int toRead = (int)Math.Min(buffer.Length, bytesLeft);
                int read = stream.Read(buffer, 0, toRead);

                if (read == 0)
                {
                    throw new EndOfStreamException("Unexpected end of stream while copying.");
                }

                output.Write(buffer, 0, read);
                bytesLeft -= (uint)read;
            }
        }
    }
}
