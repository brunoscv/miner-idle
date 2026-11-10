using System.IO;

namespace MinersIdle.Persistence
{
    /// <summary>Onde o texto do save mora. Interface para os testes poderem usar a memória ou uma pasta temporária.</summary>
    public interface ISaveStore
    {
        /// <summary>O conteúdo do save, ou <c>null</c> se não houver.</summary>
        string Read();

        /// <summary>
        /// O conteúdo do save anterior ao último (M9, E3), ou <c>null</c> se não houver: de onde o jogo abre quando o
        /// principal está estragado ou sumiu.
        /// </summary>
        string ReadBackup();

        /// <summary>Grava o save inteiro de uma vez.</summary>
        void Write(string text);

        /// <summary>Tira o save atual do caminho sem apagar (para análise) e deixa o lugar livre para um jogo novo.</summary>
        void SetAsideBad();

        /// <summary>Apaga o save e o backup (Apagar save: nada volta depois).</summary>
        void Delete();
    }

    /// <summary>
    /// Save em arquivo com escrita atômica (D-44): grava em <c>save.json.tmp</c> e só então troca pelo <c>save.json</c>
    /// (ou o arquivo dado).
    /// Se o app morrer no meio, o save anterior continua inteiro. Um <c>.tmp</c> que sobrou de uma escrita interrompida
    /// é descartado na leitura.
    ///
    /// Backup (M9, E3): na mesma troca, o save que estava lá vira <c>save.json.bak</c> (o terceiro argumento do
    /// <c>File.Replace</c>). Quem lê usa o backup quando o principal está estragado ou faltando (inclusive se o app morrer
    /// entre os dois passos da troca, quando o principal já virou backup e o novo ainda não entrou). No Windows (Editor), a troca às vezes falha por um instante porque outro processo
    /// (antivírus, indexador) ainda segura o arquivo gravado logo antes; ela é tentada de novo algumas vezes, e o save
    /// anterior continua inteiro enquanto isso.
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        public const string FileName = "save.json";

        /// <summary>Quantas vezes a troca do arquivo é tentada antes de desistir (e o erro ir para o log de quem grava).</summary>
        const int ReplaceAttempts = 4;
        const int ReplaceRetryMilliseconds = 15;

        /// <param name="fileName">Outro arquivo na mesma pasta (a escolha do DBG → Cálculos, E3b-3, usa um à parte do save).</param>
        public FileSaveStore(string directory, string fileName = FileName)
        {
            Directory = directory;
            FilePath = Path.Combine(directory, fileName);
        }

        public string Directory { get; }
        public string FilePath { get; }
        string TempPath => FilePath + ".tmp";
        public string BadPath => FilePath + ".bad";
        public string BackupPath => FilePath + ".bak";

        public string Read()
        {
            if (File.Exists(TempPath)) File.Delete(TempPath);
            return File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;
        }

        public string ReadBackup() => File.Exists(BackupPath) ? File.ReadAllText(BackupPath) : null;

        public void Write(string text)
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(TempPath, text);

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    if (File.Exists(FilePath))
                        File.Replace(TempPath, FilePath, BackupPath);
                    else
                        File.Move(TempPath, FilePath);
                    return;
                }
                catch (IOException) when (attempt < ReplaceAttempts)
                {
                    System.Threading.Thread.Sleep(ReplaceRetryMilliseconds * attempt);
                }
            }
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
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
        }
    }
}
