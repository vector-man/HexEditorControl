using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HexControl.Core;
using HexControl.PatternLanguage;
#if DEBUG
using Avalonia;
#endif

namespace HexControl.Samples.Avalonia;

public class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        DataContext = this;
    }

    public Document? Document { get; set; }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        //Document = Document.FromFile(@"..\..\..\..\..\files\sample-binary");

        var code = File.ReadAllText(@"C:\Users\joery\Downloads\memory_test.hexpat");
        Document = Document.FromFile(@"C:\Users\joery\Downloads\pad00000.meta");

        //var code = File.ReadAllText(@"C:\Users\joery\Downloads\pe.hexpat");
        //Document = Document.FromFile(@"C:\Users\joery\Downloads\MemProfilerInstaller5_7_26.exe");

        var runner = new PatternRunner(Document);
        runner.Run(code);
    }
}