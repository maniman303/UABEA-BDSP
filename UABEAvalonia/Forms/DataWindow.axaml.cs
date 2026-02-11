using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace UABEAvalonia
{
    public partial class DataWindow : Window
    {
        private InfoWindow win;
        private AssetWorkspace workspace;
        private Func<string, bool>? callback;

        public DataWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            //generated events
            Closing += DataWindow_Closing;
        }

        public DataWindow(InfoWindow win, AssetWorkspace workspace, AssetContainer cont, Func<string, bool>? callback = null) : this()
        {
            this.win = win;
            this.workspace = workspace;
            this.callback = callback;

            SetWindowTitle(workspace, cont);

            treeView.Init(win, workspace, this, callback);
            treeView.LoadComponent(cont);
        }

        private void SetWindowTitle(AssetWorkspace workspace, AssetContainer cont)
        {
            AssetNameUtils.GetDisplayNameFast(workspace, cont, false, out string assetName, out string typeName);
            if (assetName == "Unnamed asset")
                Title += $": {typeName} ({cont.FileInstance.name}/{cont.PathId})";
            else
                Title += $": {typeName} {assetName} ({cont.FileInstance.name}/{cont.PathId})";
        }

        private void DataWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            treeView.ItemsSource = null;
        }
    }
}
