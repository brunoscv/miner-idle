using System.IO;

namespace MinersIdle.Persistence
{
    /// <summary>Onde o texto do save mora. Interface para os testes poderem usar a memória ou uma pasta temporária.</summary>
    public interface ISaveStore
    {
        /// <summary>O conteúdo do save, ou <c>null</c> se não houver.</summary>
        string Read();

        /// <summary>Grava o save inteiro de uma vez.</summary>
        void Write(string text);

        /// <summary>Tira o save atual do caminho sem apagar (para análise) e deixa o lugar livre para um jogo novo.</summary>
        void SetAsideBad();

        void Delete();
    }

    /// <summary>
    /// Save em arquivo com escrita atômica (D-44): grava em <c>save.json.tmp</c> e só então troca pelo <c>save.json</c>.
    /// Se o app morrer no meio, o save anterior continua inteiro. Um <c>.tmp</c> que sobrou de uma escrita interrompida
    /// é descartado na leitura.
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        public const string FileName = "save.json";

        public FileSaveStore(string directory)
        {
            Directory = directory;
            FilePath = Path.Combine(directory, FileName);
        }

        public string Directory { get; }
        public string FilePath { get; }
        string TempPath => FilePath + ".tmp";
        public string BadPath => FilePath + ".bad";

        public string Read()
        {
            if (File.Exists(TempPath)) File.Delete(TempPath);
            return File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;
        }

        public void Write(string text)
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(TempPath, text);

            if (File.Exists(FilePath))
                File.Replace(TempPath, FilePath, null);
            else
                File.Move(TempPath, FilePath);
        }

        public void SetAsideBad()
        {
            if (!File.Exists(FilePath)) return;
            if (File.Exists(BadPath)) File.Delete(BadPath);
            File.Move(FilePath, BadPath);
        }

        public void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            if (File.Exists(TempPath)) File.Delete(TempPath);
        }
    }
}
