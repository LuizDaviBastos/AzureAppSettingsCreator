using AzureAppSettingsCreator;
using forwpf = System.Windows;


public class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        string inputFilePath, outDirectory;

        AzureAppSettings.SetSeparator();

        inputFilePath = AzureAppSettings.SetInputFilePath();
        outDirectory = AzureAppSettings.SetOuputDirectory();

        AzureAppSettings.WriteAsync(inputFilePath, outDirectory).Wait();

        Console.WriteLine($"AppSettings Created! {Path.Combine(outDirectory, AzureAppSettings.outFileName)}");
    }
}


