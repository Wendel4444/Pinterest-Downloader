namespace PinterestDOWNLOAD.Scraping
{
    internal enum TipoMidia
    {
        Imagem,
        /// <summary>URL direta de um .mp4.</summary>
        Video,
        /// <summary>URL direta de uma playlist HLS (.m3u8) — baixada via ffmpeg.</summary>
        VideoHls,
        /// <summary><see cref="MediaItem.Url"/> e a pagina de um pin de video
        /// (https://.../pin/&lt;id&gt;/) — o yt-dlp resolve e baixa o video completo.</summary>
        VideoPin
    }

    /// <summary>
    /// Uma midia encontrada no board.
    /// <para><see cref="Url"/> e a melhor URL conhecida (ex.: versao "originals" da imagem,
    /// ou a playlist master .m3u8 do video).</para>
    /// <para><see cref="UrlFallback"/> e a URL crua que apareceu na pagina, usada quando a
    /// preferida responde 403/404 (nem toda imagem tem "originals").</para>
    /// </summary>
    internal sealed record MediaItem(TipoMidia Tipo, string Url, string? UrlFallback = null)
    {
        public bool Equals(MediaItem? other) => other is not null && other.Url == Url;
        public override int GetHashCode() => Url.GetHashCode();
    }
}
