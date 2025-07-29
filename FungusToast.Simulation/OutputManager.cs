using System.Text;

namespace FungusToast.Simulation
{
    public class OutputManager : IDisposable
    {
        private readonly TextWriter? _originalOut;
        private readonly StreamWriter? _fileWriter;
        private readonly DualWriter? _dualWriter;

        public OutputManager(string outputFileName)
        {
            SetupOutputRedirection(outputFileName, out _originalOut, out _fileWriter);
            if (_originalOut != null && _fileWriter != null)
            {
                _dualWriter = new DualWriter(_originalOut, _fileWriter);
                Console.SetOut(_dualWriter);
                
                // Now write the path message to both console and file
                Console.WriteLine($"Simulation output redirected to: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimulationOutput", outputFileName)}");
            }
        }

        public void Dispose()
        {
            // Restore console output if we redirected it
            if (_originalOut != null)
            {
                try
                {
                    Console.SetOut(_originalOut);
                    _fileWriter?.Flush();
                    _fileWriter?.Close();
                    _fileWriter?.Dispose();
                }
                catch (Exception ex)
                {
                    // If cleanup fails, write error to original console
                    _originalOut.WriteLine($"Warning: Failed to cleanup output redirection: {ex.Message}");
                }
            }
        }

        private static void SetupOutputRedirection(string outputFileName, out TextWriter originalOut, out StreamWriter fileWriter)
        {
            // Always write to the SimulationOutput folder inside the FungusToast.Simulation directory
            string simulationDir = AppDomain.CurrentDomain.BaseDirectory;
            string outputDir = Path.Combine(simulationDir, "SimulationOutput");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Generate filename if not provided - use the requested format
            if (string.IsNullOrEmpty(outputFileName))
            {
                outputFileName = $"Simulation_output_{DateTime.Now:yyyy-MM-ddTHH-mm-ss}.txt";
            }

            string fullPath = Path.Combine(outputDir, outputFileName);
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (IOException)
            {
                string baseName = Path.GetFileNameWithoutExtension(outputFileName);
                string extension = Path.GetExtension(outputFileName);
                int counter = 1;
                do
                {
                    outputFileName = $"{baseName}_{counter}{extension}";
                    fullPath = Path.Combine(outputDir, outputFileName);
                    counter++;
                } while (File.Exists(fullPath));
            }

            originalOut = Console.Out;
            fileWriter = new StreamWriter(fullPath, false, System.Text.Encoding.UTF8);
        }

        public class DualWriter : TextWriter
        {
            private readonly TextWriter _console;
            private readonly TextWriter _file;

            public DualWriter(TextWriter console, TextWriter file)
            {
                _console = console;
                _file = file;
            }

            public override Encoding Encoding => _console.Encoding;

            public override void Write(char value)
            {
                _console.Write(value);
                _file.Write(value);
            }

            public override void WriteLine(string? value)
            {
                _console.WriteLine(value);
                _file.WriteLine(value);
            }

            public override void Flush()
            {
                _console.Flush();
                _file.Flush();
            }
        }
    }
}