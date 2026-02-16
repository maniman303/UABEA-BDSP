using AssetsTools.NET;
using Avalonia.Styling;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using UABEAvalonia;

namespace MeshDataPlugin
{
    public class MeshDataInfo
    {
        public uint Offset {  get; set; }
        public uint Size { get; set; }
        public string Path { get; set; } = string.Empty;

        public static MeshDataInfo? GetFromAssetContainer(AssetWorkspace workspace, AssetContainer container)
        {
            AssetTypeValueField? baseField = workspace.GetBaseField(container);
            if (baseField == null)
            {
                return null;
            }

            var dumper = new AssetImportExport();
            var token = dumper.DumpJsonAsset(null, baseField);
            var streamDataToken = token["m_StreamData"];
            if (streamDataToken == null)
            {
                return null;
            }

            var offsetToken = streamDataToken["offset"];
            if (offsetToken == null)
            {
                return null;
            }

            var sizeToken = streamDataToken["size"];
            if (sizeToken == null)
            {
                return null;
            }

            var pathToken = streamDataToken["path"];
            if (pathToken == null)
            {
                return null;
            }

            var offset = offsetToken.Value<uint>();
            var size = sizeToken.Value<uint>();
            var path = pathToken.Value<string>() ?? string.Empty;

            return new MeshDataInfo
            {
                Offset = offset,
                Size = size,
                Path = path.Split('/').Last()
            };
        }
    }
}
