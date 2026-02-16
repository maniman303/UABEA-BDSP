using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using UABEAvalonia;
using UABEAvalonia.Plugins;

namespace MeshDataPlugin
{
    public class MeshDumpReplacer : UABEAPluginOption
    {
        public bool SelectionValidForPlugin(AssetsManager am, UABEAPluginAction action, List<AssetContainer> selection, out string name)
        {
            name = "Replace Mesh Dump";

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

            var baseField = workspace.GetBaseField(selected);
            if (baseField == null)
            {
                return false;
            }

            var importer = new AssetImportExport();
            var currentToken = importer.DumpJsonAsset(null, baseField);
            if (currentToken == null)
            {
                return false;
            }

            var selectedFiles = await win.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Open",
                FileTypeFilter = new List<FilePickerFileType>()
                {
                    new FilePickerFileType("UABEA json dump") { Patterns = new List<string>() { "*.json" } }
                }
            });

            var selectedFilePath = FileDialogUtils.GetOpenFileDialogFiles(selectedFiles).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(selectedFilePath) || !File.Exists(selectedFilePath))
            {
                await MessageBoxUtil.ShowDialog(win, "Replace Mesh Dump", "Invalid json file.");
                return false;
            }

            JToken? newToken = null;
            try
            {
                newToken = JToken.Parse(File.ReadAllText(selectedFilePath));
            }
            catch
            {

            }

            if (newToken == null)
            {
                await MessageBoxUtil.ShowDialog(win, "Replace Mesh Dump", "Invalid json file.");
                return false;
            }

            if (newToken["m_StreamData"]?["size"]?.Value<uint>() != currentToken["m_StreamData"]?["size"]?.Value<uint>())
            {
                await MessageBoxUtil.ShowDialog(win, "Replace Mesh Dump", "StreamData size doesn't match.");
                return false;
            }

            newToken["m_StreamData"]?["offset"] = currentToken["m_StreamData"]?["offset"]?.Value<uint>();
            newToken["m_StreamData"]?["path"] = currentToken["m_StreamData"]?["path"]?.Value<string>();
            newToken["m_Name"] = currentToken["m_Name"]?.Value<string>();

            AssetTypeTemplateField tempField = workspace.GetTemplateField(selected);
            var bytes = importer.ImportJsonAsset(tempField, newToken, out var exceptionMessage);
            if (bytes == null)
            {
                return false;
            }

            AssetsReplacer replacer = AssetImportExport.CreateAssetReplacer(selected, bytes);
            workspace.AddReplacer(selected.FileInstance, replacer, new MemoryStream(bytes));

            return true;
        }
    }
}
