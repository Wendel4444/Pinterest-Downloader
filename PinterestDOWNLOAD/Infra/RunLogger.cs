using System;
using System.IO;
using System.Text;

namespace PinterestDOWNLOAD.Infra
{
    /// <summary>
    /// Log simples de execucao, gravado em &lt;destino&gt;/_logs/run_&lt;timestamp&gt;.log
    /// e tambem espelhado para a UI e para o Output do Visual Studio.
    /// </summary>
    internal sealed class RunLogger : IDisposable
    {
        private readonly StreamWriter? _writer;
        private readonly Action<string>? _uiSink;
        private readonly object _lock = new();

        public RunLogger(string pastaDestino, Action<string>? uiSink = null)
        {
            _uiSink = uiSink;
            try
            {
                string logsDir = AppPaths.LogsDirFor(pastaDestino);
                Directory.CreateDirectory(logsDir);
                string arquivo = Path.Combine(logsDir, $"run_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                _writer = new StreamWriter(arquivo, append: false, Encoding.UTF8) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Nao foi possivel criar o arquivo de log: {ex.Message}");
            }
        }

        public void Info(string msg) => Escrever("INFO ", msg);
        public void Aviso(string msg) => Escrever("AVISO", msg);
        public void Erro(string msg) => Escrever("ERRO ", msg);

        private void Escrever(string nivel, string msg)
        {
            string linha = $"{DateTime.Now:HH:mm:ss} [{nivel}] {msg}";
            lock (_lock)
            {
                _writer?.WriteLine(linha);
            }
            System.Diagnostics.Debug.WriteLine(linha);
            _uiSink?.Invoke(linha);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _writer?.Dispose();
            }
        }
    }
}
