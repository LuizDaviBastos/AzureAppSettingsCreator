using AzureAppSettingsCreator.Models;
using Newtonsoft.Json.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AzureAppSettingsCreator
{
    public class AzureAppSettings
    {
        public static string outFileName = "AzureAppSettings.json";
        public static string SEPARATOR = "__";
        public static void DialogCancel(DialogResult dialog) { if (dialog is DialogResult.Cancel or DialogResult.No or DialogResult.Abort) Environment.Exit(0); }

        public static void SetSeparator()
        {
            Console.WriteLine("Set SEPARATOR? (Yes) y / (No) n (Default is \"__\")");
            var result = Console.ReadLine();

            if (!string.IsNullOrEmpty(result))
            {
                result = result.ToLower();
                if (result.Contains("y"))
                {
                    Console.WriteLine("Enter with SEPARATOR:");
                    var newSEPARATOR = Console.ReadLine() ?? SEPARATOR;
                    SEPARATOR = newSEPARATOR;
                }
            }
        }

        public static string SetInputFilePath()
        {
            string inputFilePath = string.Empty;
            Console.WriteLine("Enter input file path");

            OpenFileDialog fileDialog = new OpenFileDialog();
            DialogCancel(fileDialog.ShowDialog());
            inputFilePath = fileDialog.FileName;

            if (string.IsNullOrEmpty(inputFilePath))
            {
                Console.Write("Invalid path");
                inputFilePath = SetInputFilePath();
            }

            return inputFilePath;
        }

        public static string SetOuputDirectory()
        {
            string outDirectory = string.Empty;
            Console.WriteLine("Set ouput directory");

            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            DialogCancel(folderDialog.ShowDialog());
            outDirectory = folderDialog.SelectedPath;

            if (string.IsNullOrEmpty(outDirectory))
            {
                Console.Write("Invalid directory");
                outDirectory = SetOuputDirectory();
            }

            return outDirectory;
        }

        public static async Task WriteAsync(string inputFilePath, string outDirectory)
        {
            try
            {
                string outFilePath = Path.Combine(outDirectory, outFileName);
                var flattenedJson = await FlattenJson(inputFilePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };

                if (File.Exists(outFilePath)) File.Delete(outFilePath);

                Directory.CreateDirectory(outDirectory);
                await File.WriteAllTextAsync(outFilePath, JsonSerializer.Serialize(flattenedJson, options));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
          
        }

        private static async Task<List<Setting>> FlattenJson(string filePath)
        {
            var flattenedJson = new List<Setting>();

            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };

            await using (FileStream fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true))
            using (JsonDocument document = await JsonDocument.ParseAsync(fileStream, options))
            {
                if(document.RootElement.ValueKind is not JsonValueKind.Object)
                {
                    Console.WriteLine("####### ERROR: Json format invalid #######");
                    Environment.Exit(0);
                }

                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    var keys = new List<string> { property.Name };

                    Parse(flattenedJson, keys, property);
                }
            }

            return flattenedJson;
        }

        private static void Parse(List<Setting> flattenedJson, List<string> keys, JsonProperty property)
        {
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                JsonElement childElement = property.Value;
                ParseSettings(flattenedJson, keys, childElement);
            }
            else
            {

                string value = property.Value.ValueKind == JsonValueKind.Array ?
                    JsonSerializer.Serialize<JsonElement>(property.Value) :
                    property.Value.ToString();

                var setting = new Setting
                {
                    Name = string.Join(SEPARATOR, keys),
                    Value = value
                };
                setting.Name = setting.Name.Replace(":", SEPARATOR);

                flattenedJson.Add(setting);
            }
        }

        private static void ParseSettings(List<Setting> flattenedJson, List<string> keys, JsonElement jsonElement)
        {
            foreach (JsonProperty property in jsonElement.EnumerateObject())
            {
                keys.Add(property.Name);

                Parse(flattenedJson, keys, property);

                keys.RemoveAt(keys.Count - 1);
            }
        }
    }
}
