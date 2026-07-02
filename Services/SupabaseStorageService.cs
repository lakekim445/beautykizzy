using System.Net.Http.Headers;

namespace BeautyKizzy.Services
{
    public class SupabaseStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseServiceKey;

        public SupabaseStorageService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();

            _supabaseUrl = configuration["Supabase:Url"]
                ?? throw new Exception("Falta Supabase:Url en la configuración.");

            _supabaseServiceKey = configuration["Supabase:ServiceKey"]
                ?? throw new Exception("Falta Supabase:ServiceKey en la configuración.");
        }

        /// <summary>
        /// Sube una imagen al bucket indicado y devuelve la URL pública.
        /// </summary>
        public async Task<string> SubirImagenAsync(IFormFile archivo, string bucket)
        {
            var extension = Path.GetExtension(archivo.FileName);
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";

            var uploadUrl = $"{_supabaseUrl}/storage/v1/object/{bucket}/{nombreArchivo}";

            using var contenido = new StreamContent(archivo.OpenReadStream());
            contenido.Headers.ContentType = new MediaTypeHeaderValue(archivo.ContentType);

            var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
            {
                Content = contenido
            };

            // Autenticación
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _supabaseServiceKey);

            request.Headers.Add("apikey", _supabaseServiceKey);

            // Si existe un archivo con el mismo nombre lo reemplaza
            request.Headers.Add("x-upsert", "true");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al subir la imagen a Supabase: {error}");
            }

            // Devuelve la URL pública de la imagen
            return $"{_supabaseUrl}/storage/v1/object/public/{bucket}/{nombreArchivo}";
        }
    }
}