namespace PinterestDOWNLOAD.Scraping
{
    /// <summary>Atualizacao de status enviada para a UI durante a coleta e o download.</summary>
    internal readonly record struct StatusUpdate(string Texto, int Atual = 0, int Total = 0)
    {
        public bool TemBarra => Total > 0;
    }
}
